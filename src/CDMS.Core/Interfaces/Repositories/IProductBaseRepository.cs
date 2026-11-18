using CDMS.Core.Domain.Entities;

namespace CDMS.Core.Interfaces.Repositories;

/// <summary>
/// Provides data-access operations for shared <see cref="ProductBase"/> records.
/// </summary>
public interface IProductBaseRepository
{
    /// <summary>Retrieves a <see cref="ProductBase"/> by its CDMS primary key.</summary>
    Task<ProductBase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a <see cref="ProductBase"/> by its GTIN/barcode value.</summary>
    /// <param name="gtin">The GTIN or barcode string to look up.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task<ProductBase?> GetByGtinAsync(string gtin, CancellationToken cancellationToken = default);

    /// <summary>Returns all <see cref="ProductBase"/> records associated with a given brand.</summary>
    /// <param name="brand">The brand name to filter by.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task<IReadOnlyList<ProductBase>> ListByBrandAsync(string brand, CancellationToken cancellationToken = default);

    /// <summary>Returns all <see cref="ProductBase"/> records associated with a given manufacturer.</summary>
    /// <param name="manufacturer">The manufacturer name to filter by.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    Task<IReadOnlyList<ProductBase>> ListByManufacturerAsync(string manufacturer, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new <see cref="ProductBase"/> record.</summary>
    Task AddAsync(ProductBase productBase, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing <see cref="ProductBase"/> record.</summary>
    Task UpdateAsync(ProductBase productBase, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates a <see cref="ProductBase"/> record keyed on GTIN.</summary>
    /// <param name="productBase">The record to upsert.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns>The inserted or updated record.</returns>
    Task<ProductBase> UpsertByGtinAsync(ProductBase productBase, CancellationToken cancellationToken = default);

    /// <summary>Deletes a <see cref="ProductBase"/> record by its identifier.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
