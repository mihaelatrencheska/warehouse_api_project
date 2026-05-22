using BoutiqueInventory.Domain.Entities;

namespace BoutiqueInventory.Application.Interfaces;

/// <summary>Maintains the SQLite FTS5 shadow index for product text search.</summary>
public interface IProductSearchIndex
{
    Task EnsureSchemaAsync(CancellationToken ct);

    Task RebuildAsync(CancellationToken ct);

    Task<bool> IsEmptyAsync(CancellationToken ct);

    Task UpsertAsync(Product product, CancellationToken ct);

    Task DeleteAsync(Guid productId, CancellationToken ct);
}
