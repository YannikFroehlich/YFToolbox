using System.Windows.Controls;
using YFToolbox.App.ViewModels;

namespace YFToolbox.App.Views;

public partial class AboutPage : Page
{
    public AboutPage(AboutViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
