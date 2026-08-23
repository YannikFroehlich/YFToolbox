using Microsoft.Extensions.DependencyInjection;
using YFToolbox.Core.Models;
using YFToolbox.Core.Processing;

namespace YFToolbox.Application.Contracts;

public interface IOperationHandler
{
    string OperationId { get; }

    bool CanHandle(ProcessingRequest request);

    Task<ProcessingItemResult> ExecuteAsync(
        FileDescriptor input,
        ProcessingRequest request,
        IProgress<double>? progress,
        CancellationToken cancellationToken);
}

public interface IConversionRegistry
{
    IReadOnlyCollection<IOperationHandler> Handlers { get; }

    IOperationHandler FindHandler(ProcessingRequest request);
}

public interface IJobQueue
{
    Task<JobResult> EnqueueAsync(
        ProcessingRequest request,
        IProgress<ProgressSnapshot>? progress = null,
        CancellationToken cancellationToken = default);
}

public interface IToolCatalog
{
    IReadOnlyCollection<ToolDescriptor> Tools { get; }

    IReadOnlyList<ToolDescriptor> FindFor(FileDescriptor file);
}

public interface IActionSuggestionService
{
    IReadOnlyList<ToolDescriptor> Suggest(FileDescriptor file);
}

public interface IYfFeatureModule
{
    void Register(IServiceCollection services);
}
