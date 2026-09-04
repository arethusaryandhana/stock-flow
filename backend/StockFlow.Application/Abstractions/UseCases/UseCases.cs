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
    Task<PagedResponse<ProductResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        string? status = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<ProductResponse>> CreateAsync(
        ProductRequest request,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<ProductResponse>> UpdateAsync(
        Guid id,
        ProductRequest request,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<ProductResponse>> UpdateReorderLevelAsync(
        Guid id,
        ProductReorderLevelRequest request,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult> SetActiveAsync(
        Guid id,
        bool active,
        CancellationToken cancellationToken = default);
}

public interface ICategoryUseCase
{
    Task<PagedResponse<CategoryResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<CategoryResponse>> CreateAsync(
        CategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<CategoryResponse>> UpdateAsync(
        Guid id,
        CategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult> SetActiveAsync(
        Guid id,
        bool active,
        CancellationToken cancellationToken = default);
}

public interface IInventoryUseCase
{
    Task<StockMovementPageResponse> GetMovementsAsync(
        int page,
        int pageSize,
        string? search = null,
        string? type = null,
        int? periodDays = null,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<StockAdjustmentResponse>> GetAdjustmentsAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<StockAdjustmentResponse>> CreateAdjustmentAsync(
        StockAdjustmentRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default);
}

public interface IPurchasingUseCase
{
    Task<PagedResponse<PurchaseOrderResponse>> GetPurchaseOrdersAsync(
        int page,
        int pageSize,
        string? search = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<PurchaseOrderResponse?> GetPurchaseOrderAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<PurchaseOrderResponse>> CreatePurchaseOrderAsync(
        PurchaseOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<PurchaseOrderResponse>> UpdateStatusAsync(
        Guid id,
        string status,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<GoodsReceiptResponse>> GetGoodsReceiptsAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<GoodsReceiptResponse>> CreateGoodsReceiptAsync(
        GoodsReceiptRequest request,
        Guid receivedById,
        CancellationToken cancellationToken = default);
}

public interface ISupplierUseCase
{
    Task<PagedResponse<SupplierResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<SupplierResponse>> CreateAsync(
        MasterDataRequest request,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<SupplierResponse>> UpdateAsync(
        Guid id,
        MasterDataRequest request,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult> SetActiveAsync(
        Guid id,
        bool active,
        CancellationToken cancellationToken = default);
}

public interface ICustomerUseCase
{
    Task<PagedResponse<CustomerResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<CustomerResponse>> CreateAsync(
        MasterDataRequest request,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult<CustomerResponse>> UpdateAsync(
        Guid id,
        MasterDataRequest request,
        CancellationToken cancellationToken = default);

    Task<UseCaseResult> SetActiveAsync(
        Guid id,
        bool active,
        CancellationToken cancellationToken = default);
}

public interface IReportExportUseCase
{
    Task ProcessNextAsync(string reportStoragePath, CancellationToken cancellationToken = default);
}
