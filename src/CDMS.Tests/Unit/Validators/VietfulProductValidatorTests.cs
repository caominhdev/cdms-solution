using CDMS.Core.Models;
using CDMS.Core.Services;
using Xunit;

namespace CDMS.Tests.Unit.Validators;

public class VietfulProductValidatorTests
{
    private readonly VietfulProductValidator _validator = new();

    private static VietfulProduct CreateValidProduct() => new()
    {
        Id = "VF-001",
        Sku = "SKU-VALID-01",
        Name = "Valid Product Name",
        Price = 50000,
        Quantity = 100,
        TenantId = "tenant-001",
        CreatedAt = DateTime.UtcNow.AddHours(-1)
    };

    [Fact]
    public async Task ValidateAsync_WithValidProduct_ReturnsSuccess()
    {
        var product = CreateValidProduct();
        var result = await _validator.ValidateAsync(product);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithMissingSku_ReturnsValidationError()
    {
        var product = CreateValidProduct();
        product.Sku = "";

        var result = await _validator.ValidateAsync(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "MISSING_SKU");
    }

    [Fact]
    public async Task ValidateAsync_WithNegativePrice_ReturnsValidationError()
    {
        var product = CreateValidProduct();
        product.Price = -100;

        var result = await _validator.ValidateAsync(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "NEGATIVE_PRICE");
    }

    [Fact]
    public async Task ValidateAsync_WithNegativeQuantity_ReturnsValidationError()
    {
        var product = CreateValidProduct();
        product.Quantity = -10;

        var result = await _validator.ValidateAsync(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "NEGATIVE_QUANTITY");
    }

    [Fact]
    public async Task ValidateAsync_WithMissingName_ReturnsValidationError()
    {
        var product = CreateValidProduct();
        product.Name = "   ";

        var result = await _validator.ValidateAsync(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "MISSING_NAME");
    }
}
