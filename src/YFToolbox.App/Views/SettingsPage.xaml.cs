using System.Windows.Controls;
using YFToolbox.App.ViewModels;

namespace YFToolbox.App.Views;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
