using CDMS.Core.Interfaces;
using CDMS.Core.Models;

namespace CDMS.Core.Services;

/// <summary>
/// Provides validation logic for <see cref="VietfulProduct"/> and <see cref="ShopifyProduct"/>
/// models received from external connectors.
/// </summary>
/// <remarks>
/// Validation is intentionally kept stateless and synchronous internally;
/// the <see cref="IDataValidator{T}"/> interface returns a <see cref="Task"/> to allow
/// future async rule evaluation (e.g. database lookups for duplicate detection).
/// </remarks>
public sealed class VietfulProductValidator : IDataValidator<VietfulProduct>
{
    private const int MaxNameLength = 512;
    private const int MaxSkuLength = 128;
    private const decimal MaxAllowedPrice = 1_000_000_000m; // 1 billion VND

    /// <inheritdoc/>
    public Task<ValidationResult> ValidateAsync(VietfulProduct item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var errors = new List<ValidationError>();

        // ── Required fields ──────────────────────────────────────────────────

        if (string.IsNullOrWhiteSpace(item.Id))
            errors.Add(new ValidationError("MISSING_ID", "Vietful product ID is required.", nameof(item.Id)));

        if (string.IsNullOrWhiteSpace(item.Sku))
            errors.Add(new ValidationError("MISSING_SKU", "Product SKU is required.", nameof(item.Sku)));
        else if (item.Sku.Length > MaxSkuLength)
            errors.Add(new ValidationError("SKU_TOO_LONG",
                $"SKU must not exceed {MaxSkuLength} characters.", nameof(item.Sku)));

        if (string.IsNullOrWhiteSpace(item.Name))
            errors.Add(new ValidationError("MISSING_NAME", "Product name is required.", nameof(item.Name)));
        else if (item.Name.Length > MaxNameLength)
            errors.Add(new ValidationError("NAME_TOO_LONG",
                $"Product name must not exceed {MaxNameLength} characters.", nameof(item.Name)));

        if (string.IsNullOrWhiteSpace(item.TenantId))
            errors.Add(new ValidationError("MISSING_TENANT_ID", "Tenant ID is required.", nameof(item.TenantId)));

        // ── Numeric constraints ──────────────────────────────────────────────

        if (item.Price < 0)
            errors.Add(new ValidationError("NEGATIVE_PRICE",
                "Product price must not be negative.", nameof(item.Price)));

        if (item.Price > MaxAllowedPrice)
            errors.Add(new ValidationError("PRICE_TOO_HIGH",
                $"Product price exceeds the maximum allowed value of {MaxAllowedPrice}.", nameof(item.Price)));

        if (item.Quantity < 0)
            errors.Add(new ValidationError("NEGATIVE_QUANTITY",
                "Inventory quantity must not be negative.", nameof(item.Quantity)));

        // ── Temporal constraints ─────────────────────────────────────────────

        if (item.CreatedAt == default)
            errors.Add(new ValidationError("MISSING_CREATED_AT",
                "CreatedAt timestamp is required.", nameof(item.CreatedAt)));

        if (item.UpdatedAt != default && item.UpdatedAt < item.CreatedAt)
            errors.Add(new ValidationError("INVALID_UPDATED_AT",
                "UpdatedAt must not be earlier than CreatedAt.", nameof(item.UpdatedAt)));

        // ── Image URLs ───────────────────────────────────────────────────────

        foreach (var image in item.Images)
        {
            if (string.IsNullOrWhiteSpace(image.Url))
                errors.Add(new ValidationError("INVALID_IMAGE_URL",
                    "Product image URL must not be empty.", "Images[].Url"));
            else if (!Uri.TryCreate(image.Url, UriKind.Absolute, out var uri)
                     || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                errors.Add(new ValidationError("INVALID_IMAGE_URL",
                    $"Image URL '{image.Url}' is not a valid HTTP/HTTPS URL.", "Images[].Url"));
        }

        return Task.FromResult(
            errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors));
    }
}

