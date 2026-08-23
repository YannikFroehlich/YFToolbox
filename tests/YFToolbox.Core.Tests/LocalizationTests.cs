using System.Collections;
using System.Globalization;
using System.Resources;
using YFToolbox.Core.Localization;

namespace YFToolbox.Core.Tests;

public sealed class LocalizationTests
{
    [Fact]
    public void GermanAndEnglishResourcesHaveMatchingNonEmptyKeys()
    {
        var manager = new ResourceManager(
            "YFToolbox.Core.Localization.Strings",
            typeof(AppStrings).Assembly);
        var english = Read(manager, CultureInfo.GetCultureInfo("en-US"));
        var german = Read(manager, CultureInfo.GetCultureInfo("de-DE"));

        Assert.Equal(english.Keys.Order(), german.Keys.Order());
        Assert.All(english.Values, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        Assert.All(german.Values, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        Assert.NotEqual("ToolImageConverter", german["ToolImageConverter"]);
        Assert.NotEqual("ToolBatchRename", english["ToolBatchRename"]);
        Assert.NotEqual("ToolFileHash", german["ToolFileHash"]);
    }

    private static Dictionary<string, string> Read(ResourceManager manager, CultureInfo culture)
    {
        var resourceSet = manager.GetResourceSet(culture, true, true);
        Assert.NotNull(resourceSet);
        return resourceSet.Cast<DictionaryEntry>().ToDictionary(
            entry => (string)entry.Key,
            entry => (string)entry.Value!);
    }
}
