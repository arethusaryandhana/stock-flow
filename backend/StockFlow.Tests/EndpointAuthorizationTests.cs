using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using StockFlow.Infrastructure;
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
