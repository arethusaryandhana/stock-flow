namespace StockFlow.Core;

public sealed class Category : ActivatableEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public ICollection<Product> Products { get; set; } = [];
}
