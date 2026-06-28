using Ardalis.Specification;
using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Application.Abstractions;

public interface IReadRepository<T> : IReadRepositoryBase<T> where T : class, IAggregateRoot { }