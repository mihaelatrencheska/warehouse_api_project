using BoutiqueInventory.Application.DTOs.Requests;
using BoutiqueInventory.Application.DTOs.Responses;
using BoutiqueInventory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoutiqueInventory.Api.Controllers;

/// <summary>Manages owner-defined product categories.</summary>
[ApiController]
[Authorize]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesController(ICategoryService service) : ControllerBase
{
    /// <summary>List every category.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> List(CancellationToken ct)
        => Ok(await service.ListAsync(ct));

    /// <summary>Get a single category.</summary>
    [HttpGet("{id:guid}", Name = "GetCategory")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    /// <summary>Create a new owner-defined category.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtRoute("GetCategory", new { id = dto.Id }, dto);
    }

    /// <summary>Rename or re-describe a category.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Update(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken ct)
        => Ok(await service.UpdateAsync(id, request, ct));

    /// <summary>Delete a category. Fails if any products are still assigned to it.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
