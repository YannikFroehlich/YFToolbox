using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;
using YFToolbox.App.Views;

namespace YFToolbox.App;

public partial class MainWindow : FluentWindow
{
    private readonly INavigationService _navigationService;

    public MainWindow(INavigationService navigationService, INavigationViewPageProvider pageProvider)
    {
        _navigationService = navigationService;
        InitializeComponent();
        RootNavigationView.SetPageProviderService(pageProvider);
        navigationService.SetNavigationControl(RootNavigationView);
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => _navigationService.Navigate(typeof(HomePage));

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RootNavigationView.PaneDisplayMode = ActualWidth < 980
            ? NavigationViewPaneDisplayMode.LeftMinimal
            : NavigationViewPaneDisplayMode.Left;
    }
}
