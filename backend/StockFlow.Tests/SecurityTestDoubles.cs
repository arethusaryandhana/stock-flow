using StockFlow.Application.Abstractions.Services;

namespace StockFlow.Tests;

internal static class SecurityTestDoubles
{
    public static ICurrentUserService AnonymousUser { get; } = new StaticCurrentUserService();
    public static IUserAccessReader NoAccess { get; } = new StaticUserAccessReader();

    private sealed class StaticCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
    }

    private sealed class StaticUserAccessReader : IUserAccessReader
    {
        public Task<UserAccessSnapshot?> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default) => Task.FromResult<UserAccessSnapshot?>(null);
    }
}
