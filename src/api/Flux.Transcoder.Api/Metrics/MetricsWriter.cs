using Npgsql;

namespace Flux.Transcoder.Api.Metrics;

public sealed class MetricsWriter
{
    private readonly string _connectionString;

    public MetricsWriter(IConfiguration config)
        => _connectionString = config.GetConnectionString("flux_transcoder_db")!;

    public async Task WriteAsync(
        Guid channelId, double bitrateKbps, int droppedFrames,
        double uptimeSeconds, string status, CancellationToken ct)
    {
        const string sql = """
                           INSERT INTO channel_metrics
                               (time, channel_id, bitrate_kbps, dropped_frames, uptime_seconds, status)
                           VALUES (NOW(), @cid, @bitrate, @drops, @uptime, @status)
                           """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("cid", channelId);
        cmd.Parameters.AddWithValue("bitrate", bitrateKbps);
        cmd.Parameters.AddWithValue("drops", droppedFrames);
        cmd.Parameters.AddWithValue("uptime", uptimeSeconds);
        cmd.Parameters.AddWithValue("status", status);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}