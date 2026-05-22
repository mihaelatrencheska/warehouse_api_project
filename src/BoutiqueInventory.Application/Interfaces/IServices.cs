using BoutiqueInventory.Application.Common;
using BoutiqueInventory.Application.DTOs.Requests;
using BoutiqueInventory.Application.DTOs.Responses;

namespace BoutiqueInventory.Application.Interfaces;

/// <summary>Use-case orchestrator for warehouses and their sections.</summary>
public interface IWarehouseService
{
    Task<IReadOnlyList<WarehouseSummaryResponse>> ListAsync(bool? activeOnly, CancellationToken ct);
    Task<WarehouseDetailResponse> GetAsync(Guid id, CancellationToken ct);
    Task<WarehouseDetailResponse> CreateAsync(CreateWarehouseRequest request, CancellationToken ct);
    Task<WarehouseDetailResponse> UpdateAsync(Guid id, UpdateWarehouseRequest request, CancellationToken ct);
    Task DeactivateAsync(Guid id, CancellationToken ct);
    Task ReactivateAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<ProductSummaryResponse>> GetProductsAsync(Guid warehouseId, CancellationToken ct);

    Task<IReadOnlyList<WarehouseSectionResponse>> ListSectionsAsync(Guid warehouseId, CancellationToken ct);
    Task<WarehouseSectionResponse> AddSectionAsync(Guid warehouseId, WarehouseSectionRequest request, CancellationToken ct);
    Task<WarehouseSectionResponse> RenameSectionAsync(Guid warehouseId, Guid sectionId, WarehouseSectionRequest request, CancellationToken ct);
    Task DeleteSectionAsync(Guid warehouseId, Guid sectionId, CancellationToken ct);
}

/// <summary>Use-case orchestrator for owner-defined categories.</summary>
public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken ct);
    Task<CategoryResponse> GetAsync(Guid id, CancellationToken ct);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct);
    Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

/// <summary>Use-case orchestrator for products.</summary>
public interface IProductService
{
    Task<ProductResponse> GetAsync(Guid id, CancellationToken ct);
    Task<ProductLocationResponse> GetLocationAsync(Guid id, CancellationToken ct);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct);
    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct);
    Task<ProductResponse> MoveAsync(Guid id, MoveProductRequest request, CancellationToken ct);
    Task<ProductResponse> ReplaceCategoriesAsync(Guid id, UpdateProductCategoriesRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);

    Task<PagedResult<ProductSummaryResponse>> BrowseAsync(int? page, int? pageSize, CancellationToken ct);
    Task<PagedResult<ProductSummaryResponse>> SearchAsync(ProductSearchRequest request, CancellationToken ct);
    Task<IReadOnlyList<ProductSummaryResponse>> ListExpiringAsync(int withinDays, CancellationToken ct);
    Task<int> CountExpiredAsync(CancellationToken ct);
}

/// <summary>Use-case orchestrator for expiration alerts.</summary>
public interface IAlertService
{
    Task<IReadOnlyList<ExpirationAlertResponse>> ListUnacknowledgedAsync(CancellationToken ct);
    Task AcknowledgeAsync(Guid alertId, CancellationToken ct);
}

/// <summary>
/// Encapsulates the daily expiration-window scan invoked by
/// the background job (and exposed for manual triggering).
/// </summary>
public interface IExpirationMonitor
{
    /// <summary>
    /// Inspects every product whose <c>ExpirationDate</c> lies within
    /// the next <paramref name="windowDays"/> days and creates a fresh
    /// <see cref="Domain.Entities.ExpirationAlert"/> when one does not
    /// already exist for that product in the current window. Returns
    /// the number of new alerts created.
    /// </summary>
    Task<int> RunAsync(int windowDays, CancellationToken ct);
}
