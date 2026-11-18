using System.Collections.Concurrent;
using CDMS.Core.Domain.Enums;
using CDMS.Metrics.Models;

namespace CDMS.Metrics;

public class MetricsCollector
{
    private readonly ConcurrentDictionary<string, WorkerMetricBucket> _buckets = new();

    private static string GetKey(Guid tenantId, WorkerType workerType) => $"{tenantId:N}_{workerType}";

    public void RecordSuccess(Guid tenantId, WorkerType workerType, double latencyMs)
    {
        var bucket = _buckets.GetOrAdd(GetKey(tenantId, workerType), _ => new WorkerMetricBucket(tenantId, workerType));
        bucket.RecordSuccess(latencyMs);
    }

    public void RecordInvalid(Guid tenantId, WorkerType workerType, double latencyMs)
    {
        var bucket = _buckets.GetOrAdd(GetKey(tenantId, workerType), _ => new WorkerMetricBucket(tenantId, workerType));
        bucket.RecordInvalid(latencyMs);
    }

    public void RecordFailure(Guid tenantId, WorkerType workerType, double latencyMs)
    {
        var bucket = _buckets.GetOrAdd(GetKey(tenantId, workerType), _ => new WorkerMetricBucket(tenantId, workerType));
        bucket.RecordFailure(latencyMs);
    }

    public MetricSnapshot GetSnapshot(Guid tenantId, WorkerType workerType)
    {
        if (_buckets.TryGetValue(GetKey(tenantId, workerType), out var bucket))
        {
            return bucket.GenerateSnapshot();
        }

        return new MetricSnapshot
        {
            TenantId = tenantId,
            WorkerType = workerType,
            HealthStatus = "NoData"
        };
    }

    public IReadOnlyList<MetricSnapshot> GetAllSnapshots()
    {
        return _buckets.Values.Select(b => b.GenerateSnapshot()).ToList();
    }
}

internal class WorkerMetricBucket
{
    private readonly Guid _tenantId;
    private readonly WorkerType _workerType;
    private long _processed;
    private long _invalid;
    private long _failed;
    private readonly List<double> _latencySamples = new();
    private readonly object _lock = new();
    private DateTime _lastSyncTime = DateTime.UtcNow;
    private readonly DateTime _startedAt = DateTime.UtcNow;

    public WorkerMetricBucket(Guid tenantId, WorkerType workerType)
    {
        _tenantId = tenantId;
        _workerType = workerType;
    }

    public void RecordSuccess(double latencyMs)
    {
        lock (_lock)
        {
            _processed++;
            _lastSyncTime = DateTime.UtcNow;
            AddSample(latencyMs);
        }
    }

    public void RecordInvalid(double latencyMs)
    {
        lock (_lock)
        {
            _invalid++;
            AddSample(latencyMs);
        }
    }

    public void RecordFailure(double latencyMs)
    {
        lock (_lock)
        {
            _failed++;
            AddSample(latencyMs);
        }
    }

    private void AddSample(double latencyMs)
    {
        if (_latencySamples.Count > 10_000)
        {
            _latencySamples.RemoveAt(0);
        }
        _latencySamples.Add(latencyMs);
    }

    public MetricSnapshot GenerateSnapshot()
    {
        lock (_lock)
        {
            var total = _processed + _invalid + _failed;
            var elapsed = Math.Max(1.0, (DateTime.UtcNow - _startedAt).TotalSeconds);
            var throughput = Math.Round(total / elapsed, 2);

            var sorted = _latencySamples.OrderBy(x => x).ToList();
            double p50 = GetPercentile(sorted, 0.50);
            double p95 = GetPercentile(sorted, 0.95);
            double p99 = GetPercentile(sorted, 0.99);
            double avg = sorted.Count > 0 ? Math.Round(sorted.Average(), 2) : 0;

            double errorRate = total > 0 ? Math.Round((double)(_failed + _invalid) / total * 100.0, 2) : 0;
            var freshness = DateTime.UtcNow - _lastSyncTime;

            string health = "Healthy";
            if (errorRate > 10.0 || freshness > TimeSpan.FromMinutes(15))
                health = "Degraded";
            if (errorRate > 50.0 || freshness > TimeSpan.FromHours(1))
                health = "Unhealthy";

            return new MetricSnapshot
            {
                TenantId = _tenantId,
                WorkerType = _workerType,
                Timestamp = DateTime.UtcNow,
                TotalRecordsProcessed = _processed,
                TotalRecordsInvalid = _invalid,
                TotalRecordsFailed = _failed,
                ThroughputPerSecond = throughput,
                AvgLatencyMs = avg,
                P50LatencyMs = p50,
                P95LatencyMs = p95,
                P99LatencyMs = p99,
                ErrorRatePercent = errorRate,
                DataFreshness = freshness,
                HealthStatus = health
            };
        }
    }

    private static double GetPercentile(List<double> sorted, double percentile)
    {
        if (sorted.Count == 0) return 0;
        int index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        return Math.Round(sorted[Math.Clamp(index, 0, sorted.Count - 1)], 2);
    }
}
