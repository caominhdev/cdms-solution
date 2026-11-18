using CDMS.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CDMS.Infrastructure.Persistence.Configurations;

public class ProductBaseConfiguration : IEntityTypeConfiguration<ProductBase>
{
    public void Configure(EntityTypeBuilder<ProductBase> builder)
    {
        builder.ToTable("product_bases");

        builder.HasKey(pb => pb.Id);

        builder.Property(pb => pb.Id)
            .HasColumnName("id");

        builder.Property(pb => pb.Gtin)
            .HasColumnName("gtin")
            .HasMaxLength(64);

        builder.Property(pb => pb.Barcode)
            .HasColumnName("barcode")
            .HasMaxLength(128);

        builder.Property(pb => pb.Manufacturer)
            .HasColumnName("manufacturer")
            .HasMaxLength(512);

        builder.Property(pb => pb.CountryOfOrigin)
            .HasColumnName("country_of_origin")
            .HasMaxLength(4);

        builder.Property(pb => pb.Brand)
            .HasColumnName("brand")
            .HasMaxLength(256);

        builder.Property(pb => pb.ManufacturerPartNumber)
            .HasColumnName("manufacturer_part_number")
            .HasMaxLength(256);

        builder.Property(pb => pb.StandardCategory)
            .HasColumnName("standard_category")
            .HasMaxLength(128);

        builder.Property(pb => pb.StandardCategoryName)
            .HasColumnName("standard_category_name")
            .HasMaxLength(512);

        builder.Property(pb => pb.RegulatoryInfoJson)
            .HasColumnName("regulatory_info_json")
            .HasColumnType("jsonb");

        builder.Property(pb => pb.CertificationsJson)
            .HasColumnName("certifications_json")
            .HasColumnType("jsonb");

        builder.Property(pb => pb.PhysicalAttributesJson)
            .HasColumnName("physical_attributes_json")
            .HasColumnType("jsonb");

        builder.Property(pb => pb.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(pb => pb.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Unique index on GTIN for cross-tenant product master deduplication
        builder.HasIndex(pb => pb.Gtin)
            .IsUnique()
            .HasDatabaseName("ix_product_bases_gtin")
            .HasFilter("gtin IS NOT NULL");

        builder.HasIndex(pb => pb.Brand)
            .HasDatabaseName("ix_product_bases_brand");

        builder.HasIndex(pb => pb.Manufacturer)
            .HasDatabaseName("ix_product_bases_manufacturer");
    }
}
