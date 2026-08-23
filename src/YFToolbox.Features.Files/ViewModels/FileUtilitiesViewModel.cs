using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YFToolbox.Application.Contracts;
using YFToolbox.Core.Localization;
using YFToolbox.Core.Models;

namespace YFToolbox.Features.Files.ViewModels;

public partial class FileUtilitiesViewModel(
    IFileInspectionService inspection,
    IHashService hashService,
    IClipboardService clipboard) : ObservableObject
{
    private CancellationTokenSource? _cancellation;

    [ObservableProperty]
    private FileDescriptor? selectedFile;

    [ObservableProperty]
    private string sha256 = string.Empty;

    [ObservableProperty]
    private string md5 = string.Empty;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string status = AppStrings.Ready;

    public async Task SelectFileAsync(string path, CancellationToken cancellationToken = default)
    {
        SelectedFile = await inspection.InspectAsync(path, cancellationToken);
        Sha256 = string.Empty;
        Md5 = string.Empty;
        Progress = 0;
        Status = AppStrings.Ready;
    }

    [RelayCommand]
    private async Task CalculateHashesAsync()
    {
        if (SelectedFile is null || IsBusy)
        {
            Status = AppStrings.SelectAtLeastOneFile;
            return;
        }

        _cancellation = new CancellationTokenSource();
        IsBusy = true;
        try
        {
            var progress = new Progress<double>(value => Progress = value * 100);
            Status = "SHA-256";
            Sha256 = await hashService.ComputeSha256Async(SelectedFile.FullPath, progress, _cancellation.Token);
            Progress = 0;
            Status = AppStrings.Md5Legacy;
            Md5 = await hashService.ComputeMd5Async(SelectedFile.FullPath, progress, _cancellation.Token);
            Status = AppStrings.Done;
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
    private void Cancel() => _cancellation?.Cancel();

    [RelayCommand]
    private void Copy(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            clipboard.SetText(value);
            Status = AppStrings.Copy;
        }
    }
}
