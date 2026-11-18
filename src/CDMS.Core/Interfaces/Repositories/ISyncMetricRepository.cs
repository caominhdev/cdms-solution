using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;

namespace CDMS.Core.Interfaces.Repositories;

/// <summary>
/// Provides data-access operations for <see cref="SyncMetric"/> records.
/// </summary>
public interface ISyncMetricRepository
{
    /// <summary>Retrieves a single metric record by its identifier.</summary>
    Task<SyncMetric?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a time-ordered list of metric records for a tenant and worker type
    /// within the specified UTC time range.
    /// </summary>
    /// <param name="tenantId">The tenant to query.</param>
    /// <param name="workerType">The worker type to filter on.</param>
    /// <param name="from">Inclusive start of the UTC time range.</param>
    /// <param name="to">Exclusive end of the UTC time range.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task<IReadOnlyList<SyncMetric>> ListAsync(
        Guid tenantId,
        WorkerType workerType,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent metric record for a tenant and worker type.</summary>
    Task<SyncMetric?> GetLatestAsync(Guid tenantId, WorkerType workerType, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new metric record.</summary>
    Task AddAsync(SyncMetric metric, CancellationToken cancellationToken = default);

    /// <summary>Inserts multiple metric records in a single batch.</summary>
    Task AddRangeAsync(IEnumerable<SyncMetric> metrics, CancellationToken cancellationToken = default);

    /// <summary>Deletes metric records older than the specified UTC cutoff date.</summary>
    /// <param name="cutoff">Records with <c>WindowEnd</c> before this date are deleted.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>The number of records deleted.</returns>
    Task<int> PurgeOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default);
}
