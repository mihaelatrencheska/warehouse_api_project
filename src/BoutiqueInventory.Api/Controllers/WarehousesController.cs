using BoutiqueInventory.Application.DTOs.Requests;
using BoutiqueInventory.Application.DTOs.Responses;
using BoutiqueInventory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoutiqueInventory.Api.Controllers;

/// <summary>
/// Manages warehouses and their internal sections (aisles, shelves, …).
/// </summary>
[ApiController]
[Authorize]
[Route("api/warehouses")]
[Produces("application/json")]
public class WarehousesController(IWarehouseService service) : ControllerBase
{
    /// <summary>List warehouses, optionally filtering to active ones.</summary>
    /// <param name="isActive">If <c>true</c>, only active warehouses are returned.</param>
    /// <param name="ct">Cancellation token (supplied by the framework).</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WarehouseSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WarehouseSummaryResponse>>> List(
        [FromQuery] bool? isActive,
        CancellationToken ct)
        => Ok(await service.ListAsync(isActive, ct));

    /// <summary>Get a warehouse by its identifier (includes sections).</summary>
    [HttpGet("{id:guid}", Name = "GetWarehouse")]
    [ProducesResponseType(typeof(WarehouseDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WarehouseDetailResponse>> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    /// <summary>Create a new warehouse.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WarehouseDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WarehouseDetailResponse>> Create(
        [FromBody] CreateWarehouseRequest request,
        CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtRoute("GetWarehouse", new { id = dto.Id }, dto);
    }

    /// <summary>Update warehouse name and location.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WarehouseDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WarehouseDetailResponse>> Update(
        Guid id,
        [FromBody] UpdateWarehouseRequest request,
        CancellationToken ct)
        => Ok(await service.UpdateAsync(id, request, ct));

    /// <summary>Soft-delete (close) a warehouse. The data is preserved.</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await service.DeactivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>Re-open a previously deactivated warehouse.</summary>
    [HttpPatch("{id:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        await service.ReactivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>List every product currently stored in a warehouse.</summary>
    [HttpGet("{id:guid}/products")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProductSummaryResponse>>> GetProducts(
        Guid id, CancellationToken ct)
        => Ok(await service.GetProductsAsync(id, ct));

    /// <summary>List every section inside a warehouse.</summary>
    [HttpGet("{warehouseId:guid}/sections")]
    [ProducesResponseType(typeof(IReadOnlyList<WarehouseSectionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<WarehouseSectionResponse>>> ListSections(
        Guid warehouseId, CancellationToken ct)
        => Ok(await service.ListSectionsAsync(warehouseId, ct));

    /// <summary>Add a section to a warehouse.</summary>
    [HttpPost("{warehouseId:guid}/sections")]
    [ProducesResponseType(typeof(WarehouseSectionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WarehouseSectionResponse>> AddSection(
        Guid warehouseId,
        [FromBody] WarehouseSectionRequest request,
        CancellationToken ct)
    {
        var section = await service.AddSectionAsync(warehouseId, request, ct);
        return CreatedAtAction(nameof(ListSections), new { warehouseId }, section);
    }

    /// <summary>Rename a section.</summary>
    [HttpPut("{warehouseId:guid}/sections/{id:guid}")]
    [ProducesResponseType(typeof(WarehouseSectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WarehouseSectionResponse>> RenameSection(
        Guid warehouseId, Guid id,
        [FromBody] WarehouseSectionRequest request,
        CancellationToken ct)
        => Ok(await service.RenameSectionAsync(warehouseId, id, request, ct));

    /// <summary>Remove a section, only if it is empty.</summary>
    [HttpDelete("{warehouseId:guid}/sections/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteSection(
        Guid warehouseId, Guid id, CancellationToken ct)
    {
        await service.DeleteSectionAsync(warehouseId, id, ct);
        return NoContent();
    }
}
