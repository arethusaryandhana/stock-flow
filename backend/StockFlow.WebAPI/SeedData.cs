using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Core;
using StockFlow.Infrastructure;
public static class SeedData
{
    public static async Task Run(StockFlowDbContext db, IPasswordService passwords)
    {
        if (await db.Roles.AnyAsync())
            return;
        var admin = new Role { Name = "Admin" };
        var manager = new Role { Name = "Manager" };
        var staff = new Role { Name = "Staff" };
        db.AddRange(admin, manager, staff);
        db.Add(new User { Email = "admin@stockflow.local", FullName = "Demo Administrator", PasswordHash = passwords.Hash("StockFlow123!"), Role = admin });
        var cat = new Category { Name = "General", Description = "Produk umum" };
        db.Add(cat);
        db.AddRange(new Product { Sku = "SKU-001", Name = "Sample Product", Category = cat, PurchasePrice = 50000, SellingPrice = 75000, StockOnHand = 12, ReorderLevel = 5 }, new Product { Sku = "SKU-002", Name = "Low Stock Item", Category = cat, PurchasePrice = 25000, SellingPrice = 40000, StockOnHand = 2, ReorderLevel = 5 });
        await db.SaveChangesAsync();
    }
}
