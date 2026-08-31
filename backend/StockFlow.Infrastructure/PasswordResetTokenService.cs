using System.Security.Cryptography;
using System.Text;
using StockFlow.Application.Abstractions.Services;

namespace StockFlow.Infrastructure;

public sealed class PasswordResetTokenService : IPasswordResetTokenService
{
    public string Generate() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .Replace("+", "-")
        .Replace("/", "_")
        .TrimEnd('=');

    public string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
