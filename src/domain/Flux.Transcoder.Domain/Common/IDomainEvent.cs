namespace Flux.Transcoder.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}