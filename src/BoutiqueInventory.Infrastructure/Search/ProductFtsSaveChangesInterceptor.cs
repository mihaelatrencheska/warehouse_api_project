using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BoutiqueInventory.Infrastructure.Search;

/// <summary>Keeps the FTS5 index aligned with product writes.</summary>
public sealed class ProductFtsSaveChangesInterceptor(IProductSearchIndex index) : SaveChangesInterceptor
{
    private readonly List<Product> _upserts = [];
    private readonly List<Guid> _deletes = [];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _upserts.Clear();
        _deletes.Clear();

        if (eventData.Context is not null)
        {
            foreach (var entry in eventData.Context.ChangeTracker.Entries<Product>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                    case EntityState.Modified:
                        _upserts.Add(entry.Entity);
                        break;
                    case EntityState.Deleted:
                        _deletes.Add(entry.Entity.Id);
                        break;
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var saved = await base.SavedChangesAsync(eventData, result, cancellationToken);

        if (result <= 0)
        {
            return saved;
        }

        foreach (var id in _deletes)
        {
            await index.DeleteAsync(id, cancellationToken);
        }

        foreach (var product in _upserts)
        {
            await index.UpsertAsync(product, cancellationToken);
        }

        return saved;
    }
}
