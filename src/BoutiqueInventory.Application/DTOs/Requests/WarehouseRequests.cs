namespace BoutiqueInventory.Application.DTOs.Requests;

/// <summary>Payload for <c>POST /api/warehouses</c>.</summary>
public sealed class CreateWarehouseRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
}

/// <summary>Payload for <c>PUT /api/warehouses/{id}</c>.</summary>
public sealed class UpdateWarehouseRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
}

/// <summary>Payload for adding/renaming a section under a warehouse.</summary>
public sealed class WarehouseSectionRequest
{
    public string Name { get; set; } = string.Empty;
}
