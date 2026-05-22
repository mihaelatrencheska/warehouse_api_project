using BoutiqueInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoutiqueInventory.Infrastructure.Data.Configurations;

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> b)
    {
        b.ToTable("Warehouses");
        b.HasKey(w => w.Id);

        b.Property(w => w.Name).IsRequired().HasMaxLength(120);
        b.Property(w => w.Location).HasMaxLength(250);
        b.Property(w => w.IsActive).HasDefaultValue(true);
        b.Property(w => w.CreatedAt).IsRequired();

        b.HasIndex(w => w.Name).IsUnique();
        b.HasIndex(w => w.IsActive);

        b.HasMany(w => w.Sections)
            .WithOne(s => s.Warehouse)
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
