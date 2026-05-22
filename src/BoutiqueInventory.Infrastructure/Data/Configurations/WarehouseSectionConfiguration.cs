using BoutiqueInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoutiqueInventory.Infrastructure.Data.Configurations;

internal sealed class WarehouseSectionConfiguration : IEntityTypeConfiguration<WarehouseSection>
{
    public void Configure(EntityTypeBuilder<WarehouseSection> b)
    {
        b.ToTable("WarehouseSections");
        b.HasKey(s => s.Id);

        b.Property(s => s.Name).IsRequired().HasMaxLength(80);
        b.Property(s => s.WarehouseId).IsRequired();

        b.HasIndex(s => new { s.WarehouseId, s.Name }).IsUnique();

        b.HasMany(s => s.Products)
            .WithOne(p => p.WarehouseSection)
            .HasForeignKey(p => p.WarehouseSectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
