using CDMS.Core.Domain.Enums;

namespace CDMS.Metrics.Models;

public class MetricSnapshot
{
    public Guid TenantId { get; set; }
    public WorkerType WorkerType { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public long TotalRecordsProcessed { get; set; }
    public long TotalRecordsFailed { get; set; }
    public long TotalRecordsInvalid { get; set; }
    public double ThroughputPerSecond { get; set; }
    public double AvgLatencyMs { get; set; }
    public double P50LatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    public double ErrorRatePercent { get; set; }
    public TimeSpan DataFreshness { get; set; }
    public string HealthStatus { get; set; } = "Healthy";
}
