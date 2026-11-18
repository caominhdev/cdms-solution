using System.ComponentModel.DataAnnotations;
using CDMS.Core.Domain.Enums;

namespace CDMS.Core.Domain.Entities;

/// <summary>
/// Stores the runtime configuration for a background sync worker instance
/// (e.g. polling interval, batch size, retry policy).
/// </summary>
public sealed class WorkerConfiguration
{
    /// <summary>Gets or sets the primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Relationship ────────────────────────────────────────────────────────

    /// <summary>Gets or sets the tenant this worker is configured for.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Navigation property: owning tenant.</summary>
    public Tenant? Tenant { get; set; }

    // ── Worker identity ──────────────────────────────────────────────────────

    /// <summary>Gets or sets the type of worker (Vietful push or Shopify pull).</summary>
    public WorkerType WorkerType { get; set; }

    /// <summary>
    /// Gets or sets a logical name / label for this worker instance,
    /// useful when multiple workers of the same type run in parallel.
    /// </summary>
    [MaxLength(128)]
    public string WorkerName { get; set; } = string.Empty;

    // ── Scheduling ───────────────────────────────────────────────────────────

    /// <summary>Gets or sets how often the worker polls or processes, in seconds.</summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>Gets or sets the maximum number of records processed per batch.</summary>
    public int BatchSize { get; set; } = 100;

    // ── Retry policy ─────────────────────────────────────────────────────────

    /// <summary>Gets or sets the maximum number of retry attempts for a failed record.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Gets or sets the initial retry back-off delay, in seconds.</summary>
    public int RetryBackoffSeconds { get; set; } = 5;

    // ── State ────────────────────────────────────────────────────────────────

    /// <summary>Gets or sets a value indicating whether the worker is enabled.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a serialised JSON object for worker-specific settings not covered
    /// by the standard configuration properties.
    /// </summary>
    public string? ExtendedConfigJson { get; set; }

    // ── Audit fields ─────────────────────────────────────────────────────────

    /// <summary>Gets or sets the UTC timestamp when this record was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp when this record was last updated.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
