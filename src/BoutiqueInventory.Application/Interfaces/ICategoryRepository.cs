using BoutiqueInventory.Domain.Entities;

namespace BoutiqueInventory.Application.Interfaces;

/// <summary>Persistence operations for <see cref="Category"/>.</summary>
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken ct);
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);
    Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken ct);
    Task<bool> AllExistAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task<int> CountProductsAsync(Guid categoryId, CancellationToken ct);

    void Add(Category category);
    void Update(Category category);
    void Remove(Category category);
}
