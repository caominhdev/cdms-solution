using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;

namespace CDMS.Core.Interfaces.Repositories;

/// <summary>
/// Provides data-access operations for tenant-specific <see cref="Product"/> records.
/// </summary>
public interface IProductRepository
{
    /// <summary>Retrieves a product by its CDMS primary key.</summary>
    /// <param name="id">The product's CDMS identifier.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>The <see cref="Product"/>, or <c>null</c> if not found.</returns>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a product by tenant and SKU.</summary>
    /// <param name="tenantId">The owning tenant's identifier.</param>
    /// <param name="sku">The product's SKU.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>The matching <see cref="Product"/>, or <c>null</c> if not found.</returns>
    Task<Product?> GetBySkuAsync(Guid tenantId, string sku, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a product by its Vietful platform product ID.</summary>
    /// <param name="tenantId">The owning tenant's identifier.</param>
    /// <param name="vietfulProductId">The Vietful product ID.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>The matching <see cref="Product"/>, or <c>null</c> if not found.</returns>
    Task<Product?> GetByVietfulIdAsync(Guid tenantId, string vietfulProductId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a product by its Shopify product GID.</summary>
    /// <param name="tenantId">The owning tenant's identifier.</param>
    /// <param name="shopifyProductId">The Shopify product GID.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>The matching <see cref="Product"/>, or <c>null</c> if not found.</returns>
    Task<Product?> GetByShopifyIdAsync(Guid tenantId, string shopifyProductId, CancellationToken cancellationToken = default);

    /// <summary>Returns a paginated list of products for a given tenant.</summary>
    /// <param name="tenantId">The tenant to query.</param>
    /// <param name="skip">Number of records to skip (for offset pagination).</param>
    /// <param name="take">Maximum number of records to return.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task<IReadOnlyList<Product>> ListByTenantAsync(
        Guid tenantId,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all products for a tenant that have a specific sync status.</summary>
    /// <param name="tenantId">The tenant to query.</param>
    /// <param name="status">The desired sync status.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task<IReadOnlyList<Product>> ListByStatusAsync(
        Guid tenantId,
        SyncStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>Inserts a new product record.</summary>
    /// <param name="product">The product to insert.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>Inserts multiple product records in a single batch.</summary>
    /// <param name="products">The products to insert.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task AddRangeAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing product record.</summary>
    /// <param name="product">The product with updated values.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>Updates the sync status and related fields of a product.</summary>
    /// <param name="id">The CDMS product identifier.</param>
    /// <param name="status">The new sync status.</param>
    /// <param name="syncedAt">The UTC timestamp of the sync attempt.</param>
    /// <param name="errorMessage">An error message if the sync failed; otherwise <c>null</c>.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task UpdateSyncStatusAsync(
        Guid id,
        SyncStatus status,
        DateTime syncedAt,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a product record by its identifier.</summary>
    /// <param name="id">The CDMS product identifier.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the total count of products for a tenant.</summary>
    /// <param name="tenantId">The tenant to count products for.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
