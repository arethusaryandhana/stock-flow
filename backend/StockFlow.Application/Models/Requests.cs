namespace StockFlow.Application.Models;

public sealed record LoginRequest(string Email, string Password);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record UpdateAccountProfileRequest(
    string FullName,
    string Email,
    string? CurrentPassword);

public sealed record ProductRequest(
    string Sku,
    string Name,
    Guid CategoryId,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal? ReorderLevel,
    string? Unit);

public sealed record ProductReorderLevelRequest(decimal ReorderLevel);

public sealed record MasterDataRequest(
    string Code,
    string Name,
    string? Email,
    string? Phone,
    string? Address);

public sealed record CategoryRequest(string Name, string? Description);

public sealed record StockAdjustmentRequest(
    Guid ProductId,
    decimal QuantityDelta,
    string Reason);

public sealed record PurchaseOrderItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice);

public sealed record PurchaseOrderRequest(
    Guid SupplierId,
    DateTime? ExpectedDate,
    string? Notes,
    IReadOnlyList<PurchaseOrderItemRequest> Items);

public sealed record PurchaseOrderStatusRequest(string Status);

public sealed record GoodsReceiptItemRequest(
    Guid ProductId,
    decimal Quantity);

public sealed record GoodsReceiptRequest(
    Guid PurchaseOrderId,
    IReadOnlyList<GoodsReceiptItemRequest> Items);

public sealed record SalesOrderItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice);

public sealed record SalesOrderRequest(
    Guid CustomerId,
    string? Notes,
    IReadOnlyList<SalesOrderItemRequest> Items);

public sealed record SalesOrderStatusRequest(string Status);

public sealed record ReportExportRequest(
    string ReportType,
    string Format);

public sealed record NotificationPreferencesRequest(
    bool InAppEnabled,
    bool LowStockEnabled,
    bool ReportReadyEnabled,
    bool SystemEnabled,
    bool SoundEnabled,
    int PollingIntervalSeconds);

public sealed record InventorySettingsRequest(
    decimal DefaultReorderLevel,
    string DefaultUnit,
    bool AllowNegativeStock,
    decimal GlobalLowStockThreshold);

public sealed record CompanyProfileRequest(
    string Name,
    string? Address,
    string? Email,
    string? Phone,
    string Currency,
    string? LogoUrl);
