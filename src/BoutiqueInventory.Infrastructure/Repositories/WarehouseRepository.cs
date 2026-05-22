using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Domain.Entities;
using BoutiqueInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BoutiqueInventory.Infrastructure.Repositories;

/// <inheritdoc cref="IWarehouseRepository"/>
public sealed class WarehouseRepository(AppDbContext db) : IWarehouseRepository
{
    public async Task<IReadOnlyList<Warehouse>> ListAsync(bool? activeOnly, CancellationToken ct)
    {
        var query = db.Warehouses.AsNoTracking()
            .Include(w => w.Sections).ThenInclude(s => s.Products)
            .AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(w => w.IsActive);
        }
        else if (activeOnly == false)
        {
            query = query.Where(w => !w.IsActive);
        }

        return await query.OrderBy(w => w.Name).ToListAsync(ct);
    }

    public Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct);

    public Task<Warehouse?> GetByIdWithSectionsAsync(Guid id, CancellationToken ct) =>
        db.Warehouses.AsNoTracking()
            .Include(w => w.Sections).ThenInclude(s => s.Products)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) =>
        db.Warehouses.AsNoTracking().AnyAsync(w => w.Id == id, ct);

    public Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken ct) =>
        db.Warehouses.AsNoTracking()
            .AnyAsync(w => w.Name == name && (exceptId == null || w.Id != exceptId), ct);

    public void Add(Warehouse warehouse) => db.Warehouses.Add(warehouse);

    public void Update(Warehouse warehouse) => db.Warehouses.Update(warehouse);

    public async Task<IReadOnlyList<WarehouseSection>> ListSectionsAsync(Guid warehouseId, CancellationToken ct) =>
        await db.WarehouseSections.AsNoTracking()
            .Include(s => s.Products)
            .Where(s => s.WarehouseId == warehouseId)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public Task<WarehouseSection?> GetSectionAsync(Guid warehouseId, Guid sectionId, CancellationToken ct) =>
        db.WarehouseSections.FirstOrDefaultAsync(
            s => s.Id == sectionId && s.WarehouseId == warehouseId, ct);

    public Task<WarehouseSection?> FindSectionByIdAsync(Guid sectionId, CancellationToken ct) =>
        db.WarehouseSections.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sectionId, ct);

    public Task<bool> SectionNameTakenAsync(Guid warehouseId, string name, Guid? exceptId, CancellationToken ct) =>
        db.WarehouseSections.AsNoTracking().AnyAsync(s =>
            s.WarehouseId == warehouseId &&
            s.Name == name &&
            (exceptId == null || s.Id != exceptId), ct);

    public Task<int> CountProductsInSectionAsync(Guid sectionId, CancellationToken ct) =>
        db.Products.AsNoTracking().CountAsync(p => p.WarehouseSectionId == sectionId, ct);

    public void AddSection(WarehouseSection section) => db.WarehouseSections.Add(section);

    public void UpdateSection(WarehouseSection section) => db.WarehouseSections.Update(section);

    public void RemoveSection(WarehouseSection section) => db.WarehouseSections.Remove(section);
}
