namespace StockFlow.Core;

public sealed class StockMovement : Entity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public StockMovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal BalanceAfter { get; set; }
    public string ReferenceNumber { get; set; } = "";
    public string? Reason { get; set; }
}
