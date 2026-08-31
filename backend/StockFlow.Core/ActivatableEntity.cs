namespace StockFlow.Core;

public abstract class ActivatableEntity : Entity
{
    public bool IsActive { get; set; } = true;
}
