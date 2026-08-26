using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.Application.UseCases;

public sealed class DashboardUseCase(IDashboardRepository dashboard) : IDashboardUseCase
{
    public Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default) =>
        dashboard.GetAsync(cancellationToken);
}
