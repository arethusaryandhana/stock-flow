namespace StockFlow.Core;

public sealed class Customer : ActivatableEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
