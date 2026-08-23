using YFToolbox.Application.Contracts;
using YFToolbox.Core.Processing;

namespace YFToolbox.Application.Jobs;

public sealed class ConversionRegistry(IEnumerable<IOperationHandler> handlers) : IConversionRegistry
{
    private readonly IReadOnlyCollection<IOperationHandler> _handlers = handlers.ToArray();

    public IReadOnlyCollection<IOperationHandler> Handlers => _handlers;

    public IOperationHandler FindHandler(ProcessingRequest request)
    {
        IOperationHandler? handler = _handlers.FirstOrDefault(candidate => candidate.CanHandle(request));
        return handler ?? throw new NotSupportedException(
            $"No operation handler supports '{request.OperationId}' and '{request.TargetExtension}'.");
    }
}
