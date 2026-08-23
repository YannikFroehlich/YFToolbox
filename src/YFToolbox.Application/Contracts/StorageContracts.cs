using YFToolbox.Core.Models;
using YFToolbox.Core.Settings;

namespace YFToolbox.Application.Contracts;

public interface ISettingsService
{
    AppSettings Current { get; }

    event EventHandler<AppSettings>? SettingsChanged;

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}

public interface IOutputPathResolver
{
    string ResolveOutputDirectory(string sourcePath, string? requestedDirectory, AppSettings settings);

    OutputPathResolution ResolveOutputPath(
        string sourcePath,
        string outputDirectory,
        string targetExtension,
        OutputConflictPolicy conflictPolicy);
}

public interface IOutputConflictPrompt
{
    OutputConflictPolicy Resolve(string targetPath);
}

public interface ITempFileService
{
    string CreateSiblingTempPath(string finalPath);

    Task CommitAsync(
        string tempPath,
        string finalPath,
        OutputConflictPolicy conflictPolicy,
        CancellationToken cancellationToken = default);

    void TryDelete(string path);

    Task CleanupStaleAsync(CancellationToken cancellationToken = default);
}

public interface IBuildInfoService
{
    BuildInfo Current { get; }
}

public interface IHistoryService
{
    Task<IReadOnlyList<HistoryEntry>> LoadAsync(CancellationToken cancellationToken = default);

    Task RecordAsync(HistoryEntry entry, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}

public interface IAppPaths
{
    string DataDirectory { get; }

    string SettingsFile { get; }

    string LogsDirectory { get; }

    string TempDirectory { get; }
}
