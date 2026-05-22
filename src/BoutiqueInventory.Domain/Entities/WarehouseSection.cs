namespace BoutiqueInventory.Domain.Entities;

/// <summary>
/// A subdivision inside a warehouse (e.g. "Shelf A", "Row 3") that
/// holds a set of products and lets the owner pinpoint each item.
/// </summary>
public class WarehouseSection
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
