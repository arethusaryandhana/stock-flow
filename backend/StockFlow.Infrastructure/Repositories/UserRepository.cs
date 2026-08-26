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
}
