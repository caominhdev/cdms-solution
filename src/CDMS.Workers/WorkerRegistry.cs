using System.Collections.Concurrent;
using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;
using CDMS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CDMS.Workers;

public class WorkerRegistry
{
    private readonly ConcurrentDictionary<string, IWorker> _workers = new();
    private readonly ILogger<WorkerRegistry> _logger;

    public WorkerRegistry(ILogger<WorkerRegistry> logger)
    {
        _logger = logger;
    }

    public void Register(IWorker worker)
    {
        _workers[worker.WorkerId] = worker;
        _logger.LogInformation("Registered worker {WorkerId} (Type: {Type}, Tenant: {TenantId})",
            worker.WorkerId, worker.WorkerType, worker.TenantId);
    }

    public bool Unregister(string workerId)
    {
        return _workers.TryRemove(workerId, out _);
    }

    public IWorker? GetWorker(string workerId)
    {
        _workers.TryGetValue(workerId, out var worker);
        return worker;
    }

    public IReadOnlyList<IWorker> GetAllWorkers() => _workers.Values.ToList();

    public IReadOnlyList<IWorker> GetWorkersByTenant(Guid tenantId) =>
        _workers.Values.Where(w => w.TenantId == tenantId).ToList();

    public async Task StartWorkerAsync(string workerId, CancellationToken cancellationToken = default)
    {
        if (_workers.TryGetValue(workerId, out var worker))
        {
            await worker.StartAsync(cancellationToken);
        }
        else
        {
            throw new KeyNotFoundException($"Worker with ID '{workerId}' not found in registry.");
        }
    }

    public async Task StopWorkerAsync(string workerId, CancellationToken cancellationToken = default)
    {
        if (_workers.TryGetValue(workerId, out var worker))
        {
            await worker.StopAsync(cancellationToken);
        }
        else
        {
            throw new KeyNotFoundException($"Worker with ID '{workerId}' not found in registry.");
        }
    }

    public async Task<WorkerCheckpoint?> GetCheckpointAsync(string workerId, CancellationToken cancellationToken = default)
    {
        if (_workers.TryGetValue(workerId, out var worker))
        {
            return await worker.GetCheckpointAsync(cancellationToken);
        }

        return null;
    }
}
