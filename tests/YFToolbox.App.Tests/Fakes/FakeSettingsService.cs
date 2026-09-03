using YFToolbox.Application.Contracts;
using YFToolbox.Core.Settings;

namespace YFToolbox.App.Tests.Fakes;

internal sealed class FakeSettingsService : ISettingsService
{
    public AppSettings Current { get; set; } = AppSettings.CreateDefault();

    public AppSettings? Saved { get; private set; }

    public event EventHandler<AppSettings>? SettingsChanged;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        Current = settings;
        Saved = settings;
        SettingsChanged?.Invoke(this, settings);
        return Task.CompletedTask;
    }
}
