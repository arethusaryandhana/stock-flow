namespace StockFlow.Core;

public sealed class AuditLog : Entity
{
    public Guid? ActorId { get; set; }
    public User? Actor { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string Summary { get; set; } = "";
    public string Changes { get; set; } = "{}";
}
