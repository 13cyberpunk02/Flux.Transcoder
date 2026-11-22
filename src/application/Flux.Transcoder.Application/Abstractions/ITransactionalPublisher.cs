namespace Flux.Transcoder.Application.Abstractions;

public interface ITransactionalPublisher
{
    /// <summary>
    /// Сохраняет изменения и сообщение одной транзакцией (outbox): либо есть и то и другое, либо ничего.
    /// Доставка гарантирована, порядок — нет, поэтому получатель сверяется с состоянием в БД.
    /// </summary>
    Task SaveAndPublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : notnull;
}
