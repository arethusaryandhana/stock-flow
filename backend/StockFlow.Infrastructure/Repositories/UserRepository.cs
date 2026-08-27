using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class UserRepository(StockFlowDbContext db) : IUserRepository
{
    public Task<User?> GetActiveByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return db.UsersSet
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Email == email && user.IsActive, cancellationToken);
    }

    public Task<PasswordResetToken?> GetPasswordResetTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return db.PasswordResetTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public async Task InvalidatePasswordResetTokensAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await db.PasswordResetTokens
            .Where(token => token.UserId == userId && token.UsedAt == null && token.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
            token.UsedAt = DateTime.UtcNow;
    }

    public Task AddPasswordResetTokenAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default) =>
        db.PasswordResetTokens.AddAsync(token, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
