using System.ComponentModel.DataAnnotations;
using CDMS.Core.Domain.Enums;

namespace CDMS.Core.Domain.Entities;

/// <summary>
/// Records a data-quality event (validation failure, mapping error, or data anomaly)
/// for a specific product record during a sync operation.
/// </summary>
public sealed class DataQualityLog
{
    /// <summary>Gets or sets the primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Context ──────────────────────────────────────────────────────────────

    /// <summary>Gets or sets the tenant the affected record belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the worker type that encountered the quality issue.</summary>
    public WorkerType WorkerType { get; set; }

    /// <summary>Gets or sets the CDMS product identifier, if the record could be mapped.</summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the external product identifier (Vietful ID or Shopify GID)
    /// of the record that failed validation.
    /// </summary>
    [MaxLength(256)]
    public string? ExternalProductId { get; set; }

    /// <summary>Gets or sets the SKU of the product record that failed validation.</summary>
    [MaxLength(128)]
    public string? Sku { get; set; }

    // ── Error details ────────────────────────────────────────────────────────

    /// <summary>Gets or sets the sync status that led to this log entry.</summary>
    public SyncStatus SyncStatus { get; set; }

    /// <summary>
    /// Gets or sets a short, machine-readable error code (e.g. <c>MISSING_SKU</c>,
    /// <c>INVALID_PRICE</c>, <c>SCHEMA_MISMATCH</c>).
    /// </summary>
    [Required, MaxLength(128)]
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>Gets or sets a human-readable description of the data quality issue.</summary>
    [Required]
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the field that failed validation, if applicable.</summary>
    [MaxLength(256)]
    public string? FieldName { get; set; }

    /// <summary>Gets or sets the raw field value that caused the failure, truncated to 1 KB.</summary>
    [MaxLength(1024)]
    public string? FieldValue { get; set; }

    /// <summary>
    /// Gets or sets the serialised JSON snapshot of the raw payload that caused the error,
    /// capped at 64 KB to bound storage growth.
    /// </summary>
    public string? RawPayloadJson { get; set; }

    // ── Resolution ───────────────────────────────────────────────────────────

    /// <summary>Gets or sets a value indicating whether this issue has been resolved.</summary>
    public bool IsResolved { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the issue was resolved, if applicable.</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Gets or sets notes added by an operator when resolving the issue.</summary>
    public string? ResolutionNotes { get; set; }

    // ── Audit fields ─────────────────────────────────────────────────────────

    /// <summary>Gets or sets the UTC timestamp when this record was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp when this record was last updated.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
