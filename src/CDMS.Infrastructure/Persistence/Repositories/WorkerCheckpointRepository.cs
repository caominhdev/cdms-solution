using CDMS.Core.Domain.Entities;
using CDMS.Core.Domain.Enums;
using CDMS.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CDMS.Infrastructure.Persistence.Repositories;

public class WorkerCheckpointRepository : IWorkerCheckpointRepository
{
    private readonly CdmsDbContext _context;

    public WorkerCheckpointRepository(CdmsDbContext context)
    {
        _context = context;
    }

    public async Task<WorkerCheckpoint?> GetByWorkerIdAsync(string workerId, CancellationToken cancellationToken = default)
    {
        return await _context.WorkerCheckpoints
            .FirstOrDefaultAsync(c => c.WorkerId == workerId, cancellationToken);
    }

    public async Task<WorkerCheckpoint?> GetByTenantAndTypeAsync(
        Guid tenantId,
        WorkerType workerType,
        CancellationToken cancellationToken = default)
    {
        return await _context.WorkerCheckpoints
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.WorkerType == workerType, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkerCheckpoint>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.WorkerCheckpoints
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.WorkerType)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkerCheckpoint> SaveAsync(WorkerCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        var existing = await _context.WorkerCheckpoints
            .FirstOrDefaultAsync(c => c.WorkerId == checkpoint.WorkerId, cancellationToken);

        if (existing == null)
        {
            checkpoint.UpdatedAt = DateTime.UtcNow;
            await _context.WorkerCheckpoints.AddAsync(checkpoint, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return checkpoint;
        }

        existing.TenantId = checkpoint.TenantId;
        existing.WorkerType = checkpoint.WorkerType;
        existing.LastProcessedAt = checkpoint.LastProcessedAt;
        existing.LastCursor = checkpoint.LastCursor;
        existing.LastMessageOffset = checkpoint.LastMessageOffset;
        existing.MetadataJson = checkpoint.MetadataJson;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteAsync(string workerId, CancellationToken cancellationToken = default)
    {
        var checkpoint = await _context.WorkerCheckpoints
            .FirstOrDefaultAsync(c => c.WorkerId == workerId, cancellationToken);

        if (checkpoint != null)
        {
            _context.WorkerCheckpoints.Remove(checkpoint);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
