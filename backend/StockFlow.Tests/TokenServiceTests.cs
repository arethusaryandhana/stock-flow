using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using StockFlow.Core;
using StockFlow.Infrastructure;

namespace StockFlow.Tests;

public sealed class TokenServiceTests
{
    [Fact]
    public void Create_UsesTheSingleConfiguredLifetime()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "stockflow-test-key-that-is-at-least-32-bytes-long",
                ["Jwt:Issuer"] = "StockFlow.Tests",
                ["Jwt:Audience"] = "StockFlow.Tests",
                ["Jwt:LifetimeMinutes"] = "480"
            })
            .Build();
        var user = new User
        {
            Email = "admin@stockflow.local",
            FullName = "Admin",
            Role = new Role { Name = "Admin" }
        };
        var beforeCreation = DateTime.UtcNow;

        var rawToken = new TokenService(configuration).Create(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);
        var afterCreation = DateTime.UtcNow;

        Assert.InRange(
            token.ValidTo,
            beforeCreation.AddMinutes(480).AddSeconds(-1),
            afterCreation.AddMinutes(480));
    }
}
