namespace StockFlow.Core;

public sealed class SalesOrder : Entity
{
    public string Number { get; set; } = "";
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public SalesOrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public ICollection<SalesOrderItem> Items { get; set; } = [];
}
