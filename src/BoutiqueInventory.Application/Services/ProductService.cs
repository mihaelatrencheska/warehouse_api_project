using AutoMapper;
using BoutiqueInventory.Application.Common;
using BoutiqueInventory.Application.DTOs.Requests;
using BoutiqueInventory.Application.DTOs.Responses;
using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Domain.Entities;

namespace BoutiqueInventory.Application.Services;

/// <inheritdoc cref="IProductService"/>
public sealed class ProductService(
    IProductRepository products,
    IWarehouseRepository warehouses,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IProductService
{
    public async Task<ProductResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var product = await products.GetByIdWithGraphAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);
        return mapper.Map<ProductResponse>(product);
    }

    public async Task<ProductLocationResponse> GetLocationAsync(Guid id, CancellationToken ct)
    {
        var product = await products.GetByIdWithGraphAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        return new ProductLocationResponse
        {
            WarehouseId = product.WarehouseSection.WarehouseId,
            WarehouseName = product.WarehouseSection.Warehouse?.Name ?? string.Empty,
            WarehouseLocation = product.WarehouseSection.Warehouse?.Location,
            SectionId = product.WarehouseSectionId,
            SectionName = product.WarehouseSection.Name
        };
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        await EnsureSectionUsableAsync(request.WarehouseSectionId, ct);

        var sku = request.Sku.Trim();
        if (await products.SkuTakenAsync(sku, null, ct))
        {
            throw new ConflictException($"SKU '{sku}' is already in use.");
        }

        await EnsureCategoriesExistAsync(request.CategoryIds, ct);

        var product = new Product
        {
            Name = request.Name.Trim(),
            Sku = sku,
            Description = request.Description?.Trim(),
            Size = request.Size?.Trim(),
            Type = request.Type?.Trim(),
            ExpirationDate = request.ExpirationDate,
            ImageUrl = request.ImageUrl?.Trim(),
            ImageMetadata = request.ImageMetadata,
            WarehouseSectionId = request.WarehouseSectionId
        };
        AssignCategories(product, request.CategoryIds);

        products.Add(product);
        await unitOfWork.SaveChangesAsync(ct);

        var reloaded = await products.GetByIdWithGraphAsync(product.Id, ct);
        return mapper.Map<ProductResponse>(reloaded!);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        var product = await products.GetByIdWithGraphAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        if (product.WarehouseSectionId != request.WarehouseSectionId)
        {
            await EnsureSectionUsableAsync(request.WarehouseSectionId, ct);
        }

        var sku = request.Sku.Trim();
        if (await products.SkuTakenAsync(sku, id, ct))
        {
            throw new ConflictException($"SKU '{sku}' is already in use.");
        }

        await EnsureCategoriesExistAsync(request.CategoryIds, ct);

        product.Name = request.Name.Trim();
        product.Sku = sku;
        product.Description = request.Description?.Trim();
        product.Size = request.Size?.Trim();
        product.Type = request.Type?.Trim();
        product.ExpirationDate = request.ExpirationDate;
        product.ImageUrl = request.ImageUrl?.Trim();
        product.ImageMetadata = request.ImageMetadata;
        product.WarehouseSectionId = request.WarehouseSectionId;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        product.Categories.Clear();
        AssignCategories(product, request.CategoryIds);

        products.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        var reloaded = await products.GetByIdWithGraphAsync(id, ct);
        return mapper.Map<ProductResponse>(reloaded!);
    }

    public async Task<ProductResponse> MoveAsync(Guid id, MoveProductRequest request, CancellationToken ct)
    {
        var product = await products.GetByIdWithGraphAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        if (product.WarehouseSectionId == request.WarehouseSectionId)
        {
            return mapper.Map<ProductResponse>(product);
        }

        await EnsureSectionUsableAsync(request.WarehouseSectionId, ct);

        product.WarehouseSectionId = request.WarehouseSectionId;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        products.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        var reloaded = await products.GetByIdWithGraphAsync(id, ct);
        return mapper.Map<ProductResponse>(reloaded!);
    }

    public async Task<ProductResponse> ReplaceCategoriesAsync(Guid id, UpdateProductCategoriesRequest request, CancellationToken ct)
    {
        var product = await products.GetByIdWithGraphAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        await EnsureCategoriesExistAsync(request.CategoryIds, ct);

        product.Categories.Clear();
        AssignCategories(product, request.CategoryIds);
        product.UpdatedAt = DateTimeOffset.UtcNow;
        products.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        var reloaded = await products.GetByIdWithGraphAsync(id, ct);
        return mapper.Map<ProductResponse>(reloaded!);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);
        products.Remove(product);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public Task<PagedResult<ProductSummaryResponse>> BrowseAsync(int? page, int? pageSize, CancellationToken ct)
    {
        var (p, s) = Pagination.Normalize(page, pageSize);
        return products.ListPagedAsync(p, s, ct);
    }

    public Task<PagedResult<ProductSummaryResponse>> SearchAsync(ProductSearchRequest request, CancellationToken ct)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        return products.SearchAsync(request, page, pageSize, ct);
    }

    public async Task<IReadOnlyList<ProductSummaryResponse>> ListExpiringAsync(int withinDays, CancellationToken ct)
    {
        if (withinDays <= 0) withinDays = 30;
        var items = await products.ListExpiringWithinAsync(withinDays, ct);
        return mapper.Map<IReadOnlyList<ProductSummaryResponse>>(items);
    }

    public Task<int> CountExpiredAsync(CancellationToken ct) => products.CountExpiredAsync(ct);

    private async Task EnsureSectionUsableAsync(Guid sectionId, CancellationToken ct)
    {
        var section = await warehouses.FindSectionByIdAsync(sectionId, ct)
            ?? throw new DomainValidationException($"Warehouse section '{sectionId}' does not exist.");

        var warehouse = await warehouses.GetByIdAsync(section.WarehouseId, ct);
        if (warehouse is null || !warehouse.IsActive)
        {
            throw new ConflictException("Cannot place a product in a deactivated warehouse.");
        }
    }

    private async Task EnsureCategoriesExistAsync(IList<Guid> categoryIds, CancellationToken ct)
    {
        if (categoryIds.Count == 0) return;
        var distinct = categoryIds.Distinct().ToList();
        if (!await categories.AllExistAsync(distinct, ct))
        {
            throw new DomainValidationException("One or more category IDs are invalid.");
        }
    }

    private static void AssignCategories(Product product, IList<Guid> categoryIds)
    {
        foreach (var categoryId in categoryIds.Distinct())
        {
            product.Categories.Add(new ProductCategory
            {
                ProductId = product.Id,
                CategoryId = categoryId
            });
        }
    }
}
