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
        Assert.Equal(16, permissions.Count(permission => permission.Kind == PermissionKind.Menu));
        Assert.Equal(13, permissions.Count(permission => permission.Kind == PermissionKind.Action));
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
        Assert.DoesNotContain("action.users.manage", manager);
        Assert.Contains("menu.purchase-orders", staff);
        Assert.DoesNotContain("action.purchasing.manage", staff);
        Assert.Empty(PermissionCatalog.DefaultsFor("Custom"));
    }
}
