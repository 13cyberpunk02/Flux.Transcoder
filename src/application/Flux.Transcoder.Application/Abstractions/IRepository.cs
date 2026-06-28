using Ardalis.Specification;
using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Application.Abstractions;

public interface IRepository<T> : IRepositoryBase<T> where T : class, IAggregateRoot { }