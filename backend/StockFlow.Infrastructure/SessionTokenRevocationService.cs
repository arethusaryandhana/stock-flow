using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Services;

namespace StockFlow.Infrastructure;

public sealed class SessionTokenRevocationService(StockFlowDbContext db) : ISessionTokenRevocationService
{
    public async Task RevokeAsync(
        string tokenId,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await db.RevokedSessionTokens
            .Where(token => token.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO identity.revoked_session_tokens (id, created_at, token_id, expires_at)
            VALUES ({Guid.NewGuid()}, {now}, {tokenId}, {expiresAt})
            ON CONFLICT (token_id) DO NOTHING
            """, cancellationToken);
    }

    public Task<bool> IsRevokedAsync(
        string tokenId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return db.RevokedSessionTokens.AnyAsync(
            token => token.TokenId == tokenId && token.ExpiresAt > now,
            cancellationToken);
    }
}
