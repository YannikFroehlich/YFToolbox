using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using YFToolbox.Core.Localization;
using YFToolbox.Features.Images.ViewModels;

namespace YFToolbox.Features.Images.Views;

public partial class ImageConverterView : Page
{
    private readonly ImageConverterViewModel _viewModel;

    public ImageConverterView(ImageConverterViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private async void OnChooseFiles(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Multiselect = true,
            Filter = AppStrings.ImagesFilter
        };
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
