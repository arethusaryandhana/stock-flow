namespace StockFlow.Core;

public sealed class RevokedSessionToken : Entity
{
    public string TokenId { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}
