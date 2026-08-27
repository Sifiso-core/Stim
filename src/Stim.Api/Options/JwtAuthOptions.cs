using System;

namespace Stim.Api.Options;

public class JwtAuthOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpirationInMin { get; set; }
    public int RefreshTokenExpirationDays { get; set; }
}
