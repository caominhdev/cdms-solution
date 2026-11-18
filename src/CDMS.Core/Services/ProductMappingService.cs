using System.Text.Json;
using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;
using CDMS.Core.Models;

namespace CDMS.Core.Services;

/// <summary>
/// Maps raw external API models (<see cref="VietfulProduct"/>, <see cref="ShopifyProduct"/>)
/// to CDMS domain entities (<see cref="Product"/>, <see cref="ProductBase"/>).
/// </summary>
/// <remarks>
/// All mapping is deterministic and free of side effects. No I/O is performed;
/// this service is safe to use in a synchronous or asynchronous context.
/// </remarks>
public sealed class ProductMappingService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    // ── Vietful ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps a <see cref="VietfulProduct"/> to a tenant-scoped <see cref="Product"/> entity.
    /// </summary>
    /// <param name="source">The raw Vietful API model.</param>
    /// <param name="tenantId">The CDMS tenant identifier that owns this product.</param>
    /// <returns>A fully populated <see cref="Product"/> ready for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
    public Product MapVietfulToProduct(VietfulProduct source, Guid tenantId)
    {
        ArgumentNullException.ThrowIfNull(source);

        var now = DateTime.UtcNow;

        return new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VietfulProductId = source.Id,
            Sku = NormaliseSku(source.Sku),
            Name = source.Name.Trim(),
            Description = source.Description?.Trim(),
            Price = source.Price,
            Currency = string.IsNullOrWhiteSpace(source.Currency) ? "VND" : source.Currency.ToUpperInvariant(),
            Quantity = source.Quantity,
            Unit = source.Unit?.Trim(),
            Category = source.Category?.Trim(),
            Brand = source.Brand?.Trim(),
            Barcode = source.Barcode?.Trim(),
            ImagesJson = source.Images.Count > 0
                ? JsonSerializer.Serialize(source.Images.Select(i => i.Url), _jsonOptions)
                : null,
            AttributesJson = source.Attributes.Count > 0
                ? JsonSerializer.Serialize(source.Attributes, _jsonOptions)
                : null,
            SyncStatus = SyncStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// Maps a <see cref="VietfulProduct"/> to a <see cref="ProductBase"/> entity, capturing
    /// manufacturer and regulatory data that is shared across tenants.
    /// </summary>
    /// <param name="source">The raw Vietful API model.</param>
    /// <returns>A <see cref="ProductBase"/> ready for upsert by GTIN.</returns>
    public ProductBase MapVietfulToProductBase(VietfulProduct source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var now = DateTime.UtcNow;

        return new ProductBase
        {
            Id = Guid.NewGuid(),
            Gtin = NormaliseGtin(source.Barcode),
            Barcode = source.Barcode?.Trim(),
            Manufacturer = source.Manufacturer?.Trim(),
            CountryOfOrigin = source.CountryOfOrigin?.Trim().ToUpperInvariant(),
            Brand = source.Brand?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    // ── Shopify ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps a <see cref="ShopifyProduct"/> (with its first variant) to a tenant-scoped
    /// <see cref="Product"/> entity.
    /// </summary>
    /// <param name="source">The raw Shopify GraphQL model.</param>
    /// <param name="tenantId">The CDMS tenant identifier that owns this product.</param>
    /// <returns>A fully populated <see cref="Product"/> ready for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <c>null</c>.</exception>
    public Product MapShopifyToProduct(ShopifyProduct source, Guid tenantId)
    {
        ArgumentNullException.ThrowIfNull(source);

        var now = DateTime.UtcNow;
        var firstVariant = source.Variants.Nodes.FirstOrDefault();

        return new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ShopifyProductId = source.Id,
            ShopifyVariantId = firstVariant?.Id,
            Sku = NormaliseSku(firstVariant?.Sku ?? source.Handle),
            Name = source.Title.Trim(),
            Description = source.DescriptionHtml?.Trim(),
            Price = firstVariant is not null
                ? ParseDecimal(firstVariant.Price)
                : 0m,
            Currency = "USD", // Shopify returns prices in the store currency; default USD.
            Quantity = firstVariant?.InventoryQuantity ?? 0,
            Brand = source.Vendor?.Trim(),
            Barcode = firstVariant?.Barcode?.Trim(),
            ImagesJson = source.Images.Nodes.Any()
                ? JsonSerializer.Serialize(source.Images.Nodes.Select(i => i.Url), _jsonOptions)
                : null,
            AttributesJson = BuildShopifyAttributesJson(source),
            SyncStatus = SyncStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// Maps a <see cref="ShopifyProduct"/> to a <see cref="ProductBase"/> entity.
    /// </summary>
    /// <param name="source">The raw Shopify GraphQL model.</param>
    /// <returns>A <see cref="ProductBase"/> ready for upsert by GTIN.</returns>
    public ProductBase MapShopifyToProductBase(ShopifyProduct source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var now = DateTime.UtcNow;
        var firstVariant = source.Variants.Nodes.FirstOrDefault();
        var barcode = firstVariant?.Barcode?.Trim();

        return new ProductBase
        {
            Id = Guid.NewGuid(),
            Gtin = NormaliseGtin(barcode),
            Barcode = barcode,
            Brand = source.Vendor?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    // ── Merge / update helpers ───────────────────────────────────────────────

    /// <summary>
    /// Applies updated field values from a <see cref="VietfulProduct"/> onto an existing
    /// <see cref="Product"/> entity, preserving its identity and audit fields.
    /// </summary>
    /// <param name="existing">The persisted entity to update.</param>
    /// <param name="source">The incoming Vietful data.</param>
    public void ApplyVietfulUpdate(Product existing, VietfulProduct source)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(source);

        existing.Name = source.Name.Trim();
        existing.Description = source.Description?.Trim();
        existing.Price = source.Price;
        existing.Currency = string.IsNullOrWhiteSpace(source.Currency) ? "VND" : source.Currency.ToUpperInvariant();
        existing.Quantity = source.Quantity;
        existing.Unit = source.Unit?.Trim();
        existing.Category = source.Category?.Trim();
        existing.Brand = source.Brand?.Trim();
        existing.Barcode = source.Barcode?.Trim();
        existing.ImagesJson = source.Images.Count > 0
            ? JsonSerializer.Serialize(source.Images.Select(i => i.Url), _jsonOptions)
            : null;
        existing.AttributesJson = source.Attributes.Count > 0
            ? JsonSerializer.Serialize(source.Attributes, _jsonOptions)
            : null;
        existing.SyncStatus = SyncStatus.Pending;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Applies updated field values from a <see cref="ShopifyProduct"/> onto an existing
    /// <see cref="Product"/> entity, preserving its identity and audit fields.
    /// </summary>
    /// <param name="existing">The persisted entity to update.</param>
    /// <param name="source">The incoming Shopify data.</param>
    public void ApplyShopifyUpdate(Product existing, ShopifyProduct source)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(source);

        var firstVariant = source.Variants.Nodes.FirstOrDefault();

        existing.ShopifyVariantId = firstVariant?.Id ?? existing.ShopifyVariantId;
        existing.Name = source.Title.Trim();
        existing.Description = source.DescriptionHtml?.Trim();
        existing.Price = firstVariant is not null ? ParseDecimal(firstVariant.Price) : existing.Price;
        existing.Quantity = firstVariant?.InventoryQuantity ?? existing.Quantity;
        existing.Brand = source.Vendor?.Trim();
        existing.Barcode = firstVariant?.Barcode?.Trim() ?? existing.Barcode;
        existing.ImagesJson = source.Images.Nodes.Any()
            ? JsonSerializer.Serialize(source.Images.Nodes.Select(i => i.Url), _jsonOptions)
            : null;
        existing.AttributesJson = BuildShopifyAttributesJson(source);
        existing.SyncStatus = SyncStatus.Pending;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static string NormaliseSku(string? rawSku) =>
        string.IsNullOrWhiteSpace(rawSku) ? "UNKNOWN" : rawSku.Trim().ToUpperInvariant();

    private static string? NormaliseGtin(string? barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        var digits = new string(barcode.Where(char.IsDigit).ToArray());
        return digits.Length is 8 or 12 or 13 or 14 ? digits : null;
    }

    private static decimal ParseDecimal(string? value) =>
        decimal.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : 0m;

    private static string? BuildShopifyAttributesJson(ShopifyProduct source)
    {
        var attrs = new Dictionary<string, object?>();

        if (source.Tags.Count > 0)
            attrs["tags"] = source.Tags;

        if (!string.IsNullOrWhiteSpace(source.ProductType))
            attrs["productType"] = source.ProductType;

        if (source.Metafields.Nodes.Any())
        {
            attrs["metafields"] = source.Metafields.Nodes
                .ToDictionary(m => $"{m.Namespace}.{m.Key}", m => (object?)m.Value);
        }

        return attrs.Count > 0
            ? JsonSerializer.Serialize(attrs, _jsonOptions)
            : null;
    }
}
