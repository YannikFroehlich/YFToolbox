using YFToolbox.Application.Contracts;

namespace YFToolbox.Infrastructure.FileSystem;

public sealed class AppPaths : IAppPaths
{
    public AppPaths()
    {
        DataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YFToolbox");
        SettingsFile = Path.Combine(DataDirectory, "settings.json");
        LogsDirectory = Path.Combine(DataDirectory, "Logs");
        TempDirectory = Path.Combine(DataDirectory, "Temp");

        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(TempDirectory);
    }

    public string DataDirectory { get; }

    public string SettingsFile { get; }

    public string LogsDirectory { get; }

    public string TempDirectory { get; }
}
