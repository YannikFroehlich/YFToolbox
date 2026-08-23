using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using YFToolbox.Features.Images.Models;
using YFToolbox.Features.Images.Services;

namespace YFToolbox.Features.Tests;

public sealed class ImageProcessingServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"yftoolbox-images-{Guid.NewGuid():N}");

    public ImageProcessingServiceTests() => Directory.CreateDirectory(_directory);

    [Theory]
    [InlineData("png")]
    [InlineData("jpg")]
    [InlineData("webp")]
    [InlineData("bmp")]
    [InlineData("ico")]
    public async Task ConvertsToEveryV1Target(string extension)
    {
        var source = Path.Combine(_directory, "source.png");
        var target = Path.Combine(_directory, $"target.{extension}");
        using (var image = new Image<Rgba32>(64, 48, new Rgba32(32, 128, 220, 180)))
        {
            await image.SaveAsPngAsync(source, TestContext.Current.CancellationToken);
        }

        var service = new ImageProcessingService();
        await service.ProcessAsync(
            source,
            target,
            extension,
            new ImageConversionOptions(Width: 32, Height: 32),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(File.Exists(target));
        Assert.True(new FileInfo(target).Length > 0);
        var info = Image.Identify(target);
        Assert.NotNull(info);
        if (extension == "ico")
        {
            Assert.Equal(7, info.FrameMetadataCollection.Count);
        }
    }

    [Fact]
    public async Task DoesNotUpscaleByDefault()
    {
        var source = Path.Combine(_directory, "small.png");
        var target = Path.Combine(_directory, "large.png");
        using (var image = new Image<Rgba32>(20, 10, new Rgba32(100, 149, 237)))
        {
            await image.SaveAsPngAsync(source, TestContext.Current.CancellationToken);
        }

        await new ImageProcessingService().ProcessAsync(
            source,
            target,
            "png",
            new ImageConversionOptions(Width: 200, Height: 100),
            cancellationToken: TestContext.Current.CancellationToken);

        var info = Image.Identify(target);
        Assert.Equal(20, info.Width);
        Assert.Equal(10, info.Height);
    }

    [Fact]
    public async Task JpegOutputComposesTransparencyOnWhite()
    {
        var source = Path.Combine(_directory, "transparent.png");
        var target = Path.Combine(_directory, "opaque.jpg");
        using (var image = new Image<Rgba32>(16, 16, new Rgba32(0, 0, 0, 0)))
        {
            await image.SaveAsPngAsync(source, TestContext.Current.CancellationToken);
        }

        await new ImageProcessingService().ProcessAsync(
            source,
            target,
            "jpg",
            new ImageConversionOptions(),
            cancellationToken: TestContext.Current.CancellationToken);

        using var output = await Image.LoadAsync<Rgba32>(target, TestContext.Current.CancellationToken);
        var pixel = output[8, 8];
        Assert.True(pixel.R > 245 && pixel.G > 245 && pixel.B > 245 && pixel.A == 255);
    }

    public void Dispose() => Directory.Delete(_directory, true);
}
