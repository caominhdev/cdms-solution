using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;

namespace CDMS.Core.Interfaces.Repositories;

/// <summary>
/// Provides data-access operations for <see cref="WorkerCheckpoint"/> records,
/// enabling workers to persist and retrieve their resumable state.
/// </summary>
public interface IWorkerCheckpointRepository
{
    /// <summary>
    /// Retrieves the checkpoint for a specific worker instance.
    /// </summary>
    /// <param name="workerId">The logical worker identifier.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>The <see cref="WorkerCheckpoint"/>, or <c>null</c> if none exists yet.</returns>
    Task<WorkerCheckpoint?> GetByWorkerIdAsync(string workerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the checkpoint for a tenant/worker-type combination.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="workerType">The worker type.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>The <see cref="WorkerCheckpoint"/>, or <c>null</c> if none exists.</returns>
    Task<WorkerCheckpoint?> GetByTenantAndTypeAsync(
        Guid tenantId,
        WorkerType workerType,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all checkpoints for a given tenant.</summary>
    Task<IReadOnlyList<WorkerCheckpoint>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates a checkpoint (upsert by <see cref="WorkerCheckpoint.WorkerId"/>).</summary>
    /// <param name="checkpoint">The checkpoint to save.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>The saved checkpoint.</returns>
    Task<WorkerCheckpoint> SaveAsync(WorkerCheckpoint checkpoint, CancellationToken cancellationToken = default);

    /// <summary>Deletes the checkpoint for a worker instance.</summary>
    /// <param name="workerId">The logical worker identifier.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task DeleteAsync(string workerId, CancellationToken cancellationToken = default);
}
