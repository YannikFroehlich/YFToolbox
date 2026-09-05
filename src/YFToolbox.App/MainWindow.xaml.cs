using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;
using YFToolbox.App.Views;

namespace YFToolbox.App;

public partial class MainWindow : FluentWindow
{
    private const double CompactPaneBreakpoint = 1000;

    private readonly INavigationService _navigationService;
    private bool? _isWideLayout;

    public MainWindow(INavigationService navigationService, INavigationViewPageProvider pageProvider)
    {
        _navigationService = navigationService;
        InitializeComponent();
        RootNavigationView.SetPageProviderService(pageProvider);
        navigationService.SetNavigationControl(RootNavigationView);
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyPaneLayout();
        _navigationService.Navigate(typeof(HomePage));
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => ApplyPaneLayout();

    /// <summary>
    /// Collapses the pane to an icon rail on narrow windows. The pane is only forced when the
    /// breakpoint is actually crossed, so a manual toggle survives until the next crossing.
    /// </summary>
    private void ApplyPaneLayout()
    {
        var isWide = ActualWidth >= CompactPaneBreakpoint;
        if (_isWideLayout == isWide)
        {
            return;
        }

        _isWideLayout = isWide;
        RootNavigationView.SetCurrentValue(NavigationView.IsPaneOpenProperty, isWide);
    }
}
