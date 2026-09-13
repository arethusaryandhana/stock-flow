using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class NotificationRepository(StockFlowDbContext db) : INotificationRepository
{
    public Task<NotificationPreferencesResponse?> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        db.UsersSet
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new NotificationPreferencesResponse(
                user.InAppNotificationsEnabled,
                user.LowStockNotificationsEnabled,
                user.ReportReadyNotificationsEnabled,
                user.SystemNotificationsEnabled,
                user.NotificationSoundEnabled,
                user.NotificationPollingIntervalSeconds))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<NotificationPreferencesResponse?> UpdatePreferencesAsync(
        Guid userId,
        NotificationPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.UsersSet.SingleOrDefaultAsync(
            item => item.Id == userId,
            cancellationToken);
        if (user is null)
            return null;

        user.InAppNotificationsEnabled = request.InAppEnabled;
        user.LowStockNotificationsEnabled = request.LowStockEnabled;
        user.ReportReadyNotificationsEnabled = request.ReportReadyEnabled;
        user.SystemNotificationsEnabled = request.SystemEnabled;
        user.NotificationSoundEnabled = request.SoundEnabled;
        user.NotificationPollingIntervalSeconds = request.PollingIntervalSeconds;
        await db.SaveChangesAsync(cancellationToken);

        return new NotificationPreferencesResponse(
            user.InAppNotificationsEnabled,
            user.LowStockNotificationsEnabled,
            user.ReportReadyNotificationsEnabled,
            user.SystemNotificationsEnabled,
            user.NotificationSoundEnabled,
            user.NotificationPollingIntervalSeconds);
    }

    public async Task<NotificationPageResponse> GetAllAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagination = Pagination.Normalize(page, pageSize);
        var preferences = await GetPreferencesAsync(userId, cancellationToken);
        if (preferences is null || !preferences.InAppEnabled)
        {
            return new NotificationPageResponse(
                [],
                pagination.Page,
                pagination.PageSize,
                0,
                0);
        }

        var query = db.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .Where(notification =>
                (notification.Type == NotificationType.LowStock && preferences.LowStockEnabled) ||
                (notification.Type == NotificationType.ReportReady && preferences.ReportReadyEnabled) ||
                (notification.Type == NotificationType.System && preferences.SystemEnabled));
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

    public async Task MarkAllReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var preferences = await GetPreferencesAsync(userId, cancellationToken);
        if (preferences is null || !preferences.InAppEnabled)
            return;

        await db.Notifications
            .Where(notification => notification.UserId == userId && !notification.IsRead)
            .Where(notification =>
                (notification.Type == NotificationType.LowStock && preferences.LowStockEnabled) ||
                (notification.Type == NotificationType.ReportReady && preferences.ReportReadyEnabled) ||
                (notification.Type == NotificationType.System && preferences.SystemEnabled))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(notification => notification.IsRead, true)
                    .SetProperty(notification => notification.ReadAt, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task CreateLowStockNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var recipients = await db.UsersSet
            .AsNoTracking()
            .Where(user => user.IsActive &&
                user.InAppNotificationsEnabled &&
                user.LowStockNotificationsEnabled &&
                (user.Role.Name == "Admin" || user.Role.Name == "Manager"))
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);
        if (recipients.Count == 0)
            return;

        var globalThreshold = await db.InventorySettingsSet
            .AsNoTracking()
            .Where(settings => settings.Id == InventorySettings.DefaultId)
            .Select(settings => settings.GlobalLowStockThreshold)
            .SingleOrDefaultAsync(cancellationToken);
        var products = await db.ProductsSet
            .AsNoTracking()
            .Where(product => product.IsActive &&
                product.StockOnHand <= (product.ReorderLevel >= globalThreshold ? product.ReorderLevel : globalThreshold))
            .Select(product => new
            {
                product.Id,
                product.Sku,
                product.Name,
                product.StockOnHand,
                Threshold = product.ReorderLevel >= globalThreshold ? product.ReorderLevel : globalThreshold,
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
                var threshold = product.Threshold.ToString("0.##", CultureInfo.InvariantCulture);
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
