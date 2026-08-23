using Microsoft.Extensions.DependencyInjection;
using YFToolbox.Application.Contracts;
using YFToolbox.Core.Models;
using YFToolbox.Features.Images.Services;
using YFToolbox.Features.Images.ViewModels;
using YFToolbox.Features.Images.Views;

namespace YFToolbox.Features.Images;

public sealed class ImageFeatureModule : IYfFeatureModule
{
    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IImageProcessingService, ImageProcessingService>();
        services.AddSingleton<IOperationHandler, ImageOperationHandler>();
        services.AddTransient<ImageConverterViewModel>();
        services.AddTransient<ImageConverterView>();
        services.AddSingleton(new ToolDescriptor(
            ImageOperationHandler.Id,
            "ToolImageConverter",
            FileCategory.Image,
            new HashSet<string>([".png", ".jpg", ".jpeg", ".webp", ".bmp", ".ico"], StringComparer.OrdinalIgnoreCase),
            100));
    }
}
