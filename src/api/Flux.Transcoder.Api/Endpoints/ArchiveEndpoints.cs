using Flux.Transcoder.Api.Archive;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Wolverine.Http;

namespace Flux.Transcoder.Api.Endpoints;

public class ArchiveEndpoints
{
    // Плейлист отдаётся плееру напрямую — без JWT-заголовка (плеер его не пошлёт).
    // [WolverineGet("/api/channels/{id:guid}/archive.m3u8")]
    // [AllowAnonymous]
    // public static IResult GetArchive(
    //     Guid id,
    //     DateTimeOffset from,
    //     ArchivePlaylistBuilder builder)
    // {
    //     var playlist = builder.Build(id, from);
    //     return playlist is null ? 
    //         Results.NotFound("Нет сегментов с указанного момента.") : 
    //         Results.Text(playlist, "application/vnd.apple.mpegurl");
    // }
    
    [WolverineGet("/api/channels/{id:guid}/archive")]
    [AllowAnonymous]
    [EnableCors("player")]           // <- политика для плеера
    public static IResult GetArchivePlayer(Guid id, DateTimeOffset from, ArchivePlaylistBuilder builder)
    {
        var playlist = builder.Build(id, from);
        return playlist is null ? 
            Results.NotFound("Нет сегментов с указанного момента.") : 
            Results.Text(playlist, "application/vnd.apple.mpegurl");
    }
}