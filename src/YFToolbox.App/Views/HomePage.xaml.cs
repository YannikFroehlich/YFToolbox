using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using YFToolbox.App.ViewModels;
using YFToolbox.Core.Localization;

namespace YFToolbox.App.Views;

public partial class HomePage : Page
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private async void OnChooseFiles(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new() { Multiselect = true, Filter = AppStrings.AllFilesFilter };
        if (dialog.ShowDialog() == true)
        {
            await _viewModel.AddFilesAsync(dialog.FileNames);
        }
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths && paths.All(File.Exists))
        {
            await _viewModel.AddFilesAsync(paths);
        }
    }
}
