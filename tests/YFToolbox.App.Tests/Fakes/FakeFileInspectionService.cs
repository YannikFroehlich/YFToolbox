using YFToolbox.Application.Contracts;
using YFToolbox.Core.Models;

namespace YFToolbox.App.Tests.Fakes;

internal sealed class FakeFileInspectionService : IFileInspectionService
{
    public IReadOnlyList<FileDescriptor> Result { get; set; } = [];

    public Exception? ThrowOnInspectMany { get; set; }

    public Task<FileDescriptor> InspectAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result[0]);

    public Task<IReadOnlyList<FileDescriptor>> InspectManyAsync(
        IEnumerable<string> paths,
        CancellationToken cancellationToken = default) =>
        ThrowOnInspectMany is not null
            ? throw ThrowOnInspectMany
            : Task.FromResult(Result);
}
