namespace StockFlow.Application.Models;

public sealed record LoginResponse(string Token, string FullName, string Role);

public sealed record SessionResponse(string FullName, string Role);

public sealed record PasswordResetRequestResponse(string Message, string? ResetToken = null);

public sealed record MessageResponse(string Message);

public sealed record DashboardResponse(
    int Products,
    int LowStock,
    int Purchases,
    decimal SalesToday,
    int HealthyProducts,
    int OutOfStockProducts,
    decimal TotalUnits);

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

public sealed record StockMovementSummaryResponse(
    int TodayCount,
    decimal InboundQuantity,
    decimal OutboundQuantity);

public sealed record StockMovementPageResponse(
    IReadOnlyList<StockMovementResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    StockMovementSummaryResponse Summary)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

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

public sealed record PurchaseOrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string Unit,
    decimal Quantity,
    decimal ReceivedQuantity,
    decimal UnitPrice);

public sealed record PurchaseOrderResponse(
    Guid Id,
    string Number,
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    string Status,
    DateTime OrderDate,
    DateTime? ExpectedDate,
    string? Notes,
    decimal TotalAmount,
    IReadOnlyList<PurchaseOrderItemResponse> Items);

public sealed record PurchaseOrderStatusCountsResponse(
    int Draft,
    int Submitted,
    int Approved,
    int Received,
    int Cancelled);

public sealed record PurchaseOrderPageResponse(
    IReadOnlyList<PurchaseOrderResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    PurchaseOrderStatusCountsResponse StatusCounts)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record GoodsReceiptItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string Unit,
    decimal Quantity);

public sealed record GoodsReceiptResponse(
    Guid Id,
    string Number,
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    string SupplierName,
    DateTime ReceivedAt,
    IReadOnlyList<GoodsReceiptItemResponse> Items);

public sealed record SalesOrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string Unit,
    decimal StockOnHand,
    decimal Quantity,
    decimal UnitPrice);

public sealed record SalesOrderResponse(
    Guid Id,
    string Number,
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    string Status,
    DateTime OrderDate,
    DateTime? CompletedAt,
    string? Notes,
    decimal TotalAmount,
    IReadOnlyList<SalesOrderItemResponse> Items);

public sealed record SalesOrderStatusCountsResponse(
    int Draft,
    int Confirmed,
    int Processing,
    int Completed,
    int Cancelled);

public sealed record SalesOrderPageResponse(
    IReadOnlyList<SalesOrderResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    SalesOrderStatusCountsResponse StatusCounts)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record ReportProductRow(
    string Sku,
    string Name,
    decimal StockOnHand,
    decimal ReorderLevel);

public sealed record ReportExportResponse(
    Guid Id,
    string JobNumber,
    string ReportType,
    string Format,
    string Status,
    int Progress,
    long? FileSize,
    DateTime RequestedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage);

public sealed record ReportExportStatusCountsResponse(
    int Queued,
    int Processing,
    int Completed,
    int Failed);

public sealed record ReportExportPageResponse(
    IReadOnlyList<ReportExportResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    ReportExportStatusCountsResponse StatusCounts)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record ReportDownloadResponse(
    string FilePath,
    string FileName,
    string ContentType);
