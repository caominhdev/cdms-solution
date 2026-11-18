using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;
using CDMS.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CDMS.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly CdmsDbContext _context;

    public ProductRepository(CdmsDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductBase)
            .Include(p => p.Tenant)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetBySkuAsync(Guid tenantId, string sku, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductBase)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Sku == sku, cancellationToken);
    }

    public async Task<Product?> GetByVietfulIdAsync(Guid tenantId, string vietfulProductId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductBase)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.VietfulProductId == vietfulProductId, cancellationToken);
    }

    public async Task<Product?> GetByShopifyIdAsync(Guid tenantId, string shopifyProductId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductBase)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.ShopifyProductId == shopifyProductId, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListByTenantAsync(
        Guid tenantId,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductBase)
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.UpdatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListByStatusAsync(
        Guid tenantId,
        SyncStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Include(p => p.ProductBase)
            .Where(p => p.TenantId == tenantId && p.SyncStatus == status)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.Products.AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var list = products.ToList();
        foreach (var p in list)
        {
            p.CreatedAt = now;
            p.UpdatedAt = now;
        }

        await _context.Products.AddRangeAsync(list, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSyncStatusAsync(
        Guid id,
        SyncStatus status,
        DateTime syncedAt,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product != null)
        {
            product.SyncStatus = status;
            product.LastSyncedAt = syncedAt;
            product.LastSyncError = errorMessage;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => p.TenantId == tenantId)
            .CountAsync(cancellationToken);
    }
}
