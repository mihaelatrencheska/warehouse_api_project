using BoutiqueInventory.Application.DTOs.Responses;

namespace BoutiqueInventory.Application.Interfaces;

/// <summary>
/// Image upload and perceptual-hash similarity search for products.
/// </summary>
public interface IProductImageService
{
    /// <summary>
    /// Stores the uploaded image, computes <c>aHash</c> metadata, updates <see cref="Domain.Entities.Product.ImageUrl"/> and <c>ImageMetadata</c>.
    /// </summary>
    Task<ProductResponse> UploadImageAsync(Guid productId, Stream imageStream, string? fileName, CancellationToken ct);

    /// <summary>
    /// Finds catalog products whose stored fingerprint is within <paramref name="maxHammingDistance"/> bits of the query image.
    /// </summary>
    Task<IReadOnlyList<ProductImageMatchResponse>> SearchByImageAsync(
        Stream queryImageStream,
        int maxResults,
        int maxHammingDistance,
        CancellationToken ct);
}
