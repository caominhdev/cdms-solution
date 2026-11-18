using CDMS.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CDMS.Infrastructure.Persistence.Configurations;

public class WorkerCheckpointConfiguration : IEntityTypeConfiguration<WorkerCheckpoint>
{
    public void Configure(EntityTypeBuilder<WorkerCheckpoint> builder)
    {
        builder.ToTable("worker_checkpoints");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.WorkerId)
            .HasColumnName("worker_id")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(c => c.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(c => c.WorkerType)
            .HasColumnName("worker_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(c => c.LastProcessedAt)
            .HasColumnName("last_processed_at");

        builder.Property(c => c.LastCursor)
            .HasColumnName("last_cursor")
            .HasMaxLength(512);

        builder.Property(c => c.LastMessageOffset)
            .HasColumnName("last_message_offset");

        builder.Property(c => c.MetadataJson)
            .HasColumnName("metadata_json")
            .HasColumnType("jsonb");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Unique index on worker_id for deterministic resume
        builder.HasIndex(c => c.WorkerId)
            .IsUnique()
            .HasDatabaseName("ix_worker_checkpoints_worker_id");

        builder.HasIndex(c => new { c.TenantId, c.WorkerType })
            .HasDatabaseName("ix_worker_checkpoints_tenant_type");
    }
}
