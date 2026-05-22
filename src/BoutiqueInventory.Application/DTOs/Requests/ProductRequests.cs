namespace BoutiqueInventory.Application.DTOs.Requests;

/// <summary>Payload for creating a product.</summary>
public sealed class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Size { get; set; }
    public string? Type { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageMetadata { get; set; }
    public Guid WarehouseSectionId { get; set; }
    public IList<Guid> CategoryIds { get; set; } = new List<Guid>();
}

/// <summary>Full update payload for a product (PUT).</summary>
public sealed class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Size { get; set; }
    public string? Type { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageMetadata { get; set; }
    public Guid WarehouseSectionId { get; set; }
    public IList<Guid> CategoryIds { get; set; } = new List<Guid>();
}

/// <summary>Payload for relocating a product to a different section/warehouse.</summary>
public sealed class MoveProductRequest
{
    public Guid WarehouseSectionId { get; set; }
}

/// <summary>Payload for replacing the category set of a product.</summary>
public sealed class UpdateProductCategoriesRequest
{
    public IList<Guid> CategoryIds { get; set; } = new List<Guid>();
}

/// <summary>Query-string parameters for <c>GET /api/products/search</c>.</summary>
public sealed class ProductSearchRequest
{
    /// <summary>Free-text fragment matched against name, SKU, description.</summary>
    public string? Query { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Size { get; set; }
    public string? Type { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SectionId { get; set; }
    public int? ExpiringWithinDays { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}
