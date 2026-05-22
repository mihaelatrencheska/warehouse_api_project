using AutoMapper;
using BoutiqueInventory.Application.Common;
using BoutiqueInventory.Application.DTOs.Requests;
using BoutiqueInventory.Application.DTOs.Responses;
using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Domain.Entities;

namespace BoutiqueInventory.Application.Services;

/// <inheritdoc cref="ICategoryService"/>
public sealed class CategoryService(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    IMapper mapper) : ICategoryService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken ct)
    {
        var entities = await categories.ListAsync(ct);
        return mapper.Map<IReadOnlyList<CategoryResponse>>(entities);
    }

    /// <inheritdoc/>
    public async Task<CategoryResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var entity = await categories.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);
        return mapper.Map<CategoryResponse>(entity);
    }

    /// <inheritdoc/>
    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await categories.NameTakenAsync(name, null, ct))
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }

        var category = new Category
        {
            Name = name,
            Description = request.Description?.Trim()
        };
        categories.Add(category);
        await unitOfWork.SaveChangesAsync(ct);

        return mapper.Map<CategoryResponse>(category);
    }

    /// <inheritdoc/>
    public async Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await categories.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);

        var name = request.Name.Trim();
        if (await categories.NameTakenAsync(name, id, ct))
        {
            throw new ConflictException($"A category named '{name}' already exists.");
        }

        category.Name = name;
        category.Description = request.Description?.Trim();
        categories.Update(category);
        await unitOfWork.SaveChangesAsync(ct);

        return mapper.Map<CategoryResponse>(category);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var category = await categories.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Category), id);

        var inUse = await categories.CountProductsAsync(id, ct);
        if (inUse > 0)
        {
            throw new ConflictException(
                $"Cannot delete category '{category.Name}' — it is still assigned to {inUse} product(s).");
        }

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
