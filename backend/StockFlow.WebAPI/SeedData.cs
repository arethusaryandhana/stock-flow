using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Core;
using StockFlow.Infrastructure;
public static class SeedData
{
    public static async Task Run(
        StockFlowDbContext db,
        IPasswordService passwords,
        bool seedDemo)
    {
        var roles = await db.Roles.ToDictionaryAsync(role => role.Name);
        foreach (var roleName in new[] { "Admin", "Manager", "Staff" })
        {
            if (roles.ContainsKey(roleName))
                continue;

            var role = new Role { Name = roleName };
            roles.Add(roleName, role);
            db.Roles.Add(role);
        }

        await db.SaveChangesAsync();

        if (!seedDemo)
            return;

        if (!await db.UsersSet.AnyAsync(user => user.Email == "admin@stockflow.local"))
        {
            db.UsersSet.Add(new User
            {
                Email = "admin@stockflow.local",
                FullName = "Demo Administrator",
                PasswordHash = passwords.Hash("StockFlow123!"),
                Role = roles["Admin"]
            });
        }

        var category = await db.CategoriesSet.SingleOrDefaultAsync(item => item.Name == "General");
        if (category is null)
        {
            category = new Category { Name = "General", Description = "Produk umum" };
            db.CategoriesSet.Add(category);
        }

        if (!await db.ProductsSet.AnyAsync(product => product.Sku == "SKU-001"))
        {
            db.ProductsSet.Add(new Product
            {
                Sku = "SKU-001", Name = "Sample Product", Category = category,
                PurchasePrice = 50000, SellingPrice = 75000, StockOnHand = 12, ReorderLevel = 5
            });
        }

        if (!await db.ProductsSet.AnyAsync(product => product.Sku == "SKU-002"))
        {
            db.ProductsSet.Add(new Product
            {
                Sku = "SKU-002", Name = "Low Stock Item", Category = category,
                PurchasePrice = 25000, SellingPrice = 40000, StockOnHand = 2, ReorderLevel = 5
            });
        }

        await db.SaveChangesAsync();
    }
}
