using StockFlow.Application.Models;

namespace StockFlow.Application.Abstractions.UseCases;

public interface IAuthUseCase
{
    Task<UseCaseResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<PasswordResetRequestResponse>> RequestPasswordResetAsync(
        ForgotPasswordRequest request,
        bool exposeResetToken,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<MessageResponse>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);
}

public interface IDashboardUseCase
{
    Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default);
}

public interface IProductUseCase
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UseCaseResult<ProductResponse>> CreateAsync(
        ProductRequest request,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult> SetActiveAsync(
        Guid id,
        bool active,
        CancellationToken cancellationToken = default);
}

public interface ICategoryUseCase
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UseCaseResult<CategoryResponse>> CreateAsync(
        CategoryRequest request,
        CancellationToken cancellationToken = default);
}

public interface IInventoryUseCase
{
    Task<IReadOnlyList<StockMovementResponse>> GetMovementsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockAdjustmentResponse>> GetAdjustmentsAsync(
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<StockAdjustmentResponse>> CreateAdjustmentAsync(
        StockAdjustmentRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default);
}

public interface ISupplierUseCase
{
    Task<IReadOnlyList<SupplierResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SupplierResponse> CreateAsync(
        MasterDataRequest request,
        CancellationToken cancellationToken = default);
}

public interface ICustomerUseCase
{
    Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CustomerResponse> CreateAsync(
        MasterDataRequest request,
        CancellationToken cancellationToken = default);
}

public interface IReportExportUseCase
{
    Task ProcessNextAsync(string reportStoragePath, CancellationToken cancellationToken = default);
}
