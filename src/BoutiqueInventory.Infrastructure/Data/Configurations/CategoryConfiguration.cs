using BoutiqueInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoutiqueInventory.Infrastructure.Data.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.HasKey(c => c.Id);

        b.Property(c => c.Name).IsRequired().HasMaxLength(80);
        b.Property(c => c.Description).HasMaxLength(500);
        b.Property(c => c.CreatedAt).IsRequired();

        b.HasIndex(c => c.Name).IsUnique();
    }
}
