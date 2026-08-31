namespace StockFlow.Core;

public sealed class PurchaseOrder : Entity
{
    public string Number { get; set; } = "";
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public PurchaseOrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDate { get; set; }
    public string? Notes { get; set; }
    public ICollection<PurchaseOrderItem> Items { get; set; } = [];
}
