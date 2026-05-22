namespace BoutiqueInventory.Application.Interfaces;

/// <summary>
/// Persists binary product image bytes under the web host and returns a URL path clients can request.
/// </summary>
public interface IProductImageStorage
{
    /// <summary>
    /// Saves the image for <paramref name="productId"/> and returns a site-relative URL (e.g. <c>/uploads/products/{id}.jpg</c>).
    /// </summary>
    Task<string> SaveAsync(Guid productId, Stream imageStream, string extension, CancellationToken cancellationToken = default);
}
