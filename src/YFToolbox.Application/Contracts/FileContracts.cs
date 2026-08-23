using YFToolbox.Core.Models;

namespace YFToolbox.Application.Contracts;

public interface IFileTypeDetector
{
    Task<FileDescriptor> DetectAsync(string path, CancellationToken cancellationToken = default);
}

public interface IFileInspectionService
{
    Task<FileDescriptor> InspectAsync(string path, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileDescriptor>> InspectManyAsync(
        IEnumerable<string> paths,
        CancellationToken cancellationToken = default);
}

public interface IHashService
{
    Task<string> ComputeSha256Async(
        string path,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    Task<string> ComputeMd5Async(
        string path,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

public interface IClipboardService
{
    void SetText(string text);
}
