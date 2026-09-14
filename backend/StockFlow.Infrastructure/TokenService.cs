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
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.Name),
            new Claim("token_version", user.TokenVersion.ToString())
        };

        var lifetimeMinutes = int.TryParse(configuration["Jwt:LifetimeMinutes"], out var configuredLifetime)
            ? configuredLifetime
            : 480;
        lifetimeMinutes = Math.Clamp(lifetimeMinutes, 5, 480);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(lifetimeMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
