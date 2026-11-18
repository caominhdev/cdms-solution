using CDMS.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CDMS.Infrastructure.Persistence.Configurations;

public class SyncMetricConfiguration : IEntityTypeConfiguration<SyncMetric>
{
    public void Configure(EntityTypeBuilder<SyncMetric> builder)
    {
        builder.ToTable("sync_metrics");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id");

        builder.Property(m => m.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(m => m.WorkerType)
            .HasColumnName("worker_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(m => m.WindowStart)
            .HasColumnName("window_start")
            .IsRequired();

        builder.Property(m => m.WindowEnd)
            .HasColumnName("window_end")
            .IsRequired();

        builder.Property(m => m.RecordsProcessed)
            .HasColumnName("records_processed")
            .HasDefaultValue(0L);

        builder.Property(m => m.RecordsFailed)
            .HasColumnName("records_failed")
            .HasDefaultValue(0L);

        builder.Property(m => m.RecordsInvalid)
            .HasColumnName("records_invalid")
            .HasDefaultValue(0L);

        builder.Property(m => m.AvgLatencyMs)
            .HasColumnName("avg_latency_ms")
            .HasPrecision(18, 2)
            .HasDefaultValue(0.0);

        builder.Property(m => m.ThroughputPerSecond)
            .HasColumnName("throughput_per_second")
            .HasPrecision(18, 2)
            .HasDefaultValue(0.0);

        builder.Property(m => m.PeakMemoryMb)
            .HasColumnName("peak_memory_mb");

        builder.Property(m => m.PeakCpuPercent)
            .HasColumnName("peak_cpu_percent");

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Indexes for time-series queries
        builder.HasIndex(m => new { m.TenantId, m.WorkerType, m.WindowStart })
            .HasDatabaseName("ix_sync_metrics_tenant_type_window");

        builder.HasIndex(m => m.CreatedAt)
            .HasDatabaseName("ix_sync_metrics_created_at");
    }
}
