using YFToolbox.Core.Models;
using YFToolbox.Core.Localization;

namespace YFToolbox.Core.Processing;

public sealed record ProcessingRequest(
    Guid Id,
    IReadOnlyList<FileDescriptor> Inputs,
    string OperationId,
    string TargetExtension,
    string? OutputDirectory,
    OutputConflictPolicy ConflictPolicy,
    object Options);

public sealed record ProgressSnapshot(
    Guid JobId,
    ProcessingStatus Status,
    int CurrentIndex,
    int TotalCount,
    double? Percent,
    string CurrentFile,
    string Message);

public sealed record AppError(
    AppErrorCode Code,
    string Message,
    string? TechnicalDetails = null,
    string? CorrelationId = null);

public sealed record ProcessingItemResult(
    FileDescriptor Input,
    ProcessingStatus Status,
    string? OutputPath,
    AppError? Error,
    IReadOnlyList<string>? Warnings = null)
{
    public string DisplayStatus => Status switch
    {
        ProcessingStatus.Succeeded => AppStrings.Done,
        ProcessingStatus.Failed => AppStrings.Failed,
        ProcessingStatus.Cancelled => AppStrings.Cancel,
        ProcessingStatus.Skipped => AppStrings.Skip,
        _ => AppStrings.Processing
    };
}

public sealed record JobResult(
    Guid JobId,
    ProcessingStatus Status,
    IReadOnlyList<ProcessingItemResult> Items,
    TimeSpan Duration)
{
    public int SucceededCount => Items.Count(item => item.Status == ProcessingStatus.Succeeded);

    public int FailedCount => Items.Count(item => item.Status == ProcessingStatus.Failed);

    public int CancelledCount => Items.Count(item => item.Status == ProcessingStatus.Cancelled);

    public int SkippedCount => Items.Count(item => item.Status == ProcessingStatus.Skipped);
}
