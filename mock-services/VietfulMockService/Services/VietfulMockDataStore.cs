using CDMS.Core.Domain.Entities;
using CDMS.Core.Models;
using CDMS.Infrastructure.Messaging;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Xml.Linq;

namespace VietfulMockService.Services;

public class VietfulMockDataStore
{
    private readonly ConcurrentDictionary<string, VietfulProduct> _products = new();
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<VietfulMockDataStore> _logger;
    private long _sequenceNumber = 1000;

    public VietfulMockDataStore(IMessagePublisher publisher, ILogger<VietfulMockDataStore> logger)
    {
        _publisher = publisher;
        _logger = logger;
        SeedInitialData();
    }

    private void SeedInitialData()
    {
        var tenant1 = "a1b2c3d4-0001-0000-0000-000000000001";
        var tenant2 = "a1b2c3d4-0002-0000-0000-000000000002";

        var seeds = new List<VietfulProduct>
        {
            new()
            {
                Id = "VF-001",
                Sku = "SKU-VINAMILK-001",
                Name = "Sữa Tươi Tiệt Trùng Vinamilk 100% 1L",
                Description = "Sữa tươi tiệt trùng nguyên chất từ 100% sữa bò tươi",
                Price = 36000,
                Quantity = 500,
                Unit = "Hộp",
                Category = "Thực phẩm / Sữa & Chế phẩm sữa",
                Brand = "Vinamilk",
                Manufacturer = "Vinamilk Vietnam Corporation",
                Barcode = "8934673123456",
                TenantId = tenant1,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "VF-002",
                Sku = "SKU-TH-002",
                Name = "Sữa Tươi Sạch TH true MILK Ít Đường 1L",
                Description = "Sữa tươi sạch TH true MILK nguyên chất ít đường",
                Price = 38000,
                Quantity = 320,
                Unit = "Hộp",
                Category = "Thực phẩm / Sữa & Chế phẩm sữa",
                Brand = "TH true MILK",
                Manufacturer = "TH Group Vietnam",
                Barcode = "8936036010012",
                TenantId = tenant1,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "VF-003",
                Sku = "SKU-NESTLE-003",
                Name = "Cà Phê Hòa Tan Nescafé 3in1 Đậm Đà Hài Hòa 20 Gói",
                Description = "Cà phê hòa tan đậm vị cà phê Việt Nam",
                Price = 62000,
                Quantity = 850,
                Unit = "Bịch",
                Category = "Đồ uống / Cà phê",
                Brand = "Nescafé",
                Manufacturer = "Nestlé Vietnam Ltd",
                Barcode = "8934804021356",
                TenantId = tenant2,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                UpdatedAt = DateTime.UtcNow
            },
           new() {
                Id = "VF-004",
                Sku = "SKU-G7-003",
                Name = "Cà Phê Hòa Tan Trung Nguyên G7 3in1 20 Gói",
                Description = "Cà phê hòa tan 3 trong 1 đậm đà hương vị cà phê Việt Nam",
                Price = 70500,
                Quantity = 500,
                Unit = "Bịch",
                Category = "Đồ uống / Cà phê",
                Brand = "Trung Nguyên Legend",
                Manufacturer = "Trung Nguyên",
                Barcode = "8935024173084",
                TenantId = tenant2,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "VF-005",
                Sku = "SKU-WAKEUP-003",
                Name = "Cà Phê Hòa Tan Wake-up 3in1",
                Description = "Cà phê hòa tan 3 trong 1 thơm ngon đậm vị",
                Price = 72400,
                Quantity = 450,
                Unit = "Bịch",
                Category = "Đồ uống / Cà phê",
                Brand = "Wake-up",
                Manufacturer = "Vinacafé Biên Hòa",
                Barcode = "8934683007709",
                TenantId = tenant2,
                CreatedAt = DateTime.UtcNow.AddDays(-6),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "VF-006",
                Sku = "SKU-VINACAFE-003",
                Name = "Cà Phê Hòa Tan Vinacafé 3in1 Gold Original",
                Description = "Cà phê hòa tan 3 trong 1 hương vị truyền thống",
                Price = 79000,
                Quantity = 600,
                Unit = "Bịch",
                Category = "Đồ uống / Cà phê",
                Brand = "Vinacafé",
                Manufacturer = "Vinacafé Biên Hòa",
                Barcode = "8934804020000",
                TenantId = tenant2,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "VF-007",
                Sku = "SKU-NESTLE-004",
                Name = "Cà Phê Hòa Tan Nescafé 3in1 Vị Rang Đậm 20 Gói",
                Description = "Cà phê hòa tan 3 trong 1 với hương vị rang đậm",
                Price = 79350,
                Quantity = 700,
                Unit = "Hộp",
                Category = "Đồ uống / Cà phê",
                Brand = "Nescafé",
                Manufacturer = "Nestlé Vietnam Ltd",
                Barcode = "8934804050000",
                TenantId = tenant2,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = "VF-008",
                Sku = "SKU-GO-003",
                Name = "Cà Phê Hòa Tan GO! 3in1 25 Gói",
                Description = "Cà phê hòa tan 3 trong 1 tiện lợi",
                Price = 86000,
                Quantity = 350,
                Unit = "Bịch",
                Category = "Đồ uống / Cà phê",
                Brand = "GO!",
                Manufacturer = "GO!",
                Barcode = "8930000000000",
                TenantId = tenant2,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow
            }
        };

        foreach (var p in seeds)
        {
            _products[p.Id] = p;
        }
    }

