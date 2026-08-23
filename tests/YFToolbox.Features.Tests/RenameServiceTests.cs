using YFToolbox.Core.Localization;
using YFToolbox.Features.Files.Rename;

namespace YFToolbox.Features.Tests;

public sealed class RenameServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"yftoolbox-tests-{Guid.NewGuid():N}");

    public RenameServiceTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void PreviewRejectsReservedWindowsNames()
    {
        var source = CreateFile("one.txt");
        var service = new RenameService();

        var preview = service.CreatePreview([source], new RenameOptions(Find: "one", Replace: "CON"));

        Assert.False(preview[0].IsValid);
        Assert.Equal(AppStrings.ReservedTargetName, preview[0].Error);
    }

    [Fact]
    public async Task ExecuteUsesPreviewAndRenamesAllFiles()
    {
        var first = CreateFile("one.txt");
        var second = CreateFile("two.txt");
        var service = new RenameService();
        var preview = service.CreatePreview(
            [first, second],
            new RenameOptions(Prefix: "photo-", AddNumbering: true, StartNumber: 5, Padding: 3));

        var result = await service.ExecuteAsync(preview, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.All(preview, item => Assert.True(File.Exists(item.TargetPath)));
        Assert.DoesNotContain(preview, item => File.Exists(item.SourcePath));
    }

    public void Dispose() => Directory.Delete(_directory, true);

    private string CreateFile(string name)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, "test");
        return path;
    }
}
