namespace BoutiqueInventory.Domain.Entities;

/// <summary>
/// A stocked item (clothing, perfume, accessory, …). A product lives
/// inside exactly one <see cref="WarehouseSection"/> and may belong
/// to several <see cref="Category"/> values.
/// </summary>
public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    /// <summary>Stock-keeping unit, unique across the whole catalog.</summary>
    public string Sku { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Free-form size token, e.g. "S", "M", "42", "100ml".</summary>
    public string? Size { get; set; }

    /// <summary>Free-form type/family, e.g. "Eau de Parfum", "T-Shirt".</summary>
    public string? Type { get; set; }

    /// <summary>
    /// Optional expiry date — relevant for perfumes/cosmetics. The
    /// expiration monitor surfaces an alert at least one month before
    /// this date when set.
    /// </summary>
    public DateTimeOffset? ExpirationDate { get; set; }

    /// <summary>Server-relative path or remote URL for the product image.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// JSON blob holding image lookup hints (perceptual hash, dominant
    /// color palette, etc.). Stored as a string so the boutique can
    /// adopt richer metadata over time without schema changes.
    /// </summary>
    public string? ImageMetadata { get; set; }

    public Guid WarehouseSectionId { get; set; }
    public WarehouseSection WarehouseSection { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ProductCategory> Categories { get; set; } = new List<ProductCategory>();
}
