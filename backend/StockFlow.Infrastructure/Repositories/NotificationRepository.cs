using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class NotificationRepository(StockFlowDbContext db) : INotificationRepository
{
    public async Task<NotificationPageResponse> GetAllAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagination = Pagination.Normalize(page, pageSize);
        var query = db.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId);
        var totalCount = await query.CountAsync(cancellationToken);
        var unreadCount = await query.CountAsync(notification => !notification.IsRead, cancellationToken);
        var items = await query
            .OrderBy(notification => notification.IsRead)
            .ThenByDescending(notification => notification.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(notification => new NotificationResponse(
                notification.Id,
                notification.Type.ToString(),
                notification.Title,
                notification.Message,
                notification.Link,
                notification.IsRead,
                notification.ReadAt,
                notification.CreatedAt))
            .ToListAsync(cancellationToken);

        return new NotificationPageResponse(
            items,
            pagination.Page,
            pagination.PageSize,
            totalCount,
            unreadCount);
    }

    public async Task<bool> MarkReadAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var notification = await db.Notifications.SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == userId,
            cancellationToken);
        if (notification is null)
            return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public Task MarkAllReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        db.Notifications
            .Where(notification => notification.UserId == userId && !notification.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(notification => notification.IsRead, true)
                    .SetProperty(notification => notification.ReadAt, DateTime.UtcNow),
                cancellationToken);

    public async Task CreateLowStockNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var recipients = await db.UsersSet
            .AsNoTracking()
            .Where(user => user.IsActive &&
                (user.Role.Name == "Admin" || user.Role.Name == "Manager"))
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);
        if (recipients.Count == 0)
            return;

        var products = await db.ProductsSet
            .AsNoTracking()
            .Where(product => product.IsActive && product.StockOnHand <= product.ReorderLevel)
            .Select(product => new
            {
                product.Id,
                product.Sku,
                product.Name,
                product.StockOnHand,
                product.ReorderLevel,
                product.Unit
            })
            .ToListAsync(cancellationToken);
        if (products.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var dateKey = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        foreach (var userId in recipients)
        {
            foreach (var product in products)
            {
                var balance = product.StockOnHand.ToString("0.##", CultureInfo.InvariantCulture);
                var threshold = product.ReorderLevel.ToString("0.##", CultureInfo.InvariantCulture);
                var deduplicationKey = $"low-stock:{userId:N}:{product.Id:N}:{dateKey}";
                var message = $"{product.Sku} - {product.Name} tersisa {balance} {product.Unit} (minimum {threshold}).";

                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO identity.notifications
                        (id, user_id, type, title, message, link, deduplication_key, is_read, created_at)
                    VALUES
                        ({Guid.NewGuid()}, {userId}, {(int)NotificationType.LowStock}, {"Stok menipis"},
                         {message}, {"/products"}, {deduplicationKey}, {false}, {now})
                    ON CONFLICT (deduplication_key) WHERE deduplication_key IS NOT NULL DO NOTHING
                    """, cancellationToken);
            }
        }
    }
}
