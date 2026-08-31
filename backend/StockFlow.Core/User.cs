namespace StockFlow.Core;

public sealed class User : ActivatableEntity
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
