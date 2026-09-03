using YFToolbox.App.Tests.Fakes;
using YFToolbox.App.ViewModels;
using YFToolbox.Core.Models;
using YFToolbox.Core.Settings;

namespace YFToolbox.App.Tests;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void ConstructorSeedsPropertiesFromCurrentSettings()
    {
        var settings = new FakeSettingsService
        {
            Current = AppSettings.CreateDefault() with
            {
                Theme = ThemePreference.Dark,
                Language = LanguagePreference.German,
                OutputDirectory = @"D:\output",
                OutputMode = OutputMode.SourceFolder,
                CollisionPolicy = OutputConflictPolicy.Overwrite,
                PreserveMetadata = false,
                ImageQualityPreset = QualityPreset.High,
                AllowUpscale = true,
                HistoryEnabled = true
            }
        };

        var sut = new SettingsViewModel(settings);

        Assert.Equal(ThemePreference.Dark, sut.Theme);
        Assert.Equal(LanguagePreference.German, sut.Language);
        Assert.Equal(@"D:\output", sut.OutputDirectory);
        Assert.Equal(OutputMode.SourceFolder, sut.OutputMode);
        Assert.Equal(OutputConflictPolicy.Overwrite, sut.CollisionPolicy);
        Assert.False(sut.PreserveMetadata);
        Assert.Equal(QualityPreset.High, sut.ImageQualityPreset);
        Assert.True(sut.AllowUpscale);
        Assert.True(sut.HistoryEnabled);
    }

    [Fact]
    public void OptionListsExposeEveryEnumValueExactlyOnce()
    {
        var sut = new SettingsViewModel(new FakeSettingsService());

        Assert.Equal(Enum.GetValues<ThemePreference>().Length, sut.Themes.Count);
        Assert.Equal(Enum.GetValues<LanguagePreference>().Length, sut.Languages.Count);
        Assert.Equal(Enum.GetValues<OutputMode>().Length, sut.OutputModes.Count);
        Assert.Equal(Enum.GetValues<OutputConflictPolicy>().Length, sut.CollisionPolicies.Count);
        Assert.Equal(Enum.GetValues<QualityPreset>().Length, sut.QualityPresets.Count);
    }
}
