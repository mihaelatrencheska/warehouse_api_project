using BoutiqueInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoutiqueInventory.Infrastructure.Data.Configurations;

internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> b)
    {
        b.ToTable("ProductCategories");
        b.HasKey(pc => new { pc.ProductId, pc.CategoryId });

        b.HasOne(pc => pc.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(pc => pc.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(pc => pc.CategoryId);
    }
}
