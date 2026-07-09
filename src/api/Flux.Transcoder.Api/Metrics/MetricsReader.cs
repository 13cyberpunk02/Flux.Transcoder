using Npgsql;

namespace Flux.Transcoder.Api.Metrics;

public sealed class MetricsReader(IConfiguration config)
{
    private readonly string _connectionString = config.GetConnectionString("flux_transcoder_db")!;

    /// <summary>Поминутные метрики канала за последние N минут.</summary>
    public async Task<IReadOnlyList<MetricPoint>> GetMinutelyAsync(
        Guid channelId, int lastMinutes, CancellationToken ct)
    {
        const string sql = """
                           SELECT bucket, avg_bitrate, max_bitrate, dropped_frames, uptime_seconds
                           FROM channel_metrics_1m
                           WHERE channel_id = @cid
                             AND bucket >= NOW() - (@mins || ' minutes')::interval
                           ORDER BY bucket ASC
                           """;

        var points = new List<MetricPoint>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("cid", channelId);
        cmd.Parameters.AddWithValue("mins", lastMinutes);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            points.Add(new MetricPoint(
                reader.GetFieldValue<DateTimeOffset>(0),
                reader.IsDBNull(1) ? 0 : reader.GetDouble(1),
                reader.IsDBNull(2) ? 0 : reader.GetDouble(2),
                reader.IsDBNull(3) ? 0 : reader.GetInt64(3),
                reader.IsDBNull(4) ? 0 : reader.GetDouble(4)));
        }
        return points;
    }
}