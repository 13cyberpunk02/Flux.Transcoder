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
    private readonly FfmpegOptions _options = options.Value;

    public Task StartAsync(Channel channel, CancellationToken cancellationToken = default)
    {
        if (supervisor.IsRunning(channel.Id))
        {
            logger.LogWarning("Channel {ChannelId} already running", channel.Id);
            return Task.CompletedTask;
        }

        var args = argumentBuilder.Build(channel);
        logger.LogInformation("FFMPEG ARGS: {Args}", string.Join(" ", args));
        var psi = new ProcessStartInfo
        {
            FileName = _options.BinaryPath,
            RedirectStandardOutput = true,   // -progress pipe:1
            RedirectStandardError = true,    // лог ffmpeg
            UseShellExecute = false,
            CreateNoWindow = true
        };
        // аргументы передаём списком — .NET сам корректно экранирует,
        // никакой склейки в одну строку (та же защита, что давал CliWrap)
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

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

        process.Exited += (_, _) =>
        {
            var exitCode = process.ExitCode;

            // ВАЖНО: проверяем флаг ДО Unregister (после — запись исчезнет)
            var wasIntentional = supervisor.WasIntentionalStop(channel.Id);

            logger.LogInformation(
                "ffmpeg exited for {ChannelId}, code {Code}, intentional={Intentional}",
                channel.Id, exitCode, wasIntentional);

            supervisor.Unregister(channel.Id);

            // перезапускаем ТОЛЬКО если:
            // - остановка НЕ была намеренной (не пользователь нажал Stop)
            // - и процесс завершился с ошибкой (код != 0)
            if (!wasIntentional && exitCode != 0)
            {
                logger.LogWarning(
                    "ffmpeg для {ChannelId} умер неожиданно (код {Code}) — запускаю перезапуск",
                    channel.Id, exitCode);
                restartCoordinator.OnStalled(channel.Id);
            }
        };

        if (!process.Start())
            throw new InvalidOperationException($"Failed to start ffmpeg for channel {channel.Id}.");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        supervisor.Register(channel.Id, process.Id, cts, process);
        logger.LogInformation(
            "ffmpeg started for {ChannelId}, pid {Pid}", channel.Id, process.Id);

        // когда токен отменяют (Stop) — мягко гасим процесс
        cts.Token.Register(() => TerminateGracefully(channel.Id, process));

        return Task.CompletedTask;
    }

    public Task StopAsync(Guid channelId, CancellationToken cancellationToken = default)
    {
        supervisor.Stop(channelId);
        return Task.CompletedTask;
    }

    private void TerminateGracefully(Guid channelId, Process process)
    {
        try
        {
            if (process.HasExited) return;

            // ffmpeg по умолчанию завершается по SIGTERM/'q'; на Linux Kill(false) шлёт SIGTERM
            process.Kill(entireProcessTree: true);

            // даём время на флаш сегментов
            if (!process.WaitForExit(5000))
                logger.LogWarning("ffmpeg {ChannelId} did not exit in time", channelId);
        }
        catch (InvalidOperationException)
        {
            // процесс уже завершился — игнорируем
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error terminating ffmpeg {ChannelId}", channelId);
        }
    }
}