namespace StockFlow.Core;

public sealed class Product : ActivatableEntity
{
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal StockOnHand { get; set; }
    public decimal ReorderLevel { get; set; }
    public string Unit { get; set; } = "pcs";
}
