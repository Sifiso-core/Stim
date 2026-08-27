using Stim.Api.Models.Authentication;

namespace Stim.Api.Models.User;

public static class UserMappings
{
    public static Entities.User ToEntity(this RegisterUserDto registerUserDto, string identityId)
    {
        return new()
        {
            Id = $"u_{Guid.CreateVersion7()}",
            CreatedAtUtc = DateTime.UtcNow,
            Email = registerUserDto.Email,
            IdentityId = identityId,
            Password = registerUserDto.Password
        };
    }
}