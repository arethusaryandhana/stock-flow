using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.UseCases;
using StockFlow.Infrastructure.Repositories;

namespace StockFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddScoped<ICurrentUserService, SystemCurrentUserService>();
        services.AddStockFlowDatabase(configuration);
        services.AddInfrastructureServices();
        services.AddRepositories();
        services.AddUseCases();

        return services;
    }

    private static void AddStockFlowDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<StockFlowDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Database"),
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    StockFlowDbContext.Schemas.Identity)));
    }

    private static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();
        services.AddScoped<ITokenService, TokenService>();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IReportExportRepository, ReportExportRepository>();
    }

    private static void AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<IAuthUseCase, AuthUseCase>();
        services.AddScoped<IDashboardUseCase, DashboardUseCase>();
        services.AddScoped<IProductUseCase, ProductUseCase>();
        services.AddScoped<IInventoryUseCase, InventoryUseCase>();
        services.AddScoped<ICategoryUseCase, CategoryUseCase>();
        services.AddScoped<ISupplierUseCase, SupplierUseCase>();
        services.AddScoped<ICustomerUseCase, CustomerUseCase>();
        services.AddScoped<IReportExportUseCase, ReportExportUseCase>();
    }
}
