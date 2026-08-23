using YFToolbox.Core.Models;

namespace YFToolbox.Core.Tests;

public sealed class CoreModelTests
{
    [Fact]
    public void FileDescriptorFormatsSizeAndDimensions()
    {
        var descriptor = new FileDescriptor(
            "C:\\image.png",
            "image.png",
            ".png",
            1_572_864,
            DateTimeOffset.UtcNow,
            "image/png",
            FileCategory.Image,
            DetectionConfidence.DecoderVerified,
            true,
            1920,
            1080);

        Assert.Matches(@"^1[,.]5 MB$", descriptor.DisplaySize);
        Assert.Equal("1920 × 1080", descriptor.Dimensions);
    }

    [Fact]
    public void BuildInfoUsesSevenCharacterShortCommit()
    {
        var info = new BuildInfo("1.2.3", "abcdef0123456789", "Stable", null);

        Assert.Equal("abcdef0", info.ShortCommit);
    }
}
