using YFToolbox.App.Tests.Fakes;
using YFToolbox.App.ViewModels;
using YFToolbox.Core.Models;

namespace YFToolbox.App.Tests;

public sealed class AboutViewModelTests
{
    [Fact]
    public void PropertiesAreProjectedFromBuildInfo()
    {
        var buildTime = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var buildInfo = new BuildInfo("1.2.3", "abcdef1234567", "stable", buildTime);
        var sut = new AboutViewModel(new FakeBuildInfoService(buildInfo));

        Assert.Equal("1.2.3", sut.Version);
        Assert.Equal("abcdef1", sut.Commit);
        Assert.Equal("stable", sut.Channel);
        Assert.Equal(
            buildTime.ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture),
            sut.BuildTime);
    }

    [Fact]
    public void BuildTimeFallsBackToLocalWhenBuildInfoHasNoTimestamp()
    {
        var buildInfo = new BuildInfo("0.1.0-dev", "0000000", "dev", null);
        var sut = new AboutViewModel(new FakeBuildInfoService(buildInfo));

        Assert.Equal("local", sut.BuildTime);
    }
}
