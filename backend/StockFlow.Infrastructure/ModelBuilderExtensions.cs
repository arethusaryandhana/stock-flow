using Microsoft.EntityFrameworkCore;
using StockFlow.Core;

namespace StockFlow.Infrastructure;

internal static class ModelBuilderExtensions
{
    public static void ConfigureStockFlowModel(this ModelBuilder modelBuilder)
    {
        ConfigureTables(modelBuilder);
        ApplySnakeCaseNaming(modelBuilder);
        ConfigureIndexes(modelBuilder);
        ConfigureRelationships(modelBuilder);
        ConfigureDecimalPrecision(modelBuilder);
    }

    private static void ConfigureTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().ToTable("roles", StockFlowDbContext.Schemas.Identity);
        modelBuilder.Entity<User>().ToTable("users", StockFlowDbContext.Schemas.Identity);
        modelBuilder.Entity<PasswordResetToken>().ToTable("password_reset_tokens", StockFlowDbContext.Schemas.Identity);
        modelBuilder.Entity<Notification>().ToTable("notifications", StockFlowDbContext.Schemas.Identity);

        modelBuilder.Entity<Category>().ToTable("categories_set", StockFlowDbContext.Schemas.Master);
        modelBuilder.Entity<Product>().ToTable("products_set", StockFlowDbContext.Schemas.Master);
        modelBuilder.Entity<Supplier>().ToTable("suppliers_set", StockFlowDbContext.Schemas.Master);
        modelBuilder.Entity<Customer>().ToTable("customers_set", StockFlowDbContext.Schemas.Master);

        modelBuilder.Entity<PurchaseOrder>().ToTable("purchase_orders", StockFlowDbContext.Schemas.Purchasing);
        modelBuilder.Entity<PurchaseOrderItem>().ToTable("purchase_order_items", StockFlowDbContext.Schemas.Purchasing);
        modelBuilder.Entity<GoodsReceipt>().ToTable("goods_receipts", StockFlowDbContext.Schemas.Purchasing);
        modelBuilder.Entity<GoodsReceiptItem>().ToTable("goods_receipt_items", StockFlowDbContext.Schemas.Purchasing);

        modelBuilder.Entity<SalesOrder>().ToTable("sales_orders", StockFlowDbContext.Schemas.Sales);
        modelBuilder.Entity<SalesOrderItem>().ToTable("sales_order_items", StockFlowDbContext.Schemas.Sales);

        modelBuilder.Entity<StockMovement>().ToTable("stock_movements", StockFlowDbContext.Schemas.Inventory);
        modelBuilder.Entity<StockAdjustment>().ToTable("stock_adjustments", StockFlowDbContext.Schemas.Inventory);

        modelBuilder.Entity<ReportExportJob>().ToTable("report_export_jobs", StockFlowDbContext.Schemas.Reporting);
    }

    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasIndex(entity => entity.Name).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(entity => entity.Sku).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(entity => new { entity.CategoryId, entity.IsActive });
        modelBuilder.Entity<Supplier>().HasIndex(entity => entity.Code).IsUnique();
        modelBuilder.Entity<Customer>().HasIndex(entity => entity.Code).IsUnique();
        modelBuilder.Entity<User>().HasIndex(entity => entity.Email).IsUnique();
        modelBuilder.Entity<PasswordResetToken>().HasIndex(entity => entity.TokenHash).IsUnique();
        modelBuilder.Entity<PasswordResetToken>().HasIndex(entity => new { entity.UserId, entity.ExpiresAt });
        modelBuilder.Entity<PurchaseOrder>().HasIndex(entity => new { entity.Status, entity.OrderDate });
        modelBuilder.Entity<SalesOrder>().HasIndex(entity => new { entity.Status, entity.OrderDate });
        modelBuilder.Entity<StockMovement>().HasIndex(entity => new { entity.ProductId, entity.CreatedAt });
        modelBuilder.Entity<ReportExportJob>().HasIndex(entity => new { entity.Status, entity.RequestedAt });
        modelBuilder.Entity<ReportExportJob>().HasIndex(entity => entity.JobNumber).IsUnique();
    }

    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrderItem>()
            .HasOne(entity => entity.PurchaseOrder)
            .WithMany(order => order.Items)
            .HasForeignKey(entity => entity.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SalesOrderItem>()
            .HasOne(entity => entity.SalesOrder)
            .WithMany(order => order.Items)
            .HasForeignKey(entity => entity.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GoodsReceiptItem>()
            .HasOne(entity => entity.GoodsReceipt)
            .WithMany(receipt => receipt.Items)
            .HasForeignKey(entity => entity.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Product>()
            .HasOne(entity => entity.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(entity => entity.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(entity => entity.Supplier)
            .WithMany()
            .HasForeignKey(entity => entity.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SalesOrder>()
            .HasOne(entity => entity.Customer)
            .WithMany()
            .HasForeignKey(entity => entity.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GoodsReceipt>()
            .HasOne(entity => entity.PurchaseOrder)
            .WithMany()
            .HasForeignKey(entity => entity.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne(entity => entity.Product)
            .WithMany()
            .HasForeignKey(entity => entity.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockAdjustment>()
            .HasOne(entity => entity.Product)
            .WithMany()
            .HasForeignKey(entity => entity.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PasswordResetToken>()
            .HasOne(entity => entity.User)
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
    {
        var entityTypes = new[]
        {
            typeof(Product),
            typeof(PurchaseOrderItem),
            typeof(SalesOrderItem),
            typeof(StockMovement),
            typeof(StockAdjustment)
        };

        foreach (var entityType in entityTypes)
        {
            var decimalProperties = modelBuilder.Entity(entityType)
                .Metadata
                .GetProperties()
                .Where(property => property.ClrType == typeof(decimal));

            foreach (var property in decimalProperties)
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }
        }
    }

    private static void ApplySnakeCaseNaming(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            entityType.SetTableName(ToSnakeCase(entityType.GetTableName()!));

            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(property.Name switch
                {
                    nameof(Entity.CreatedById) => "created_by",
                    nameof(Entity.UpdatedById) => "updated_by",
                    _ => ToSnakeCase(property.Name)
                });
            }
        }
    }

    private static string ToSnakeCase(string value) => string.Concat(
        value.Select((character, index) =>
            char.IsUpper(character) && index > 0
                ? "_" + char.ToLowerInvariant(character)
                : char.ToLowerInvariant(character).ToString()));
}
