namespace BoutiqueInventory.Application.Interfaces;

/// <summary>
/// Computes a compact perceptual fingerprint for image similarity search
/// (8×8 average hash / aHash).
/// </summary>
public interface IImageHashService
{
    /// <summary>
    /// Reads an image from the stream (JPEG/PNG/WebP/GIF, etc.), computes a 64-bit aHash,
    /// and returns it. The stream must be seekable or fully readable from current position.
    /// </summary>
    /// <exception cref="InvalidOperationException">When the stream does not contain a decodable image.</exception>
    ulong ComputeAverageHash64(Stream imageStream);
}
