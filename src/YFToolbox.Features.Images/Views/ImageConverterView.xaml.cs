using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

    private void OnDragEnter(object sender, DragEventArgs e) => SetDropHighlight(true);

    private void OnDragLeave(object sender, DragEventArgs e) => SetDropHighlight(false);

    private void SetDropHighlight(bool isActive)
    {
        DropOutline.Stroke = (Brush)FindResource(isActive
            ? "AccentFillColorDefaultBrush"
            : "ControlStrokeColorDefaultBrush");
        DropOutline.StrokeThickness = isActive ? 2 : 1;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        SetDropHighlight(false);
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths && paths.All(File.Exists))
        {
            await _viewModel.AddFilesAsync(paths);
        }
    }
}
