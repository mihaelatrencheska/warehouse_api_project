using BoutiqueInventory.Application.Common;
using BoutiqueInventory.Application.DTOs.Requests;
using BoutiqueInventory.Application.DTOs.Responses;
using BoutiqueInventory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoutiqueInventory.Api.Controllers;

/// <summary>Manages products and exposes search / expiration-monitoring queries.</summary>
[ApiController]
[Authorize]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController(IProductService service, IProductImageService imageService) : ControllerBase
{
    /// <summary>Browse the full catalog with pagination (no text query).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductSummaryResponse>>> Browse(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
        => Ok(await service.BrowseAsync(page, pageSize, ct));

    /// <summary>
    /// Search products with FTS5 full-text and structured filters.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<ProductSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductSummaryResponse>>> Search(
        [FromQuery] ProductSearchRequest request,
        CancellationToken ct)
        => Ok(await service.SearchAsync(request, ct));

    /// <summary>List products whose <c>ExpirationDate</c> falls inside the next N days.</summary>
    [HttpGet("expiring")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductSummaryResponse>>> Expiring(
        [FromQuery] int withinDays = 30,
        CancellationToken ct = default)
        => Ok(await service.ListExpiringAsync(withinDays, ct));

    /// <summary>Count products whose expiration date is in the past.</summary>
    [HttpGet("expired/count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> ExpiredCount(CancellationToken ct)
        => Ok(await service.CountExpiredAsync(ct));

    /// <summary>
    /// Upload a product image; stores file under <c>wwwroot/uploads/products</c> and saves an 8×8 average-hash fingerprint in <c>ImageMetadata</c>.
    /// </summary>
    [HttpPost("{id:guid}/image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> UploadImage(
        Guid id,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Detail = "No image file was uploaded.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        await using var stream = file.OpenReadStream();
        var dto = await imageService.UploadImageAsync(id, stream, file.FileName, ct);
        return Ok(dto);
    }

    /// <summary>
    /// Find catalog products whose stored perceptual hash is within <paramref name="maxHammingDistance"/> bits of the uploaded query image (lower distance = more similar).
    /// </summary>
    [HttpPost("search-by-image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(IReadOnlyList<ProductImageMatchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ProductImageMatchResponse>>> SearchByImage(
        IFormFile file,
        [FromQuery] int maxResults = 10,
        [FromQuery] int maxHammingDistance = 12,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Detail = "No query image was uploaded.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        await using var stream = file.OpenReadStream();
        var results = await imageService.SearchByImageAsync(stream, maxResults, maxHammingDistance, ct);
        return Ok(results);
    }

    /// <summary>Get a single product, including location and categories.</summary>
    [HttpGet("{id:guid}", Name = "GetProduct")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    /// <summary>Get the warehouse / section a product is currently in.</summary>
    [HttpGet("{id:guid}/location")]
    [ProducesResponseType(typeof(ProductLocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductLocationResponse>> GetLocation(Guid id, CancellationToken ct)
        => Ok(await service.GetLocationAsync(id, ct));

    /// <summary>Create a product, assigning it to a section and any number of categories.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken ct)
    {
        var dto = await service.CreateAsync(request, ct);
        return CreatedAtRoute("GetProduct", new { id = dto.Id }, dto);
    }

    /// <summary>Full update of a product (including its category set).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken ct)
        => Ok(await service.UpdateAsync(id, request, ct));

    /// <summary>Move a product to another section (possibly in another warehouse).</summary>
    [HttpPatch("{id:guid}/move")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Move(
        Guid id,
        [FromBody] MoveProductRequest request,
        CancellationToken ct)
        => Ok(await service.MoveAsync(id, request, ct));

    /// <summary>Replace the entire category set of a product.</summary>
    [HttpPatch("{id:guid}/categories")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> ReplaceCategories(
        Guid id,
        [FromBody] UpdateProductCategoriesRequest request,
        CancellationToken ct)
        => Ok(await service.ReplaceCategoriesAsync(id, request, ct));

    /// <summary>Delete a product (hard delete).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
