namespace BoutiqueInventory.Domain.Entities;

/// <summary>
/// Join entity for the many-to-many relationship between
/// <see cref="Product"/> and <see cref="Category"/>.
/// </summary>
public class ProductCategory
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
