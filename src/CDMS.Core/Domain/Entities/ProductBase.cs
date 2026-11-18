using System.ComponentModel.DataAnnotations;

namespace CDMS.Core.Domain.Entities;

/// <summary>
/// Stores product information that is shared across all tenants: manufacturer details,
/// regulatory compliance data, certifications, and standardised categorisation.
/// </summary>
/// <remarks>
/// <see cref="ProductBase"/> records are keyed on GTIN/barcode and are not tenant-scoped,
/// allowing a single authoritative source for manufacturer and regulatory data that can
/// be referenced by many tenant-specific <see cref="Product"/> records.
/// </remarks>
public sealed class ProductBase
{
    /// <summary>Gets or sets the primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Identification ───────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the Global Trade Item Number (GTIN-8, GTIN-12, GTIN-13 or GTIN-14).
    /// Used as the natural key for de-duplication across tenants.
    /// </summary>
    [MaxLength(64)]
    public string? Gtin { get; set; }

    /// <summary>Gets or sets any additional barcode (e.g. QR, DataMatrix).</summary>
    [MaxLength(128)]
    public string? Barcode { get; set; }

    // ── Manufacturer information ─────────────────────────────────────────────

    /// <summary>Gets or sets the name of the product manufacturer.</summary>
    [MaxLength(512)]
    public string? Manufacturer { get; set; }

    /// <summary>Gets or sets the manufacturer's country of origin (ISO 3166-1 alpha-2).</summary>
    [MaxLength(4)]
    public string? CountryOfOrigin { get; set; }

    /// <summary>Gets or sets the brand under which the product is sold.</summary>
    [MaxLength(256)]
    public string? Brand { get; set; }

    /// <summary>Gets or sets the manufacturer's model number or product code.</summary>
    [MaxLength(256)]
    public string? ManufacturerPartNumber { get; set; }

    // ── Categorisation ───────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the standardised GS1 or internal category code
    /// (e.g. <c>10000248</c> – Beauty &amp; Personal Care).
    /// </summary>
    [MaxLength(128)]
    public string? StandardCategory { get; set; }

    /// <summary>Gets or sets the human-readable standard category name.</summary>
    [MaxLength(512)]
    public string? StandardCategoryName { get; set; }

    // ── Regulatory &amp; compliance ──────────────────────────────────────────

    /// <summary>
    /// Gets or sets a serialised JSON object containing regulatory information
    /// (e.g. Vietnam DKSP registration numbers, FDA codes, CE marks).
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///   "vn_dksp": "123456789",
    ///   "ce_mark": true,
    ///   "fda_registration": "K210001"
    /// }
    /// </code>
    /// </example>
    public string? RegulatoryInfoJson { get; set; }

    /// <summary>
    /// Gets or sets a serialised JSON array of certification identifiers or objects
    /// (e.g. ISO 9001, HACCP, organic certifications).
    /// </summary>
    /// <example>
    /// <code>
    /// [
    ///   { "name": "ISO 9001:2015", "issuedBy": "Bureau Veritas", "expiresAt": "2026-12-31" },
    ///   { "name": "HACCP", "issuedBy": "SGS", "expiresAt": null }
    /// ]
    /// </code>
    /// </example>
    public string? CertificationsJson { get; set; }

    /// <summary>
    /// Gets or sets a serialised JSON object with physical attributes:
    /// weight, dimensions, materials, age-group restrictions, etc.
    /// </summary>
    public string? PhysicalAttributesJson { get; set; }

    // ── Audit fields ─────────────────────────────────────────────────────────

    /// <summary>Gets or sets the UTC timestamp when this record was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp when this record was last updated.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ───────────────────────────────────────────────────────────

    /// <summary>Navigation property: tenant products that reference this base record.</summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
