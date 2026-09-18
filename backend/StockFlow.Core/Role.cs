namespace StockFlow.Core;

public sealed class Role : Entity
{
    public string Name { get; set; } = "";
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<User> Users { get; set; } = [];
    public ICollection<RolePermission> Permissions { get; set; } = [];
}
