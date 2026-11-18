using CDMS.Core.Models;
using CDMS.Core.Services;
using Xunit;

namespace CDMS.Tests.Unit.Services;

public class ProductMappingServiceTests
{
    private readonly ProductMappingService _mapper = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void MapVietfulToProduct_SetsExpectedFields()
    {
        var vietful = new VietfulProduct
        {
            Id = "VF-999",
            Sku = "SKU-TEST-999",
            Name = "Mapped Vietful Product",
            Description = "Description text",
            Price = 120000,
            Quantity = 45,
            Category = "Electronics",
            Brand = "Sony",
            Barcode = "8931234567890",
            TenantId = _tenantId.ToString()
        };

        var product = _mapper.MapVietfulToProduct(vietful, _tenantId);

        Assert.Equal(_tenantId, product.TenantId);
        Assert.Equal("VF-999", product.VietfulProductId);
        Assert.Equal("SKU-TEST-999", product.Sku);
        Assert.Equal("Mapped Vietful Product", product.Name);
        Assert.Equal(120000, product.Price);
        Assert.Equal(45, product.Quantity);
        Assert.Equal("8931234567890", product.Barcode);
    }

    [Fact]
    public void MapVietfulToProductBase_ExtractsMasterCatalogData()
    {
        var vietful = new VietfulProduct
        {
            Id = "VF-999",
            Sku = "SKU-BASE-01",
            Name = "Base Product",
            Brand = "Samsung",
            Manufacturer = "Samsung Electronics Vietnam",
            CountryOfOrigin = "VN",
            Barcode = "8801234567890"
        };

        var productBase = _mapper.MapVietfulToProductBase(vietful);

        Assert.Equal("Samsung", productBase.Brand);
        Assert.Equal("Samsung Electronics Vietnam", productBase.Manufacturer);
        Assert.Equal("VN", productBase.CountryOfOrigin);
        Assert.Equal("8801234567890", productBase.Barcode);
    }

    [Fact]
    public void MapShopifyToProduct_SetsExpectedFields()
    {
        var shopify = new ShopifyProduct
        {
            Id = "gid://shopify/Product/888",
            Title = "Shopify T-Shirt",
            Handle = "shopify-t-shirt",
            DescriptionHtml = "<p>Nice shirt</p>",
            ProductType = "Apparel"
        };
        shopify.Variants.Edges.Add(new ShopifyEdge<ShopifyProductVariant>
        {
            Node = new ShopifyProductVariant
            {
                Id = "gid://shopify/ProductVariant/777",
                Sku = "SHIRT-RED-L",
                Price = "29.99",
                InventoryQuantity = 15,
                Barcode = "012345678901"
            }
        });

        var product = _mapper.MapShopifyToProduct(shopify, _tenantId);

        Assert.Equal(_tenantId, product.TenantId);
        Assert.Equal("gid://shopify/Product/888", product.ShopifyProductId);
        Assert.Equal("SHIRT-RED-L", product.Sku);
        Assert.Equal("Shopify T-Shirt", product.Name);
        Assert.Equal(29.99m, product.Price);
        Assert.Equal("012345678901", product.Barcode);
    }
}
