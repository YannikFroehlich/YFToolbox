using Microsoft.Extensions.DependencyInjection;
using YFToolbox.Application.Catalog;
using YFToolbox.Application.Contracts;
using YFToolbox.Application.Jobs;

namespace YFToolbox.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddYfToolboxApplication(this IServiceCollection services)
    {
        services.AddSingleton<IToolCatalog, ToolCatalog>();
        services.AddSingleton<IActionSuggestionService, ActionSuggestionService>();
        services.AddSingleton<IConversionRegistry, ConversionRegistry>();
        services.AddSingleton<BackgroundJobQueue>();
        services.AddSingleton<IJobQueue>(provider => provider.GetRequiredService<BackgroundJobQueue>());
        services.AddHostedService(provider => provider.GetRequiredService<BackgroundJobQueue>());
        return services;
    }
}
