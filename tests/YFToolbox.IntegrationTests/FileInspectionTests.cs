using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using YFToolbox.Core.Models;
using YFToolbox.Features.Images.Models;
using YFToolbox.Features.Images.Services;
using YFToolbox.Infrastructure.FileSystem;

namespace YFToolbox.IntegrationTests;

public sealed class FileInspectionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"yftoolbox-inspection-{Guid.NewGuid():N}");

    public FileInspectionTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task DecoderDetectsImageAndExtensionMismatch()
    {
        var path = Path.Combine(_directory, "misnamed.jpg");
        using (var image = new Image<Rgba32>(17, 19))
        {
            await image.SaveAsPngAsync(path, TestContext.Current.CancellationToken);
        }

        var detector = new FileTypeDetector();
        var descriptor = await detector.DetectAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(FileCategory.Image, descriptor.Category);
        Assert.Equal(DetectionConfidence.DecoderVerified, descriptor.Confidence);
        Assert.False(descriptor.ExtensionMatchesContent);
        Assert.Equal(17, descriptor.PixelWidth);
        Assert.Equal(19, descriptor.PixelHeight);
    }

    [Theory]
    [InlineData("png", "image/png")]
    [InlineData("jpg", "image/jpeg")]
    [InlineData("webp", "image/webp")]
    [InlineData("bmp", "image/bmp")]
    public async Task FreeHeaderInspectorRecognizesRasterFormats(string extension, string expectedMimeType)
    {
        var path = Path.Combine(_directory, $"header.{extension}");
        using (var image = new Image<Rgba32>(23, 29, new Rgba32(12, 34, 56, 255)))
        {
            switch (extension)
            {
                case "png": await image.SaveAsPngAsync(path, TestContext.Current.CancellationToken); break;
                case "jpg": await image.SaveAsJpegAsync(path, TestContext.Current.CancellationToken); break;
                case "webp": await image.SaveAsWebpAsync(path, TestContext.Current.CancellationToken); break;
                case "bmp": await image.SaveAsBmpAsync(path, TestContext.Current.CancellationToken); break;
            }
        }

        var descriptor = await new FileTypeDetector().DetectAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(DetectionConfidence.DecoderVerified, descriptor.Confidence);
        Assert.Equal(expectedMimeType, descriptor.MimeType);
        Assert.Equal(23, descriptor.PixelWidth);
        Assert.Equal(29, descriptor.PixelHeight);
    }

    [Fact]
    public async Task FreeHeaderInspectorRecognizesMultiFrameIcon()
    {
        var source = Path.Combine(_directory, "icon-source.png");
        var icon = Path.Combine(_directory, "header.ico");
        using (var image = new Image<Rgba32>(64, 48, new Rgba32(12, 34, 56, 200)))
        {
            await image.SaveAsPngAsync(source, TestContext.Current.CancellationToken);
        }

        await new ImageProcessingService().ProcessAsync(
            source,
            icon,
            "ico",
            new ImageConversionOptions(),
            cancellationToken: TestContext.Current.CancellationToken);
        var descriptor = await new FileTypeDetector().DetectAsync(icon, TestContext.Current.CancellationToken);

        Assert.Equal(DetectionConfidence.DecoderVerified, descriptor.Confidence);
        Assert.Equal("image/x-icon", descriptor.MimeType);
        Assert.Equal(256, descriptor.PixelWidth);
        Assert.Equal(256, descriptor.PixelHeight);
        Assert.Equal(7, descriptor.FrameCount);
    }

    public void Dispose() => Directory.Delete(_directory, true);
}
