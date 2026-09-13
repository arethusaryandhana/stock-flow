namespace StockFlow.Core;

public sealed class CompanyProfile : Entity
{
    public static readonly Guid DefaultId = Guid.Parse("5f7b8c29-1a42-4d6e-9b10-2c3d4e5f6071");

    public string Name { get; set; } = "StockFlow Demo";
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Currency { get; set; } = "IDR";
    public string? LogoUrl { get; set; }
}
