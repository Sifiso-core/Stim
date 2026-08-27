using System;

namespace Stim.Api.Models.Authentication;

public class RefreshTokenDto
{
    public required string RefreshToken { get; set; }
}
