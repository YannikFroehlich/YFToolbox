using YFToolbox.Application.Contracts;
using YFToolbox.Core.Models;

namespace YFToolbox.App.Tests.Fakes;

internal sealed class FakeBuildInfoService(BuildInfo current) : IBuildInfoService
{
    public BuildInfo Current { get; } = current;
}
