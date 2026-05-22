using AutoMapper;
using BoutiqueInventory.Application.Common;
using BoutiqueInventory.Application.DTOs.Requests;
using BoutiqueInventory.Application.DTOs.Responses;
using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Domain.Entities;

namespace BoutiqueInventory.Application.Services;

/// <inheritdoc cref="IWarehouseService"/>
public sealed class WarehouseService(
    IWarehouseRepository warehouses,
    IProductRepository products,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IWarehouseService
{
    public async Task<IReadOnlyList<WarehouseSummaryResponse>> ListAsync(bool? activeOnly, CancellationToken ct)
    {
        var entities = await warehouses.ListAsync(activeOnly, ct);
        return mapper.Map<IReadOnlyList<WarehouseSummaryResponse>>(entities);
    }

    public async Task<WarehouseDetailResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var entity = await warehouses.GetByIdWithSectionsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Warehouse), id);
        return mapper.Map<WarehouseDetailResponse>(entity);
    }

    public async Task<WarehouseDetailResponse> CreateAsync(CreateWarehouseRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await warehouses.NameTakenAsync(name, null, ct))
        {
            throw new ConflictException($"A warehouse named '{name}' already exists.");
        }

        var warehouse = new Warehouse
        {
            Name = name,
            Location = request.Location?.Trim(),
            IsActive = true
        };
        warehouses.Add(warehouse);
        await unitOfWork.SaveChangesAsync(ct);

        return mapper.Map<WarehouseDetailResponse>(warehouse);
    }

    public async Task<WarehouseDetailResponse> UpdateAsync(Guid id, UpdateWarehouseRequest request, CancellationToken ct)
    {
        var warehouse = await warehouses.GetByIdWithSectionsAsync(id, ct)
            ?? throw new NotFoundException(nameof(Warehouse), id);

        var name = request.Name.Trim();
        if (await warehouses.NameTakenAsync(name, id, ct))
        {
            throw new ConflictException($"A warehouse named '{name}' already exists.");
        }

        warehouse.Name = name;
        warehouse.Location = request.Location?.Trim();
        warehouses.Update(warehouse);
        await unitOfWork.SaveChangesAsync(ct);

        return mapper.Map<WarehouseDetailResponse>(warehouse);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct)
    {
        var warehouse = await warehouses.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Warehouse), id);

        if (!warehouse.IsActive)
        {
            return;
        }

        warehouse.IsActive = false;
        warehouse.DeactivatedAt = DateTimeOffset.UtcNow;
        warehouses.Update(warehouse);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ReactivateAsync(Guid id, CancellationToken ct)
    {
        var warehouse = await warehouses.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Warehouse), id);

        if (warehouse.IsActive)
        {
            return;
        }

        warehouse.IsActive = true;
        warehouse.DeactivatedAt = null;
        warehouses.Update(warehouse);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ProductSummaryResponse>> GetProductsAsync(Guid warehouseId, CancellationToken ct)
    {
        if (!await warehouses.ExistsAsync(warehouseId, ct))
        {
            throw new NotFoundException(nameof(Warehouse), warehouseId);
        }
        var items = await products.ListByWarehouseAsync(warehouseId, ct);
        return mapper.Map<IReadOnlyList<ProductSummaryResponse>>(items);
    }

    public async Task<IReadOnlyList<WarehouseSectionResponse>> ListSectionsAsync(Guid warehouseId, CancellationToken ct)
    {
        if (!await warehouses.ExistsAsync(warehouseId, ct))
        {
            throw new NotFoundException(nameof(Warehouse), warehouseId);
        }
        var sections = await warehouses.ListSectionsAsync(warehouseId, ct);
        return mapper.Map<IReadOnlyList<WarehouseSectionResponse>>(sections);
    }

    public async Task<WarehouseSectionResponse> AddSectionAsync(Guid warehouseId, WarehouseSectionRequest request, CancellationToken ct)
    {
        var warehouse = await warehouses.GetByIdAsync(warehouseId, ct)
            ?? throw new NotFoundException(nameof(Warehouse), warehouseId);

        if (!warehouse.IsActive)
        {
            throw new ConflictException($"Warehouse '{warehouse.Name}' is deactivated.");
        }

        var name = request.Name.Trim();
        if (await warehouses.SectionNameTakenAsync(warehouseId, name, null, ct))
        {
            throw new ConflictException($"Section '{name}' already exists in this warehouse.");
        }

        var section = new WarehouseSection
        {
            WarehouseId = warehouseId,
            Name = name
        };
        warehouses.AddSection(section);
        await unitOfWork.SaveChangesAsync(ct);

        return mapper.Map<WarehouseSectionResponse>(section);
    }

    public async Task<WarehouseSectionResponse> RenameSectionAsync(Guid warehouseId, Guid sectionId, WarehouseSectionRequest request, CancellationToken ct)
    {
        var section = await warehouses.GetSectionAsync(warehouseId, sectionId, ct)
            ?? throw new NotFoundException(nameof(WarehouseSection), sectionId);

        var name = request.Name.Trim();
        if (await warehouses.SectionNameTakenAsync(warehouseId, name, sectionId, ct))
        {
            throw new ConflictException($"Section '{name}' already exists in this warehouse.");
        }

        section.Name = name;
        warehouses.UpdateSection(section);
        await unitOfWork.SaveChangesAsync(ct);

        return mapper.Map<WarehouseSectionResponse>(section);
    }

    public async Task DeleteSectionAsync(Guid warehouseId, Guid sectionId, CancellationToken ct)
    {
        var section = await warehouses.GetSectionAsync(warehouseId, sectionId, ct)
            ?? throw new NotFoundException(nameof(WarehouseSection), sectionId);

        var productCount = await warehouses.CountProductsInSectionAsync(sectionId, ct);
        if (productCount > 0)
        {
            throw new ConflictException("Cannot delete a section that still contains products.");
        }

        warehouses.RemoveSection(section);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
