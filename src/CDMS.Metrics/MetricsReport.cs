using System.Text;
using CDMS.Metrics.Models;

namespace CDMS.Metrics;

public static class MetricsReport
{
    public static string GenerateMarkdownReport(IReadOnlyList<MetricSnapshot> snapshots)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# CDMS Sync Performance & Health Report");
        sb.AppendLine($"Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n");

        if (snapshots.Count == 0)
        {
            sb.AppendLine("_No active worker metrics recorded._");
            return sb.ToString();
        }

        sb.AppendLine("| Tenant ID | Worker Type | Processed | Invalid | Failed | Throughput (rec/s) | Avg Latency (ms) | P95 (ms) | P99 (ms) | Error Rate | Status |");
        sb.AppendLine("|-----------|-------------|-----------|---------|--------|---------------------|------------------|----------|----------|------------|--------|");

        foreach (var s in snapshots)
        {
            sb.AppendLine($"| `{s.TenantId:N}` | {s.WorkerType} | {s.TotalRecordsProcessed} | {s.TotalRecordsInvalid} | {s.TotalRecordsFailed} | {s.ThroughputPerSecond:F1} | {s.AvgLatencyMs:F1} | {s.P95LatencyMs:F1} | {s.P99LatencyMs:F1} | {s.ErrorRatePercent:F1}% | **{s.HealthStatus}** |");
        }

        return sb.ToString();
    }
}
