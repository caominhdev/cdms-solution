using CDMS.Core.Models;

namespace ShopifyMockService.Services;

public class MockShopifyStore
{
    private readonly List<ShopifyProduct> _products = new();
    private readonly object _lock = new();
    private bool _simulateRateLimit;

    public bool SimulateRateLimit
    {
        get => _simulateRateLimit;
        set => _simulateRateLimit = value;
    }

    public MockShopifyStore()
    {
        SeedProducts();
    }

    private void SeedProducts()
    {
        for (int i = 1; i <= 25; i++)
        {
            var p = new ShopifyProduct
            {
                Id = $"gid://shopify/Product/{1000 + i}",
                Title = $"Organic Green Tea Blend #{i}",
                Handle = $"organic-green-tea-blend-{i}",
                DescriptionHtml = $"<p>Premium hand-picked organic green tea #{i} with rich antioxidants.</p>",
                ProductType = "Beverages",
                Vendor = "Moc Chau Tea Estate",
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow.AddDays(-30 + i),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                Tags = new List<string> { "tea", "organic", "vietnam", "beverage" }
            };

            p.Variants.Edges.Add(new ShopifyEdge<ShopifyProductVariant>
            {
                Node = new ShopifyProductVariant
                {
                    Id = $"gid://shopify/ProductVariant/{5000 + i}",
                    Title = "250g Tin",
                    Sku = $"TEA-GRN-{100 + i}",
                    Price = "12.50",
                    InventoryQuantity = 100 + (i * 10),
                    Barcode = $"8938500{10000 + i}"
                }
            });

            p.Images.Edges.Add(new ShopifyEdge<ShopifyProductImage>
            {
                Node = new ShopifyProductImage
                {
                    Id = $"gid://shopify/ProductImage/{8000 + i}",
                    Url = $"https://cdn.shopify.com/s/files/1/mock/products/tea-{i}.jpg",
                    AltText = $"Organic Green Tea #{i}"
                }
            });

            p.Metafields.Edges.Add(new ShopifyEdge<ShopifyMetafield>
            {
                Node = new ShopifyMetafield
                {
                    Id = $"gid://shopify/Metafield/{9000 + i}",
                    Namespace = "cdms",
                    Key = "regulatory_cert",
                    Value = "ISO-22000-Certified",
                    Type = "single_line_text_field"
                }
            });

            _products.Add(p);
        }
    }

    public (List<ShopifyProduct> items, bool hasNextPage, string? endCursor) GetPage(string? after, int first)
    {
        lock (_lock)
        {
            int startIndex = 0;
            if (!string.IsNullOrEmpty(after))
            {
                if (int.TryParse(after, out var idx))
                {
                    startIndex = idx + 1;
                }
            }

            var items = _products.Skip(startIndex).Take(first).ToList();
            int lastItemIndex = startIndex + items.Count - 1;
            bool hasNext = startIndex + items.Count < _products.Count;
            string? nextCursor = hasNext ? lastItemIndex.ToString() : null;

            return (items, hasNext, nextCursor);
        }
    }

    public void InjectBadData(string scenario)
    {
        lock (_lock)
        {
            var id = 9000 + _products.Count;
            switch (scenario.ToLowerInvariant())
            {
                case "empty_title":
                    _products.Add(new ShopifyProduct
                    {
                        Id = $"gid://shopify/Product/{id}",
                        Title = "", // Invalid
                        Handle = $"bad-title-{id}",
                        Status = "ACTIVE"
                    });
                    break;

                case "negative_price":
                    var negProduct = new ShopifyProduct
                    {
                        Id = $"gid://shopify/Product/{id}",
                        Title = $"Bad Price Product #{id}",
                        Handle = $"bad-price-{id}",
                        Status = "ACTIVE"
                    };
                    negProduct.Variants.Edges.Add(new ShopifyEdge<ShopifyProductVariant>
                    {
                        Node = new ShopifyProductVariant
                        {
                            Id = $"gid://shopify/ProductVariant/{id * 2}",
                            Price = "-25.00", // Invalid
                            Sku = $"SKU-NEG-{id}"
                        }
                    });
                    _products.Add(negProduct);
                    break;

                case "invalid_status":
                    _products.Add(new ShopifyProduct
                    {
                        Id = $"gid://shopify/Product/{id}",
                        Title = $"Invalid Status #{id}",
                        Handle = $"bad-status-{id}",
                        Status = "DELETED_UNKNOWN" // Invalid
                    });
                    break;
            }
        }
    }
}
