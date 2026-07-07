using Ardalis.Specification;
using Flux.Transcoder.Domain.Users;

namespace Flux.Transcoder.Application.Specifications;

public sealed class UserByUsernameSpec : Specification<User>, ISingleResultSpecification<User>
{
    public UserByUsernameSpec(string username) =>
        Query.Where(u => u.Username == username);
}