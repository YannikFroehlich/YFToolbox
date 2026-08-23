using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using YFToolbox.Core.Models;
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

    public void Dispose() => Directory.Delete(_directory, true);
}
