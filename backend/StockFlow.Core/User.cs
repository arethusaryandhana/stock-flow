namespace StockFlow.Core;

public sealed class User : ActivatableEntity
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int TokenVersion { get; set; }
    public bool InAppNotificationsEnabled { get; set; } = true;
    public bool LowStockNotificationsEnabled { get; set; } = true;
    public bool ReportReadyNotificationsEnabled { get; set; } = true;
    public bool SystemNotificationsEnabled { get; set; } = true;
    public bool NotificationSoundEnabled { get; set; }
    public int NotificationPollingIntervalSeconds { get; set; } = 30;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
