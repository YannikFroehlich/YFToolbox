using YFToolbox.Application.Contracts;
using YFToolbox.Core.Models;

namespace YFToolbox.App.Tests.Fakes;

internal sealed class FakeActionSuggestionService : IActionSuggestionService
{
    public IReadOnlyList<ToolDescriptor> Suggestions { get; set; } = [];

    public IReadOnlyList<ToolDescriptor> Suggest(FileDescriptor file) => Suggestions;
}
