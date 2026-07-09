using Flux.Transcoder.Api.Metrics;
using Wolverine.Http;

namespace Flux.Transcoder.Api.Endpoints;

public class MetricsEndpoints
{
    [WolverineGet("/api/channels/{id:guid}/metrics")]
    public static async Task<IReadOnlyList<MetricPoint>> GetMetrics(
        Guid id, MetricsReader reader, int minutes = 60)
    {
        return await reader.GetMinutelyAsync(id, minutes, CancellationToken.None);
    }
}