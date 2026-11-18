using CDMS.Core.Domain.Entities;
using CDMS.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CDMS.Infrastructure.Persistence.Repositories;

public class ProductBaseRepository : IProductBaseRepository
{
    private readonly CdmsDbContext _context;

    public ProductBaseRepository(CdmsDbContext context)
    {
        _context = context;
    }

    public async Task<ProductBase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductBases
            .FirstOrDefaultAsync(pb => pb.Id == id, cancellationToken);
    }

    public async Task<ProductBase?> GetByGtinAsync(string gtin, CancellationToken cancellationToken = default)
    {
        return await _context.ProductBases
            .FirstOrDefaultAsync(pb => pb.Gtin == gtin || pb.Barcode == gtin, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductBase>> ListByBrandAsync(string brand, CancellationToken cancellationToken = default)
    {
        return await _context.ProductBases
            .Where(pb => pb.Brand == brand)
            .OrderBy(pb => pb.StandardCategory)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductBase>> ListByManufacturerAsync(string manufacturer, CancellationToken cancellationToken = default)
    {
        return await _context.ProductBases
            .Where(pb => pb.Manufacturer == manufacturer)
            .OrderBy(pb => pb.Brand)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProductBase productBase, CancellationToken cancellationToken = default)
    {
        productBase.CreatedAt = DateTime.UtcNow;
        productBase.UpdatedAt = DateTime.UtcNow;
        await _context.ProductBases.AddAsync(productBase, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProductBase productBase, CancellationToken cancellationToken = default)
    {
        productBase.UpdatedAt = DateTime.UtcNow;
        _context.ProductBases.Update(productBase);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductBase> UpsertByGtinAsync(ProductBase productBase, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productBase.Gtin) && string.IsNullOrWhiteSpace(productBase.Barcode))
        {
            await AddAsync(productBase, cancellationToken);
            return productBase;
        }

        var lookup = productBase.Gtin ?? productBase.Barcode;
        var existing = await GetByGtinAsync(lookup!, cancellationToken);

        if (existing == null)
        {
            await AddAsync(productBase, cancellationToken);
            return productBase;
        }

        // Merge attributes into existing master record
        if (!string.IsNullOrWhiteSpace(productBase.Manufacturer))
            existing.Manufacturer = productBase.Manufacturer;

        if (!string.IsNullOrWhiteSpace(productBase.CountryOfOrigin))
            existing.CountryOfOrigin = productBase.CountryOfOrigin;

        if (!string.IsNullOrWhiteSpace(productBase.Brand))
            existing.Brand = productBase.Brand;

        if (!string.IsNullOrWhiteSpace(productBase.ManufacturerPartNumber))
            existing.ManufacturerPartNumber = productBase.ManufacturerPartNumber;

        if (!string.IsNullOrWhiteSpace(productBase.StandardCategory))
            existing.StandardCategory = productBase.StandardCategory;

        if (!string.IsNullOrWhiteSpace(productBase.StandardCategoryName))
            existing.StandardCategoryName = productBase.StandardCategoryName;

        if (!string.IsNullOrWhiteSpace(productBase.RegulatoryInfoJson))
            existing.RegulatoryInfoJson = productBase.RegulatoryInfoJson;

        if (!string.IsNullOrWhiteSpace(productBase.CertificationsJson))
            existing.CertificationsJson = productBase.CertificationsJson;

        if (!string.IsNullOrWhiteSpace(productBase.PhysicalAttributesJson))
            existing.PhysicalAttributesJson = productBase.PhysicalAttributesJson;

        existing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var pb = await _context.ProductBases.FindAsync(new object[] { id }, cancellationToken);
        if (pb != null)
        {
            _context.ProductBases.Remove(pb);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
