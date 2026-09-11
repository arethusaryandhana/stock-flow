namespace StockFlow.Core;

public sealed class Notification : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public NotificationType Type { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Link { get; set; }
    public string? DeduplicationKey { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
