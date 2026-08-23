using YFToolbox.Application.Contracts;
using YFToolbox.Core.Errors;
using YFToolbox.Core.Models;
using YFToolbox.Infrastructure.FileSystem;

namespace YFToolbox.IntegrationTests;

public sealed class OutputPathResolverTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"yftoolbox-output-{Guid.NewGuid():N}");

    public OutputPathResolverTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void UniquePolicyNeverReturnsExistingPath()
    {
        var source = CreateFile("source.png");
        CreateFile("source.jpg");
        var resolver = new OutputPathResolver(new FixedPrompt(OutputConflictPolicy.Skip));

        var result = resolver.ResolveOutputPath(source, _directory, "jpg", OutputConflictPolicy.CreateUnique);

        Assert.False(File.Exists(result.Path));
        Assert.EndsWith("source (2).jpg", result.Path, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(OutputConflictPolicy.CreateUnique, result.EffectiveConflictPolicy);
    }

    [Fact]
    public void AskPolicyCarriesExplicitOverwriteDecision()
    {
        var source = CreateFile("source.png");
        var target = CreateFile("source.jpg");
        var resolver = new OutputPathResolver(new FixedPrompt(OutputConflictPolicy.Overwrite));

        var result = resolver.ResolveOutputPath(source, _directory, "jpg", OutputConflictPolicy.Ask);

        Assert.Equal(target, result.Path);
        Assert.Equal(OutputConflictPolicy.Overwrite, result.EffectiveConflictPolicy);
    }

    [Fact]
    public void SkipPolicyProducesStructuredSkippedError()
    {
        var source = CreateFile("source.png");
        CreateFile("source.jpg");
        var resolver = new OutputPathResolver(new FixedPrompt(OutputConflictPolicy.Skip));

        var exception = Assert.Throws<AppOperationException>(() =>
            resolver.ResolveOutputPath(source, _directory, "jpg", OutputConflictPolicy.Skip));

        Assert.True(exception.IsSkipped);
        Assert.Equal(AppErrorCode.NameConflict, exception.Code);
    }

    public void Dispose() => Directory.Delete(_directory, true);

    private string CreateFile(string name)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, "fixture");
        return path;
    }

    private sealed class FixedPrompt(OutputConflictPolicy policy) : IOutputConflictPrompt
    {
        public OutputConflictPolicy Resolve(string targetPath) => policy;
    }
}
