namespace Stim.Api.Models.Authentication;

public class LoginUserDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}
