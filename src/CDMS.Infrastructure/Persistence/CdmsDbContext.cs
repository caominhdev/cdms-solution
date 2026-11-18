using CDMS.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CDMS.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for the CDMS multi-tenant data store.
/// Designed for high-performance operation on CockroachDB and PostgreSQL clusters.
/// </summary>
public class CdmsDbContext : DbContext
{
    public CdmsDbContext(DbContextOptions<CdmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductBase> ProductBases => Set<ProductBase>();
    public DbSet<WorkerConfiguration> WorkerConfigurations => Set<WorkerConfiguration>();
    public DbSet<WorkerCheckpoint> WorkerCheckpoints => Set<WorkerCheckpoint>();
    public DbSet<SyncMetric> SyncMetrics => Set<SyncMetric>();
    public DbSet<DataQualityLog> DataQualityLogs => Set<DataQualityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CdmsDbContext).Assembly);
    }
}
