using System.Text.Json.Serialization;

namespace CDMS.Core.Models;

/// <summary>
/// Raw response model for a Shopify product, as returned by the Shopify Admin GraphQL API.
/// Field names and structure match the Shopify <c>Product</c> object definition.
/// </summary>
/// <remarks>
/// This model is intentionally kept as a faithful mapping of the Shopify GraphQL schema.
/// Use <see cref="Services.ProductMappingService"/> to convert instances into domain entities.
/// </remarks>
public sealed class ShopifyProduct
{
    /// <summary>Gets or sets the Shopify global ID (GID), e.g. <c>gid://shopify/Product/123456789</c>.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the product title displayed to customers.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL-friendly handle (slug) for the product.</summary>
    [JsonPropertyName("handle")]
    public string Handle { get; set; } = string.Empty;

    /// <summary>Gets or sets the product description in HTML format.</summary>
    [JsonPropertyName("descriptionHtml")]
    public string? DescriptionHtml { get; set; }

    /// <summary>Gets or sets the product type (custom product category).</summary>
    [JsonPropertyName("productType")]
    public string? ProductType { get; set; }

    /// <summary>Gets or sets the name of the product vendor.</summary>
    [JsonPropertyName("vendor")]
    public string? Vendor { get; set; }

    /// <summary>Gets or sets the list of tags attached to the product.</summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    /// <summary>Gets or sets the product's publication status (<c>ACTIVE</c>, <c>DRAFT</c>, <c>ARCHIVED</c>).</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "DRAFT";

    /// <summary>Gets or sets the product variants (at least one required by Shopify).</summary>
    [JsonPropertyName("variants")]
    public ShopifyConnection<ShopifyProductVariant> Variants { get; set; } = new();

    /// <summary>Gets or sets the product images.</summary>
    [JsonPropertyName("images")]
    public ShopifyConnection<ShopifyProductImage> Images { get; set; } = new();

    /// <summary>Gets or sets the product metafields (custom metadata).</summary>
    [JsonPropertyName("metafields")]
    public ShopifyConnection<ShopifyMetafield> Metafields { get; set; } = new();

    /// <summary>Gets or sets the SEO title for the product.</summary>
    [JsonPropertyName("seo")]
    public ShopifySeo? Seo { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the product was created in Shopify.</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the product was last updated in Shopify.</summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Represents a Shopify GraphQL <c>Connection</c> wrapper with edges and page info.
/// </summary>
/// <typeparam name="T">The node type within the connection.</typeparam>
public sealed class ShopifyConnection<T>
{
    /// <summary>Gets or sets the edges of the connection.</summary>
    [JsonPropertyName("edges")]
    public List<ShopifyEdge<T>> Edges { get; set; } = new();

    /// <summary>Gets or sets pagination information for this connection.</summary>
    [JsonPropertyName("pageInfo")]
    public ShopifyPageInfo? PageInfo { get; set; }

    /// <summary>Convenience accessor: returns the nodes from all edges.</summary>
    [JsonIgnore]
    public IEnumerable<T> Nodes => Edges.Select(e => e.Node);
}

/// <summary>Represents a single edge in a Shopify GraphQL connection.</summary>
/// <typeparam name="T">The node type.</typeparam>
public sealed class ShopifyEdge<T>
{
    /// <summary>Gets or sets the cursor for this edge, used for pagination.</summary>
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }

    /// <summary>Gets or sets the node (data object) for this edge.</summary>
    [JsonPropertyName("node")]
    public T Node { get; set; } = default!;
}

/// <summary>Pagination information returned alongside a Shopify GraphQL connection.</summary>
public sealed class ShopifyPageInfo
{
    /// <summary>Gets or sets a value indicating whether there is a previous page.</summary>
    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage { get; set; }

    /// <summary>Gets or sets a value indicating whether there is a next page.</summary>
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    /// <summary>Gets or sets the cursor for the first item in this page.</summary>
    [JsonPropertyName("startCursor")]
    public string? StartCursor { get; set; }

    /// <summary>Gets or sets the cursor for the last item in this page.</summary>
    [JsonPropertyName("endCursor")]
    public string? EndCursor { get; set; }
}

/// <summary>Represents a Shopify product variant.</summary>
public sealed class ShopifyProductVariant
{
    /// <summary>Gets or sets the variant GID.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the variant's SKU.</summary>
    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    /// <summary>Gets or sets the variant title (combination of option values).</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Gets or sets the retail price as a decimal string.</summary>
    [JsonPropertyName("price")]
    public string Price { get; set; } = "0.00";

    /// <summary>Gets or sets the compare-at price as a decimal string, or <c>null</c> if not set.</summary>
    [JsonPropertyName("compareAtPrice")]
    public string? CompareAtPrice { get; set; }

    /// <summary>Gets or sets the barcode (EAN / UPC / GTIN) for this variant.</summary>
    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    /// <summary>Gets or sets the inventory quantity currently available.</summary>
    [JsonPropertyName("inventoryQuantity")]
    public int InventoryQuantity { get; set; }

    /// <summary>Gets or sets the weight of the variant in grams.</summary>
    [JsonPropertyName("weight")]
    public double? Weight { get; set; }

    /// <summary>Gets or sets the weight unit (<c>GRAMS</c>, <c>KILOGRAMS</c>, <c>OUNCES</c>, <c>POUNDS</c>).</summary>
    [JsonPropertyName("weightUnit")]
    public string? WeightUnit { get; set; }

    /// <summary>Gets or sets whether inventory is tracked for this variant.</summary>
    [JsonPropertyName("inventoryPolicy")]
    public string? InventoryPolicy { get; set; }

    /// <summary>Gets or sets whether the variant requires shipping.</summary>
    [JsonPropertyName("requiresShipping")]
    public bool RequiresShipping { get; set; } = true;

    /// <summary>Gets or sets the variant's position within the product's variant list.</summary>
    [JsonPropertyName("position")]
    public int Position { get; set; }

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC last-updated timestamp.</summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Represents a Shopify product image.</summary>
public sealed class ShopifyProductImage
{
    /// <summary>Gets or sets the image GID.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the publicly accessible URL of the image.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>Gets or sets the alternative text for the image.</summary>
    [JsonPropertyName("altText")]
    public string? AltText { get; set; }

    /// <summary>Gets or sets the width of the image in pixels.</summary>
    [JsonPropertyName("width")]
    public int? Width { get; set; }

    /// <summary>Gets or sets the height of the image in pixels.</summary>
    [JsonPropertyName("height")]
    public int? Height { get; set; }
}

/// <summary>Represents a Shopify metafield (custom metadata on a resource).</summary>
public sealed class ShopifyMetafield
{
    /// <summary>Gets or sets the metafield GID.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the metafield namespace (e.g. <c>cdms</c>, <c>custom</c>).</summary>
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>Gets or sets the metafield key within the namespace.</summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the metafield value as a string.</summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>Gets or sets the Shopify metafield type (e.g. <c>single_line_text_field</c>, <c>json</c>).</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

/// <summary>Represents the SEO metadata of a Shopify product.</summary>
public sealed class ShopifySeo
{
    /// <summary>Gets or sets the SEO title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Gets or sets the SEO description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
