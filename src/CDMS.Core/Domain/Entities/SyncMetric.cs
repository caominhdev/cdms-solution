using CDMS.Core.Domain.Enums;

namespace CDMS.Core.Domain.Entities;

/// <summary>
/// Records aggregate performance and quality metrics for a worker's sync activity
/// within a fixed time window.
/// </summary>
/// <remarks>
/// Each row represents one observation window (e.g. one minute, one hour) for a
/// specific tenant and worker type, enabling time-series analysis and alerting.
/// </remarks>
public sealed class SyncMetric
{
    /// <summary>Gets or sets the primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Dimensions ───────────────────────────────────────────────────────────

    /// <summary>Gets or sets the tenant this metric was recorded for.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the type of worker that produced these metrics.</summary>
    public WorkerType WorkerType { get; set; }

    // ── Time window ──────────────────────────────────────────────────────────

    /// <summary>Gets or sets the inclusive UTC start of the observation window.</summary>
    public DateTime WindowStart { get; set; }

    /// <summary>Gets or sets the exclusive UTC end of the observation window.</summary>
    public DateTime WindowEnd { get; set; }

    // ── Counters ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the number of records that were successfully processed
    /// during the observation window.
    /// </summary>
    public long RecordsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of records that failed due to transient or permanent
    /// errors (network errors, API failures) during the observation window.
    /// </summary>
    public long RecordsFailed { get; set; }

    /// <summary>
    /// Gets or sets the number of records rejected by the data-validation layer
    /// (business-rule violations, schema mismatches) during the observation window.
    /// </summary>
    public long RecordsInvalid { get; set; }

    // ── Performance ──────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the average end-to-end latency per record in milliseconds
    /// measured during the observation window.
    /// </summary>
    public double AvgLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the throughput expressed as records processed per second
    /// over the observation window.
    /// </summary>
    public double ThroughputPerSecond { get; set; }

    /// <summary>Gets or sets the peak memory usage in megabytes during the window.</summary>
    public double? PeakMemoryMb { get; set; }

    /// <summary>Gets or sets the peak CPU usage percentage during the window (0–100).</summary>
    public double? PeakCpuPercent { get; set; }

    // ── Audit fields ─────────────────────────────────────────────────────────

    /// <summary>Gets or sets the UTC timestamp when this record was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp when this record was last updated.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Computed helpers ─────────────────────────────────────────────────────

    /// <summary>Gets the total number of records attempted within the window.</summary>
    public long TotalRecordsAttempted => RecordsProcessed + RecordsFailed + RecordsInvalid;

    /// <summary>Gets the success rate as a value between 0 and 1.</summary>
    public double SuccessRate =>
        TotalRecordsAttempted == 0 ? 0d : (double)RecordsProcessed / TotalRecordsAttempted;
}
