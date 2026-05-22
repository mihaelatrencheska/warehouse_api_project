using BoutiqueInventory.Domain.Entities;

namespace BoutiqueInventory.Application.Interfaces;

/// <summary>Persistence operations for <see cref="Warehouse"/> aggregate.</summary>
public interface IWarehouseRepository
{
    Task<IReadOnlyList<Warehouse>> ListAsync(bool? activeOnly, CancellationToken ct);

    Task<Warehouse?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Returns warehouse with sections eager-loaded.</summary>
    Task<Warehouse?> GetByIdWithSectionsAsync(Guid id, CancellationToken ct);

    Task<bool> ExistsAsync(Guid id, CancellationToken ct);

    Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken ct);

    void Add(Warehouse warehouse);
    void Update(Warehouse warehouse);

    Task<IReadOnlyList<WarehouseSection>> ListSectionsAsync(Guid warehouseId, CancellationToken ct);
    Task<WarehouseSection?> GetSectionAsync(Guid warehouseId, Guid sectionId, CancellationToken ct);

    /// <summary>Find a section without knowing its warehouse upfront.</summary>
    Task<WarehouseSection?> FindSectionByIdAsync(Guid sectionId, CancellationToken ct);

    Task<bool> SectionNameTakenAsync(Guid warehouseId, string name, Guid? exceptId, CancellationToken ct);
    Task<int> CountProductsInSectionAsync(Guid sectionId, CancellationToken ct);

    void AddSection(WarehouseSection section);
    void UpdateSection(WarehouseSection section);
    void RemoveSection(WarehouseSection section);
}
