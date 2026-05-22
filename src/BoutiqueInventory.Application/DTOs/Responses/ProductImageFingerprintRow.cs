namespace BoutiqueInventory.Application.DTOs.Responses;

/// <summary>Lightweight row for in-memory perceptual-hash comparison.</summary>
public sealed class ProductImageFingerprintRow
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
    public string HashHex { get; set; } = string.Empty;
}
