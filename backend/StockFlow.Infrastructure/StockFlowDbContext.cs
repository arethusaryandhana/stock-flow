using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Core;

namespace StockFlow.Infrastructure;

public sealed class StockFlowDbContext(
    DbContextOptions<StockFlowDbContext> options,
    ICurrentUserService? currentUser = null) : DbContext(options)
{
    public static class Schemas
    {
        public const string Identity = "identity";
        public const string Master = "master";
        public const string Purchasing = "purchasing";
        public const string Sales = "sales";
        public const string Inventory = "inventory";
        public const string Reporting = "reporting";
    }

    public DbSet<Product> ProductsSet => Set<Product>();
    public DbSet<Category> CategoriesSet => Set<Category>();
    public DbSet<Supplier> SuppliersSet => Set<Supplier>();
    public DbSet<Customer> CustomersSet => Set<Customer>();
    public DbSet<User> UsersSet => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ReportExportJob> ReportExportJobs => Set<ReportExportJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ConfigureStockFlowModel();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditFields();
        AddAuditLogs();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        AddAuditLogs();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditFields()
    {
        var userId = currentUser?.UserId;

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedById ??= userId;
                entry.Entity.UpdatedById = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(entity => entity.CreatedAt).IsModified = false;
                entry.Property(entity => entity.CreatedById).IsModified = false;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedById = userId;
            }
        }
    }

    private void AddAuditLogs()
    {
        var actorId = currentUser?.UserId;
        var now = DateTime.UtcNow;
        var entries = ChangeTracker.Entries<Entity>()
            .Where(entry => IsAuditable(entry.Entity) &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted => "Deleted",
                _ => throw new InvalidOperationException("Unsupported audit action.")
            };
            var changes = entry.Properties
                .Where(property => ShouldAuditProperty(property.Metadata.Name, entry.State, property.IsModified))
                .ToDictionary(
                    property => property.Metadata.Name,
                    property => new AuditValue(
                        entry.State == EntityState.Added ? null : NormalizeAuditValue(property.OriginalValue),
                        entry.State == EntityState.Deleted ? null : NormalizeAuditValue(property.CurrentValue)));

            if (entry.State == EntityState.Modified && changes.Count == 0)
                continue;

            AuditLogs.Add(new AuditLog
            {
                ActorId = actorId,
                Action = action,
                EntityType = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id,
                Summary = GetAuditSummary(entry.Entity),
                Changes = JsonSerializer.Serialize(changes),
                CreatedById = actorId,
                CreatedAt = now
            });
        }
    }

    private static bool IsAuditable(Entity entity) => entity is
        Product or Category or Supplier or Customer or PurchaseOrder or GoodsReceipt or
        SalesOrder or StockAdjustment;

    private static bool ShouldAuditProperty(
        string propertyName,
        EntityState state,
        bool isModified)
    {
        if (propertyName is nameof(Entity.CreatedAt) or nameof(Entity.CreatedById) or
            nameof(Entity.UpdatedAt) or nameof(Entity.UpdatedById))
        {
            return false;
        }

        return state != EntityState.Modified || isModified;
    }

    private static object? NormalizeAuditValue(object? value) => value switch
    {
        null => null,
        DateTime dateTime => dateTime.ToUniversalTime().ToString("O"),
        Enum enumValue => enumValue.ToString(),
        _ => value
    };

    private static string GetAuditSummary(Entity entity) => entity switch
    {
        Product product => $"{product.Sku} - {product.Name}",
        Category category => category.Name,
        Supplier supplier => $"{supplier.Code} - {supplier.Name}",
        Customer customer => $"{customer.Code} - {customer.Name}",
        PurchaseOrder order => order.Number,
        GoodsReceipt receipt => receipt.Number,
        SalesOrder order => order.Number,
        StockAdjustment adjustment => adjustment.Number,
        _ => entity.Id.ToString()
    };

    private sealed record AuditValue(object? Before, object? After);
}
