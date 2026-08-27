namespace StockFlow.Application.Models;

public sealed record LoginResponse(string Token, string FullName, string Role);

public sealed record PasswordResetRequestResponse(string Message, string? ResetToken = null);

public sealed record MessageResponse(string Message);

public sealed record DashboardResponse(
    int Products,
    int LowStock,
    int Purchases,
    decimal SalesToday,
    IReadOnlyList<LowStockProductResponse> Attention);

public sealed record LowStockProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string Category,
    decimal StockOnHand,
    decimal ReorderLevel,
    string Unit);

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    Guid CategoryId,
    string Category,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal StockOnHand,
    decimal ReorderLevel,
    string Unit,
    bool IsActive);

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record SupplierResponse(
    Guid Id,
    string Code,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CustomerResponse(
    Guid Id,
    string Code,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record StockMovementResponse(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string Unit,
    string Type,
    decimal Quantity,
    decimal BalanceAfter,
    string ReferenceNumber,
    string? Reason,
    DateTime CreatedAt);

public sealed record StockAdjustmentResponse(
    Guid Id,
    string Number,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string Unit,
    decimal QuantityDelta,
    string Reason,
    DateTime CreatedAt);

public sealed record ReportProductRow(
    string Sku,
    string Name,
    decimal StockOnHand,
    decimal ReorderLevel);
