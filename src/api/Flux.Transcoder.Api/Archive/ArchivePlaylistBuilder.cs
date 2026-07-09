using System.Globalization;
using System.Text;

namespace Flux.Transcoder.Api.Archive;

public sealed class ArchivePlaylistBuilder(IConfiguration config)
{
    private readonly string _archiveRoot = config["Archive:SegmentRoot"] ?? @"D:\fluxcast\storage";   // D:\fluxcast\hot (корень, где лежат каналы)
    private readonly string _nginxBaseUrl = config["Archive:NginxBaseUrl"] ?? "http://localhost:3088";  // http://localhost:3088

    /// <summary>
    /// Собирает плейлист из архивных сегментов канала, начиная с момента from
    /// и до последнего доступного сегмента.
    /// </summary>
    public string? Build(Guid channelId, DateTimeOffset from)
    {
        var channelArchive = Path.Combine(_archiveRoot, channelId.ToString(), "archive");
        Console.WriteLine($"[ARCHIVE] root={_archiveRoot}");
        Console.WriteLine($"[ARCHIVE] channelArchive={channelArchive}");
        Console.WriteLine($"[ARCHIVE] exists={Directory.Exists(channelArchive)}");        
        
        if (!Directory.Exists(channelArchive))
            return null;
        
        var all = EnumerateSegments(channelArchive).ToList();
        Console.WriteLine($"[ARCHIVE] найдено файлов={all.Count}, from={from:O}");
        foreach (var s in all.Take(3))
            Console.WriteLine($"[ARCHIVE]   сегмент: time={s.Time:O} url={s.HttpUrl}");

        var segments = EnumerateSegments(channelArchive)
            .Where(s => s.Time >= from)     
            .OrderBy(s => s.Time)
            .ToList();
        Console.WriteLine($"[ARCHIVE] после фильтра >= from: {segments.Count}");

        if (segments.Count == 0)
            return null;

        var sb = new StringBuilder();
        sb.AppendLine("#EXTM3U");
        sb.AppendLine("#EXT-X-VERSION:6");
        sb.AppendLine("#EXT-X-TARGETDURATION:5");
        sb.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");
        sb.AppendLine("#EXT-X-INDEPENDENT-SEGMENTS");

        for (var i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            double duration = i < segments.Count - 1
                ? (segments[i + 1].Time - seg.Time).TotalSeconds
                : 4.0;
            if (duration <= 0 || duration > 15) duration = 4.0;

            sb.AppendLine($"#EXTINF:{duration.ToString("0.000", CultureInfo.InvariantCulture)},");
            sb.AppendLine($"#EXT-X-PROGRAM-DATE-TIME:{seg.Time:yyyy-MM-ddTHH:mm:ss.fffzzz}");
            sb.AppendLine(seg.HttpUrl);
        }

        return sb.ToString();
    }

    private IEnumerable<(DateTimeOffset Time, string HttpUrl)> EnumerateSegments(string channelArchive)
    {
        // структура: archive/YYYY-MM-DD/HH/seg_YYYYMMDD_HHMMSS.ts
        foreach (var file in Directory.EnumerateFiles(channelArchive, "seg_*.ts", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(file);   // seg_20260702_234433
            var time = ParseSegmentTime(name);
            if (time is null) continue;

            // относительный путь от корня hot -> HTTP-URL через nginx
            var relative = Path.GetRelativePath(_archiveRoot, file)
                .Replace('\\', '/');
            var url = $"{_nginxBaseUrl}/{relative}";

            yield return (time.Value, url);
        }
    }

    private static DateTimeOffset? ParseSegmentTime(string fileName)
    {
        // seg_20260702_234433  ->  2026-07-02 23:44:33
        var parts = fileName.Split('_');
        if (parts.Length != 3) return null;

        if (DateTime.TryParseExact(
                $"{parts[1]}{parts[2]}",
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
        {
            return new DateTimeOffset(dt, TimeZoneInfo.Local.GetUtcOffset(dt));
        }
        return null;
    }
}