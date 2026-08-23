using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using YFToolbox.Core.Models;
using YFToolbox.Core.Localization;
using YFToolbox.Features.Images.Models;

namespace YFToolbox.Features.Images.Services;

public interface IImageProcessingService
{
    Task ProcessAsync(
        string inputPath,
        string outputPath,
        string targetExtension,
        ImageConversionOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class ImageProcessingService : IImageProcessingService
{
    private const long MaximumPixelsWithoutConfirmation = 100_000_000;
    private const long MaximumEstimatedBytesWithoutConfirmation = 1_000_000_000;
    private static readonly int[] IconSizes = [16, 24, 32, 48, 64, 128, 256];

    public async Task ProcessAsync(
        string inputPath,
        string outputPath,
        string targetExtension,
        ImageConversionOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedTarget = targetExtension.TrimStart('.').ToLowerInvariant();
        EnsureSupported(normalizedTarget);
        ValidateOptions(options);
        progress?.Report(5);

        var info = Image.Identify(inputPath);
        var estimatedBytes = checked((long)info.Width * info.Height * 4);
        if (!options.AllowLargeImage &&
            ((long)info.Width * info.Height > MaximumPixelsWithoutConfirmation ||
             estimatedBytes > MaximumEstimatedBytesWithoutConfirmation))
        {
            throw new InvalidOperationException(AppStrings.LargeImageConfirmation);
        }

        if (Path.GetExtension(inputPath).Equals(".webp", StringComparison.OrdinalIgnoreCase) &&
            info.FrameMetadataCollection.Count > 1)
        {
            throw new NotSupportedException(AppStrings.AnimatedWebpUnsupported);
        }

        await using var input = new FileStream(
            inputPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var image = await Image.LoadAsync<Rgba32>(input, cancellationToken).ConfigureAwait(false);
        progress?.Report(25);

        image.Mutate(context =>
        {
            context.AutoOrient();
            ApplyResize(context, image.Width, image.Height, options);
            if (options.RotationDegrees != 0)
            {
                context.Rotate(options.RotationDegrees);
            }

            if (options.FlipHorizontal)
            {
                context.Flip(FlipMode.Horizontal);
            }

            if (options.FlipVertical)
            {
                context.Flip(FlipMode.Vertical);
            }

            if (normalizedTarget is "jpg" or "jpeg")
            {
                context.BackgroundColor(Color.ParseHex(options.JpegBackground.TrimStart('#')));
            }
        });
        progress?.Report(60);

        await using var output = new FileStream(
            outputPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            1024 * 1024,
            FileOptions.Asynchronous | FileOptions.WriteThrough);

        if (normalizedTarget == "ico")
        {
            await WriteMultiSizeIconAsync(image, output, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var encoder = CreateEncoder(normalizedTarget, options);
            await image.SaveAsync(output, encoder, cancellationToken).ConfigureAwait(false);
        }

        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        progress?.Report(100);
    }

    private static void ValidateOptions(ImageConversionOptions options)
    {
        if (options.Width is <= 0 || options.Height is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Width and height must be positive.");
        }

        if (options.RotationDegrees is not (0 or 90 or 180 or 270))
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Rotation must be 0, 90, 180, or 270 degrees.");
        }
    }

    private static void ApplyResize(
        IImageProcessingContext context,
        int sourceWidth,
        int sourceHeight,
        ImageConversionOptions options)
    {
        if (options.Width is null && options.Height is null)
        {
            return;
        }

        var targetWidth = options.Width ?? (int)Math.Round(sourceWidth * (options.Height!.Value / (double)sourceHeight));
        var targetHeight = options.Height ?? (int)Math.Round(sourceHeight * (options.Width!.Value / (double)sourceWidth));

        if (options.LockAspectRatio && options.Width is not null && options.Height is not null)
        {
            var ratio = Math.Min(options.Width.Value / (double)sourceWidth, options.Height.Value / (double)sourceHeight);
            targetWidth = Math.Max(1, (int)Math.Round(sourceWidth * ratio));
            targetHeight = Math.Max(1, (int)Math.Round(sourceHeight * ratio));
        }

        if (!options.AllowUpscale && targetWidth >= sourceWidth && targetHeight >= sourceHeight)
        {
            return;
        }

        context.Resize(new ResizeOptions
        {
            Size = new Size(targetWidth, targetHeight),
            Mode = ResizeMode.Stretch,
            Sampler = KnownResamplers.Lanczos3,
            Compand = true
        });
    }

    private static IImageEncoder CreateEncoder(string target, ImageConversionOptions options)
    {
        var quality = options.QualityPreset switch
        {
            QualityPreset.High => 92,
            QualityPreset.Small => 70,
            _ => 85
        };

        return target switch
        {
            "jpg" or "jpeg" => new JpegEncoder
            {
                Quality = quality,
                SkipMetadata = !options.PreserveMetadata
            },
            "png" => new PngEncoder
            {
                CompressionLevel = options.QualityPreset switch
                {
                    QualityPreset.High => PngCompressionLevel.Level6,
                    QualityPreset.Small => PngCompressionLevel.BestCompression,
                    _ => PngCompressionLevel.Level7
                },
                SkipMetadata = !options.PreserveMetadata
            },
            "webp" => new WebpEncoder
            {
                Quality = quality,
                FileFormat = WebpFileFormatType.Lossy,
                SkipMetadata = !options.PreserveMetadata
            },
            "bmp" => new BmpEncoder { SkipMetadata = !options.PreserveMetadata },
            _ => throw new NotSupportedException($"The target format '{target}' is not supported.")
        };
    }

    private static async Task WriteMultiSizeIconAsync(
        Image<Rgba32> source,
        Stream destination,
        CancellationToken cancellationToken)
    {
        var images = new List<byte[]>(IconSizes.Length);
        foreach (var size in IconSizes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var frame = source.Clone(context => context.Resize(new ResizeOptions
            {
                Size = new Size(size, size),
                Mode = ResizeMode.Pad,
                PadColor = Color.Transparent,
                Sampler = KnownResamplers.Lanczos3
            }));
            await using var buffer = new MemoryStream();
            await frame.SaveAsPngAsync(buffer, new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.Level6,
                SkipMetadata = true
            }, cancellationToken).ConfigureAwait(false);
            images.Add(buffer.ToArray());
        }

        using var writer = new BinaryWriter(destination, System.Text.Encoding.UTF8, true);
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)images.Count);
        var offset = 6 + images.Count * 16;
        for (var index = 0; index < images.Count; index++)
        {
            var size = IconSizes[index];
            writer.Write((byte)(size == 256 ? 0 : size));
            writer.Write((byte)(size == 256 ? 0 : size));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(images[index].Length);
            writer.Write(offset);
            offset += images[index].Length;
        }

        foreach (var bytes in images)
        {
            await destination.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void EnsureSupported(string target)
    {
        if (target is not ("png" or "jpg" or "jpeg" or "webp" or "bmp" or "ico"))
        {
            throw new NotSupportedException($"The target format '{target}' is not supported.");
        }
    }
}
