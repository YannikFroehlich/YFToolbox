using YFToolbox.Core.Models;

namespace YFToolbox.Core.Settings;

public sealed record AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public OutputMode OutputMode { get; init; } = OutputMode.CentralFolder;

    public string OutputDirectory { get; init; } = GetDefaultOutputFolder();

    public bool OpenOutputAfterCompletion { get; init; }

    public OutputConflictPolicy CollisionPolicy { get; init; } = OutputConflictPolicy.CreateUnique;

    public ThemePreference Theme { get; init; } = ThemePreference.System;

    public LanguagePreference Language { get; init; } = LanguagePreference.System;

    public QualityPreset ImageQualityPreset { get; init; } = QualityPreset.Balanced;

    public bool PreserveMetadata { get; init; } = true;

    public bool AllowUpscale { get; init; }

    public bool HistoryEnabled { get; init; }

    public static AppSettings CreateDefault() => new();

    public static string GetDefaultOutputFolder()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documents, "YF Toolbox", "Output");
    }
}
