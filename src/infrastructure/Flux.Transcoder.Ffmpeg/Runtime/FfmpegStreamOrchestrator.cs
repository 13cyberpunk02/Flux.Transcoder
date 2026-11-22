using System.Collections.Concurrent;
using System.Diagnostics;
using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Ffmpeg.Configuration;
using Flux.Transcoder.Ffmpeg.Encoding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flux.Transcoder.Ffmpeg.Runtime;

/// <summary>
/// StartAsync/StopAsync не ждут выхода ffmpeg. Порядок команд канала сохраняется так:
/// Start во время остановки запоминается и выполняется в Exited старого процесса,
/// следующий Stop этот отложенный старт отменяет.
/// </summary>
public sealed class FfmpegStreamOrchestrator(
    FfmpegArgumentBuilder argumentBuilder,
    ProcessSupervisor supervisor,
    ProgressParser progressParser,
    RestartCoordinator restartCoordinator,
    IOptions<FfmpegOptions> options,
    ILogger<FfmpegStreamOrchestrator> logger)
    : IStreamOrchestrator
{
    private readonly FfmpegOptions _options = options.Value;
    private readonly ConcurrentDictionary<Guid, Lock> _locks = new();

    private Lock LockFor(Guid channelId) => _locks.GetOrAdd(channelId, _ => new Lock());

    // Токен отменяет только сам запуск. С процессом он НЕ связывается: ffmpeg живёт дольше,
    // чем обработка сообщения, которое его запустило.
    public Task StartAsync(Channel channel, CancellationToken cancellationToken = default)
    {
        lock (LockFor(channel.Id))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existing = supervisor.Get(channel.Id);
            if (existing is { HasExited: false })
            {
                if (existing.IntentionalStop)
                {
                    existing.StartAfterExit = channel;
                    logger.LogInformation(
                        "Channel {ChannelId} is stopping, start is deferred until exit", channel.Id);
                }
                else
                {
                    logger.LogWarning("Channel {ChannelId} already running", channel.Id);
                }

                return Task.CompletedTask;
            }

            if (existing is not null)
            {
                // процесс уже умер, но Exited ещё не успел снять запись
                existing.StartAfterExit = null;
                supervisor.Unregister(existing);
            }

            Launch(channel);
            return Task.CompletedTask;
        }
    }

    public Task StopAsync(Guid channelId, CancellationToken cancellationToken = default)
    {
        lock (LockFor(channelId))
        {
            // ручная остановка/рестарт — отменяет авто-перезапуск и обнуляет его счётчик
            restartCoordinator.Reset(channelId);

            if (supervisor.Get(channelId) is { } running)
            {
                running.StartAfterExit = null;
                _ = supervisor.Stop(running);
            }

            return Task.CompletedTask;
        }
    }

    private void Launch(Channel channel)
    {
        var channelId = channel.Id;
        var args = argumentBuilder.Build(channel);
        logger.LogInformation("FFMPEG ARGS: {Args}", string.Join(" ", args));
        var psi = new ProcessStartInfo
        {
            FileName = _options.BinaryPath,
            WorkingDirectory = argumentBuilder.WorkingDirectory, // пути в args — относительно неё
            RedirectStandardInput = true,    // 'q' для мягкой остановки
            RedirectStandardOutput = true,   // -progress pipe:1
            RedirectStandardError = true,    // лог ffmpeg
            UseShellExecute = false,
            CreateNoWindow = true
        };
        // аргументы передаём списком — .NET сам корректно экранирует
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        var process = new Process { StartInfo = psi };

        // stdout -> парсер прогресса
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                progressParser.Feed(channelId, e.Data);
        };

        // stderr -> логи
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                logger.LogDebug("[ffmpeg {ChannelId}] {Line}", channelId, e.Data);
        };

        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException($"Failed to start ffmpeg for channel {channelId}.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var running = supervisor.Register(channelId, process.Id, process);
        logger.LogInformation("ffmpeg started for {ChannelId}, pid {Pid}", channelId, running.ProcessId);

        // события включаем ПОСЛЕ регистрации: у Exited всегда есть свой экземпляр.
        // Если процесс уже умер, событие всё равно придёт.
        process.Exited += (_, _) => OnExited(running);
        process.EnableRaisingEvents = true;
    }

    private void OnExited(RunningProcess running)
    {
        var channelId = running.ChannelId;
        try
        {
            var needsRestart = false;

            lock (LockFor(channelId))
            {
                int exitCode;
                try { exitCode = running.Process.ExitCode; }
                catch (InvalidOperationException) { exitCode = -1; }

                // флаг берём у СВОЕГО экземпляра: в супервизоре уже может лежать новый процесс
                var wasIntentional = running.IntentionalStop;

                logger.LogInformation(
                    "ffmpeg exited for {ChannelId}, code {Code}, intentional={Intentional}",
                    channelId, exitCode, wasIntentional);

                supervisor.Unregister(running);
                running.MarkExited();
                running.Process.Dispose();

                var next = running.StartAfterExit;
                running.StartAfterExit = null;

                if (next is not null && supervisor.Get(channelId) is null)
                {
                    try
                    {
                        Launch(next);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Не удался отложенный старт канала {ChannelId}", channelId);
                        needsRestart = true;
                    }
                }
                else if (!wasIntentional && exitCode != 0)
                {
                    // перезапускаем ТОЛЬКО если остановка не намеренная и процесс завершился с ошибкой
                    logger.LogWarning(
                        "ffmpeg для {ChannelId} умер неожиданно (код {Code}) — запускаю перезапуск",
                        channelId, exitCode);
                    needsRestart = true;
                }
            }

            if (needsRestart)
                restartCoordinator.OnStalled(channelId);
        }
        catch (Exception ex)
        {
            // исключение из обработчика события на пуле потоков уронило бы весь воркер
            logger.LogError(ex, "Ошибка обработки завершения ffmpeg {ChannelId}", channelId);
        }
    }
}
