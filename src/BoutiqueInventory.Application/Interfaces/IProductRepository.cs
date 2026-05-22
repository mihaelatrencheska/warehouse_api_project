using BoutiqueInventory.Application.Common;
using BoutiqueInventory.Application.DTOs.Requests;
using BoutiqueInventory.Application.DTOs.Responses;
using BoutiqueInventory.Domain.Entities;

namespace BoutiqueInventory.Application.Interfaces;

/// <summary>Persistence operations for <see cref="Product"/>.</summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Eager-loads section, warehouse and categories.</summary>
    Task<Product?> GetByIdWithGraphAsync(Guid id, CancellationToken ct);

    Task<bool> SkuTakenAsync(string sku, Guid? exceptId, CancellationToken ct);

    Task<IReadOnlyList<Product>> ListByWarehouseAsync(Guid warehouseId, CancellationToken ct);

    Task<IReadOnlyList<Product>> ListExpiringWithinAsync(int days, CancellationToken ct);

    /// <summary>Products that have image fingerprint metadata (for similarity search).</summary>
    Task<IReadOnlyList<Product>> ListWithImageFingerprintAsync(CancellationToken ct);

    /// <summary>Catalog rows with stored image hashes (no EF graph hydration).</summary>
    Task<IReadOnlyList<ProductImageFingerprintRow>> ListImageFingerprintRowsAsync(CancellationToken ct);

    /// <summary>Paginated catalog browse (no text query).</summary>
    Task<PagedResult<ProductSummaryResponse>> ListPagedAsync(int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Performs the search query implemented with Dapper for performance.
    /// Returns lightweight summary rows + total match count.
    /// </summary>
    Task<PagedResult<ProductSummaryResponse>> SearchAsync(
        ProductSearchRequest request,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<int> CountExpiredAsync(CancellationToken ct);

    void Add(Product product);
    void Update(Product product);
    void Remove(Product product);
}
