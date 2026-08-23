using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui;
using YFToolbox.Application.Contracts;
using YFToolbox.Core.Localization;
using YFToolbox.Core.Models;
using YFToolbox.Features.Files.Views;
using YFToolbox.Features.Images.Views;

namespace YFToolbox.App.ViewModels;

public partial class HomeViewModel(
    IFileInspectionService inspection,
    IActionSuggestionService suggestions,
    INavigationService navigation) : ObservableObject
{
    private static readonly CompositeFormat FilesInspectedMessage =
        CompositeFormat.Parse(AppStrings.FilesInspectedFormat);

    public ObservableCollection<DashboardFileItem> Files { get; } = [];

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string status = AppStrings.Ready;

    public async Task AddFilesAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        Status = AppStrings.Processing;
        try
        {
            var inspected = await inspection.InspectManyAsync(paths, cancellationToken);
            foreach (var descriptor in inspected)
            {
                var actions = suggestions.Suggest(descriptor).Select(tool => AppStrings.Get(tool.NameResourceKey)).ToArray();
                Files.Add(new DashboardFileItem(
                    descriptor,
                    actions.Length == 0 ? AppStrings.UnsupportedFile : string.Join(" · ", actions),
                    descriptor.InspectionError == AppErrorCode.CorruptInput
                        ? AppStrings.CorruptImage
                        : descriptor.ExtensionMatchesContent ? string.Empty : AppStrings.ExtensionMismatch));
            }

            Status = string.Format(CultureInfo.CurrentCulture, FilesInspectedMessage, Files.Count);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Status = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Files.Clear();
        Status = AppStrings.Ready;
    }

    [RelayCommand]
    private void OpenConverter() => navigation.Navigate(typeof(ImageConverterView));

    [RelayCommand]
    private void OpenRename() => navigation.Navigate(typeof(BatchRenameView));

    [RelayCommand]
    private void OpenUtilities() => navigation.Navigate(typeof(FileUtilitiesView));
}

public sealed record DashboardFileItem(FileDescriptor File, string Actions, string Warning);
