namespace StockFlow.Core;

public sealed class GoodsReceipt : Entity
{
    public string Number { get; set; } = "";
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public Guid ReceivedById { get; set; }
    public User ReceivedBy { get; set; } = null!;
    public ICollection<GoodsReceiptItem> Items { get; set; } = [];
}
