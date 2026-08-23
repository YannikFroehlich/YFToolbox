using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YFToolbox.Application.Contracts;
using YFToolbox.Infrastructure.Build;
using YFToolbox.Infrastructure.FileSystem;
using YFToolbox.Infrastructure.Settings;
using YFToolbox.Infrastructure.History;

namespace YFToolbox.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddYfToolboxInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAppPaths, AppPaths>();
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<IFileTypeDetector, FileTypeDetector>();
        services.AddSingleton<IFileInspectionService, FileInspectionService>();
        services.AddSingleton<IHashService, HashService>();
        services.AddSingleton<IOutputPathResolver, OutputPathResolver>();
        services.TryAddSingleton<IOutputConflictPrompt, RejectingOutputConflictPrompt>();
        services.AddSingleton<ITempFileService, TempFileService>();
        services.AddSingleton<IBuildInfoService, BuildInfoService>();
        services.AddSingleton<IHistoryService, JsonHistoryService>();
        return services;
    }
}
