using System.Globalization;
using System.Numerics;
using System.Text.Json;
using AutoMapper;
using BoutiqueInventory.Application.Common;
using BoutiqueInventory.Application.DTOs.Responses;
using BoutiqueInventory.Application.Interfaces;
using BoutiqueInventory.Domain.Entities;

namespace BoutiqueInventory.Application.Services;

/// <inheritdoc cref="IProductImageService"/>
public sealed class ProductImageService(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    IImageHashService imageHash,
    IProductImageStorage imageStorage,
    IMapper mapper) : IProductImageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <inheritdoc />
    public async Task<ProductResponse> UploadImageAsync(Guid productId, Stream imageStream, string? fileName, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(productId, ct)
            ?? throw new NotFoundException(nameof(Product), productId);

        await using var buffer = new MemoryStream();
        await imageStream.CopyToAsync(buffer, ct);
        if (buffer.Length == 0)
        {
            throw new DomainValidationException("Image upload was empty.");
        }

        buffer.Position = 0;
        ulong hash;
        try
        {
            hash = imageHash.ComputeAverageHash64(buffer);
        }
        catch (InvalidOperationException)
        {
            throw new DomainValidationException("The uploaded file is not a supported image format.");
        }

        buffer.Position = 0;
        var ext = string.IsNullOrWhiteSpace(fileName)
            ? ".jpg"
            : Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext))
        {
            ext = ".jpg";
        }

        if (!AllowedExtensions.Contains(ext))
        {
            throw new DomainValidationException(
                $"Image extension '{ext}' is not allowed. Use: .jpg, .jpeg, .png, .webp, .gif.");
        }

        string relativeUrl;
        try
        {
            relativeUrl = await imageStorage.SaveAsync(productId, buffer, ext, ct);
        }
        catch (InvalidOperationException ex)
        {
            throw new DomainValidationException(ex.Message);
        }

        var metadataJson = JsonSerializer.Serialize(
            new { algorithm = "aHash-8x8", hashHex = hash.ToString("X16", CultureInfo.InvariantCulture) },
            JsonOptions);

        product.ImageUrl = relativeUrl;
        product.ImageMetadata = metadataJson;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        products.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        var graph = await products.GetByIdWithGraphAsync(productId, ct);
        return mapper.Map<ProductResponse>(graph!);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductImageMatchResponse>> SearchByImageAsync(
        Stream queryImageStream,
        int maxResults,
        int maxHammingDistance,
        CancellationToken ct)
    {
        if (maxResults <= 0) maxResults = 10;
        if (maxResults > 100) maxResults = 100;
        if (maxHammingDistance < 0) maxHammingDistance = 6;

        await using var buffer = new MemoryStream();
        await queryImageStream.CopyToAsync(buffer, ct);
        if (buffer.Length == 0)
        {
            throw new DomainValidationException("Query image was empty.");
        }

        buffer.Position = 0;
        ulong queryHash;
        try
        {
            queryHash = imageHash.ComputeAverageHash64(buffer);
        }
        catch (InvalidOperationException)
        {
            throw new DomainValidationException("The query file is not a supported image format.");
        }

        var candidates = await products.ListImageFingerprintRowsAsync(ct);
        var matches = new List<(ProductImageFingerprintRow Row, int Dist)>();

        foreach (var row in candidates)
        {
            if (!ulong.TryParse(row.HashHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var storedHash))
            {
                continue;
            }

            var dist = HammingDistance(queryHash, storedHash);
            if (dist <= maxHammingDistance)
            {
                matches.Add((row, dist));
            }
        }

        var ordered = matches
            .OrderBy(x => x.Dist)
            .ThenBy(x => x.Row.Name)
            .Take(maxResults)
            .ToList();

        var result = new List<ProductImageMatchResponse>(ordered.Count);
        foreach (var (row, dist) in ordered)
        {
            result.Add(new ProductImageMatchResponse
            {
                Id = row.Id,
                Name = row.Name,
                Sku = row.Sku,
                Size = row.Size,
                Type = row.Type,
                ExpirationDate = row.ExpirationDate,
                ImageUrl = row.ImageUrl,
                WarehouseId = row.WarehouseId,
                WarehouseName = row.WarehouseName,
                SectionId = row.SectionId,
                SectionName = row.SectionName,
                HammingDistance = dist
            });
        }

        return result;
    }

    private static int HammingDistance(ulong a, ulong b) => BitOperations.PopCount(a ^ b);

    internal static bool TryParseStoredHash(string? imageMetadata, out ulong hash)
    {
        hash = 0;
        if (string.IsNullOrWhiteSpace(imageMetadata))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(imageMetadata);
            var root = doc.RootElement;
            string? hex = null;
            if (root.TryGetProperty("hashHex", out var hx))
            {
                hex = hx.GetString();
            }
            else if (root.TryGetProperty("hash", out var h))
            {
                hex = h.GetString();
            }

            if (string.IsNullOrWhiteSpace(hex))
            {
                return false;
            }

            return ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hash);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
