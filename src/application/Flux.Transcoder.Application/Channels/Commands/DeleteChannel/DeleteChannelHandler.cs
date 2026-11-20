using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Application.Channels.Commands.DeleteChannel;

/// <summary>
/// Удаляет канал и его настройки. Записанный архив остаётся на диске —
/// его удалит только ручная чистка (чистильщик обходит лишь существующие каналы).
/// </summary>
public class DeleteChannelHandler
{
    public static async Task HandleAsync(
        DeleteChannelRequest request,
        IRepository<Channel> channels,
        CancellationToken cancellationToken)
    {
        var channel = await channels.FirstOrDefaultAsync(
                          new ChannelByIdSpec(request.ChannelId), cancellationToken)
                      ?? throw new NotFoundException($"Канал с Id {request.ChannelId} не найден.");

        channel.EnsureCanBeDeleted();   // активный канал -> 409

        await channels.DeleteAsync(channel, cancellationToken);   // Ardalis сохраняет сам
    }
}
