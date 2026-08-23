using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YFToolbox.Core.Localization;
using YFToolbox.Features.Files.Rename;

namespace YFToolbox.Features.Files.ViewModels;

public partial class BatchRenameViewModel(IRenameService renameService) : ObservableObject
{
    private static readonly CompositeFormat FilesReadyMessage = CompositeFormat.Parse(AppStrings.FilesReadyFormat);
    private static readonly CompositeFormat InvalidRowsMessage = CompositeFormat.Parse(AppStrings.InvalidRowsFormat);

    public ObservableCollection<string> Files { get; } = [];

    public ObservableCollection<RenamePreviewItem> PreviewItems { get; } = [];

    [ObservableProperty]
    private string prefix = string.Empty;

    [ObservableProperty]
    private string suffix = string.Empty;

    [ObservableProperty]
    private string find = string.Empty;

    [ObservableProperty]
    private string replace = string.Empty;

    [ObservableProperty]
    private bool addNumbering;

    [ObservableProperty]
    private int startNumber = 1;

    [ObservableProperty]
    private int padding = 2;

    [ObservableProperty]
    private bool preserveExtension = true;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string status = AppStrings.Ready;

    public void AddFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Files.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                Files.Add(path);
            }
        }

        BuildPreview();
    }

    [RelayCommand]
    private void BuildPreview()
    {
        PreviewItems.Clear();
        foreach (var item in renameService.CreatePreview(Files, CreateOptions()))
        {
            PreviewItems.Add(item);
        }

        var invalidCount = PreviewItems.Count(item => !item.IsValid);
        Status = invalidCount == 0
            ? string.Format(CultureInfo.CurrentCulture, FilesReadyMessage, PreviewItems.Count)
            : string.Format(CultureInfo.CurrentCulture, InvalidRowsMessage, invalidCount);
    }

    [RelayCommand]
    private void Clear()
    {
        Files.Clear();
        PreviewItems.Clear();
        Status = AppStrings.Ready;
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (IsBusy || PreviewItems.Count == 0 || PreviewItems.Any(item => !item.IsValid))
        {
            Status = AppStrings.ResolvePreviewErrors;
            return;
        }

        IsBusy = true;
        try
        {
            var result = await renameService.ExecuteAsync(PreviewItems.ToArray());
            Status = result.Succeeded ? AppStrings.Done : string.Join(Environment.NewLine, result.Errors);
            if (result.Succeeded)
            {
                Files.Clear();
                PreviewItems.Clear();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private RenameOptions CreateOptions() => new(
        Prefix,
        Suffix,
        Find,
        Replace,
        AddNumbering,
        StartNumber,
        Padding,
        PreserveExtension);

    partial void OnPrefixChanged(string value) => BuildPreview();

    partial void OnSuffixChanged(string value) => BuildPreview();

    partial void OnFindChanged(string value) => BuildPreview();

    partial void OnReplaceChanged(string value) => BuildPreview();

    partial void OnAddNumberingChanged(bool value) => BuildPreview();

    partial void OnStartNumberChanged(int value) => BuildPreview();

    partial void OnPaddingChanged(int value) => BuildPreview();

    partial void OnPreserveExtensionChanged(bool value) => BuildPreview();
}
