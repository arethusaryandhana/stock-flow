using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> GetActiveByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public interface IDashboardRepository
{
    Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default);
}

public interface IProductRepository
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Product?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ISupplierRepository
{
    Task<IReadOnlyList<SupplierResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICustomerRepository
{
    Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IReportExportRepository
{
    Task<ReportExportJob?> ClaimNextAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportProductRow>> GetProductRowsAsync(CancellationToken cancellationToken = default);

    Task CompleteAsync(
        ReportExportJob job,
        string filePath,
        long fileSize,
        CancellationToken cancellationToken = default);

    Task FailAsync(
        ReportExportJob job,
        string errorMessage,
        CancellationToken cancellationToken = default);
}
