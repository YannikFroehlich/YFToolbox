using System.Globalization;
using YFToolbox.Application.Contracts;

namespace YFToolbox.App.ViewModels;

public sealed class AboutViewModel(IBuildInfoService buildInfo)
{
    public string Version => buildInfo.Current.SemanticVersion;

    public string Commit => buildInfo.Current.ShortCommit;

    public string Channel => buildInfo.Current.Channel;

    public string BuildTime => buildInfo.Current.BuildTime?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? "local";
}
