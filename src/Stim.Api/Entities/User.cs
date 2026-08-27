using System;

namespace Stim.Api.Entities;

public class User
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string? IdentityId { get; set; }
}
