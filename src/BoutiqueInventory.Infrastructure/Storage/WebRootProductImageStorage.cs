using BoutiqueInventory.Application.Interfaces;
using Microsoft.Extensions.Hosting;

namespace BoutiqueInventory.Infrastructure.Storage;

/// <summary>
/// Writes files under <c>wwwroot/uploads/products</c> relative to the host content root.
/// </summary>
public sealed class WebRootProductImageStorage(IHostEnvironment env) : IProductImageStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    /// <inheritdoc />
    public async Task<string> SaveAsync(Guid productId, Stream imageStream, string extension, CancellationToken cancellationToken = default)
    {
        var ext = extension.StartsWith(".", StringComparison.Ordinal)
            ? extension
            : "." + extension;

        if (!AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException(
                $"Extension '{ext}' is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}.");
        }

        var webRoot = Path.Combine(env.ContentRootPath, "wwwroot");
        var dir = Path.Combine(webRoot, "uploads", "products");
        Directory.CreateDirectory(dir);

        var fileName = $"{productId:N}{ext}";
        var physicalPath = Path.Combine(dir, fileName);

        await using (var fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await imageStream.CopyToAsync(fs, cancellationToken);
        }

        return $"/uploads/products/{fileName}";
    }
}
