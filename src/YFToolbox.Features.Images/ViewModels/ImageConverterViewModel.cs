using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YFToolbox.Application.Contracts;
using YFToolbox.Core.Localization;
using YFToolbox.Core.Models;
using YFToolbox.Core.Processing;
using YFToolbox.Features.Images.Models;
using YFToolbox.Features.Images.Services;

namespace YFToolbox.Features.Images.ViewModels;

public partial class ImageConverterViewModel(
    IFileInspectionService inspection,
    IJobQueue jobQueue,
    ISettingsService settings) : ObservableObject
{
    private static readonly CompositeFormat FilesReadyMessage = CompositeFormat.Parse(AppStrings.FilesReadyFormat);
    private static readonly CompositeFormat ResultsSummaryMessage = CompositeFormat.Parse(AppStrings.ResultsSummaryFormat);

    private CancellationTokenSource? _cancellation;

    public ObservableCollection<FileDescriptor> Items { get; } = [];

    public ObservableCollection<ProcessingItemResult> Results { get; } = [];

    public IReadOnlyList<string> TargetFormats { get; } = ["PNG", "JPG", "WEBP", "BMP", "ICO"];

    public IReadOnlyList<LocalizedOption<QualityPreset>> QualityPresets { get; } =
    [
        new(QualityPreset.Small, AppStrings.Small),
        new(QualityPreset.Balanced, AppStrings.Balanced),
        new(QualityPreset.High, AppStrings.High)
    ];

    public IReadOnlyList<int> Rotations { get; } = [0, 90, 180, 270];

    [ObservableProperty]
    private string targetFormat = "PNG";

    [ObservableProperty]
    private string width = string.Empty;

    [ObservableProperty]
    private string height = string.Empty;

    [ObservableProperty]
    private bool lockAspectRatio = true;

    [ObservableProperty]
    private bool allowUpscale = settings.Current.AllowUpscale;

    [ObservableProperty]
    private bool preserveMetadata = settings.Current.PreserveMetadata;

    [ObservableProperty]
    private bool flipHorizontal;

    [ObservableProperty]
    private bool flipVertical;

    [ObservableProperty]
    private bool allowLargeImage;

    [ObservableProperty]
    private int rotation;

    [ObservableProperty]
    private QualityPreset qualityPreset = settings.Current.ImageQualityPreset;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private string status = AppStrings.Ready;

    [ObservableProperty]
    private string transparencyWarning = string.Empty;

    public async Task AddFilesAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        Status = AppStrings.Processing;
        try
        {
            var descriptors = await inspection.InspectManyAsync(paths, cancellationToken);
            foreach (var descriptor in descriptors.Where(item => item.Category == FileCategory.Image))
            {
                if (Items.All(existing => !string.Equals(
                        existing.FullPath,
                        descriptor.FullPath,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    Items.Add(descriptor);
                }
            }

            Status = Items.Count == 0
                ? AppStrings.UnsupportedFile
                : string.Format(CultureInfo.CurrentCulture, FilesReadyMessage, Items.Count);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Status = exception.Message;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Items.Clear();
        Results.Clear();
        Progress = 0;
        Status = AppStrings.Ready;
    }

    [RelayCommand]
    private void Cancel() => _cancellation?.Cancel();

    [RelayCommand]
    private async Task StartAsync()
    {
        if (IsBusy || Items.Count == 0)
        {
            Status = AppStrings.SelectAtLeastOneFile;
            return;
        }

        if (!TryParseDimension(Width, out var parsedWidth) || !TryParseDimension(Height, out var parsedHeight))
        {
            Status = AppStrings.WidthHeightPositive;
            return;
        }

        _cancellation = new CancellationTokenSource();
        IsBusy = true;
        Progress = 0;
        Results.Clear();
        try
        {
            var options = new ImageConversionOptions(
                parsedWidth,
                parsedHeight,
                LockAspectRatio,
                AllowUpscale,
                Rotation,
                FlipHorizontal,
                FlipVertical,
                QualityPreset,
                PreserveMetadata,
                AllowLargeImage);
            var request = new ProcessingRequest(
                Guid.NewGuid(),
                Items.ToArray(),
                ImageOperationHandler.Id,
                TargetFormat.ToLowerInvariant(),
                null,
                settings.Current.CollisionPolicy,
                options);
            var progress = new Progress<ProgressSnapshot>(snapshot =>
            {
                var itemPortion = snapshot.TotalCount == 0 ? 0 : 100d / snapshot.TotalCount;
                Progress = Math.Clamp(
                    ((snapshot.CurrentIndex - 1) * itemPortion) + ((snapshot.Percent ?? 0) / 100d * itemPortion),
                    0,
                    100);
                Status = string.IsNullOrWhiteSpace(snapshot.CurrentFile)
                    ? snapshot.Message
                    : $"{snapshot.CurrentFile} — {snapshot.Message}";
            });
            var result = await jobQueue.EnqueueAsync(request, progress, _cancellation.Token);
            foreach (var item in result.Items)
            {
                Results.Add(item);
            }

            Progress = 100;
            Status = string.Format(
                CultureInfo.CurrentCulture,
                ResultsSummaryMessage,
                result.SucceededCount,
                result.FailedCount,
                result.SkippedCount);
        }
        catch (OperationCanceledException)
        {
            Status = AppStrings.Cancel;
        }
        finally
        {
            IsBusy = false;
            _cancellation.Dispose();
            _cancellation = null;
        }
    }

    [RelayCommand]
    private async Task RetryFailedAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var retryItems = Results
            .Where(item => item.Status == ProcessingStatus.Failed)
            .Select(item => item.Input)
            .ToArray();
        if (retryItems.Length == 0)
        {
            Status = AppStrings.Done;
            return;
        }

        Items.Clear();
        foreach (var item in retryItems)
        {
            Items.Add(item);
        }

        await StartAsync();
    }

    private static bool TryParseDimension(string value, out int? result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            return true;
        }

        if (int.TryParse(value, out var parsed) && parsed > 0)
        {
            result = parsed;
            return true;
        }

        result = null;
        return false;
    }

    partial void OnTargetFormatChanged(string value) =>
        TransparencyWarning = value.Equals("JPG", StringComparison.OrdinalIgnoreCase)
            ? AppStrings.JpegTransparencyWarning
            : string.Empty;
}
