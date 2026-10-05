using StockFlow.Core;

namespace StockFlow.Application.Abstractions.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
}

public interface ITokenService
{
    string Create(User user);
}

public interface ISessionTokenRevocationService
{
    Task RevokeAsync(string tokenId, DateTime expiresAt, CancellationToken cancellationToken = default);

    Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken = default);
}

public interface IPasswordService
{
    string Hash(string password);

    bool Verify(string password, string hash);
}

public interface IPasswordResetTokenService
{
    string Generate();

    string Hash(string token);
}
