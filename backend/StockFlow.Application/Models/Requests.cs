namespace StockFlow.Application.Models;

public sealed record LoginRequest(string Email, string Password);

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
