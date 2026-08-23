using Microsoft.Extensions.DependencyInjection;
using YFToolbox.Application.Contracts;
using YFToolbox.Core.Models;
using YFToolbox.Features.Files.Rename;
using YFToolbox.Features.Files.ViewModels;
using YFToolbox.Features.Files.Views;

namespace YFToolbox.Features.Files;

public sealed class FileFeatureModule : IYfFeatureModule
{
    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IRenameService, RenameService>();
        services.AddTransient<BatchRenameViewModel>();
        services.AddTransient<FileUtilitiesViewModel>();
        services.AddTransient<BatchRenameView>();
        services.AddTransient<FileUtilitiesView>();
        services.AddSingleton(new ToolDescriptor(
            "files.rename",
            "ToolBatchRename",
            FileCategory.Unknown,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            200));
        services.AddSingleton(new ToolDescriptor(
            "files.hash",
            "ToolFileHash",
            FileCategory.Unknown,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            300,
            AvailableForUnknown: true));
    }
}
