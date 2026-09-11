using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.Application.UseCases;

public sealed class AuditLogUseCase(IAuditLogRepository auditLogs) : IAuditLogUseCase
{
    public Task<PagedResponse<AuditLogResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        string? entityType = null,
        string? action = null,
        CancellationToken cancellationToken = default) =>
        auditLogs.GetAllAsync(page, pageSize, search, entityType, action, cancellationToken);
}
