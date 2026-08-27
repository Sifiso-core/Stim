using System;
using Microsoft.AspNetCore.Identity;

namespace Stim.Api.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required string Token { get; set; }
    public required DateTime ExpiresAtUtc { get; set; }
    public bool IsRevoked { get; set; }
    public IdentityUser User { get; set; }
}
