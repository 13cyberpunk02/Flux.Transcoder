using Ardalis.Specification.EntityFrameworkCore;
using Flux.Transcoder.Application.Abstractions;
using Flux.Transcoder.Domain.Common;

namespace Flux.Transcoder.Infrastructure.Database.Persistence.Repositories;

public class EfReadRepository<T>(AppDbContext dbContext) : RepositoryBase<T>(dbContext), IReadRepository<T>
    where T : class, IAggregateRoot;