using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using YFToolbox.Core.Localization;
using YFToolbox.Features.Files.ViewModels;

namespace YFToolbox.Features.Files.Views;

public partial class BatchRenameView : Page
{
    private readonly BatchRenameViewModel _viewModel;

    public BatchRenameView(BatchRenameViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private void OnChooseFiles(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new() { Multiselect = true, Filter = AppStrings.AllFilesFilter };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.AddFiles(dialog.FileNames);
        }
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths && paths.All(File.Exists))
        {
            _viewModel.AddFiles(paths);
        }
    }
}
