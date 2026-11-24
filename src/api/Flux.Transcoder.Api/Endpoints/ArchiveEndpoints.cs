using Flux.Transcoder.Api.Archive;
using Microsoft.AspNetCore.Authorization;
using Wolverine.Http;

namespace Flux.Transcoder.Api.Endpoints;

[Authorize]
public class ArchiveEndpoints
{
    /// <summary>
    /// DVR-плейлист с момента from. Плеер (hls.js) присылает JWT в заголовке,
    /// поэтому анонимно перебирать архив больше нельзя.
    /// </summary>
    [WolverineGet("/api/channels/{id:guid}/archive")]
    public static IResult GetArchivePlayer(Guid id, DateTimeOffset? from, ArchivePlaylistBuilder builder)
    {
        if (from is null)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Validation failed", detail: "Укажите момент начала архива: параметр from.");

        var playlist = builder.Build(id, from.Value);
        return playlist is null
            ? Results.NotFound("Нет сегментов с указанного момента.")
            : Results.Text(playlist, "application/vnd.apple.mpegurl");
    }
}
