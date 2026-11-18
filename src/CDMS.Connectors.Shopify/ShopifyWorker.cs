using System.Diagnostics;
using System.Text.Json;
using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;
using CDMS.Core.Interfaces;
using CDMS.Core.Interfaces.Repositories;
using CDMS.Core.Models;
using CDMS.Core.Services;
using Microsoft.Extensions.Logging;

namespace CDMS.Connectors.Shopify;

public class ShopifyWorker : IWorker, IDisposable
{
    private readonly ShopifyWorkerOptions _options;
    private readonly ShopifyConnector _connector;
    private readonly IProductRepository _productRepository;
    private readonly IProductBaseRepository _productBaseRepository;
    private readonly IWorkerCheckpointRepository _checkpointRepository;
    private readonly ISyncMetricRepository _metricRepository;
    private readonly IDataQualityLogRepository _qualityLogRepository;
    private readonly IDataValidator<ShopifyProduct> _validator;
    private readonly ProductMappingService _mapper;
    private readonly ILogger<ShopifyWorker> _logger;

    private WorkerState _state = WorkerState.Idle;
    private CancellationTokenSource? _workerCts;
    private WorkerCheckpoint? _currentCheckpoint;
    private string? _lastCursor;

    // Window metrics
    private long _processedCount;
    private long _failedCount;
    private long _invalidCount;
    private double _totalLatencyMs;
    private DateTime _windowStart = DateTime.UtcNow;

    public string WorkerId => _options.WorkerId;
    public WorkerType WorkerType => WorkerType.ShopifyPull;
    public Guid TenantId => _options.TenantId;
    public WorkerState State => _state;

    public ShopifyWorker(
        ShopifyWorkerOptions options,
        ShopifyConnector connector,
        IProductRepository productRepository,
        IProductBaseRepository productBaseRepository,
        IWorkerCheckpointRepository checkpointRepository,
        ISyncMetricRepository metricRepository,
        IDataQualityLogRepository qualityLogRepository,
        IDataValidator<ShopifyProduct> validator,
        ProductMappingService mapper,
        ILogger<ShopifyWorker> logger)
    {
        _options = options;
        _connector = connector;
        _productRepository = productRepository;
        _productBaseRepository = productBaseRepository;
        _checkpointRepository = checkpointRepository;
        _metricRepository = metricRepository;
        _qualityLogRepository = qualityLogRepository;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_state == WorkerState.Running)
        {
            _logger.LogWarning("Worker {WorkerId} is already running", WorkerId);
            return;
        }

        _logger.LogInformation("Starting Shopify pull worker {WorkerId} for tenant {TenantId}...", WorkerId, TenantId);
        _workerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // 1. Load checkpoint for cursor-based resumption
        _currentCheckpoint = await _checkpointRepository.GetByWorkerIdAsync(WorkerId, cancellationToken);
        if (_currentCheckpoint != null)
        {
            _lastCursor = _currentCheckpoint.LastCursor;
            _logger.LogInformation(
                "Worker {WorkerId} resuming from checkpoint: LastCursor={Cursor}, LastProcessedAt={Time}",
                WorkerId, _lastCursor, _currentCheckpoint.LastProcessedAt);
        }
        else
        {
            _lastCursor = null;
            _currentCheckpoint = new WorkerCheckpoint
            {
                WorkerId = WorkerId,
                TenantId = TenantId,
                WorkerType = WorkerType.ShopifyPull
            };
            _logger.LogInformation("No checkpoint found for worker {WorkerId}. Starting pull from beginning.", WorkerId);
        }

        _windowStart = DateTime.UtcNow;
        _state = WorkerState.Running;

