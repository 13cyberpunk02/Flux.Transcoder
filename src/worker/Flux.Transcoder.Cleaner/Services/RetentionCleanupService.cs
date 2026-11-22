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

        CleanupOrphans(channels.Select(c => c.Id).ToHashSet(), now - RetentionPolicy.MaxRetention);
    }

    /// <summary>
    /// Папки удалённых каналов. Их срок хранения уже неизвестен, поэтому берём максимальный:
    /// так и при подключении не к той БД не удалится ничего моложе MaxRetention.
    /// Трогаем только свою структуру (папка-GUID, archive/дата/час, live).
    /// </summary>
    private void CleanupOrphans(IReadOnlySet<Guid> known, DateTimeOffset cutoff)
    {
        if (!Directory.Exists(_options.ArchiveRoot))
            return;

        foreach (var dir in Directory.GetDirectories(_options.ArchiveRoot))
        {
            if (!Guid.TryParseExact(Path.GetFileName(dir), "D", out var id) || known.Contains(id))
                continue;

            try
            {
                var archive = Path.Combine(dir, "archive");
                var live = Path.Combine(dir, "live");

                // по ссылкам не ходим: за ними может быть чужая папка
                if (IsLink(dir) || IsLink(archive) || IsLink(live))
                    continue;

                CleanupChannel(id, cutoff);

                DeleteExpiredFiles(archive, "current.m3u8*", cutoff);
                DeleteExpiredFiles(live, "seg_*.ts", cutoff);
                DeleteExpiredFiles(live, "live.m3u8*", cutoff);

                DeleteIfEmpty(archive);
                DeleteIfEmpty(live);
                DeleteIfEmpty(dir);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Не удалось очистить папку удалённого канала {Dir}", dir);
            }
        }
    }

    private static bool IsLink(string dir) =>
        Directory.Exists(dir) && new DirectoryInfo(dir).LinkTarget is not null;

    private void DeleteExpiredFiles(string dir, string pattern, DateTimeOffset cutoff)
    {
        if (!Directory.Exists(dir))
            return;

        foreach (var file in Directory.GetFiles(dir, pattern))
        {
            if (File.GetLastWriteTimeUtc(file) < cutoff.UtcDateTime)
                DeleteFileSafe(file);
        }
    }

    private void DeleteIfEmpty(string dir)
    {
        if (!Directory.Exists(dir) || Directory.EnumerateFileSystemEntries(dir).Any())
            return;

        Directory.Delete(dir);   // не рекурсивно
        logger.LogInformation("Удалена пустая папка удалённого канала: {Dir}", dir);
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