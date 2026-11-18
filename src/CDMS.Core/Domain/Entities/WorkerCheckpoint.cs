using System.ComponentModel.DataAnnotations;
using CDMS.Core.Domain.Enums;

namespace CDMS.Core.Domain.Entities;

/// <summary>
/// Persists the resumable state of a background sync worker so it can continue
/// from exactly where it left off after a restart or failure.
/// </summary>
/// <remarks>
/// <para>
/// For Shopify pull workers, <see cref="LastCursor"/> stores the GraphQL page-info cursor.
/// </para>
/// <para>
/// For Vietful push workers, <see cref="LastMessageOffset"/> stores the offset within
/// the message/event stream that has already been processed.
/// </para>
/// </remarks>
public sealed class WorkerCheckpoint
{
    /// <summary>Gets or sets the primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Worker identity ──────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a stable identifier for the logical worker instance
    /// (typically derived from <c>TenantId + WorkerType</c>).
    /// </summary>
    [Required, MaxLength(256)]
    public string WorkerId { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant this checkpoint belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the worker type this checkpoint tracks.</summary>
    public WorkerType WorkerType { get; set; }

    // ── Progress markers ─────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the UTC timestamp of the last successfully processed message or record.
    /// Used to resume time-based queries.
    /// </summary>
    public DateTime? LastProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets the Shopify GraphQL pagination cursor returned by the previous page.
    /// <c>null</c> indicates the worker should start from the beginning.
    /// </summary>
    [MaxLength(1024)]
    public string? LastCursor { get; set; }

    /// <summary>
    /// Gets or sets the Vietful message stream offset of the last committed message.
    /// <c>null</c> indicates the worker should start from the earliest offset.
    /// </summary>
    public long? LastMessageOffset { get; set; }

    /// <summary>
    /// Gets or sets the total number of records that have been successfully processed
    /// by this worker instance across all runs.
    /// </summary>
    public long TotalRecordsProcessed { get; set; }

    // ── Extended state ───────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a serialised JSON object for additional worker-specific checkpoint
    /// data not covered by the standard fields (e.g. partial batch state).
    /// </summary>
    public string? MetadataJson { get; set; }

    // ── Audit fields ─────────────────────────────────────────────────────────

    /// <summary>Gets or sets the UTC timestamp when this record was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp when this record was last updated.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
