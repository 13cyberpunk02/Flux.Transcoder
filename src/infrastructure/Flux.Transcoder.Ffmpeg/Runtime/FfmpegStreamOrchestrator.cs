using System.Diagnostics;
using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Ffmpeg.Configuration;
using Flux.Transcoder.Ffmpeg.Encoding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flux.Transcoder.Ffmpeg.Runtime;

public sealed class FfmpegStreamOrchestrator(
    FfmpegArgumentBuilder argumentBuilder,
    ProcessSupervisor supervisor,
    ProgressParser progressParser,
    RestartCoordinator restartCoordinator,
    IOptions<FfmpegOptions> options,
    ILogger<FfmpegStreamOrchestrator> logger)
    : IStreamOrchestrator
{
    private static readonly TimeSpan GracefulStopTimeout = TimeSpan.FromSeconds(5);

    private readonly FfmpegOptions _options = options.Value;

    // cancellationToken намеренно НЕ связывается с процессом: ffmpeg живёт дольше,
    // чем обработка сообщения, которое его запустило.
    public Task StartAsync(Channel channel, CancellationToken cancellationToken = default)
    {
        var existing = supervisor.Get(channel.Id);
        if (existing is not null)
        {
            if (!existing.HasExited)
            {
                logger.LogWarning("Channel {ChannelId} already running", channel.Id);
                return Task.CompletedTask;
            }

            // процесс уже умер, но Exited ещё не успел снять запись (например, сразу после Stop при рестарте)
            supervisor.Unregister(existing);
        }

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

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var cts = new CancellationTokenSource();

        // присваивается после Register; Exited читает его через замыкание
        RunningProcess? running = null;

        // stdout -> парсер прогресса
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                progressParser.Feed(channel.Id, e.Data);
        };

        // stderr -> логи
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                logger.LogDebug("[ffmpeg {ChannelId}] {Line}", channel.Id, e.Data);
        };

        process.Exited += (_, _) => OnExited(channel.Id, process, running);

        if (!process.Start())
        {
            cts.Dispose();
            process.Dispose();
            throw new InvalidOperationException($"Failed to start ffmpeg for channel {channel.Id}.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        running = supervisor.Register(channel.Id, process.Id, cts, process);
        logger.LogInformation(
            "ffmpeg started for {ChannelId}, pid {Pid}", channel.Id, process.Id);

        // когда токен отменяют (Stop) — мягко гасим процесс
        var registered = running;
        cts.Token.Register(() => TerminateGracefully(registered));

        return Task.CompletedTask;
    }

    public Task StopAsync(Guid channelId, CancellationToken cancellationToken = default)
    {
        // ручная остановка/рестарт — счётчик авто-перезапусков начинается заново
        restartCoordinator.Reset(channelId);
        supervisor.Stop(channelId);
        return Task.CompletedTask;
    }

    private void OnExited(Guid channelId, Process process, RunningProcess? running)
    {
        try
        {
            int exitCode;
            try { exitCode = process.ExitCode; }
            catch (InvalidOperationException) { exitCode = -1; }

            // флаг берём у СВОЕГО экземпляра, а не по channelId — после рестарта
            // в супервизоре уже может лежать новый процесс
            var wasIntentional = running?.IntentionalStop ?? false;

            logger.LogInformation(
                "ffmpeg exited for {ChannelId}, code {Code}, intentional={Intentional}",
                channelId, exitCode, wasIntentional);

            if (running is not null)
                supervisor.Unregister(running);

            process.Dispose();

            // перезапускаем ТОЛЬКО если остановка не намеренная и процесс завершился с ошибкой
            if (!wasIntentional && exitCode != 0)
            {
                logger.LogWarning(
                    "ffmpeg для {ChannelId} умер неожиданно (код {Code}) — запускаю перезапуск",
                    channelId, exitCode);
                restartCoordinator.OnStalled(channelId);
            }
        }
        catch (Exception ex)
        {
            // исключение из обработчика события на пуле потоков уронило бы весь воркер
            logger.LogError(ex, "Ошибка обработки завершения ffmpeg {ChannelId}", channelId);
        }
    }

    private void TerminateGracefully(RunningProcess running)
    {
        var process = running.Process;
        try
        {
            if (process.HasExited) return;

            // 'q' в stdin — штатное завершение ffmpeg: дописывает сегмент и плейлист
            try
            {
                process.StandardInput.Write('q');
                process.StandardInput.Flush();
            }
            catch (IOException)
            {
                // stdin уже закрыт — сразу к Kill
            }

            if (process.WaitForExit(GracefulStopTimeout))
                return;

            logger.LogWarning(
                "ffmpeg {ChannelId} не завершился по 'q' за {Timeout:g} — убиваю",
                running.ChannelId, GracefulStopTimeout);
            process.Kill(entireProcessTree: true);
            process.WaitForExit(TimeSpan.FromSeconds(2));
        }
        catch (InvalidOperationException)
        {
            // процесс уже завершился или освобождён обработчиком Exited (ObjectDisposedException)
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error terminating ffmpeg {ChannelId}", running.ChannelId);
        }
    }
}
