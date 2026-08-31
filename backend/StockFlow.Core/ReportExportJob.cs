namespace StockFlow.Core;

public sealed class ReportExportJob : Entity
{
    public string JobNumber { get; set; } = "";
    public string ReportType { get; set; } = "";
    public string Parameters { get; set; } = "{}";
    public string Format { get; set; } = "csv";
    public ReportJobStatus Status { get; set; }
    public int Progress { get; set; }
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; } = null!;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
