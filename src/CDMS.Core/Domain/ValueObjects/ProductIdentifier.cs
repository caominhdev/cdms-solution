namespace CDMS.Core.Domain.ValueObjects;

/// <summary>
/// An immutable value object that uniquely identifies a product within the CDMS system
/// by combining tenant context with both internal and external identifiers.
/// </summary>
/// <remarks>
/// Value-object equality is based on all constituent parts; two
/// <see cref="ProductIdentifier"/> instances are equal when every field matches.
/// </remarks>
public sealed class ProductIdentifier : IEquatable<ProductIdentifier>
{
    /// <summary>Gets the CDMS tenant identifier.</summary>
    public Guid TenantId { get; }

    /// <summary>Gets the Stock Keeping Unit (SKU) assigned by the merchant.</summary>
    public string Sku { get; }

    /// <summary>
    /// Gets the Vietful platform product identifier, or <c>null</c> if not yet mapped.
    /// </summary>
    public string? VietfulProductId { get; }

    /// <summary>
    /// Gets the Shopify global product ID (GID, e.g. <c>gid://shopify/Product/123</c>),
    /// or <c>null</c> if not yet published to Shopify.
    /// </summary>
    public string? ShopifyProductId { get; }

    /// <summary>
    /// Initialises a new <see cref="ProductIdentifier"/>.
    /// </summary>
    /// <param name="tenantId">The owning tenant's identifier.</param>
    /// <param name="sku">The product SKU (must not be null or whitespace).</param>
    /// <param name="vietfulProductId">Optional Vietful product ID.</param>
    /// <param name="shopifyProductId">Optional Shopify product GID.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sku"/> is null or whitespace.
    /// </exception>
    public ProductIdentifier(
        Guid tenantId,
        string sku,
        string? vietfulProductId = null,
        string? shopifyProductId = null)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU must not be null or whitespace.", nameof(sku));

        TenantId = tenantId;
        Sku = sku.Trim();
        VietfulProductId = vietfulProductId?.Trim();
        ShopifyProductId = shopifyProductId?.Trim();
    }

    /// <inheritdoc/>
    public bool Equals(ProductIdentifier? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return TenantId == other.TenantId
            && string.Equals(Sku, other.Sku, StringComparison.OrdinalIgnoreCase)
            && string.Equals(VietfulProductId, other.VietfulProductId, StringComparison.Ordinal)
            && string.Equals(ShopifyProductId, other.ShopifyProductId, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as ProductIdentifier);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(
            TenantId,
            Sku.ToUpperInvariant(),
            VietfulProductId,
            ShopifyProductId);

    /// <summary>Returns a human-readable representation of the identifier.</summary>
    public override string ToString() =>
        $"Tenant={TenantId}, SKU={Sku}, Vietful={VietfulProductId ?? "N/A"}, Shopify={ShopifyProductId ?? "N/A"}";

    /// <summary>Equality operator.</summary>
    public static bool operator ==(ProductIdentifier? left, ProductIdentifier? right) =>
        left?.Equals(right) ?? right is null;

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(ProductIdentifier? left, ProductIdentifier? right) =>
        !(left == right);
}
