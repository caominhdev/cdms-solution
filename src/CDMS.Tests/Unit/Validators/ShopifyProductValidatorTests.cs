using CDMS.Core.Models;
using CDMS.Core.Services;
using Xunit;

namespace CDMS.Tests.Unit.Validators;

public class ShopifyProductValidatorTests
{
    private readonly ShopifyProductValidator _validator = new();

    private static ShopifyProduct CreateValidProduct()
    {
        var product = new ShopifyProduct
        {
            Id = "gid://shopify/Product/123",
            Title = "Valid Shopify Product",
            Handle = "valid-shopify-product",
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        product.Variants.Edges.Add(new ShopifyEdge<ShopifyProductVariant>
        {
            Node = new ShopifyProductVariant
            {
                Id = "gid://shopify/ProductVariant/456",
                Sku = "SKU-SHOPIFY-01",
                Price = "19.99",
                InventoryQuantity = 50
            }
        });

        return product;
    }

    [Fact]
    public async Task ValidateAsync_WithValidProduct_ReturnsSuccess()
    {
        var product = CreateValidProduct();
        var result = await _validator.ValidateAsync(product);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyTitle_ReturnsValidationError()
    {
        var product = CreateValidProduct();
        product.Title = "";

        var result = await _validator.ValidateAsync(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "MISSING_TITLE");
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidStatus_ReturnsValidationError()
    {
        var product = CreateValidProduct();
        product.Status = "INVALID_STATUS_CODE";

        var result = await _validator.ValidateAsync(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "INVALID_STATUS");
    }
}
