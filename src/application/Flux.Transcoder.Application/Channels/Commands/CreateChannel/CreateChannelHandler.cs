using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Domain.Channels;
using Flux.Transcoder.Domain.Common;
using Flux.Transcoder.Domain.ValueObjects;

namespace Flux.Transcoder.Application.Channels.Commands.CreateChannel;

public static class CreateChannelHandler
{
    public static async Task<CreateChannelResult> HandleAsync(
        CreateChannelCommand command,
        IRepository<Channel> channels,
        IReadRepository<EncodingProfile> profiles,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var profile = await profiles.GetByIdAsync(command.ProfileId, cancellationToken)
                      ?? throw new DomainException($"Профиль декодирования {command.ProfileId} не найдено.");

        var source = new SourceUrl(command.SourceUrl); // валидирует схему, кидает DomainException
        var channel = Channel.Create(source, profile);

        await channels.AddAsync(channel, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateChannelResult(channel.Id);
    }
}