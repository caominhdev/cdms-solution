using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;
using CDMS.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CDMS.Infrastructure.Persistence.Repositories;

public class DataQualityLogRepository : IDataQualityLogRepository
{
    private readonly CdmsDbContext _context;

    public DataQualityLogRepository(CdmsDbContext context)
    {
        _context = context;
    }

    public async Task<DataQualityLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DataQualityLogs
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DataQualityLog>> ListUnresolvedByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.DataQualityLogs
            .Where(l => l.TenantId == tenantId && !l.IsResolved)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DataQualityLog>> ListByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.DataQualityLogs
            .Where(l => l.ProductId == productId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DataQualityLog>> ListAsync(
        Guid tenantId,
        WorkerType? workerType = null,
        string? errorCode = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataQualityLogs.Where(l => l.TenantId == tenantId);

        if (workerType.HasValue)
            query = query.Where(l => l.WorkerType == workerType.Value);

        if (!string.IsNullOrWhiteSpace(errorCode))
            query = query.Where(l => l.ErrorCode == errorCode);

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DataQualityLog log, CancellationToken cancellationToken = default)
    {
        log.CreatedAt = DateTime.UtcNow;
        log.UpdatedAt = DateTime.UtcNow;
        await _context.DataQualityLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<DataQualityLog> logs, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var list = logs.ToList();
        foreach (var l in list)
        {
            l.CreatedAt = now;
            l.UpdatedAt = now;
        }

        await _context.DataQualityLogs.AddRangeAsync(list, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ResolveAsync(Guid id, DateTime resolvedAt, string? notes = null, CancellationToken cancellationToken = default)
    {
        var log = await _context.DataQualityLogs.FindAsync(new object[] { id }, cancellationToken);
        if (log != null)
        {
            log.IsResolved = true;
            log.ResolvedAt = resolvedAt;
            log.ResolutionNotes = notes;
            log.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyDictionary<string, int>> CountUnresolvedByErrorCodeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var counts = await _context.DataQualityLogs
            .Where(l => l.TenantId == tenantId && !l.IsResolved)
            .GroupBy(l => l.ErrorCode)
            .Select(g => new { ErrorCode = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.ErrorCode, x => x.Count);
    }
}
