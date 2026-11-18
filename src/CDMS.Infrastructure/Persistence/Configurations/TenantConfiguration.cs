using CDMS.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CDMS.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id");

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(t => t.ShopifyStoreDomain)
            .HasColumnName("shopify_store_domain")
            .HasMaxLength(512);

        builder.Property(t => t.ShopifyAccessToken)
            .HasColumnName("shopify_access_token")
            .HasMaxLength(512);

        builder.Property(t => t.VietfulTenantId)
            .HasColumnName("vietful_tenant_id")
            .HasMaxLength(128);

        builder.Property(t => t.VietfulAccessToken)
            .HasColumnName("vietful_access_token")
            .HasMaxLength(512);

        builder.Property(t => t.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(t => t.VietfulTenantId)
            .HasDatabaseName("ix_tenants_vietful_tenant_id");

        builder.HasIndex(t => t.ShopifyStoreDomain)
            .HasDatabaseName("ix_tenants_shopify_store_domain");
    }
}
