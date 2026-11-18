using System.ComponentModel.DataAnnotations;
using CDMS.Core.Domain.Enums;

namespace CDMS.Core.Domain.Entities;

/// <summary>
/// Represents a tenant-specific product record that is synchronised between
/// the Vietful platform and a tenant's Shopify store.
/// </summary>
/// <remarks>
/// Each <see cref="Product"/> belongs to exactly one <see cref="Tenant"/> and
/// may reference shared manufacturer / regulatory data via <see cref="ProductBase"/>.
/// </remarks>
public sealed class Product
{
    /// <summary>Gets or sets the primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Tenant relationship ──────────────────────────────────────────────────

    /// <summary>Gets or sets the owning tenant's identifier.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Navigation property: the owning tenant.</summary>
    public Tenant? Tenant { get; set; }

    // ── Shared base relationship ─────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the optional reference to the shared <see cref="ProductBase"/> record,
    /// which holds manufacturer and regulatory information common across tenants.
    /// </summary>
    public Guid? ProductBaseId { get; set; }

    /// <summary>Navigation property: shared product base data.</summary>
    public ProductBase? ProductBase { get; set; }

    // ── External identifiers ─────────────────────────────────────────────────

    /// <summary>Gets or sets the product's SKU (Stock Keeping Unit).</summary>
    [Required, MaxLength(128)]
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the Vietful platform product ID for this tenant.</summary>
    [MaxLength(256)]
    public string? VietfulProductId { get; set; }

    /// <summary>Gets or sets the Shopify global product ID (GID format).</summary>
    [MaxLength(256)]
    public string? ShopifyProductId { get; set; }

    /// <summary>Gets or sets the Shopify variant ID that maps to this product record.</summary>
    [MaxLength(256)]
    public string? ShopifyVariantId { get; set; }

    // ── Core product fields ──────────────────────────────────────────────────

    /// <summary>Gets or sets the product display name.</summary>
    [Required, MaxLength(512)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the product description (may contain HTML).</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the retail price in the tenant's configured currency.</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the ISO 4217 currency code (e.g. <c>VND</c>, <c>USD</c>).</summary>
    [MaxLength(8)]
    public string Currency { get; set; } = "VND";

    /// <summary>Gets or sets the available inventory quantity.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets or sets the unit of measure (e.g. <c>pcs</c>, <c>kg</c>, <c>box</c>).</summary>
    [MaxLength(64)]
    public string? Unit { get; set; }

    /// <summary>Gets or sets the product category path.</summary>
    [MaxLength(512)]
    public string? Category { get; set; }

    /// <summary>Gets or sets the brand name of the product.</summary>
    [MaxLength(256)]
    public string? Brand { get; set; }

    /// <summary>Gets or sets the barcode / GTIN of the product variant.</summary>
    [MaxLength(64)]
    public string? Barcode { get; set; }

    /// <summary>
    /// Gets or sets a serialised JSON array of image URLs.
    /// </summary>
    public string? ImagesJson { get; set; }

    /// <summary>
    /// Gets or sets a serialised JSON object of additional product attributes.
    /// </summary>
    public string? AttributesJson { get; set; }

    // ── Sync state ───────────────────────────────────────────────────────────

    /// <summary>Gets or sets the last sync status for this product.</summary>
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;

    /// <summary>Gets or sets the UTC timestamp of the most recent successful sync.</summary>
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>Gets or sets any error message produced during the last sync attempt.</summary>
    public string? LastSyncError { get; set; }

    // ── Audit fields ─────────────────────────────────────────────────────────

    /// <summary>Gets or sets the UTC timestamp when this record was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp when this record was last updated.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
