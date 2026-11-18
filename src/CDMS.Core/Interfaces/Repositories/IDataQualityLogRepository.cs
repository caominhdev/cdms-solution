using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;

namespace CDMS.Core.Interfaces.Repositories;

/// <summary>
/// Provides data-access operations for <see cref="DataQualityLog"/> records.
/// </summary>
public interface IDataQualityLogRepository
{
    /// <summary>Retrieves a single log entry by its identifier.</summary>
    Task<DataQualityLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all unresolved data quality log entries for a tenant.</summary>
    /// <param name="tenantId">The tenant to query.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task<IReadOnlyList<DataQualityLog>> ListUnresolvedByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Returns log entries for a specific product (by CDMS product ID).</summary>
    Task<IReadOnlyList<DataQualityLog>> ListByProductAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Returns log entries filtered by worker type and optionally by error code.</summary>
    /// <param name="tenantId">The tenant to query.</param>
    /// <param name="workerType">The worker type to filter on.</param>
    /// <param name="errorCode">Optional machine-readable error code filter.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Maximum number of records to return.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task<IReadOnlyList<DataQualityLog>> ListAsync(
        Guid tenantId,
        WorkerType? workerType = null,
        string? errorCode = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>Inserts a new data quality log entry.</summary>
    Task AddAsync(DataQualityLog log, CancellationToken cancellationToken = default);

    /// <summary>Inserts multiple log entries in a single batch.</summary>
    Task AddRangeAsync(IEnumerable<DataQualityLog> logs, CancellationToken cancellationToken = default);

    /// <summary>Marks a log entry as resolved.</summary>
    /// <param name="id">The log entry identifier.</param>
    /// <param name="resolvedAt">The UTC timestamp of resolution.</param>
    /// <param name="notes">Optional resolution notes.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task ResolveAsync(Guid id, DateTime resolvedAt, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>Returns the count of unresolved issues grouped by error code for a tenant.</summary>
    Task<IReadOnlyDictionary<string, int>> CountUnresolvedByErrorCodeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
