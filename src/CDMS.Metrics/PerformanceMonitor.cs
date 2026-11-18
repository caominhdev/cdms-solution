using CDMS.Metrics.Models;
using Microsoft.Extensions.Logging;

namespace CDMS.Metrics;

public class PerformanceMonitor
{
    private readonly MetricsCollector _collector;
    private readonly ILogger<PerformanceMonitor> _logger;

    public PerformanceMonitor(MetricsCollector collector, ILogger<PerformanceMonitor> logger)
    {
        _collector = collector;
        _logger = logger;
    }

    public List<Alert> CheckHealth()
    {
        var alerts = new List<Alert>();
        var snapshots = _collector.GetAllSnapshots();

        foreach (var s in snapshots)
        {
            if (s.P95LatencyMs > 500.0)
            {
                alerts.Add(new Alert(
                    Severity.Warning,
                    $"Tenant {s.TenantId:N} ({s.WorkerType}) P95 latency is high: {s.P95LatencyMs}ms (threshold 500ms)"));
            }

            if (s.ErrorRatePercent > 5.0)
            {
                alerts.Add(new Alert(
                    s.ErrorRatePercent > 20.0 ? Severity.Critical : Severity.Warning,
                    $"Tenant {s.TenantId:N} ({s.WorkerType}) Error rate elevated: {s.ErrorRatePercent}% (threshold 5%)"));
            }

            if (s.DataFreshness > TimeSpan.FromMinutes(30) && s.TotalRecordsProcessed > 0)
            {
                alerts.Add(new Alert(
                    Severity.Warning,
                    $"Tenant {s.TenantId:N} ({s.WorkerType}) Sync staleness: {s.DataFreshness.TotalMinutes:F1} minutes since last record"));
            }
        }

        return alerts;
    }
}

public enum Severity { Info, Warning, Critical }

public record Alert(Severity Severity, string Message, DateTime Timestamp = default)
{
    public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;
}
