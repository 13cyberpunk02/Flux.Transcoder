using System.Globalization;
using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Cleaner.Configuration;
using Flux.Transcoder.Domain.Channels;
using Microsoft.Extensions.Options;

namespace Flux.Transcoder.Cleaner.Services;

public sealed class RetentionCleanupService(
    IServiceProvider services,
    IOptions<CleanerOptions> options,
    ILogger<RetentionCleanupService> logger)
    : BackgroundService
{
    private readonly CleanerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
        catch (OperationCanceledException) { return; }

        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        logger.LogInformation(
            "Retention-чистильщик запущен. Архив: {Root}, интервал: {Interval} мин",
            _options.ArchiveRoot, interval.TotalMinutes);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка в цикле retention-чистки");
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        List<(Guid Id, TimeSpan ArchiveRetention)> channels;
        await using (var scope = services.CreateAsyncScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IReadRepository<Channel>>();
            var all = await repo.ListAsync(ct);
            channels = all.Select(c => (c.Id, c.Retention.ArchiveRetention)).ToList();
        }

        var now = DateTimeOffset.Now;
        foreach (var (channelId, retention) in channels)
        {
            var cutoff = now - retention;
            CleanupChannel(channelId, cutoff);
        }
    }

    private void CleanupChannel(Guid channelId, DateTimeOffset cutoff)
    {
        var archiveRoot = Path.Combine(_options.ArchiveRoot, channelId.ToString(), "archive");
        if (!Directory.Exists(archiveRoot))
            return;

        foreach (var dayDir in Directory.GetDirectories(archiveRoot))
        {
            var dayName = Path.GetFileName(dayDir);   // 2026-07-05
            if (!DateTime.TryParseExact(dayName, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
                continue;

            var dayEnd = new DateTimeOffset(day.AddDays(1), DateTimeOffset.Now.Offset);
            if (dayEnd <= cutoff)
            {
                DeleteDirectorySafe(dayDir);
                continue;
            }

            foreach (var hourDir in Directory.GetDirectories(dayDir))
            {
                var hourName = Path.GetFileName(hourDir);   // 14
                if (!int.TryParse(hourName, out var hour) || hour is < 0 or > 23)
                    continue;

                var hourStart = new DateTimeOffset(
                    day.Year, day.Month, day.Day, hour, 0, 0, DateTimeOffset.Now.Offset);
                var hourEnd = hourStart.AddHours(1);

                if (hourEnd <= cutoff)
                {
                    DeleteDirectorySafe(hourDir);
                    continue;
                }

                if (hourStart < cutoff && hourEnd > cutoff)
                    CleanupSegmentsInHour(hourDir, cutoff);
            }

            if (Directory.Exists(dayDir) && !Directory.EnumerateFileSystemEntries(dayDir).Any())
                DeleteDirectorySafe(dayDir);
        }
    }

    private void CleanupSegmentsInHour(string hourDir, DateTimeOffset cutoff)
    {
        foreach (var file in Directory.GetFiles(hourDir, "seg_*.ts"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var time = ParseSegmentTime(name);
            if (time is null) continue;

            if (time.Value < cutoff)
                DeleteFileSafe(file);
        }

        if (!Directory.EnumerateFileSystemEntries(hourDir).Any())
            DeleteDirectorySafe(hourDir);
    }

    private static DateTimeOffset? ParseSegmentTime(string fileName)
    {
        var parts = fileName.Split('_');
        if (parts.Length != 3) return null;

        if (DateTime.TryParseExact($"{parts[1]}{parts[2]}", "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return new DateTimeOffset(dt, DateTimeOffset.Now.Offset);
        return null;
    }

    private void DeleteDirectorySafe(string dir)
    {
        try
        {
            Directory.Delete(dir, recursive: true);
            logger.LogInformation("Удалено по retention: {Dir}", dir);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось удалить папку {Dir}", dir);
        }
    }

    private void DeleteFileSafe(string file)
    {
        try { File.Delete(file); }
        catch (Exception ex) { logger.LogWarning(ex, "Не удалось удалить файл {File}", file); }
    }
}