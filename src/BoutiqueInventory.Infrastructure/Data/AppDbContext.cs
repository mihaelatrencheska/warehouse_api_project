using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BoutiqueInventory.Infrastructure.Data;

/// <summary>
/// EF Core context. Implements <see cref="IUnitOfWork"/> so the
/// Application layer can commit work without referencing EF Core
/// types directly.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseSection> WarehouseSections => Set<WarehouseSection>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ExpirationAlert> ExpirationAlerts => Set<ExpirationAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampProductTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampProductTimestamps();
        return base.SaveChanges();
    }

    private void StampProductTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Product>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
