namespace BoutiqueInventory.Application.DTOs.Requests;

/// <summary>Payload for creating an owner-defined category.</summary>
public sealed class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>Payload for updating a category.</summary>
public sealed class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
