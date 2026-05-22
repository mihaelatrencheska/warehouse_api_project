namespace BoutiqueInventory.Application.DTOs.Responses;

/// <summary>Owner-defined category.</summary>
public sealed class CategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int ProductCount { get; set; }
}
