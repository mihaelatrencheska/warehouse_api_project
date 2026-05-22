namespace BoutiqueInventory.Domain.Entities;

/// <summary>
/// Owner-defined classification (e.g. "Perfume", "Summer Capsule").
/// A product can belong to many categories.
/// </summary>
public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ProductCategory> Products { get; set; } = new List<ProductCategory>();
}
