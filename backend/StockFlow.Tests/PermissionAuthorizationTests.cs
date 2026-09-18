using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Core;
using StockFlow.WebAPI;

namespace StockFlow.Tests;

public sealed class PermissionAuthorizationTests
{
    [Fact]
    public void SharedReadPoliciesOnlyReferenceCataloguePermissions()
    {
        var knownCodes = PermissionCatalog.All.Select(permission => permission.Code).ToHashSet();

        Assert.All(PermissionPolicies.ReadPolicies.Values.SelectMany(codes => codes),
            code => Assert.Contains(code, knownCodes));
        Assert.Contains("menu.dashboard", PermissionPolicies.ReadPolicies[PermissionPolicies.ProductsRead]);
        Assert.Contains("menu.dashboard", PermissionPolicies.ReadPolicies[PermissionPolicies.MovementsRead]);
    }

    [Fact]
    public async Task HandlerGrantsOnlyAssignedPermissions()
    {
        var userId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
        var handler = new PermissionAuthorizationHandler(new StubAccessReader(
            new UserAccessSnapshot("Manager", new HashSet<string> { "menu.purchase-orders" })));

        var allowed = new AnyPermissionRequirement(["menu.purchase-orders", "menu.receiving"]);
        var denied = new AnyPermissionRequirement(["action.purchasing.manage"]);
        var context = new AuthorizationHandlerContext([allowed, denied], principal, null);

        await handler.HandleAsync(context);

        Assert.DoesNotContain(allowed, context.PendingRequirements);
        Assert.Contains(denied, context.PendingRequirements);
    }

    [Fact]
    public async Task HandlerFailsClosedWhenRoleHasNoPermissions()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"));
        var requirement = new AnyPermissionRequirement(["menu.users"]);
        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await new PermissionAuthorizationHandler(new StubAccessReader(null)).HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private sealed class StubAccessReader(UserAccessSnapshot? snapshot) : IUserAccessReader
    {
        public Task<UserAccessSnapshot?> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshot);
    }
}
