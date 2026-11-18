using CDMS.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CDMS.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(p => p.ProductBaseId)
            .HasColumnName("product_base_id");

        builder.Property(p => p.Sku)
            .HasColumnName("sku")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(p => p.VietfulProductId)
            .HasColumnName("vietful_product_id")
            .HasMaxLength(256);

        builder.Property(p => p.ShopifyProductId)
            .HasColumnName("shopify_product_id")
            .HasMaxLength(256);

        builder.Property(p => p.ShopifyVariantId)
            .HasColumnName("shopify_variant_id")
            .HasMaxLength(256);

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description");

        builder.Property(p => p.Price)
            .HasColumnName("price")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasColumnName("currency")
            .HasMaxLength(8)
            .HasDefaultValue("VND")
            .IsRequired();

        builder.Property(p => p.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(p => p.Unit)
            .HasColumnName("unit")
            .HasMaxLength(64);

        builder.Property(p => p.Category)
            .HasColumnName("category")
            .HasMaxLength(512);

        builder.Property(p => p.Brand)
            .HasColumnName("brand")
            .HasMaxLength(256);

        builder.Property(p => p.Barcode)
            .HasColumnName("barcode")
            .HasMaxLength(64);

        builder.Property(p => p.ImagesJson)
            .HasColumnName("images_json")
            .HasColumnType("jsonb");

        builder.Property(p => p.AttributesJson)
            .HasColumnName("attributes_json")
            .HasColumnType("jsonb");

        builder.Property(p => p.SyncStatus)
            .HasColumnName("sync_status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.LastSyncedAt)
            .HasColumnName("last_synced_at");

        builder.Property(p => p.LastSyncError)
            .HasColumnName("last_sync_error");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Foreign keys
        builder.HasOne(p => p.Tenant)
            .WithMany(t => t.Products)
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.ProductBase)
            .WithMany(pb => pb.Products)
            .HasForeignKey(p => p.ProductBaseId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for multi-tenant querying
        builder.HasIndex(p => new { p.TenantId, p.Sku })
            .IsUnique()
            .HasDatabaseName("ix_products_tenant_sku");

        builder.HasIndex(p => new { p.TenantId, p.VietfulProductId })
            .HasDatabaseName("ix_products_tenant_vietful_id");

        builder.HasIndex(p => new { p.TenantId, p.ShopifyProductId })
            .HasDatabaseName("ix_products_tenant_shopify_id");

        builder.HasIndex(p => new { p.TenantId, p.SyncStatus })
            .HasDatabaseName("ix_products_tenant_sync_status");

        builder.HasIndex(p => p.Barcode)
            .HasDatabaseName("ix_products_barcode");
    }
}
