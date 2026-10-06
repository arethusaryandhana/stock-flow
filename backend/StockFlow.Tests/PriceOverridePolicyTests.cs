using StockFlow.Core;

namespace StockFlow.Tests;

public sealed class PriceOverridePolicyTests
{
    [Fact]
    public void Standard_master_price_does_not_need_an_override_permission()
    {
        Assert.True(PriceOverridePolicy.IsAllowed(null, PermissionCatalog.SalesPriceOverride, 100, 100));
    }

    [Fact]
    public void Different_price_is_rejected_without_the_matching_permission()
    {
        Assert.False(PriceOverridePolicy.IsAllowed(
            new HashSet<string> { "action.sales.manage" },
            PermissionCatalog.SalesPriceOverride,
            100,
            95));
    }

    [Fact]
    public void Different_price_is_allowed_with_the_matching_permission()
    {
        Assert.True(PriceOverridePolicy.IsAllowed(
            new HashSet<string> { PermissionCatalog.SalesPriceOverride },
            PermissionCatalog.SalesPriceOverride,
            100,
            95));
    }

    [Fact]
    public void Sales_permission_does_not_authorize_purchase_price_override()
    {
        Assert.False(PriceOverridePolicy.IsAllowed(
            new HashSet<string> { PermissionCatalog.SalesPriceOverride },
            PermissionCatalog.PurchasingPriceOverride,
            100,
            95));
    }
}
