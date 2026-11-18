using CDMS.Core.Models;

namespace CDMS.Core.Interfaces;

/// <summary>
/// Generic connector abstraction for external data-source integrations.
/// Implementations must handle authentication, rate-limiting, and serialisation
/// for their respective external systems (Vietful, Shopify, etc.).
/// </summary>
/// <typeparam name="TResult">
/// The type of the domain/DTO object returned by connector operations.
/// </typeparam>
public interface IConnector<TResult>
{
    /// <summary>Gets the human-readable name of this connector (e.g. <c>"Vietful"</c>).</summary>
    string ConnectorName { get; }

    /// <summary>Gets the tenant identifier this connector instance is scoped to.</summary>
    Guid TenantId { get; }

    /// <summary>
    /// Fetches a single item by its external identifier.
    /// </summary>
    /// <param name="externalId">The identifier used by the external system.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>
    /// A <see cref="ConnectorResult{T}"/> containing the fetched item, or errors if the
    /// operation failed.
    /// </returns>
    Task<ConnectorResult<TResult>> FetchOneAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches a page of items from the external system, supporting cursor-based pagination.
    /// </summary>
    /// <param name="cursor">
    /// An opaque pagination cursor returned by the previous page call;
    /// pass <c>null</c> to fetch from the beginning.
    /// </param>
    /// <param name="pageSize">Maximum number of items to include in the page.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>
    /// A <see cref="ConnectorResult{T}"/> wrapping a <see cref="ConnectorPage{TResult}"/>
    /// that includes the items and the next cursor.
    /// </returns>
    Task<ConnectorResult<ConnectorPage<TResult>>> FetchPageAsync(
        string? cursor,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes (creates or updates) an item in the external system.
    /// </summary>
    /// <param name="item">The item to push.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>
    /// A <see cref="ConnectorResult{T}"/> containing the system's view of the item after
    /// the operation, or errors if the push failed.
    /// </returns>
    Task<ConnectorResult<TResult>> PushAsync(TResult item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity and authentication with the external system.
    /// </summary>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns><c>true</c> if the connector can reach and authenticate with the system.</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a single page of results from a connector's paginated fetch operation.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class ConnectorPage<T>
{
    /// <summary>Gets or sets the items in this page.</summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// Gets or sets the cursor to pass to the next page call, or <c>null</c>
    /// when there are no more pages.
    /// </summary>
    public string? NextCursor { get; init; }

    /// <summary>Gets or sets a value indicating whether more pages are available.</summary>
    public bool HasNextPage => NextCursor is not null;

    /// <summary>Gets or sets the total number of items available, if known.</summary>
    public int? TotalCount { get; init; }
}
