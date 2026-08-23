using System.Reflection;
using YFToolbox.Application.Contracts;
using YFToolbox.Core.Models;

namespace YFToolbox.Infrastructure.Build;

public sealed class BuildInfoService : IBuildInfoService
{
    public BuildInfoService()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(BuildInfoService).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "0.1.0-dev+local";
        var parts = informational.Split('+', 2);
        var semanticVersion = parts[0];
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        var commit = Environment.GetEnvironmentVariable("YFTOOLBOX_COMMIT_SHA")
            ?? metadata.GetValueOrDefault("CommitSha")
            ?? (parts.Length > 1 ? parts[1] : "local");
        var channel = semanticVersion.StartsWith("0.", StringComparison.Ordinal) ? "Preview" : "Stable";
        var buildTimeValue = Environment.GetEnvironmentVariable("YFTOOLBOX_BUILD_TIME")
            ?? metadata.GetValueOrDefault("BuildTimeUtc");
        var buildTime = DateTimeOffset.TryParse(
            buildTimeValue,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal,
            out var parsedBuildTime)
            ? parsedBuildTime
            : (DateTimeOffset?)null;
        Current = new BuildInfo(semanticVersion, commit, channel, buildTime);
    }

    public BuildInfo Current { get; }
}
