using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.UseCases;
using StockFlow.Core;
using StockFlow.Infrastructure.Repositories;

namespace StockFlow.Infrastructure;

public sealed class StockFlowDbContext(DbContextOptions<StockFlowDbContext> options) : DbContext(options)
{
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
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("stockflow");
        foreach (var t in b.Model.GetEntityTypes())
        {
            t.SetTableName(ToSnake(t.GetTableName()!));
            foreach (var p in t.GetProperties())
                p.SetColumnName(ToSnake(p.Name));
        }
        b.Entity<Category>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        b.Entity<Product>().HasIndex(x => new { x.CategoryId, x.IsActive });
        b.Entity<Supplier>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Customer>().HasIndex(x => x.Code).IsUnique();
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<PasswordResetToken>().HasIndex(x => x.TokenHash).IsUnique();
        b.Entity<PasswordResetToken>().HasIndex(x => new { x.UserId, x.ExpiresAt });
        b.Entity<PurchaseOrder>().HasIndex(x => new { x.Status, x.OrderDate });
        b.Entity<SalesOrder>().HasIndex(x => new { x.Status, x.OrderDate });
        b.Entity<StockMovement>().HasIndex(x => new { x.ProductId, x.CreatedAt });
        b.Entity<ReportExportJob>().HasIndex(x => new { x.Status, x.RequestedAt });
        b.Entity<ReportExportJob>().HasIndex(x => x.JobNumber).IsUnique();
        b.Entity<PurchaseOrderItem>().HasOne(x => x.PurchaseOrder).WithMany(x => x.Items).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SalesOrderItem>().HasOne(x => x.SalesOrder).WithMany(x => x.Items).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<GoodsReceiptItem>().HasOne(x => x.GoodsReceipt).WithMany(x => x.Items).HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Product>().HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PurchaseOrder>().HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SalesOrder>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GoodsReceipt>().HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StockMovement>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StockAdjustment>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PasswordResetToken>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        foreach (var e in new[] { typeof(Product), typeof(PurchaseOrderItem), typeof(SalesOrderItem) })
            foreach (var p in b.Entity(e).Metadata.GetProperties().Where(p => p.ClrType == typeof(decimal)))
            {
                p.SetPrecision(18);
                p.SetScale(2);
            }
    }
    private static string ToSnake(string s) => string.Concat(s.Select((c, i) => char.IsUpper(c) && i > 0 ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
public sealed class PasswordService : IPasswordService
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password); public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
public sealed class PasswordResetTokenService : IPasswordResetTokenService
{
    public string Generate() => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
        .Replace("+", "-")
        .Replace("/", "_")
        .TrimEnd('=');

    public string Hash(string token) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
public sealed class TokenService(IConfiguration config) : ITokenService
{
    public string Create(User u)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString()), new Claim(JwtRegisteredClaimNames.Email, u.Email), new Claim(ClaimTypes.Name, u.FullName), new Claim(ClaimTypes.Role, u.Role.Name) };
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer: config["Jwt:Issuer"], audience: config["Jwt:Audience"], claims: claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
    }
}
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<StockFlowDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Database")));

        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();
        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IReportExportRepository, ReportExportRepository>();

        services.AddScoped<IAuthUseCase, AuthUseCase>();
        services.AddScoped<IDashboardUseCase, DashboardUseCase>();
        services.AddScoped<IProductUseCase, ProductUseCase>();
        services.AddScoped<IInventoryUseCase, InventoryUseCase>();
        services.AddScoped<ICategoryUseCase, CategoryUseCase>();
        services.AddScoped<ISupplierUseCase, SupplierUseCase>();
        services.AddScoped<ICustomerUseCase, CustomerUseCase>();
        services.AddScoped<IReportExportUseCase, ReportExportUseCase>();

        return services;
    }
}
