namespace StockFlow.WebAPI.Endpoints;

public static class EndpointRegistration
{
    public static IServiceCollection AddStockFlowEndpoints(this IServiceCollection services)
    {
        services.AddSingleton<IEndpoint, AuthEndpoints>();
        services.AddSingleton<IEndpoint, DashboardEndpoints>();
        services.AddSingleton<IEndpoint, ProductEndpoints>();
        services.AddSingleton<IEndpoint, InventoryEndpoints>();
        services.AddSingleton<IEndpoint, CategoryEndpoints>();
        services.AddSingleton<IEndpoint, SupplierEndpoints>();
        services.AddSingleton<IEndpoint, CustomerEndpoints>();

        return services;
    }

    public static IEndpointRouteBuilder MapStockFlowEndpoints(this IEndpointRouteBuilder app)
    {
        foreach (var endpoint in app.ServiceProvider.GetServices<IEndpoint>())
        {
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
