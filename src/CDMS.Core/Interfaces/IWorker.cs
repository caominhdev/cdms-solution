using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;

namespace CDMS.Core.Interfaces;

/// <summary>
/// Defines the contract for a background sync worker that can be started, stopped,
/// and resumed from a persisted checkpoint.
/// </summary>
public interface IWorker
{
    /// <summary>Gets the stable logical identifier for this worker instance.</summary>
    string WorkerId { get; }

    /// <summary>Gets the type of this worker.</summary>
    WorkerType WorkerType { get; }

    /// <summary>Gets the tenant this worker is operating on behalf of.</summary>
    Guid TenantId { get; }

    /// <summary>Gets the current operational state of the worker.</summary>
    WorkerState State { get; }

    /// <summary>
    /// Starts the worker. If a valid <see cref="WorkerCheckpoint"/> exists in the
    /// persistence store, the worker resumes from that checkpoint rather than starting
    /// from the beginning.
    /// </summary>
    /// <param name="cancellationToken">
    /// Signals that the host wants the worker to stop; the worker should honour
    /// cancellation promptly and persist its current checkpoint before exiting.
    /// </param>
    /// <returns>A <see cref="Task"/> that completes when the worker has fully stopped.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a graceful stop of the worker. The worker completes any in-flight
    /// record and then persists its checkpoint before returning.
    /// </summary>
    /// <param name="cancellationToken">
    /// Signals that the host wants the worker to abort immediately if graceful stop
    /// takes too long.
    /// </param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recently persisted checkpoint for this worker.
    /// Returns <c>null</c> when no checkpoint has been saved yet.
    /// </summary>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task<WorkerCheckpoint?> GetCheckpointAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces the worker to persist its current state as a checkpoint immediately,
    /// regardless of the normal checkpoint interval.
    /// </summary>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task SaveCheckpointAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the operational state of a background sync worker.
/// </summary>
public enum WorkerState
{
    /// <summary>The worker has been created but has not yet started.</summary>
    Idle = 0,

    /// <summary>The worker is actively processing records.</summary>
    Running = 1,

    /// <summary>The worker is paused (e.g. waiting for the next polling interval).</summary>
    Paused = 2,

    /// <summary>The worker has been gracefully stopped.</summary>
    Stopped = 3,

    /// <summary>The worker has encountered a fatal error and cannot continue without intervention.</summary>
    Faulted = 4,
}
