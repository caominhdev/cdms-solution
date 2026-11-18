using System.Text.Json;
using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;
using CDMS.Core.Interfaces;
using CDMS.Core.Interfaces.Repositories;
using CDMS.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace CDMS.Connectors.Vietful;

public class VietfulWorker : IWorker, IDisposable
{
    private readonly VietfulWorkerOptions _options;
    private readonly IMessageConsumer _consumer;
    private readonly VietfulMessageProcessor _processor;
    private readonly IWorkerCheckpointRepository _checkpointRepository;
    private readonly ISyncMetricRepository _metricRepository;
    private readonly ILogger<VietfulWorker> _logger;

    private WorkerState _state = WorkerState.Idle;
    private CancellationTokenSource? _workerCts;
    private WorkerCheckpoint? _currentCheckpoint;

    // In-memory metrics counters for the active window
    private long _processedCount;
    private long _failedCount;
    private long _invalidCount;
    private double _totalLatencyMs;
    private DateTime _windowStart = DateTime.UtcNow;
    private long _lastOffset;

    public string WorkerId => _options.WorkerId;
    public WorkerType WorkerType => WorkerType.VietfulPush;
    public Guid TenantId => _options.TenantId;
    public WorkerState State => _state;

    public VietfulWorker(
        VietfulWorkerOptions options,
        IMessageConsumer consumer,
        VietfulMessageProcessor processor,
        IWorkerCheckpointRepository checkpointRepository,
        ISyncMetricRepository metricRepository,
        ILogger<VietfulWorker> logger)
    {
        _options = options;
        _consumer = consumer;
        _processor = processor;
        _checkpointRepository = checkpointRepository;
        _metricRepository = metricRepository;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_state == WorkerState.Running)
        {
            _logger.LogWarning("Worker {WorkerId} is already running", WorkerId);
            return;
        }

        _logger.LogInformation("Starting Vietful push worker {WorkerId} for tenant {TenantId}...", WorkerId, TenantId);
        _workerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // 1. Load checkpoint for deterministic resume
        _currentCheckpoint = await _checkpointRepository.GetByWorkerIdAsync(WorkerId, cancellationToken);
        if (_currentCheckpoint != null)
        {
            _lastOffset = _currentCheckpoint.LastMessageOffset ?? 0;
            _logger.LogInformation(
                "Worker {WorkerId} resuming from checkpoint: LastOffset={Offset}, LastProcessedAt={Time}",
                WorkerId, _lastOffset, _currentCheckpoint.LastProcessedAt);
        }
        else
        {
            _lastOffset = 0;
            _currentCheckpoint = new WorkerCheckpoint
            {
                WorkerId = WorkerId,
                TenantId = TenantId,
                WorkerType = WorkerType.VietfulPush,
                LastMessageOffset = 0
            };
            _logger.LogInformation("No checkpoint found for worker {WorkerId}. Starting from offset 0.", WorkerId);
        }

        _windowStart = DateTime.UtcNow;
        _state = WorkerState.Running;

        // 2. Subscribe to RabbitMQ queue
        var tenantRoutingKey = $"tenant.{TenantId:N}.inventory.#";
        await _consumer.SubscribeAsync(
            _options.QueueName,
            _options.ExchangeName,
            tenantRoutingKey,
            HandleMessageAsync,
            _workerCts.Token);

        _logger.LogInformation("Worker {WorkerId} subscribed to {Queue} on {Exchange}", WorkerId, _options.QueueName, _options.ExchangeName);
    }

    public async Task<bool> HandleMessageAsync(MessageEnvelope envelope, ulong deliveryTag)
    {
        if (_state != WorkerState.Running)
            return false;

        // Checkpoint filter: ignore duplicate deliveries already processed
        if (envelope.SequenceNumber > 0 && envelope.SequenceNumber <= _lastOffset)
        {
            _logger.LogDebug("Skipping duplicate message {SequenceNumber} (already processed up to {Offset})", envelope.SequenceNumber, _lastOffset);
            return true;
        }

        var result = await _processor.ProcessAsync(envelope, TenantId, _workerCts?.Token ?? CancellationToken.None);

        lock (this)
        {
            if (result.IsSuccess)
                _processedCount++;
            else if (result.IsInvalid)
                _invalidCount++;
            else
                _failedCount++;

            _totalLatencyMs += result.LatencyMs;

            if (envelope.SequenceNumber > 0)
                _lastOffset = envelope.SequenceNumber;
            else
                _lastOffset = (long)deliveryTag;
        }

        // Periodic checkpoint
        var total = _processedCount + _failedCount + _invalidCount;
        if (total > 0 && total % _options.CheckpointIntervalRecords == 0)
        {
            await FlushCheckpointAndMetricsAsync();
        }

        return result.IsSuccess || result.IsInvalid; // Acknowledge invalid data (it's captured in DLQ/log)
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_state != WorkerState.Running)
            return;

        _logger.LogInformation("Stopping Vietful push worker {WorkerId}...", WorkerId);
        _state = WorkerState.Stopped;

        _workerCts?.Cancel();

        // Flush remaining state
        await FlushCheckpointAndMetricsAsync();

        _logger.LogInformation("Worker {WorkerId} stopped cleanly. Checkpoint saved at offset {Offset}.", WorkerId, _lastOffset);
    }

    public async Task<WorkerCheckpoint?> GetCheckpointAsync(CancellationToken cancellationToken = default)
    {
        return await _checkpointRepository.GetByWorkerIdAsync(WorkerId, cancellationToken);
    }

    public async Task SaveCheckpointAsync(CancellationToken cancellationToken = default)
    {
        await FlushCheckpointAndMetricsAsync();
    }

    private async Task FlushCheckpointAndMetricsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;

            // Save checkpoint
            if (_currentCheckpoint != null)
            {
                _currentCheckpoint.LastProcessedAt = now;
                _currentCheckpoint.LastMessageOffset = _lastOffset;
                _currentCheckpoint.MetadataJson = JsonSerializer.Serialize(new
                {
                    processed = _processedCount,
                    failed = _failedCount,
                    invalid = _invalidCount,
                    lastOffset = _lastOffset
                });

                await _checkpointRepository.SaveAsync(_currentCheckpoint);
            }

            // Save metrics snapshot if any records were processed
            var windowDurationSeconds = Math.Max(1.0, (now - _windowStart).TotalSeconds);
            var recordsInWindow = _processedCount + _failedCount + _invalidCount;

            if (recordsInWindow > 0)
            {
                var metric = new SyncMetric
                {
                    TenantId = TenantId,
                    WorkerType = WorkerType.VietfulPush,
                    WindowStart = _windowStart,
                    WindowEnd = now,
                    RecordsProcessed = _processedCount,
                    RecordsFailed = _failedCount,
                    RecordsInvalid = _invalidCount,
                    AvgLatencyMs = _processedCount > 0 ? Math.Round(_totalLatencyMs / _processedCount, 2) : 0,
                    ThroughputPerSecond = Math.Round(recordsInWindow / windowDurationSeconds, 2)
                };

                await _metricRepository.AddAsync(metric);

                // Reset window counters
                _windowStart = now;
                _processedCount = 0;
                _failedCount = 0;
                _invalidCount = 0;
                _totalLatencyMs = 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush checkpoint and metrics for worker {WorkerId}", WorkerId);
        }
    }

    public void Dispose()
    {
        _workerCts?.Dispose();
        _consumer?.Dispose();
    }
}
