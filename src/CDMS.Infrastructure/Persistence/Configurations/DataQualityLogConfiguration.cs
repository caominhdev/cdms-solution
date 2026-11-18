using CDMS.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CDMS.Infrastructure.Persistence.Configurations;

public class DataQualityLogConfiguration : IEntityTypeConfiguration<DataQualityLog>
{
    public void Configure(EntityTypeBuilder<DataQualityLog> builder)
    {
        builder.ToTable("data_quality_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id");

        builder.Property(l => l.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(l => l.WorkerType)
            .HasColumnName("worker_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(l => l.ProductId)
            .HasColumnName("product_id");

        builder.Property(l => l.ExternalProductId)
            .HasColumnName("external_product_id")
            .HasMaxLength(256);

        builder.Property(l => l.Sku)
            .HasColumnName("sku")
            .HasMaxLength(128);

        builder.Property(l => l.SyncStatus)
            .HasColumnName("sync_status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(l => l.ErrorCode)
            .HasColumnName("error_code")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(l => l.ErrorMessage)
            .HasColumnName("error_message")
            .IsRequired();

        builder.Property(l => l.FieldName)
            .HasColumnName("field_name")
            .HasMaxLength(256);

        builder.Property(l => l.FieldValue)
            .HasColumnName("field_value")
            .HasMaxLength(1024);

        builder.Property(l => l.RawPayloadJson)
            .HasColumnName("raw_payload_json")
            .HasColumnType("jsonb");

        builder.Property(l => l.IsResolved)
            .HasColumnName("is_resolved")
            .HasDefaultValue(false);

        builder.Property(l => l.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(l => l.ResolutionNotes)
            .HasColumnName("resolution_notes");

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Indexes
        builder.HasIndex(l => new { l.TenantId, l.IsResolved })
            .HasDatabaseName("ix_data_quality_logs_tenant_resolved");

        builder.HasIndex(l => l.ErrorCode)
            .HasDatabaseName("ix_data_quality_logs_error_code");

        builder.HasIndex(l => l.CreatedAt)
            .HasDatabaseName("ix_data_quality_logs_created_at");
    }
}
