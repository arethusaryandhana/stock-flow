namespace StockFlow.Core;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid(); public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime? UpdatedAt
    {
        get; set;
    }
}
public abstract class ActivatableEntity : Entity
{
    public bool IsActive { get; set; } = true;
}

public enum PurchaseOrderStatus
{
    Draft, Submitted, Approved, Received, Cancelled
}
public enum SalesOrderStatus
{
    Draft, Confirmed, Processing, Completed, Cancelled
}
public enum StockMovementType
{
    GoodsReceipt, Sale, AdjustmentIn, AdjustmentOut
}
public enum ReportJobStatus
{
    Queued, Processing, Completed, Failed
}
public enum NotificationType
{
    LowStock, ReportReady, System
}

public sealed class Category : ActivatableEntity
{
    public string Name { get; set; } = ""; public string? Description
    {
        get; set;
    }
    public ICollection<Product> Products { get; set; } = [];
}
public sealed class Product : ActivatableEntity
{
    public string Sku { get; set; } = ""; public string Name { get; set; } = ""; public Guid CategoryId
    {
        get; set;
    }
    public Category Category { get; set; } = null!; public decimal PurchasePrice
    {
        get; set;
    }
    public decimal SellingPrice
    {
        get; set;
    }
    public decimal StockOnHand
    {
        get; set;
    }
    public decimal ReorderLevel
    {
        get; set;
    }
    public string Unit { get; set; } = "pcs";
}
public sealed class Supplier : ActivatableEntity
{
    public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string? Email
    {
        get; set;
    }
    public string? Phone
    {
        get; set;
    }
    public string? Address
    {
        get; set;
    }
}
public sealed class Customer : ActivatableEntity
{
    public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string? Email
    {
        get; set;
    }
    public string? Phone
    {
        get; set;
    }
    public string? Address
    {
        get; set;
    }
}

public sealed class PurchaseOrder : Entity
{
    public string Number { get; set; } = ""; public Guid SupplierId
    {
        get; set;
    }
    public Supplier Supplier { get; set; } = null!; public PurchaseOrderStatus Status
    {
        get; set;
    }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow; public DateTime? ExpectedDate
    {
        get; set;
    }
    public string? Notes
    {
        get; set;
    }
    public ICollection<PurchaseOrderItem> Items { get; set; } = [];
}
public sealed class PurchaseOrderItem : Entity
{
    public Guid PurchaseOrderId
    {
        get; set;
    }
    public PurchaseOrder PurchaseOrder { get; set; } = null!; public Guid ProductId
    {
        get; set;
    }
    public Product Product { get; set; } = null!; public decimal Quantity
    {
        get; set;
    }
    public decimal UnitPrice
    {
        get; set;
    }
}
public sealed class GoodsReceipt : Entity
{
    public string Number { get; set; } = ""; public Guid PurchaseOrderId
    {
        get; set;
    }
    public PurchaseOrder PurchaseOrder { get; set; } = null!; public DateTime ReceivedAt { get; set; } = DateTime.UtcNow; public Guid ReceivedById
    {
        get; set;
    }
    public User ReceivedBy { get; set; } = null!; public ICollection<GoodsReceiptItem> Items { get; set; } = [];
}
public sealed class GoodsReceiptItem : Entity
{
    public Guid GoodsReceiptId
    {
        get; set;
    }
    public GoodsReceipt GoodsReceipt { get; set; } = null!; public Guid ProductId
    {
        get; set;
    }
    public Product Product { get; set; } = null!; public decimal Quantity
    {
        get; set;
    }
}
public sealed class SalesOrder : Entity
{
    public string Number { get; set; } = ""; public Guid CustomerId
    {
        get; set;
    }
    public Customer Customer { get; set; } = null!; public SalesOrderStatus Status
    {
        get; set;
    }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow; public string? Notes
    {
        get; set;
    }
    public ICollection<SalesOrderItem> Items { get; set; } = [];
}
public sealed class SalesOrderItem : Entity
{
    public Guid SalesOrderId
    {
        get; set;
    }
    public SalesOrder SalesOrder { get; set; } = null!; public Guid ProductId
    {
        get; set;
    }
    public Product Product { get; set; } = null!; public decimal Quantity
    {
        get; set;
    }
    public decimal UnitPrice
    {
        get; set;
    }
}
public sealed class StockMovement : Entity
{
    public Guid ProductId
    {
        get; set;
    }
    public Product Product { get; set; } = null!; public StockMovementType Type
    {
        get; set;
    }
    public decimal Quantity
    {
        get; set;
    }
    public decimal BalanceAfter
    {
        get; set;
    }
    public string ReferenceNumber { get; set; } = ""; public string? Reason
    {
        get; set;
    }
    public Guid CreatedById
    {
        get; set;
    }
}
public sealed class StockAdjustment : Entity
{
    public string Number { get; set; } = ""; public Guid ProductId
    {
        get; set;
    }
    public Product Product { get; set; } = null!; public decimal QuantityDelta
    {
        get; set;
    }
    public string Reason { get; set; } = ""; public Guid CreatedById
    {
        get; set;
    }
}
public sealed class Role : Entity
{
    public string Name { get; set; } = ""; public ICollection<User> Users { get; set; } = [];
}
public sealed class User : ActivatableEntity
{
    public string Email { get; set; } = ""; public string FullName { get; set; } = ""; public string PasswordHash { get; set; } = ""; public Guid RoleId
    {
        get; set;
    }
    public Role Role { get; set; } = null!;
}
public sealed class Notification : Entity
{
    public Guid UserId
    {
        get; set;
    }
    public User User { get; set; } = null!; public NotificationType Type
    {
        get; set;
    }
    public string Title { get; set; } = ""; public string Message { get; set; } = ""; public bool IsRead
    {
        get; set;
    }
}
public sealed class ReportExportJob : Entity
{
    public string JobNumber { get; set; } = ""; public string ReportType { get; set; } = ""; public string Parameters { get; set; } = "{}"; public string Format { get; set; } = "csv"; public ReportJobStatus Status
    {
        get; set;
    }
    public int Progress
    {
        get; set;
    }
    public string? FilePath
    {
        get; set;
    }
    public long? FileSize
    {
        get; set;
    }
    public Guid RequestedById
    {
        get; set;
    }
    public User RequestedBy { get; set; } = null!; public DateTime RequestedAt { get; set; } = DateTime.UtcNow; public DateTime? StartedAt
    {
        get; set;
    }
    public DateTime? CompletedAt
    {
        get; set;
    }
    public string? ErrorMessage
    {
        get; set;
    }
}
