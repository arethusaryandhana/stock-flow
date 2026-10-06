using StockFlow.WebAPI;

namespace StockFlow.Tests;

public sealed class PermissionDelegationPolicyTests
{
    [Fact]
    public void CanDelegate_allows_a_subset_of_the_actor_permissions()
    {
        var result = PermissionDelegationPolicy.CanDelegate(
            ["menu.users", "action.users.manage", "menu.dashboard"],
            ["menu.users", "menu.dashboard"]);

        Assert.True(result);
    }

    [Fact]
    public void CanDelegate_rejects_permissions_the_actor_does_not_have()
    {
        var result = PermissionDelegationPolicy.CanDelegate(
            ["menu.users", "action.users.manage"],
            ["menu.users", "action.access.manage"]);

        Assert.False(result);
    }

    [Fact]
    public void CanDelegate_allows_an_empty_permission_set()
    {
        Assert.True(PermissionDelegationPolicy.CanDelegate(["menu.users"], []));
    }
}
