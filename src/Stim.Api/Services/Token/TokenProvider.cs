using System;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Stim.Api.Models.Authentication;
using Stim.Api.Options;

namespace Stim.Api.Services.Token;

public class TokenProvider(IOptions<JwtAuthOptions> options)
{
    private readonly JwtAuthOptions options = options.Value;
    public AccessTokenDto Create(TokenRequest tokenRequest)
    {

        return new(AccessTokenGenerator(tokenRequest), RefreshTokenGenerator());
    }
    private string AccessTokenGenerator(TokenRequest tokenRequest)
    {
        var keyBytes = Encoding.UTF8.GetBytes(options.Key);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("JWT Signing Key must be at least 32 bytes");
        }

        var symmetricSecurityKey = new SymmetricSecurityKey(keyBytes);

        var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>()
        {
            new(JwtRegisteredClaimNames.Sub,tokenRequest.UserId),
            new(JwtRegisteredClaimNames.Email,tokenRequest.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = signingCredentials,
            Issuer = options.Issuer,
            Expires = DateTime.UtcNow.AddMinutes(options.ExpirationInMin),
            Audience = options.Audience
        };

        return new JsonWebTokenHandler().CreateToken(tokenDescriptor);

    }
    private string RefreshTokenGenerator()
    {
        return string.Empty;
    }

}
