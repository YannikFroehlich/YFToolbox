using YFToolbox.Application.Contracts;
using YFToolbox.Core.Models;

namespace YFToolbox.Application.Catalog;

public sealed class ToolCatalog(IEnumerable<ToolDescriptor> tools) : IToolCatalog
{
    private readonly IReadOnlyCollection<ToolDescriptor> _tools = tools
        .OrderBy(tool => tool.Priority)
        .ToArray();

    public IReadOnlyCollection<ToolDescriptor> Tools => _tools;

    public IReadOnlyList<ToolDescriptor> FindFor(FileDescriptor file)
    {
        return _tools
            .Where(tool =>
                (file.Category == FileCategory.Unknown || file.InspectionError is not null
                    ? tool.AvailableForUnknown
                    : tool.Category == FileCategory.Unknown || tool.Category == file.Category) &&
                (tool.SupportedExtensions.Count == 0 || tool.SupportedExtensions.Contains(file.Extension)))
            .ToArray();
    }
}

public sealed class ActionSuggestionService(IToolCatalog catalog) : IActionSuggestionService
{
    public IReadOnlyList<ToolDescriptor> Suggest(FileDescriptor file) => catalog.FindFor(file);
}
