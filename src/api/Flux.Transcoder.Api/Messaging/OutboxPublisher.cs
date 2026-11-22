using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Infrastructure.Database.Persistence;
using Wolverine.EntityFrameworkCore;

namespace Flux.Transcoder.Api.Messaging;

/// <summary>Outbox Wolverine на том же scoped AppDbContext, с которым работают репозитории обработчика.</summary>
public sealed class OutboxPublisher(IDbContextOutbox<AppDbContext> outbox) : ITransactionalPublisher
{
    public async Task SaveAndPublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : notnull
    {
        await outbox.PublishAsync(message);
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
