namespace Stim.Api.Models.Authentication;

public class RegisterUserDto
{
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required string Password { get; set; }
    public required string ConfirmPassword { get; set; }
}
