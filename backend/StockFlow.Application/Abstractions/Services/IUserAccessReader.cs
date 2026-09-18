namespace StockFlow.Application.Abstractions.Services;

public sealed record UserAccessSnapshot(string Role, IReadOnlySet<string> Permissions);

public interface IUserAccessReader
{
    Task<UserAccessSnapshot?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}
