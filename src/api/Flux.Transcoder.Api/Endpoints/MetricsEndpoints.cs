using Flux.Transcoder.Api.Metrics;
using Microsoft.AspNetCore.Authorization;
using Wolverine.Http;

namespace Flux.Transcoder.Api.Endpoints;

[Authorize]
public class MetricsEndpoints
{
    /// <summary>Поминутные агрегаты хранятся 14 дней — больше запрашивать бессмысленно.</summary>
    private const int MaxMinutes = 14 * 24 * 60;

    [WolverineGet("/api/channels/{id:guid}/metrics")]
    public static async Task<IResult> GetMetrics(
        Guid id, MetricsReader reader, CancellationToken cancellationToken, int? minutes)
    {
        // значение по умолчанию у параметра Wolverine не применяет: без ?minutes приходил 0
        var range = minutes ?? 60;
        if (range is < 1 or > MaxMinutes)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Validation failed", detail: $"minutes: от 1 до {MaxMinutes}.");

        return Results.Ok(await reader.GetMinutelyAsync(id, range, cancellationToken));
    }
}
