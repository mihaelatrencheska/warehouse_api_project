using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Domain.Entities;
using BoutiqueInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BoutiqueInventory.Infrastructure.Repositories;

/// <inheritdoc cref="ICategoryRepository"/>
public sealed class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken ct) =>
        await db.Categories.AsNoTracking()
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Categories.AsNoTracking()
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) =>
        db.Categories.AsNoTracking().AnyAsync(c => c.Id == id, ct);

    /// <inheritdoc/>
    public Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken ct) =>
        db.Categories.AsNoTracking().AnyAsync(c =>
            c.Name == name && (exceptId == null || c.Id != exceptId), ct);

    /// <inheritdoc/>
    public async Task<bool> AllExistAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return true;
        var found = await db.Categories.AsNoTracking().CountAsync(c => idList.Contains(c.Id), ct);
        return found == idList.Count;
    }

    /// <inheritdoc/>
    public Task<int> CountProductsAsync(Guid categoryId, CancellationToken ct) =>
        db.ProductCategories.AsNoTracking().CountAsync(pc => pc.CategoryId == categoryId, ct);

    /// <inheritdoc/>
    public void Add(Category category) => db.Categories.Add(category);
    /// <inheritdoc/>
    public void Update(Category category) => db.Categories.Update(category);
    /// <inheritdoc/>
    public void Remove(Category category) => db.Categories.Remove(category);
}
