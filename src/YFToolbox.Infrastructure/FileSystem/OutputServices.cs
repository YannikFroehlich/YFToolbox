using YFToolbox.Application.Contracts;
using YFToolbox.Core.Errors;
using YFToolbox.Core.Localization;
using YFToolbox.Core.Models;
using YFToolbox.Core.Settings;

namespace YFToolbox.Infrastructure.FileSystem;

public sealed class OutputPathResolver(IOutputConflictPrompt conflictPrompt) : IOutputPathResolver
{
    public string ResolveOutputDirectory(string sourcePath, string? requestedDirectory, AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(requestedDirectory))
        {
            return Path.GetFullPath(requestedDirectory);
        }

        return settings.OutputMode == OutputMode.SourceFolder
            ? Path.GetDirectoryName(Path.GetFullPath(sourcePath))!
            : Path.GetFullPath(settings.OutputDirectory);
    }

    public OutputPathResolution ResolveOutputPath(
        string sourcePath,
        string outputDirectory,
        string targetExtension,
        OutputConflictPolicy conflictPolicy)
    {
        Directory.CreateDirectory(outputDirectory);
        var normalizedExtension = targetExtension.StartsWith('.') ? targetExtension : $".{targetExtension}";
        var sourceFullPath = Path.GetFullPath(sourcePath);
        var candidate = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(sourceFullPath)}{normalizedExtension}");

        if (string.Equals(candidate, sourceFullPath, StringComparison.OrdinalIgnoreCase) &&
            conflictPolicy != OutputConflictPolicy.Overwrite)
        {
            candidate = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(sourceFullPath)}-converted{normalizedExtension}");
        }

        if (!File.Exists(candidate) || conflictPolicy == OutputConflictPolicy.Overwrite)
        {
            return new OutputPathResolution(candidate, conflictPolicy);
        }

        if (conflictPolicy == OutputConflictPolicy.Skip)
        {
            throw new AppOperationException(
                AppErrorCode.NameConflict,
                AppStrings.TargetExists,
                isSkipped: true);
        }

        if (conflictPolicy == OutputConflictPolicy.Ask)
        {
            var resolution = conflictPrompt.Resolve(candidate);
            if (resolution == OutputConflictPolicy.Skip)
            {
                throw new AppOperationException(
                    AppErrorCode.NameConflict,
                    AppStrings.FileSkipped,
                    isSkipped: true);
            }

            if (resolution == OutputConflictPolicy.Overwrite)
            {
                return new OutputPathResolution(candidate, OutputConflictPolicy.Overwrite);
            }
        }

        var baseName = Path.GetFileNameWithoutExtension(candidate);
        for (var index = 2; index < 10_000; index++)
        {
            var unique = Path.Combine(outputDirectory, $"{baseName} ({index}){normalizedExtension}");
            if (!File.Exists(unique))
            {
                return new OutputPathResolution(unique, OutputConflictPolicy.CreateUnique);
            }
        }

        throw new IOException("A unique output name could not be created.");
    }
}

public sealed class RejectingOutputConflictPrompt : IOutputConflictPrompt
{
    public OutputConflictPolicy Resolve(string targetPath) => OutputConflictPolicy.Skip;
}

public sealed class TempFileService(IAppPaths paths) : ITempFileService
{
    public string CreateSiblingTempPath(string finalPath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(finalPath))!;
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, $".{Path.GetFileName(finalPath)}.{Guid.NewGuid():N}.tmp");
    }

    public Task CommitAsync(
        string tempPath,
        string finalPath,
        OutputConflictPolicy conflictPolicy,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(tempPath))
        {
            throw new FileNotFoundException("The temporary output is missing.", tempPath);
        }

        if (File.Exists(finalPath) && conflictPolicy != OutputConflictPolicy.Overwrite)
        {
            throw new AppOperationException(
                AppErrorCode.NameConflict,
                AppStrings.TargetExists,
                isSkipped: conflictPolicy == OutputConflictPolicy.Skip);
        }

        File.Move(tempPath, finalPath, conflictPolicy == OutputConflictPolicy.Overwrite);
        return Task.CompletedTask;
    }

    public void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; the next startup retries stale application-owned entries.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup.
        }
    }

    public Task CleanupStaleAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(paths.TempDirectory))
        {
            return Task.CompletedTask;
        }

        var cutoff = DateTime.UtcNow.AddHours(-24);
        foreach (var directory in Directory.EnumerateDirectories(paths.TempDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Directory.GetLastWriteTimeUtc(directory) < cutoff)
            {
                TryDelete(directory);
            }
        }

        return Task.CompletedTask;
    }
}
