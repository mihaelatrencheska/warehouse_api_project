using BoutiqueInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoutiqueInventory.Infrastructure.Data.Configurations;

internal sealed class ExpirationAlertConfiguration : IEntityTypeConfiguration<ExpirationAlert>
{
    public void Configure(EntityTypeBuilder<ExpirationAlert> b)
    {
        b.ToTable("ExpirationAlerts");
        b.HasKey(a => a.Id);

        b.Property(a => a.AlertDate).IsRequired();
        b.Property(a => a.DaysUntilExpiration).IsRequired();
        b.Property(a => a.IsAcknowledged).HasDefaultValue(false);

        b.HasIndex(a => a.IsAcknowledged);
        b.HasIndex(a => a.AlertDate);

        b.HasOne(a => a.Product)
            .WithMany()
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
