using BoutiqueInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoutiqueInventory.Infrastructure.Data.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products");
        b.HasKey(p => p.Id);

        b.Property(p => p.Name).IsRequired().HasMaxLength(150);
        b.Property(p => p.Sku).IsRequired().HasMaxLength(60);
        b.Property(p => p.Description).HasMaxLength(1000);
        b.Property(p => p.Size).HasMaxLength(40);
        b.Property(p => p.Type).HasMaxLength(80);
        b.Property(p => p.ImageUrl).HasMaxLength(500);
        b.Property(p => p.ImageMetadata).HasColumnType("TEXT");
        b.Property(p => p.CreatedAt).IsRequired();
        b.Property(p => p.UpdatedAt).IsRequired();

        b.HasIndex(p => p.Sku).IsUnique();
        b.HasIndex(p => p.Name);
        b.HasIndex(p => p.ExpirationDate);
        b.HasIndex(p => p.WarehouseSectionId);

        b.HasMany(p => p.Categories)
            .WithOne(pc => pc.Product)
            .HasForeignKey(pc => pc.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