/// <summary>
/// Provides validation logic for <see cref="ShopifyProduct"/> models pulled from the
/// Shopify Admin GraphQL API.
/// </summary>
public sealed class ShopifyProductValidator : IDataValidator<ShopifyProduct>
{
    private static readonly HashSet<string> ValidStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "ACTIVE", "DRAFT", "ARCHIVED" };

    /// <inheritdoc/>
    public Task<ValidationResult> ValidateAsync(ShopifyProduct item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var errors = new List<ValidationError>();

        // ── Required fields ──────────────────────────────────────────────────

        if (string.IsNullOrWhiteSpace(item.Id))
            errors.Add(new ValidationError("MISSING_ID", "Shopify product GID is required.", nameof(item.Id)));

        if (string.IsNullOrWhiteSpace(item.Title))
            errors.Add(new ValidationError("MISSING_TITLE", "Product title is required.", nameof(item.Title)));

        if (string.IsNullOrWhiteSpace(item.Handle))
            errors.Add(new ValidationError("MISSING_HANDLE", "Product handle (slug) is required.", nameof(item.Handle)));

        // ── Status ───────────────────────────────────────────────────────────

        if (!ValidStatuses.Contains(item.Status))
            errors.Add(new ValidationError("INVALID_STATUS",
                $"Product status '{item.Status}' is not valid. Expected: ACTIVE, DRAFT, or ARCHIVED.",
                nameof(item.Status)));

        // ── Variants ─────────────────────────────────────────────────────────

        if (!item.Variants.Nodes.Any())
            errors.Add(new ValidationError("NO_VARIANTS",
                "A Shopify product must have at least one variant.", nameof(item.Variants)));

        foreach (var variant in item.Variants.Nodes)
        {
            if (string.IsNullOrWhiteSpace(variant.Id))
                errors.Add(new ValidationError("MISSING_VARIANT_ID",
                    "Variant ID (GID) is required.", "Variants[].Id"));

            if (!decimal.TryParse(variant.Price, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var price))
            {
                errors.Add(new ValidationError("INVALID_VARIANT_PRICE",
                    $"Variant price '{variant.Price}' is not a valid decimal.", "Variants[].Price"));
            }
            else if (price < 0)
            {
                errors.Add(new ValidationError("NEGATIVE_VARIANT_PRICE",
                    "Variant price must not be negative.", "Variants[].Price"));
            }

            if (variant.InventoryQuantity < 0)
                errors.Add(new ValidationError("NEGATIVE_INVENTORY",
                    "Variant inventory quantity must not be negative.", "Variants[].InventoryQuantity"));
        }

        // ── Image URLs ───────────────────────────────────────────────────────

        foreach (var image in item.Images.Nodes)
        {
            if (string.IsNullOrWhiteSpace(image.Url))
                errors.Add(new ValidationError("INVALID_IMAGE_URL",
                    "Product image URL must not be empty.", "Images[].Url"));
            else if (!Uri.TryCreate(image.Url, UriKind.Absolute, out var uri)
                     || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                errors.Add(new ValidationError("INVALID_IMAGE_URL",
                    $"Image URL '{image.Url}' is not a valid HTTP/HTTPS URL.", "Images[].Url"));
        }

        // ── Temporal ─────────────────────────────────────────────────────────

        if (item.CreatedAt == default)
            errors.Add(new ValidationError("MISSING_CREATED_AT",
                "CreatedAt timestamp is required.", nameof(item.CreatedAt)));

        if (item.UpdatedAt != default && item.UpdatedAt < item.CreatedAt)
            errors.Add(new ValidationError("INVALID_UPDATED_AT",
                "UpdatedAt must not be earlier than CreatedAt.", nameof(item.UpdatedAt)));

        return Task.FromResult(
            errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors));
    }
}
