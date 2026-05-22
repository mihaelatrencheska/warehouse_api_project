namespace BoutiqueInventory.Application.DTOs.Responses;

/// <summary>Where a product physically sits in the boutique.</summary>
public sealed class ProductLocationResponse
{
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string? WarehouseLocation { get; set; }
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
}

/// <summary>Detail view of a product (single-item endpoints).</summary>
public sealed class ProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Size { get; set; }
    public string? Type { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageMetadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ProductLocationResponse Location { get; set; } = new();
    public IReadOnlyList<CategoryResponse> Categories { get; set; } = Array.Empty<CategoryResponse>();
}

/// <summary>Lightweight row used by search and list endpoints.</summary>
public class ProductSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string? Type { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public string? ImageUrl { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string? CategoryNames { get; set; }
}

/// <summary>Product row plus Hamming distance from a perceptual-hash query.</summary>
public sealed class ProductImageMatchResponse : ProductSummaryResponse
{
    /// <summary>Lower is more similar (0 = identical aHash).</summary>
    public int HammingDistance { get; set; }
}