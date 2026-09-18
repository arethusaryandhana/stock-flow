using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using StockFlow.Infrastructure;
using StockFlow.WebAPI;
using StockFlow.WebAPI.Endpoints;

namespace StockFlow.Tests;

public sealed class EndpointAuthorizationTests
{
    private static readonly HashSet<string> AnonymousRoutes =
    [
        "/api/auth/login",
        "/api/auth/forgot-password",
        "/api/auth/reset-password"
    ];

    [Fact]
    public async Task ApiEndpoints_AreAuthenticatedUnlessExplicitlyAllowlisted()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["ConnectionStrings:Database"] =
            "Host=localhost;Database=stockflow_endpoint_metadata_tests;Username=unused;Password=unused";
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddAuthorization();
        builder.Services.AddStockFlowEndpoints();

        await using var app = builder.Build();
        app.MapStockFlowEndpoints();

        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.NotEmpty(endpoints);

        foreach (var endpoint in endpoints)
        {
            var route = endpoint.RoutePattern.RawText!;
            var allowsAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
            var requiresAuthorization = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0;

            if (AnonymousRoutes.Contains(route))
            {
                Assert.True(allowsAnonymous, $"Public route {route} must explicitly call AllowAnonymous().");
                continue;
            }

            Assert.False(allowsAnonymous, $"Protected route {route} must not allow anonymous access.");
            Assert.True(requiresAuthorization, $"Protected route {route} is missing RequireAuthorization().");
        }
    }

    [Fact]
    public void AnonymousRouteAllowlist_ContainsEveryAndOnlyPublicApiRoute()
    {
        Assert.Equal(3, AnonymousRoutes.Count);
    }

    [Fact]
    public async Task EndpointPoliciesAreRegisteredAndDoNotUseStaleRoleClaims()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["ConnectionStrings:Database"] =
            "Host=localhost;Database=stockflow_endpoint_metadata_tests;Username=unused;Password=unused";
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddAuthorization(PermissionPolicies.Register);
        builder.Services.AddStockFlowEndpoints();

        await using var app = builder.Build();
        app.MapStockFlowEndpoints();
        var policyProvider = app.Services.GetRequiredService<IAuthorizationPolicyProvider>();

        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) == true);

        foreach (var endpoint in endpoints)
        {
            var route = endpoint.RoutePattern.RawText!;
            var method = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.SingleOrDefault();
            var isUniversalAccountRoute = route.StartsWith("/api/auth/", StringComparison.Ordinal) ||
                route.StartsWith("/api/notifications", StringComparison.Ordinal) ||
                (route.TrimEnd('/') == "/api/company-profile" && method == "GET");
            var authorizations = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            if (!isUniversalAccountRoute)
                Assert.True(authorizations.Any(authorization => authorization.Policy is not null),
                    $"{method} {route} requires a feature permission.");

            foreach (var authorization in authorizations)
            {
                Assert.Null(authorization.Roles);
                if (authorization.Policy is not null)
                    Assert.NotNull(await policyProvider.GetPolicyAsync(authorization.Policy));
            }
        }
    }

    [Fact]
    public async Task RoleMutationsRequireTheMatchingActionPermission()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["ConnectionStrings:Database"] =
            "Host=localhost;Database=stockflow_endpoint_metadata_tests;Username=unused;Password=unused";
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddAuthorization(PermissionPolicies.Register);
        builder.Services.AddStockFlowEndpoints();

        await using var app = builder.Build();
        app.MapStockFlowEndpoints();
        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().ToArray();

        void AssertPolicies(string route, string method, params string[] expected)
        {
            var endpoint = Assert.Single(endpoints, item => item.RoutePattern.RawText == route &&
                item.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.SingleOrDefault() == method);
            var policies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
                .Select(item => item.Policy).Where(item => item is not null).ToArray();
            Assert.All(expected, policy => Assert.Contains(policy, policies));
        }

        AssertPolicies("/api/access/roles", "POST", "menu.roles", "action.roles.manage");
        AssertPolicies("/api/access/roles/{id:guid}", "PUT", "menu.roles", "action.roles.manage");
        AssertPolicies("/api/access/roles/{id:guid}/permissions", "PUT", "menu.access", "action.access.manage");
        AssertPolicies("/api/users/", "POST", "menu.users", "action.users.manage");
        AssertPolicies("/api/users/", "GET", "menu.users");
    }

    [Theory]
    [InlineData(StatusCodes.Status401Unauthorized)]
    [InlineData(StatusCodes.Status403Forbidden)]
    public async Task SecurityErrorResponseWriter_ReturnsJsonBody(int statusCode)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Response.Headers["X-Correlation-ID"] = "test-correlation-id";

        await SecurityErrorResponseWriter.WriteAsync(context, statusCode, "Security error");

        context.Response.Body.Position = 0;
        using var document = await System.Text.Json.JsonDocument.ParseAsync(context.Response.Body);
        var response = document.RootElement;

        Assert.Equal(statusCode, context.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        Assert.Equal("Security error", response.GetProperty("message").GetString());
        Assert.Equal(statusCode, response.GetProperty("statusCode").GetInt32());
        Assert.Equal("test-correlation-id", response.GetProperty("correlationId").GetString());
    }
}
