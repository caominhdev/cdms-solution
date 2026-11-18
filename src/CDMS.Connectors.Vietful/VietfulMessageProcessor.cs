using System.Diagnostics;
using System.Text.Json;
using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;
using CDMS.Core.Interfaces;
using CDMS.Core.Interfaces.Repositories;
using CDMS.Core.Models;
using CDMS.Core.Services;
using CDMS.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace CDMS.Connectors.Vietful;

public class VietfulMessageProcessor
{
    private readonly IProductRepository _productRepository;
    private readonly IProductBaseRepository _productBaseRepository;
    private readonly IDataQualityLogRepository _qualityLogRepository;
    private readonly IDataValidator<VietfulProduct> _validator;
    private readonly ProductMappingService _mapper;
    private readonly ILogger<VietfulMessageProcessor> _logger;

    public VietfulMessageProcessor(
        IProductRepository productRepository,
        IProductBaseRepository productBaseRepository,
        IDataQualityLogRepository qualityLogRepository,
        IDataValidator<VietfulProduct> validator,
        ProductMappingService mapper,
        ILogger<VietfulMessageProcessor> logger)
    {
        _productRepository = productRepository;
        _productBaseRepository = productBaseRepository;
        _qualityLogRepository = qualityLogRepository;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProcessResult> ProcessAsync(
        MessageEnvelope envelope,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        VietfulProduct? vietfulProduct;
        try
        {
            vietfulProduct = JsonSerializer.Deserialize<VietfulProduct>(envelope.Payload);
            if (vietfulProduct == null)
            {
                await LogQualityErrorAsync(
                    tenantId,
                    null,
                    null,
                    "EMPTY_PAYLOAD",
                    "Deserialized Vietful product was null",
                    null,
                    null,
                    envelope.Payload,
                    cancellationToken);

                return ProcessResult.Invalid(sw.Elapsed.TotalMilliseconds);
            }
        }
        catch (JsonException ex)
        {
            await LogQualityErrorAsync(
                tenantId,
                null,
                null,
                "JSON_PARSE_ERROR",
                $"Invalid JSON payload: {ex.Message}",
                null,
                null,
                envelope.Payload,
                cancellationToken);

            return ProcessResult.Invalid(sw.Elapsed.TotalMilliseconds);
        }

        // Validate
        var validation = await _validator.ValidateAsync(vietfulProduct, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var err in validation.Errors)
            {
                await LogQualityErrorAsync(
                    tenantId,
                    vietfulProduct.Id,
                    vietfulProduct.Sku,
                    err.ErrorCode,
                    err.Message,
                    err.FieldName,
                    null,
                    envelope.Payload,
                    cancellationToken);
            }

            return ProcessResult.Invalid(sw.Elapsed.TotalMilliseconds);
        }

        try
        {
            // 1. Process ProductBase (shared manufacturer & regulatory info)
            Guid? baseId = null;
            if (!string.IsNullOrWhiteSpace(vietfulProduct.Barcode) || !string.IsNullOrWhiteSpace(vietfulProduct.Manufacturer))
            {
                var baseProduct = _mapper.MapVietfulToProductBase(vietfulProduct);
                var upserted = await _productBaseRepository.UpsertByGtinAsync(baseProduct, cancellationToken);
                baseId = upserted.Id;
            }

            // 2. Process Tenant Product
            var product = _mapper.MapVietfulToProduct(vietfulProduct, tenantId);
            product.ProductBaseId = baseId;
            product.SyncStatus = SyncStatus.Success;
            product.LastSyncedAt = DateTime.UtcNow;

            var existing = await _productRepository.GetBySkuAsync(tenantId, product.Sku, cancellationToken);
            if (existing != null)
            {
                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.Quantity = product.Quantity;
                existing.Unit = product.Unit;
                existing.Category = product.Category;
                existing.Brand = product.Brand;
                existing.Barcode = product.Barcode;
                existing.VietfulProductId = product.VietfulProductId;
                existing.ProductBaseId = baseId ?? existing.ProductBaseId;
                existing.ImagesJson = product.ImagesJson;
                existing.AttributesJson = product.AttributesJson;
                existing.SyncStatus = SyncStatus.Success;
                existing.LastSyncedAt = DateTime.UtcNow;
                existing.LastSyncError = null;

                await _productRepository.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                await _productRepository.AddAsync(product, cancellationToken);
            }

            sw.Stop();
            return ProcessResult.Success(sw.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Failed to persist Vietful product {Sku} for tenant {TenantId}", vietfulProduct.Sku, tenantId);

            await LogQualityErrorAsync(
                tenantId,
                vietfulProduct.Id,
                vietfulProduct.Sku,
                "PERSISTENCE_ERROR",
                ex.Message,
                null,
                null,
                envelope.Payload,
                cancellationToken);

            return ProcessResult.Failed(sw.Elapsed.TotalMilliseconds);
        }
    }

    private async Task LogQualityErrorAsync(
        Guid tenantId,
        string? externalId,
        string? sku,
        string errorCode,
        string errorMessage,
        string? fieldName,
        string? fieldValue,
        string rawPayload,
        CancellationToken cancellationToken)
    {
        var log = new DataQualityLog
        {
            TenantId = tenantId,
            WorkerType = WorkerType.VietfulPush,
            ExternalProductId = externalId,
            Sku = sku,
            SyncStatus = SyncStatus.InvalidData,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            FieldName = fieldName,
            FieldValue = fieldValue,
            RawPayloadJson = rawPayload.Length > 65535 ? rawPayload[..65535] : rawPayload,
            IsResolved = false
        };

        try
        {
            await _qualityLogRepository.AddAsync(log, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log data quality record for tenant {TenantId}", tenantId);
        }
    }
}

public class ProcessResult
{
    public bool IsSuccess { get; private init; }
    public bool IsInvalid { get; private init; }
    public bool IsFailure { get; private init; }
    public double LatencyMs { get; private init; }

    public static ProcessResult Success(double latencyMs) => new() { IsSuccess = true, LatencyMs = latencyMs };
    public static ProcessResult Invalid(double latencyMs) => new() { IsInvalid = true, LatencyMs = latencyMs };
    public static ProcessResult Failed(double latencyMs) => new() { IsFailure = true, LatencyMs = latencyMs };
}