    public IReadOnlyList<VietfulProduct> ListProducts(int page = 1, int limit = 50)
    {
        return _products.Values
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToList();
    }

    public VietfulProduct? GetById(string id)
    {
        _products.TryGetValue(id, out var p);
        return p;
    }

    public void AddOrUpdate(VietfulProduct product)
    {
        _products[product.Id] = product;
    }

    public async Task<MessageEnvelope> SimulatePushAsync(VietfulProduct product, string tenantId)
    {
        var seq = Interlocked.Increment(ref _sequenceNumber);
        var envelope = new MessageEnvelope
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "inventory.product.updated",
            TenantId = tenantId,
            Timestamp = DateTime.UtcNow,
            SequenceNumber = seq,
            Payload = JsonSerializer.Serialize(product),
            Source = "vietful"
        };

        var exchange = "vietful.events";
        var routingKey = $"tenant.{tenantId.Replace("-", "")}.inventory.product";

        await _publisher.PublishAsync(exchange, routingKey, envelope);
        _logger.LogInformation("Pushed simulated event {EventId} (Seq: {Seq}) to RabbitMQ for tenant {TenantId}", envelope.EventId, seq, tenantId);

        return envelope;
    }

    public async Task<MessageEnvelope> TriggerScenarioAsync(string scenarioName, string tenantId)
    {
        var seq = Interlocked.Increment(ref _sequenceNumber);
        string payloadJson;

        switch (scenarioName.ToLowerInvariant())
        {
            case "missing_sku":
                var badSku = new VietfulProduct
                {
                    Id = $"VF-BAD-{seq}",
                    Sku = "", // Invalid: Missing SKU
                    Name = "Sản phẩm lỗi thiếu SKU",
                    Price = 25000,
                    Quantity = 10,
                    TenantId = tenantId
                };
                payloadJson = JsonSerializer.Serialize(badSku);
                break;

            case "negative_price":
                var badPrice = new VietfulProduct
                {
                    Id = $"VF-BAD-{seq}",
                    Sku = $"SKU-NEG-{seq}",
                    Name = "Sản phẩm lỗi giá âm",
                    Price = -50000, // Invalid: Negative price
                    Quantity = 20,
                    TenantId = tenantId
                };
                payloadJson = JsonSerializer.Serialize(badPrice);
                break;

            case "invalid_barcode":
                var badBarcode = new VietfulProduct
                {
                    Id = $"VF-BAD-{seq}",
                    Sku = $"SKU-BARCODE-{seq}",
                    Name = "Sản phẩm lỗi mã vạch",
                    Price = 15000,
                    Quantity = 5,
                    Barcode = "123", // Invalid: Too short / malformed barcode
                    TenantId = tenantId
                };
                payloadJson = JsonSerializer.Serialize(badBarcode);
                break;

            case "null_name":
                var badName = new VietfulProduct
                {
                    Id = $"VF-BAD-{seq}",
                    Sku = $"SKU-NONAME-{seq}",
                    Name = "", // Invalid: Missing Name
                    Price = 30000,
                    Quantity = 10,
                    TenantId = tenantId
                };
                payloadJson = JsonSerializer.Serialize(badName);
                break;

            case "overflow_quantity":
                var badQty = new VietfulProduct
                {
                    Id = $"VF-BAD-{seq}",
                    Sku = $"SKU-OVERFLOW-{seq}",
                    Name = "Sản phẩm số lượng âm",
                    Price = 10000,
                    Quantity = -999, // Invalid: Negative quantity
                    TenantId = tenantId
                };
                payloadJson = JsonSerializer.Serialize(badQty);
                break;

            case "empty_payload":
                payloadJson = "{}"; // Invalid: completely empty object
                break;

            default:
                // Normal valid product
                var normal = new VietfulProduct
                {
                    Id = $"VF-NORM-{seq}",
                    Sku = $"SKU-NORM-{seq}",
                    Name = $"Sữa tươi sạch Ba Vì {seq} ml",
                    Price = 42000,
                    Quantity = 150,
                    Unit = "Chai",
                    Category = "Thực phẩm / Sữa tươi",
                    Brand = "Ba Vì Milk",
                    Manufacturer = "Ba Vi Milk Joint Stock Company",
                    Barcode = "8935001700105",
                    TenantId = tenantId
                };
                payloadJson = JsonSerializer.Serialize(normal);
                break;
        }

        var envelope = new MessageEnvelope
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "inventory.product.updated",
            TenantId = tenantId,
            Timestamp = DateTime.UtcNow,
            SequenceNumber = seq,
            Payload = payloadJson,
            Source = "vietful"
        };

        var exchange = "vietful.events";
        var routingKey = $"tenant.{tenantId.Replace("-", "")}.inventory.product";

        await _publisher.PublishAsync(exchange, routingKey, envelope);
        _logger.LogInformation("Triggered bad data scenario '{Scenario}' (Seq: {Seq}) for tenant {TenantId}", scenarioName, seq, tenantId);

        return envelope;
    }
}
