using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class UserRepository(StockFlowDbContext db) : IUserRepository
{
    public async Task RecordFailedLoginAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var maximumAttempts = LoginSecurityPolicy.MaximumFailedAttempts;
        var lockoutMinutes = (int)LoginSecurityPolicy.LockoutDuration.TotalMinutes;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE identity.users
            SET failed_login_attempts = CASE
                    WHEN login_lockout_end IS NOT NULL AND login_lockout_end <= NOW() THEN 1
                    ELSE failed_login_attempts + 1
                END,
                login_lockout_end = CASE
                    WHEN (CASE
                            WHEN login_lockout_end IS NOT NULL AND login_lockout_end <= NOW() THEN 1
                            ELSE failed_login_attempts + 1
                          END) >= {maximumAttempts}
                    THEN NOW() + ({lockoutMinutes} * INTERVAL '1 minute')
                    ELSE NULL
                END
            WHERE id = {userId}
              AND (login_lockout_end IS NULL OR login_lockout_end <= NOW())
            """, cancellationToken);
    }

    public async Task ResetFailedLoginsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE identity.users
            SET failed_login_attempts = 0,
                login_lockout_end = NULL
            WHERE id = {userId}
              AND (failed_login_attempts <> 0 OR login_lockout_end IS NOT NULL)
            """, cancellationToken);
    }

    public Task<User?> GetActiveByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return db.UsersSet
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Email == email && user.IsActive && user.Role.IsActive, cancellationToken);
    }

    public Task<User?> GetActiveByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        db.UsersSet
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Id == id && user.IsActive && user.Role.IsActive, cancellationToken);

    public Task<bool> EmailExistsForOtherUserAsync(
        string email,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        db.UsersSet.AnyAsync(user => user.Email == email && user.Id != userId, cancellationToken);

    public Task<PasswordResetToken?> GetPasswordResetTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return db.PasswordResetTokens
            .AsNoTracking()
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<bool> ConsumePasswordResetTokenAsync(
        Guid tokenId,
        string newPasswordHash,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM identity.password_reset_tokens WHERE id = {tokenId} FOR UPDATE",
            cancellationToken);

        var resetToken = await db.PasswordResetTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.Id == tokenId, cancellationToken);
        var now = DateTime.UtcNow;
        if (resetToken is null || resetToken.UsedAt is not null || resetToken.ExpiresAt <= now || !resetToken.User.IsActive)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        resetToken.User.PasswordHash = newPasswordHash;
        resetToken.User.TokenVersion++;
        resetToken.User.FailedLoginAttempts = 0;
        resetToken.User.LoginLockoutEnd = null;
        resetToken.UsedAt = now;
        await InvalidatePasswordResetTokensAsync(resetToken.UserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
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
