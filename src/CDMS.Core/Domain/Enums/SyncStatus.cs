namespace CDMS.Core.Domain.Enums;

/// <summary>
/// Represents the current synchronisation state of a product record or operation.
/// </summary>
public enum SyncStatus
{
    /// <summary>The record has been queued but not yet processed.</summary>
    Pending = 0,

    /// <summary>The record is currently being processed by a worker.</summary>
    InProgress = 1,

    /// <summary>The record was successfully synchronised to the target system.</summary>
    Success = 2,

    /// <summary>
    /// The sync attempt failed due to a transient or permanent error
    /// (e.g. API timeout, network failure). May be retried.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The record was rejected by the data-validation layer due to invalid or
    /// incomplete data. Manual review is required before it can be retried.
    /// </summary>
    InvalidData = 4,
}
