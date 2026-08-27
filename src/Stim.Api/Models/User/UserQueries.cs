using System.Linq.Expressions;

namespace Stim.Api.Models.User;

public static class UserQueries
{
    public static Expression<Func<Entities.User, UserDto>> ProjectToDto()
    {
        return u => new()
        {
            Id = u.Id,
            Email = u.Email,
            Password = u.Password,
            CreatedAtUtc = u.CreatedAtUtc,
        };
    }
}
