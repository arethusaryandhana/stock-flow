using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Core;

namespace StockFlow.WebAPI;

public static class PermissionPolicies
{
    public const string CategoriesRead = "read.categories";
    public const string ProductsRead = "read.products";
    public const string MovementsRead = "read.movements";
    public const string SuppliersRead = "read.suppliers";
    public const string CustomersRead = "read.customers";
    public const string PurchaseOrdersRead = "read.purchase-orders";
    public const string GoodsReceiptsRead = "read.receiving";
    public const string RolesRead = "read.roles";

    private static readonly IReadOnlyDictionary<string, string[]> ReadAlternatives =
        new Dictionary<string, string[]>
        {
            [CategoriesRead] = ["menu.master.categories", "menu.master.products", "menu.products", "menu.purchase-orders", "menu.sales-orders", "menu.adjustments"],
            [ProductsRead] = ["menu.dashboard", "menu.master.products", "menu.products", "menu.purchase-orders", "menu.sales-orders", "menu.receiving", "menu.adjustments"],
            [MovementsRead] = ["menu.dashboard", "menu.movements"],
            [SuppliersRead] = ["menu.master.suppliers", "menu.suppliers", "menu.purchase-orders", "menu.receiving"],
            [CustomersRead] = ["menu.master.customers", "menu.sales-orders"],
            [PurchaseOrdersRead] = ["menu.purchase-orders", "menu.receiving"],
            [GoodsReceiptsRead] = ["menu.receiving", "menu.purchase-orders"],
            [RolesRead] = ["menu.roles", "menu.access"]
        };

    public static void Register(AuthorizationOptions options)
    {
        foreach (var permission in PermissionCatalog.All)
            options.AddPolicy(permission.Code, policy => policy.RequireAuthenticatedUser()
                .AddRequirements(new AnyPermissionRequirement([permission.Code])));

        foreach (var (name, codes) in ReadAlternatives)
            options.AddPolicy(name, policy => policy.RequireAuthenticatedUser()
                .AddRequirements(new AnyPermissionRequirement(codes)));
    }

    public static IReadOnlyDictionary<string, string[]> ReadPolicies => ReadAlternatives;
}

public sealed record AnyPermissionRequirement(IReadOnlyCollection<string> Codes) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler(IUserAccessReader access)
    : AuthorizationHandler<AnyPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AnyPermissionRequirement requirement)
    {
        var rawUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(rawUserId, out var userId))
            return;

        var snapshot = await access.GetAsync(userId);
        if (snapshot is not null && requirement.Codes.Any(snapshot.Permissions.Contains))
            context.Succeed(requirement);
    }
}
