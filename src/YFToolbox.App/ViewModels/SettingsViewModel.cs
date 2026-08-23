using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YFToolbox.Application.Contracts;
using YFToolbox.Core.Localization;
using YFToolbox.Core.Models;

namespace YFToolbox.App.ViewModels;

public partial class SettingsViewModel(ISettingsService settings) : ObservableObject
{
    private readonly LanguagePreference _initialLanguage = settings.Current.Language;

    public IReadOnlyList<LocalizedOption<ThemePreference>> Themes { get; } =
    [
        new(ThemePreference.System, AppStrings.SystemValue),
        new(ThemePreference.Light, AppStrings.Light),
        new(ThemePreference.Dark, AppStrings.Dark)
    ];

    public IReadOnlyList<LocalizedOption<LanguagePreference>> Languages { get; } =
    [
        new(LanguagePreference.System, AppStrings.SystemValue),
        new(LanguagePreference.German, AppStrings.German),
        new(LanguagePreference.English, AppStrings.English)
    ];

    public IReadOnlyList<LocalizedOption<OutputMode>> OutputModes { get; } =
    [
        new(OutputMode.CentralFolder, AppStrings.CentralFolder),
        new(OutputMode.SourceFolder, AppStrings.SourceFolder)
    ];

    public IReadOnlyList<LocalizedOption<OutputConflictPolicy>> CollisionPolicies { get; } =
    [
        new(OutputConflictPolicy.CreateUnique, AppStrings.CreateUnique),
        new(OutputConflictPolicy.Skip, AppStrings.Skip),
        new(OutputConflictPolicy.Ask, AppStrings.Ask),
        new(OutputConflictPolicy.Overwrite, AppStrings.Overwrite)
    ];

    public IReadOnlyList<LocalizedOption<QualityPreset>> QualityPresets { get; } =
    [
        new(QualityPreset.Small, AppStrings.Small),
        new(QualityPreset.Balanced, AppStrings.Balanced),
        new(QualityPreset.High, AppStrings.High)
    ];

    [ObservableProperty]
    private ThemePreference theme = settings.Current.Theme;

    [ObservableProperty]
    private LanguagePreference language = settings.Current.Language;

    [ObservableProperty]
    private string outputDirectory = settings.Current.OutputDirectory;

    [ObservableProperty]
    private OutputMode outputMode = settings.Current.OutputMode;

    [ObservableProperty]
    private OutputConflictPolicy collisionPolicy = settings.Current.CollisionPolicy;

    [ObservableProperty]
    private bool preserveMetadata = settings.Current.PreserveMetadata;

    [ObservableProperty]
    private QualityPreset imageQualityPreset = settings.Current.ImageQualityPreset;

    [ObservableProperty]
    private bool allowUpscale = settings.Current.AllowUpscale;

    [ObservableProperty]
    private bool historyEnabled = settings.Current.HistoryEnabled;

    [ObservableProperty]
    private string status = string.Empty;

    [RelayCommand]
    private async Task SaveAsync()
    {
        var updated = settings.Current with
        {
            Theme = Theme,
            Language = Language,
            OutputDirectory = OutputDirectory,
            OutputMode = OutputMode,
            CollisionPolicy = CollisionPolicy,
            PreserveMetadata = PreserveMetadata,
            ImageQualityPreset = ImageQualityPreset,
            AllowUpscale = AllowUpscale,
            HistoryEnabled = HistoryEnabled
        };
        await settings.SaveAsync(updated);
        App.ApplyTheme(Theme);
        Status = Language == _initialLanguage
            ? AppStrings.SettingsSaved
            : $"{AppStrings.SettingsSaved} {AppStrings.RestartForLanguage}";
    }
}
