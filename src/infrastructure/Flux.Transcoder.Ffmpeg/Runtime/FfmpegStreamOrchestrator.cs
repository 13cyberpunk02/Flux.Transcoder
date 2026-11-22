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
/// StartAsync/StopAsync идемпотентны и не ждут выхода ffmpeg.
/// StartAsync означает «должен работать процесс ревизии channel.RunRevision»: процесс другой ревизии
/// останавливается, а новый запускается в его Exited. Следующий Stop отложенный старт отменяет.
/// </summary>
public sealed class FfmpegStreamOrchestrator(
    FfmpegArgumentBuilder argumentBuilder,
    ProcessSupervisor supervisor,
    ProgressParser progressParser,
    RestartCoordinator restartCoordinator,
    ChildProcessGuard processGuard,
    IOptions<FfmpegOptions> options,
    ILogger<FfmpegStreamOrchestrator> logger)
    : IStreamOrchestrator
{
    private readonly FfmpegOptions _options = options.Value;
    private readonly ConcurrentDictionary<Guid, Lock> _locks = new();

    private Lock LockFor(Guid channelId) => _locks.GetOrAdd(channelId, _ => new Lock());

    // Токен отменяет только сам запуск. С процессом он НЕ связывается: его жизнь
    // ограничена жизнью воркера (ShutdownAsync + ChildProcessGuard).
    public Task StartAsync(Channel channel, CancellationToken cancellationToken = default)
    {
        lock (LockFor(channel.Id))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existing = supervisor.Get(channel.Id);
            if (existing is null)
            {
                Launch(channel);
                return Task.CompletedTask;
            }

            // нужный процесс уже работает (или только что упал — это обработает его Exited)
            if (!existing.IntentionalStop && existing.Revision == channel.RunRevision)
                return Task.CompletedTask;

            existing.StartAfterExit = channel;
            _ = supervisor.Stop(existing);
            return Task.CompletedTask;
        }
    }

    public Task StopAsync(Guid channelId, CancellationToken cancellationToken = default)
    {
        lock (LockFor(channelId))
        {
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
        if (supervisor.IsClosing)
            return;

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

        if (!processGuard.Start(process))
        {
            process.Dispose();
            throw new InvalidOperationException($"Failed to start ffmpeg for channel {channelId}.");
        }

        // stdout -> парсер прогресса, stderr -> логи
        ReadOnOwnThread(process.StandardOutput, "stdout",
            line => progressParser.Feed(channelId, line));
        ReadOnOwnThread(process.StandardError, "stderr",
            line => logger.LogDebug("[ffmpeg {ChannelId}] {Line}", channelId, line));

        var running = supervisor.Register(channelId, channel.RunRevision, process.Id, process);
        logger.LogInformation("ffmpeg started for {ChannelId}, pid {Pid}", channelId, running.ProcessId);

        // события включаем ПОСЛЕ регистрации: у Exited всегда есть свой экземпляр.
        // Если процесс уже умер, событие всё равно придёт.
        process.Exited += (_, _) => OnExited(running);
        process.EnableRaisingEvents = true;
    }

    // Не BeginOutputReadLine: на Windows пайпы процесса синхронные, и каждое такое чтение навсегда
    // занимает поток пула. Уже на 40 каналах пул голодал: старт шёл по каналу в секунду.
    private void ReadOnOwnThread(StreamReader reader, string name, Action<string> handle)
    {
        new Thread(() =>
        {
            try
            {
                using (reader)
                {
                    while (reader.ReadLine() is { } line)
                        handle(line);
                }
            }
            catch (Exception ex)
            {
                // необработанное исключение в своём потоке уронило бы воркер
                logger.LogWarning(ex, "Чтение {Stream} ffmpeg прервано", name);
            }
        })
        {
            IsBackground = true,
            Name = $"ffmpeg-{name}"
        }.Start();
    }

    private void OnExited(RunningProcess running)
    {
        var channelId = running.ChannelId;
        try
        {
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

                // Любой выход не по нашей команде — сбой, в том числе код 0: у живого канала это значит,
                // что источник закрылся (EOF). Пауза ставится ДО Unregister, иначе сверка с БД,
                // увидев канал без процесса, запустила бы его мгновенно, в обход backoff.
                if (!wasIntentional && !supervisor.IsClosing)
                    restartCoordinator.OnFailure(channelId, running.Revision);

                supervisor.Unregister(running);
                running.MarkExited();
                running.Process.Dispose();

                var next = running.StartAfterExit;
                running.StartAfterExit = null;

                if (next is null || supervisor.Get(channelId) is not null)
                    return;

                try
                {
                    Launch(next);
                }
                catch (Exception ex)
                {
                    restartCoordinator.OnLaunchFailed(channelId, next.RunRevision, ex);
                }
            }
        }
        catch (Exception ex)
        {
            // исключение из обработчика события на пуле потоков уронило бы весь воркер
            logger.LogError(ex, "Ошибка обработки завершения ffmpeg {ChannelId}", channelId);
        }
    }
}
