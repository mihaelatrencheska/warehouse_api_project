namespace BoutiqueInventory.Domain.Entities;

/// <summary>
/// A physical location operated by the boutique. Closing a warehouse
/// flips <see cref="IsActive"/> to <c>false</c>; the data is preserved
/// for historical reporting.
/// </summary>
public class Warehouse
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DeactivatedAt { get; set; }

    public ICollection<WarehouseSection> Sections { get; set; } = new List<WarehouseSection>();
}
