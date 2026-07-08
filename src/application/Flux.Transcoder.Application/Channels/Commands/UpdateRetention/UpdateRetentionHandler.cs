using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Application.Specifications;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Application.Channels.Commands.UpdateRetention;

public class UpdateRetentionHandler
{
    public static async Task HandleAsync(
        UpdateRetentionCommand command,
        IRepository<Channel> channels,
        IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var channel = await channels.FirstOrDefaultAsync(
                          new ChannelByIdSpec(command.ChannelId), ct)
                      ?? throw new NotFoundException($"Канал {command.ChannelId} не найден.");

        // создаём политику через доменный конструктор — он валидирует (1 мин .. 14 дней)
        var policy = new RetentionPolicy(TimeSpan.FromMinutes(command.RetentionMinutes));
        channel.UpdateRetention(policy);

        await unitOfWork.SaveChangesAsync(ct);
    }
}