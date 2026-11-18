using System.Text.Json.Serialization;

namespace CDMS.Core.Models;

/// <summary>
/// Raw API response model for a Vietful product, matching the schema defined at
/// <see href="https://ext.stg.vnfai.com/doc/index.html#tag/Products"/>.
/// </summary>
/// <remarks>
/// This model is intentionally kept as a faithful mapping of the external API contract.
/// No business logic should be placed here; use <see cref="Services.ProductMappingService"/>
/// to convert instances into domain entities.
/// </remarks>
public sealed class VietfulProduct
{
    /// <summary>Gets or sets the Vietful platform-assigned product identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the product's Stock Keeping Unit.</summary>
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets or sets the product display name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the product description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Gets or sets the retail price.</summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    /// <summary>Gets or sets the ISO 4217 currency code (default: <c>VND</c>).</summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "VND";

    /// <summary>Gets or sets the available inventory quantity.</summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    /// <summary>Gets or sets the unit of measure (e.g. <c>pcs</c>, <c>kg</c>).</summary>
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    /// <summary>Gets or sets the product category path string.</summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>Gets or sets the brand name.</summary>
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    /// <summary>Gets or sets the name of the manufacturer.</summary>
    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; set; }

    /// <summary>Gets or sets the barcode or GTIN of the product.</summary>
    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    /// <summary>Gets or sets the list of product image URLs.</summary>
    [JsonPropertyName("images")]
    public List<VietfulProductImage> Images { get; set; } = new();

    /// <summary>Gets or sets arbitrary product attributes as key-value pairs.</summary>
    [JsonPropertyName("attributes")]
    public Dictionary<string, string?> Attributes { get; set; } = new();

    /// <summary>Gets or sets the Vietful tenant identifier that owns this product.</summary>
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the product status on the Vietful platform.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Gets or sets the country of origin (ISO 3166-1 alpha-2).</summary>
    [JsonPropertyName("countryOfOrigin")]
    public string? CountryOfOrigin { get; set; }

    /// <summary>Gets or sets the UTC timestamp when this product was created on Vietful.</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the most recent update on Vietful.</summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Represents a single image associated with a Vietful product.</summary>
public sealed class VietfulProductImage
{
    /// <summary>Gets or sets the platform-assigned image identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Gets or sets the publicly accessible URL of the image.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the alternative text for the image.</summary>
    [JsonPropertyName("altText")]
    public string? AltText { get; set; }

    /// <summary>Gets or sets the display sort order of the image.</summary>
    [JsonPropertyName("position")]
    public int Position { get; set; }
}
