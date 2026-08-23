using System.Text.Json;
using Microsoft.Extensions.Logging;
using YFToolbox.Application.Contracts;
using YFToolbox.Core.Models;

namespace YFToolbox.Infrastructure.History;

public sealed class JsonHistoryService : IHistoryService
{
    private const int MaximumEntries = 100;
    private readonly string _historyFile;
    private readonly ISettingsService _settings;
    private readonly ILogger<JsonHistoryService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonHistoryService(
        IAppPaths paths,
        ISettingsService settings,
        ILogger<JsonHistoryService> logger)
    {
        _historyFile = Path.Combine(paths.DataDirectory, "history.json");
        _settings = settings;
        _logger = logger;
        _settings.SettingsChanged += OnSettingsChanged;
    }

    public async Task<IReadOnlyList<HistoryEntry>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Current.HistoryEnabled || !File.Exists(_historyFile))
        {
            return [];
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var stream = File.OpenRead(_historyFile);
            return await JsonSerializer.DeserializeAsync<List<HistoryEntry>>(
                stream,
                cancellationToken: cancellationToken).ConfigureAwait(false) ?? [];
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "The pathless action history could not be loaded.");
            return [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RecordAsync(HistoryEntry entry, CancellationToken cancellationToken = default)
    {
        if (!_settings.Current.HistoryEnabled)
        {
            return;
        }

        var entries = (await LoadAsync(cancellationToken).ConfigureAwait(false)).ToList();
        entries.Insert(0, entry);
        if (entries.Count > MaximumEntries)
        {
            entries.RemoveRange(MaximumEntries, entries.Count - MaximumEntries);
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_historyFile)!);
            var temporaryPath = _historyFile + $".{Guid.NewGuid():N}.tmp";
            try
            {
                await using var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    16_384,
                    FileOptions.Asynchronous | FileOptions.WriteThrough);
                await JsonSerializer.SerializeAsync(stream, entries, cancellationToken: cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                File.Move(temporaryPath, _historyFile, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(_historyFile))
            {
                File.Delete(_historyFile);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async void OnSettingsChanged(object? sender, YFToolbox.Core.Settings.AppSettings settings)
    {
        if (!settings.HistoryEnabled)
        {
            try
            {
                await ClearAsync().ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(exception, "The disabled action history could not be deleted.");
            }
        }
    }
}
