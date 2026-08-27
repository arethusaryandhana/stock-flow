namespace StockFlow.Application.Models;

public sealed record LoginRequest(string Email, string Password);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record ProductRequest(
    string Sku,
    string Name,
    Guid CategoryId,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal ReorderLevel,
    string Unit);

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
