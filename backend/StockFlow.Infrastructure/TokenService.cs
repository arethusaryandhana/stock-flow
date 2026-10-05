using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Core;

namespace StockFlow.Infrastructure;

public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    public string Create(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(GetLifetimeMinutes());
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

        var sessionId = Guid.NewGuid().ToString("N");
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.Name),
            new Claim("token_version", user.TokenVersion.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, sessionId),
            new Claim("session_id", sessionId),
            new Claim("session_expires_at", new DateTimeOffset(expiresAt).ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetLifetimeMinutes() =>
        Math.Clamp(ParseConfiguredInt("Jwt:LifetimeMinutes", 480), 5, 480);

    private int ParseConfiguredInt(string key, int fallback) =>
        int.TryParse(configuration[key], out var configuredValue) ? configuredValue : fallback;
}
