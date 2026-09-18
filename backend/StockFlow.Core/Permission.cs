namespace StockFlow.Core;

public enum PermissionKind
{
    Menu,
    Action
}

public sealed class Permission
{
    public string Code { get; set; } = "";
    public string Group { get; set; } = "";
    public string Name { get; set; } = "";
    public PermissionKind Kind { get; set; }
    public ICollection<RolePermission> Roles { get; set; } = [];
}
