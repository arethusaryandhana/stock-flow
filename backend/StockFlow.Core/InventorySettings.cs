namespace StockFlow.Core;

public sealed class InventorySettings : Entity
{
    public static readonly Guid DefaultId = Guid.Parse("a13263ef-4e59-4c6e-8a9d-94652b5b2141");

    public decimal DefaultReorderLevel { get; set; } = 5;
    public string DefaultUnit { get; set; } = "pcs";
    public bool AllowNegativeStock { get; set; }
    public decimal GlobalLowStockThreshold { get; set; }
}
