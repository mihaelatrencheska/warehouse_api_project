namespace BoutiqueInventory.Application.DTOs.Responses;

/// <summary>Summary view of a warehouse (used in list endpoints).</summary>
public class WarehouseSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeactivatedAt { get; set; }
    public int SectionCount { get; set; }
    public int ProductCount { get; set; }
}

/// <summary>Full view of a warehouse including its sections.</summary>
public sealed class WarehouseDetailResponse : WarehouseSummaryResponse
{
    public IReadOnlyList<WarehouseSectionResponse> Sections { get; set; } =
        Array.Empty<WarehouseSectionResponse>();
}

/// <summary>Section inside a warehouse.</summary>
public sealed class WarehouseSectionResponse
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProductCount { get; set; }
}
