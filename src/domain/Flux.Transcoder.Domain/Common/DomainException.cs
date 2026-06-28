namespace Flux.Transcoder.Domain.Common;

public class DomainException(string message) : Exception(message);

public sealed class NotFoundException(string message) : DomainException(message);

// И отдельный для нелегального перехода состояния:
public sealed class InvalidStateTransitionException(string message) : DomainException(message);