        // 2. Start polling loop in background
        _ = Task.Run(() => PollingLoopAsync(_workerCts.Token), _workerCts.Token);
    }

    private async Task PollingLoopAsync(CancellationToken ct)
    {
        _logger.LogInformation("Worker {WorkerId} polling loop started with interval {Interval}s", WorkerId, _options.PollingIntervalSeconds);

        while (!ct.IsCancellationRequested && _state == WorkerState.Running)
        {
            try
            {
                await ExecuteSyncCycleAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Shopify sync cycle for worker {WorkerId}", WorkerId);
            }

            try
            {
                _state = WorkerState.Paused;
                await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), ct);
                if (_state == WorkerState.Paused)
                    _state = WorkerState.Running;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _state = WorkerState.Stopped;
        _logger.LogInformation("Worker {WorkerId} polling loop exited.", WorkerId);
    }

    public async Task ExecuteSyncCycleAsync(CancellationToken ct)
    {
        bool hasNextPage = true;
        string? cursor = _lastCursor;

        _logger.LogInformation("Worker {WorkerId} beginning sync page fetch from cursor: {Cursor}", WorkerId, cursor ?? "(start)");

        while (hasNextPage && !ct.IsCancellationRequested)
        {
            var sw = Stopwatch.StartNew();
            var pageResult = await _connector.FetchPageAsync(cursor, _options.PageSize, ct);
            sw.Stop();

            if (!pageResult.IsSuccess)
            {
                _logger.LogError("Failed to fetch Shopify page: {Error}", pageResult.FirstError);
                _failedCount++;
                break;
            }

            var page = pageResult.Value;
            var batchSw = Stopwatch.StartNew();

            foreach (var shopifyProduct in page.Items)
            {
                await ProcessProductAsync(shopifyProduct, ct);
            }

            batchSw.Stop();
            _totalLatencyMs += sw.Elapsed.TotalMilliseconds + batchSw.Elapsed.TotalMilliseconds;

            cursor = page.NextCursor;
            hasNextPage = page.HasNextPage && !string.IsNullOrEmpty(cursor);

            // Update checkpoint
            _lastCursor = cursor;
            await SaveCheckpointInternalAsync(cursor);

            if (!hasNextPage)
            {
                _logger.LogInformation("Worker {WorkerId} completed all pages. Next sync in {Interval}s.", WorkerId, _options.PollingIntervalSeconds);
            }
        }

        await FlushMetricsAsync();
    }

    private async Task ProcessProductAsync(ShopifyProduct shopifyProduct, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(shopifyProduct, ct);
        if (!validation.IsValid)
        {
            foreach (var err in validation.Errors)
            {
                await LogQualityErrorAsync(
                    TenantId,
                    shopifyProduct.Id,
                    shopifyProduct.Handle,
                    err.ErrorCode,
                    err.Message,
                    err.FieldName,
                    JsonSerializer.Serialize(shopifyProduct),
                    ct);
            }

            _invalidCount++;
            return;
        }

        try
        {
            // 1. Process ProductBase (shared across tenants)
            Guid? baseId = null;
            var firstVariant = shopifyProduct.Variants.Nodes.FirstOrDefault();
            var barcode = firstVariant?.Barcode;

            if (!string.IsNullOrWhiteSpace(barcode) || !string.IsNullOrWhiteSpace(shopifyProduct.Vendor))
            {
                var baseProduct = _mapper.MapShopifyToProductBase(shopifyProduct);
                var upserted = await _productBaseRepository.UpsertByGtinAsync(baseProduct, ct);
                baseId = upserted.Id;
            }

            // 2. Process Tenant Product
            var product = _mapper.MapShopifyToProduct(shopifyProduct, TenantId);
            product.ProductBaseId = baseId;
            product.SyncStatus = SyncStatus.Success;
            product.LastSyncedAt = DateTime.UtcNow;

            var existing = await _productRepository.GetBySkuAsync(TenantId, product.Sku, ct);
            if (existing != null)
            {
                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.Quantity = product.Quantity;
                existing.Category = product.Category;
                existing.Brand = product.Brand;
                existing.Barcode = product.Barcode;
                existing.ShopifyProductId = product.ShopifyProductId;
                existing.ShopifyVariantId = product.ShopifyVariantId;
                existing.ProductBaseId = baseId ?? existing.ProductBaseId;
                existing.ImagesJson = product.ImagesJson;
                existing.AttributesJson = product.AttributesJson;
                existing.SyncStatus = SyncStatus.Success;
                existing.LastSyncedAt = DateTime.UtcNow;
                existing.LastSyncError = null;

                await _productRepository.UpdateAsync(existing, ct);
            }
            else
            {
                await _productRepository.AddAsync(product, ct);
            }

            _processedCount++;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist Shopify product {Id} for tenant {TenantId}", shopifyProduct.Id, TenantId);
            _failedCount++;

            await LogQualityErrorAsync(
                TenantId,
                shopifyProduct.Id,
                shopifyProduct.Handle,
                "PERSISTENCE_ERROR",
                ex.Message,
                null,
                JsonSerializer.Serialize(shopifyProduct),
                ct);
        }
    }

    private async Task LogQualityErrorAsync(
        Guid tenantId,
        string? externalId,
        string? sku,
        string errorCode,
        string errorMessage,
        string? fieldName,
        string rawPayload,
        CancellationToken ct)
    {
        var log = new DataQualityLog
        {
            TenantId = tenantId,
            WorkerType = WorkerType.ShopifyPull,
            ExternalProductId = externalId,
            Sku = sku,
            SyncStatus = SyncStatus.InvalidData,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            FieldName = fieldName,
            RawPayloadJson = rawPayload.Length > 65535 ? rawPayload[..65535] : rawPayload,
            IsResolved = false
        };

        try
        {
            await _qualityLogRepository.AddAsync(log, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log Shopify data quality issue for tenant {TenantId}", tenantId);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_state != WorkerState.Running && _state != WorkerState.Paused)
            return;

        _logger.LogInformation("Stopping Shopify pull worker {WorkerId}...", WorkerId);
        _state = WorkerState.Stopped;
        _workerCts?.Cancel();

        await SaveCheckpointInternalAsync(_lastCursor);
        await FlushMetricsAsync();

        _logger.LogInformation("Worker {WorkerId} stopped cleanly. Checkpoint saved at cursor {Cursor}.", WorkerId, _lastCursor ?? "(none)");
    }

    public async Task<WorkerCheckpoint?> GetCheckpointAsync(CancellationToken cancellationToken = default)
    {
        return await _checkpointRepository.GetByWorkerIdAsync(WorkerId, cancellationToken);
    }

    public async Task SaveCheckpointAsync(CancellationToken cancellationToken = default)
    {
        await SaveCheckpointInternalAsync(_lastCursor);
        await FlushMetricsAsync();
    }

    private async Task SaveCheckpointInternalAsync(string? cursor)
    {
        try
        {
            if (_currentCheckpoint != null)
            {
                _currentCheckpoint.LastProcessedAt = DateTime.UtcNow;
                _currentCheckpoint.LastCursor = cursor;
                _currentCheckpoint.MetadataJson = JsonSerializer.Serialize(new
                {
                    processed = _processedCount,
                    failed = _failedCount,
                    invalid = _invalidCount,
                    lastCursor = cursor
                });

                await _checkpointRepository.SaveAsync(_currentCheckpoint);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save checkpoint for worker {WorkerId}", WorkerId);
        }
    }

    private async Task FlushMetricsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var windowDurationSeconds = Math.Max(1.0, (now - _windowStart).TotalSeconds);
            var recordsInWindow = _processedCount + _failedCount + _invalidCount;

            if (recordsInWindow > 0)
            {
                var metric = new SyncMetric
                {
                    TenantId = TenantId,
                    WorkerType = WorkerType.ShopifyPull,
                    WindowStart = _windowStart,
                    WindowEnd = now,
                    RecordsProcessed = _processedCount,
                    RecordsFailed = _failedCount,
                    RecordsInvalid = _invalidCount,
                    AvgLatencyMs = _processedCount > 0 ? Math.Round(_totalLatencyMs / _processedCount, 2) : 0,
                    ThroughputPerSecond = Math.Round(recordsInWindow / windowDurationSeconds, 2)
                };

                await _metricRepository.AddAsync(metric);

                _windowStart = now;
                _processedCount = 0;
                _failedCount = 0;
                _invalidCount = 0;
                _totalLatencyMs = 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush metrics for worker {WorkerId}", WorkerId);
        }
    }

    public void Dispose()
    {
        _workerCts?.Dispose();
    }
}
