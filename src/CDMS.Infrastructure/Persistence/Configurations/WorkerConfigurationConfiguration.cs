using CDMS.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CDMS.Infrastructure.Persistence.Configurations;

public class WorkerConfigurationConfiguration : IEntityTypeConfiguration<WorkerConfiguration>
{
    public void Configure(EntityTypeBuilder<WorkerConfiguration> builder)
    {
        builder.ToTable("worker_configurations");

        builder.HasKey(wc => wc.Id);

        builder.Property(wc => wc.Id)
            .HasColumnName("id");

        builder.Property(wc => wc.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(wc => wc.WorkerType)
            .HasColumnName("worker_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(wc => wc.WorkerName)
            .HasColumnName("worker_name")
            .HasMaxLength(128);

        builder.Property(wc => wc.PollingIntervalSeconds)
            .HasColumnName("polling_interval_seconds")
            .HasDefaultValue(60);

        builder.Property(wc => wc.BatchSize)
            .HasColumnName("batch_size")
            .HasDefaultValue(100);

        builder.Property(wc => wc.MaxRetryAttempts)
            .HasColumnName("max_retry_attempts")
            .HasDefaultValue(3);

        builder.Property(wc => wc.RetryBackoffSeconds)
            .HasColumnName("retry_backoff_seconds")
            .HasDefaultValue(5);

        builder.Property(wc => wc.IsEnabled)
            .HasColumnName("is_enabled")
            .HasDefaultValue(true);

        builder.Property(wc => wc.ExtendedConfigJson)
            .HasColumnName("extended_config_json")
            .HasColumnType("jsonb");

        builder.Property(wc => wc.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(wc => wc.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(wc => wc.Tenant)
            .WithMany(t => t.WorkerConfigurations)
            .HasForeignKey(wc => wc.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(wc => new { wc.TenantId, wc.WorkerType })
            .HasDatabaseName("ix_worker_configs_tenant_type");
    }
}
