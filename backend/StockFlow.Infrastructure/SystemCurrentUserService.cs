using StockFlow.Application.Abstractions.Services;

namespace StockFlow.Infrastructure;

public sealed class SystemCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
}
