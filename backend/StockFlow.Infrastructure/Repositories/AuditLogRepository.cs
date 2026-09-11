using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;

namespace StockFlow.Infrastructure.Repositories;

public sealed class AuditLogRepository(StockFlowDbContext db) : IAuditLogRepository
{
    public async Task<PagedResponse<AuditLogResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        string? entityType = null,
        string? action = null,
        CancellationToken cancellationToken = default)
    {
        var pagination = Pagination.Normalize(page, pageSize);
        var query = db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(log =>
                log.Summary.ToLower().Contains(term) ||
                log.EntityType.ToLower().Contains(term) ||
                (log.Actor != null &&
                    (log.Actor.FullName.ToLower().Contains(term) ||
                     log.Actor.Email.ToLower().Contains(term))));
        }

        if (!string.IsNullOrWhiteSpace(entityType) &&
            !string.Equals(entityType, "all", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedEntityType = entityType.Trim().ToLower();
            query = query.Where(log => log.EntityType.ToLower() == normalizedEntityType);
        }

        if (!string.IsNullOrWhiteSpace(action) &&
            !string.Equals(action, "all", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedAction = action.Trim().ToLower();
            query = query.Where(log => log.Action.ToLower() == normalizedAction);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(log => new AuditLogResponse(
                log.Id,
                log.ActorId,
                log.Actor == null ? "System" : log.Actor.FullName,
                log.Actor == null ? null : log.Actor.Email,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.Summary,
                log.Changes,
                log.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<AuditLogResponse>(
            items,
            pagination.Page,
            pagination.PageSize,
            totalCount);
    }
}
