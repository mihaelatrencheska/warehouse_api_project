using BoutiqueInventory.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BoutiqueInventory.Infrastructure.Imaging;

/// <inheritdoc cref="IImageHashService"/>
public sealed class ImageHashService : IImageHashService
{
    /// <inheritdoc />
    public ulong ComputeAverageHash64(Stream imageStream)
    {
        try
        {
            using var image = Image.Load<Rgba32>(imageStream);
            image.Mutate(x => x.Resize(8, 8));

            Span<byte> lum = stackalloc byte[64];
            var idx = 0;
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    var p = image[x, y];
                    lum[idx++] = (byte)(0.299 * p.R + 0.587 * p.G + 0.114 * p.B);
                }
            }

            var sum = 0;
            for (var i = 0; i < 64; i++)
            {
                sum += lum[i];
            }

            var avg = sum / 64.0;

            ulong hash = 0;
            for (var i = 0; i < 64; i++)
            {
                if (lum[i] >= avg)
                {
                    hash |= 1UL << i;
                }
            }

            return hash;
        }
        catch (UnknownImageFormatException ex)
        {
            throw new InvalidOperationException("Unsupported or corrupt image.", ex);
        }
    }
}
