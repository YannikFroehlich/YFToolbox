using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using YFToolbox.App.ViewModels;

namespace YFToolbox.App.Views;

public partial class SettingsPage : Page
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private void OnBrowseOutputFolder(object sender, RoutedEventArgs e)
    {
        OpenFolderDialog dialog = new() { Multiselect = false };
        if (Directory.Exists(_viewModel.OutputDirectory))
        {
            dialog.InitialDirectory = _viewModel.OutputDirectory;
        }

        if (dialog.ShowDialog() == true)
        {
            _viewModel.OutputDirectory = dialog.FolderName;
        }
    }
}
