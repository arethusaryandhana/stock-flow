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

    public bool CanTransitionTo(PurchaseOrderStatus nextStatus) => (Status, nextStatus) switch
    {
        (PurchaseOrderStatus.Draft, PurchaseOrderStatus.Submitted) => true,
        (PurchaseOrderStatus.Draft, PurchaseOrderStatus.Cancelled) => true,
        (PurchaseOrderStatus.Submitted, PurchaseOrderStatus.Approved) => true,
        (PurchaseOrderStatus.Submitted, PurchaseOrderStatus.Cancelled) => true,
        (PurchaseOrderStatus.Approved, PurchaseOrderStatus.Cancelled) => true,
        _ => false
    };
}
