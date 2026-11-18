using CDMS.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CDMS.Infrastructure.Persistence.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByVietfulTenantIdAsync(string vietfulTenantId, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByShopifyDomainAsync(string shopifyDomain, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> ListAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkerConfiguration>> GetWorkerConfigsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task AddWorkerConfigAsync(WorkerConfiguration config, CancellationToken cancellationToken = default);
    Task UpdateWorkerConfigAsync(WorkerConfiguration config, CancellationToken cancellationToken = default);
}

public class TenantRepository : ITenantRepository
{
    private readonly CdmsDbContext _context;

    public TenantRepository(CdmsDbContext context)
    {
        _context = context;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Include(t => t.WorkerConfigurations)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Tenant?> GetByVietfulTenantIdAsync(string vietfulTenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Include(t => t.WorkerConfigurations)
            .FirstOrDefaultAsync(t => t.VietfulTenantId == vietfulTenantId, cancellationToken);
    }

    public async Task<Tenant?> GetByShopifyDomainAsync(string shopifyDomain, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Include(t => t.WorkerConfigurations)
            .FirstOrDefaultAsync(t => t.ShopifyStoreDomain == shopifyDomain, cancellationToken);
    }

    public async Task<IReadOnlyList<Tenant>> ListAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Tenants.Include(t => t.WorkerConfigurations).AsQueryable();
        if (!includeInactive)
            query = query.Where(t => t.IsActive);

        return await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkerConfiguration>> GetWorkerConfigsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.WorkerConfigurations
            .Where(wc => wc.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        tenant.CreatedAt = DateTime.UtcNow;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _context.Tenants.AddAsync(tenant, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        tenant.UpdatedAt = DateTime.UtcNow;
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddWorkerConfigAsync(WorkerConfiguration config, CancellationToken cancellationToken = default)
    {
        config.CreatedAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;
        await _context.WorkerConfigurations.AddAsync(config, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateWorkerConfigAsync(WorkerConfiguration config, CancellationToken cancellationToken = default)
    {
        config.UpdatedAt = DateTime.UtcNow;
        _context.WorkerConfigurations.Update(config);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
