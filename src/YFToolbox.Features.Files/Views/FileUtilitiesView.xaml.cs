using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using YFToolbox.Core.Localization;
using YFToolbox.Features.Files.ViewModels;

namespace YFToolbox.Features.Files.Views;

public partial class FileUtilitiesView : Page
{
    private readonly FileUtilitiesViewModel _viewModel;

    public FileUtilitiesView(FileUtilitiesViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private async void OnChooseFile(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new() { Filter = AppStrings.AllFilesFilter };
        if (dialog.ShowDialog() == true)
        {
            await _viewModel.SelectFileAsync(dialog.FileName);
        }
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths && File.Exists(paths[0]))
        {
            await _viewModel.SelectFileAsync(paths[0]);
        }
    }
}
