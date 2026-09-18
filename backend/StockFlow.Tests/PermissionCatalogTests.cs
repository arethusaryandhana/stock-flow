using StockFlow.Core;

namespace StockFlow.Tests;

public sealed class PermissionCatalogTests
{
    [Fact]
    public void CatalogueHasUniqueCodesAndCoversEveryBuiltInMenu()
    {
        var permissions = PermissionCatalog.All;

        Assert.Equal(permissions.Count, permissions.Select(permission => permission.Code).Distinct().Count());
        Assert.All(permissions, permission =>
        {
            Assert.False(string.IsNullOrWhiteSpace(permission.Group));
            Assert.False(string.IsNullOrWhiteSpace(permission.Name));
            Assert.True(permission.Code.Length <= 96);
        });
        Assert.Equal(18, permissions.Count(permission => permission.Kind == PermissionKind.Menu));
        Assert.Equal(15, permissions.Count(permission => permission.Kind == PermissionKind.Action));
    }

    [Fact]
    public void BuiltInDefaultsPreserveCurrentAccessLevels()
    {
        var admin = PermissionCatalog.DefaultsFor("Admin");
        var manager = PermissionCatalog.DefaultsFor("Manager");
        var staff = PermissionCatalog.DefaultsFor("Staff");

        Assert.Equal(PermissionCatalog.All.Count, admin.Count);
        Assert.Contains("menu.users", admin);
        Assert.Contains("action.users.manage", admin);
        Assert.Contains("action.purchasing.manage", manager);
        Assert.Contains("action.receiving.manage", manager);
        Assert.Contains("action.sales.manage", manager);
        Assert.Contains("action.inventory.adjust", manager);
        Assert.DoesNotContain("action.users.manage", manager);
        Assert.DoesNotContain("menu.master.products", manager);
        Assert.Contains("menu.purchase-orders", staff);
        Assert.Contains("menu.sales-orders", staff);
        Assert.Contains("menu.reports", staff);
        Assert.DoesNotContain("action.purchasing.manage", staff);
        Assert.DoesNotContain("action.inventory.adjust", staff);
        Assert.DoesNotContain("menu.users", staff);
        Assert.Empty(PermissionCatalog.DefaultsFor("Custom"));
    }

    [Fact]
    public void EveryActionRequiresAnExistingMenuAndBuiltInDefaultsIncludeIt()
    {
        var codes = PermissionCatalog.All.Select(permission => permission.Code).ToHashSet();
        var actions = PermissionCatalog.All.Where(permission => permission.Kind == PermissionKind.Action);

        foreach (var action in actions)
        {
            Assert.True(PermissionCatalog.RequiredMenuForAction.TryGetValue(action.Code, out var menu));
            Assert.Contains(menu!, codes);
            foreach (var role in new[] { "Admin", "Manager", "Staff" })
            {
                var defaults = PermissionCatalog.DefaultsFor(role);
                if (defaults.Contains(action.Code)) Assert.Contains(menu!, defaults);
            }
        }
    }
}
