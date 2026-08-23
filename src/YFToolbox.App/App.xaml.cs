using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.DependencyInjection;
using YFToolbox.Application;
using YFToolbox.Application.Contracts;
using YFToolbox.App.Services;
using YFToolbox.App.ViewModels;
using YFToolbox.App.Views;
using YFToolbox.Core.Models;
using YFToolbox.Features.Files;
using YFToolbox.Features.Images;
using YFToolbox.Infrastructure;

namespace YFToolbox.App;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private bool _isSmokeTest;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _isSmokeTest = e.Args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase);
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YFToolbox",
            "Logs");
        Directory.CreateDirectory(logDirectory);

        _host = Host.CreateDefaultBuilder(e.Args)
            .UseSerilog((_, configuration) => configuration
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.File(
                    Path.Combine(logDirectory, "yftoolbox-.log"),
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 14,
                    shared: true))
            .ConfigureServices(services =>
            {
                services.AddYfToolboxApplication();
                services.AddYfToolboxInfrastructure();
                new ImageFeatureModule().Register(services);
                new FileFeatureModule().Register(services);
                services.AddNavigationViewPageProvider();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IClipboardService, ClipboardService>();
                services.AddSingleton<IOutputConflictPrompt, OutputConflictPrompt>();
                services.AddSingleton<MainWindow>();
                services.AddTransient<HomeViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<AboutViewModel>();
                services.AddTransient<HomePage>();
                services.AddTransient<SettingsPage>();
                services.AddTransient<AboutPage>();
            })
            .Build();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            await _host.StartAsync();
            var settings = _host.Services.GetRequiredService<ISettingsService>();
            await settings.LoadAsync();
            ApplyCulture(settings.Current.Language);
            ApplyTheme(settings.Current.Theme);
            await _host.Services.GetRequiredService<ITempFileService>().CleanupStaleAsync();
            MainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow.Show();
            if (_isSmokeTest)
            {
                await Dispatcher.InvokeAsync(static () => { }, DispatcherPriority.ApplicationIdle);
                Shutdown(0);
            }
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "YF Toolbox failed during startup.");
            if (!_isSmokeTest)
            {
                System.Windows.MessageBox.Show(
                    exception.Message,
                    "YF Toolbox",
                    System.Windows.MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            Shutdown(-1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        await Log.CloseAndFlushAsync();
        base.OnExit(e);
    }

    public static void ApplyTheme(ThemePreference theme)
    {
        var applicationTheme = theme switch
        {
            ThemePreference.Dark => ApplicationTheme.Dark,
            ThemePreference.Light => ApplicationTheme.Light,
            _ => ApplicationThemeManager.GetSystemTheme() switch
            {
                SystemTheme.Dark => ApplicationTheme.Dark,
                SystemTheme.HCWhite or SystemTheme.HCBlack or SystemTheme.HC1 or SystemTheme.HC2 =>
                    ApplicationTheme.HighContrast,
                _ => ApplicationTheme.Light
            }
        };

        ApplicationThemeManager.Apply(applicationTheme, WindowBackdropType.None);
    }

    private static void ApplyCulture(LanguagePreference language)
    {
        var culture = language switch
        {
            LanguagePreference.German => CultureInfo.GetCultureInfo("de-DE"),
            LanguagePreference.English => CultureInfo.GetCultureInfo("en-US"),
            _ => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("de", StringComparison.OrdinalIgnoreCase)
                ? CultureInfo.GetCultureInfo("de-DE")
                : CultureInfo.GetCultureInfo("en-US")
        };
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var logger = _host?.Services.GetService<ILogger<App>>();
        logger?.LogCritical(e.Exception, "Unhandled UI exception. Correlation {CorrelationId}", Guid.NewGuid());
        System.Windows.MessageBox.Show(
            e.Exception.Message,
            "YF Toolbox",
            System.Windows.MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var logger = _host?.Services.GetService<ILogger<App>>();
        logger?.LogError(e.Exception, "Unobserved background task exception.");
        e.SetObserved();
    }
}
