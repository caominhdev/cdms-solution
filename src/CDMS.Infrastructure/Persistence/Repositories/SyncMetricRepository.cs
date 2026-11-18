using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;
using CDMS.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CDMS.Infrastructure.Persistence.Repositories;

public class SyncMetricRepository : ISyncMetricRepository
{
    private readonly CdmsDbContext _context;

    public SyncMetricRepository(CdmsDbContext context)
    {
        _context = context;
    }

    public async Task<SyncMetric?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SyncMetrics
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<SyncMetric>> ListAsync(
        Guid tenantId,
        WorkerType workerType,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _context.SyncMetrics
            .Where(m => m.TenantId == tenantId &&
                        m.WorkerType == workerType &&
                        m.WindowStart >= from &&
                        m.WindowEnd <= to)
            .OrderBy(m => m.WindowStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<SyncMetric?> GetLatestAsync(Guid tenantId, WorkerType workerType, CancellationToken cancellationToken = default)
    {
        return await _context.SyncMetrics
            .Where(m => m.TenantId == tenantId && m.WorkerType == workerType)
            .OrderByDescending(m => m.WindowEnd)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(SyncMetric metric, CancellationToken cancellationToken = default)
    {
        metric.CreatedAt = DateTime.UtcNow;
        metric.UpdatedAt = DateTime.UtcNow;
        await _context.SyncMetrics.AddAsync(metric, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<SyncMetric> metrics, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var list = metrics.ToList();
        foreach (var m in list)
        {
            m.CreatedAt = now;
            m.UpdatedAt = now;
        }

        await _context.SyncMetrics.AddRangeAsync(list, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> PurgeOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        var old = await _context.SyncMetrics
            .Where(m => m.WindowEnd < cutoff)
            .ToListAsync(cancellationToken);

        _context.SyncMetrics.RemoveRange(old);
        await _context.SaveChangesAsync(cancellationToken);
        return old.Count;
    }
}
