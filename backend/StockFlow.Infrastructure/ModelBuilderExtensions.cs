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
        ConfigureDataRules(modelBuilder);
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
        modelBuilder.Entity<Role>().HasIndex(entity => entity.Name).IsUnique();
        modelBuilder.Entity<Category>().HasIndex(entity => entity.Name).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(entity => entity.Sku).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(entity => new { entity.CategoryId, entity.IsActive });
        modelBuilder.Entity<Supplier>().HasIndex(entity => entity.Code).IsUnique();
        modelBuilder.Entity<Customer>().HasIndex(entity => entity.Code).IsUnique();
        modelBuilder.Entity<User>().HasIndex(entity => entity.Email).IsUnique();
        modelBuilder.Entity<PasswordResetToken>().HasIndex(entity => entity.TokenHash).IsUnique();
        modelBuilder.Entity<PasswordResetToken>().HasIndex(entity => new { entity.UserId, entity.ExpiresAt });
        modelBuilder.Entity<PurchaseOrder>().HasIndex(entity => new { entity.Status, entity.OrderDate });
        modelBuilder.Entity<PurchaseOrder>().HasIndex(entity => entity.Number).IsUnique();
        modelBuilder.Entity<SalesOrder>().HasIndex(entity => new { entity.Status, entity.OrderDate });
        modelBuilder.Entity<SalesOrder>().HasIndex(entity => entity.Number).IsUnique();
        modelBuilder.Entity<GoodsReceipt>().HasIndex(entity => entity.Number).IsUnique();
        modelBuilder.Entity<StockMovement>().HasIndex(entity => new { entity.ProductId, entity.CreatedAt });
        modelBuilder.Entity<StockAdjustment>().HasIndex(entity => entity.Number).IsUnique();
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

    private static void ConfigureDataRules(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().Property(entity => entity.Name).HasMaxLength(64);

        modelBuilder.Entity<User>().Property(entity => entity.Email).HasMaxLength(254);
        modelBuilder.Entity<User>().Property(entity => entity.FullName).HasMaxLength(160);
        modelBuilder.Entity<User>().Property(entity => entity.PasswordHash).HasMaxLength(255);

        modelBuilder.Entity<PasswordResetToken>().Property(entity => entity.TokenHash).HasMaxLength(128);

        modelBuilder.Entity<Notification>().Property(entity => entity.Title).HasMaxLength(160);
        modelBuilder.Entity<Notification>().Property(entity => entity.Message).HasMaxLength(1000);

        modelBuilder.Entity<Category>().Property(entity => entity.Name).HasMaxLength(160);
        modelBuilder.Entity<Category>().Property(entity => entity.Description).HasMaxLength(500);

        modelBuilder.Entity<Product>().Property(entity => entity.Sku).HasMaxLength(80);
        modelBuilder.Entity<Product>().Property(entity => entity.Name).HasMaxLength(160);
        modelBuilder.Entity<Product>().Property(entity => entity.Unit).HasMaxLength(24);

        ConfigurePartyLengths(modelBuilder.Entity<Supplier>());
        ConfigurePartyLengths(modelBuilder.Entity<Customer>());

        modelBuilder.Entity<PurchaseOrder>().Property(entity => entity.Number).HasMaxLength(50);
        modelBuilder.Entity<PurchaseOrder>().Property(entity => entity.Notes).HasMaxLength(500);
        modelBuilder.Entity<GoodsReceipt>().Property(entity => entity.Number).HasMaxLength(50);
        modelBuilder.Entity<SalesOrder>().Property(entity => entity.Number).HasMaxLength(50);
        modelBuilder.Entity<SalesOrder>().Property(entity => entity.Notes).HasMaxLength(500);
        modelBuilder.Entity<StockMovement>().Property(entity => entity.ReferenceNumber).HasMaxLength(50);
        modelBuilder.Entity<StockMovement>().Property(entity => entity.Reason).HasMaxLength(500);
        modelBuilder.Entity<StockAdjustment>().Property(entity => entity.Number).HasMaxLength(50);
        modelBuilder.Entity<StockAdjustment>().Property(entity => entity.Reason).HasMaxLength(300);

        modelBuilder.Entity<ReportExportJob>().Property(entity => entity.JobNumber).HasMaxLength(50);
        modelBuilder.Entity<ReportExportJob>().Property(entity => entity.ReportType).HasMaxLength(80);
        modelBuilder.Entity<ReportExportJob>().Property(entity => entity.Parameters).HasMaxLength(4000);
        modelBuilder.Entity<ReportExportJob>().Property(entity => entity.Format).HasMaxLength(16);
        modelBuilder.Entity<ReportExportJob>().Property(entity => entity.FilePath).HasMaxLength(1000);
        modelBuilder.Entity<ReportExportJob>().Property(entity => entity.ErrorMessage).HasMaxLength(1000);

        modelBuilder.Entity<Product>().ToTable(table =>
        {
            table.HasCheckConstraint("ck_products_non_negative_prices", "purchase_price >= 0 AND selling_price >= 0");
            table.HasCheckConstraint("ck_products_non_negative_stock", "stock_on_hand >= 0 AND reorder_level >= 0");
        });
        modelBuilder.Entity<PurchaseOrderItem>().ToTable(table =>
            table.HasCheckConstraint("ck_purchase_order_items_values", "quantity > 0 AND unit_price >= 0"));
        modelBuilder.Entity<SalesOrderItem>().ToTable(table =>
            table.HasCheckConstraint("ck_sales_order_items_values", "quantity > 0 AND unit_price >= 0"));
        modelBuilder.Entity<GoodsReceiptItem>().ToTable(table =>
            table.HasCheckConstraint("ck_goods_receipt_items_quantity", "quantity > 0"));
        modelBuilder.Entity<StockMovement>().ToTable(table =>
            table.HasCheckConstraint("ck_stock_movements_values", "quantity > 0 AND balance_after >= 0"));
        modelBuilder.Entity<StockAdjustment>().ToTable(table =>
            table.HasCheckConstraint("ck_stock_adjustments_quantity", "quantity_delta <> 0"));
        modelBuilder.Entity<ReportExportJob>().ToTable(table =>
            table.HasCheckConstraint("ck_report_export_jobs_progress", "progress >= 0 AND progress <= 100"));
    }

    private static void ConfigurePartyLengths<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : ActivatableEntity
    {
        entity.Property("Code").HasMaxLength(80);
        entity.Property("Name").HasMaxLength(160);
        entity.Property("Email").HasMaxLength(254);
        entity.Property("Phone").HasMaxLength(40);
        entity.Property("Address").HasMaxLength(300);
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
