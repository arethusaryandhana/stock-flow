using Microsoft.EntityFrameworkCore;
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
    public DbSet<ReportExportJob> ReportExportJobs => Set<ReportExportJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ConfigureStockFlowModel();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
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
}
