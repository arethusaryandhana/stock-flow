namespace StockFlow.Core;

public sealed class StockAdjustment : Entity
{
    public string Number { get; set; } = "";
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal QuantityDelta { get; set; }
    public string Reason { get; set; } = "";
}
