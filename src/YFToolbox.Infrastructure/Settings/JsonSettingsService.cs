using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using YFToolbox.Application.Contracts;
using YFToolbox.Core.Settings;

namespace YFToolbox.Infrastructure.Settings;

public sealed class JsonSettingsService(
    IAppPaths paths,
    ILogger<JsonSettingsService> logger) : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SemaphoreSlim _gate = new(1, 1);

    public AppSettings Current { get; private set; } = AppSettings.CreateDefault();

    public event EventHandler<AppSettings>? SettingsChanged;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(paths.SettingsFile))
            {
                Current = AppSettings.CreateDefault();
            }
            else
            {
                await using var stream = new FileStream(
                    paths.SettingsFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    16_384,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                Current = Validate(await JsonSerializer.DeserializeAsync<AppSettings>(
                    stream,
                    SerializerOptions,
                    cancellationToken).ConfigureAwait(false));
            }
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Settings could not be loaded; defaults will be used.");
            TryBackupCorruptSettings();
            Current = AppSettings.CreateDefault();
        }
        finally
        {
            _gate.Release();
        }

        SettingsChanged?.Invoke(this, Current);
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var validated = Validate(settings);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(paths.DataDirectory);
            var temporaryPath = paths.SettingsFile + $".{Guid.NewGuid():N}.tmp";
            try
            {
                await using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    16_384,
                    FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await JsonSerializer.SerializeAsync(
                        stream,
                        validated,
                        SerializerOptions,
                        cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                File.Move(temporaryPath, paths.SettingsFile, true);
                Current = validated;
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

        SettingsChanged?.Invoke(this, Current);
    }

    private static AppSettings Validate(AppSettings? settings)
    {
        if (settings is null || settings.SchemaVersion != AppSettings.CurrentSchemaVersion)
        {
            return AppSettings.CreateDefault();
        }

        var outputDirectory = string.IsNullOrWhiteSpace(settings.OutputDirectory)
            ? AppSettings.CreateDefault().OutputDirectory
            : Path.GetFullPath(Environment.ExpandEnvironmentVariables(settings.OutputDirectory));
        return settings with { OutputDirectory = outputDirectory };
    }

    private void TryBackupCorruptSettings()
    {
        try
        {
            if (File.Exists(paths.SettingsFile))
            {
                var backupPath = $"{paths.SettingsFile}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}";
                File.Move(paths.SettingsFile, backupPath, true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(exception, "The corrupt settings file could not be backed up.");
        }
    }
}
