using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

    private void OnDragEnter(object sender, DragEventArgs e) => SetDropHighlight(true);

    private void OnDragLeave(object sender, DragEventArgs e) => SetDropHighlight(false);

    private async void OnDrop(object sender, DragEventArgs e)
    {
        SetDropHighlight(false);
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths && paths.All(File.Exists))
        {
            await _viewModel.AddFilesAsync(paths);
        }
    }

    private void SetDropHighlight(bool isActive)
    {
        DropOutline.Stroke = (Brush)FindResource(isActive
            ? "AccentFillColorDefaultBrush"
            : "ControlStrokeColorDefaultBrush");
        DropOutline.StrokeThickness = isActive ? 2 : 1;
    }
}
