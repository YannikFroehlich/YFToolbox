using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YFToolbox.Application.Contracts;
using YFToolbox.Core.Errors;
using YFToolbox.Core.Localization;
using YFToolbox.Core.Models;
using YFToolbox.Core.Processing;

namespace YFToolbox.Application.Jobs;

public sealed class BackgroundJobQueue(
    IConversionRegistry registry,
    IHistoryService history,
    ISettingsService settings,
    ILogger<BackgroundJobQueue> logger) : BackgroundService, IJobQueue
{
    private readonly Channel<QueuedJob> _channel = Channel.CreateBounded<QueuedJob>(
        new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    public async Task<JobResult> EnqueueAsync(
        ProcessingRequest request,
        IProgress<ProgressSnapshot>? progress = null,
        CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<JobResult> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await _channel.Writer.WriteAsync(
            new QueuedJob(request, progress, cancellationToken, completion),
            cancellationToken).ConfigureAwait(false);
        return await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (QueuedJob job in _channel.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                JobResult result = await ExecuteJobAsync(job, stoppingToken).ConfigureAwait(false);
                job.Completion.TrySetResult(result);
            }
            catch (OperationCanceledException) when (job.CancellationToken.IsCancellationRequested)
            {
                job.Completion.TrySetCanceled(job.CancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unexpected failure in processing job {JobId}", job.Request.Id);
                job.Completion.TrySetException(exception);
            }
        }
    }

    private async Task<JobResult> ExecuteJobAsync(QueuedJob queuedJob, CancellationToken stoppingToken)
    {
        ProcessingRequest request = queuedJob.Request;
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<ProcessingItemResult> results = [];
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            queuedJob.CancellationToken,
            stoppingToken);

        IOperationHandler handler = registry.FindHandler(request);
        int index = 0;

        foreach (FileDescriptor input in request.Inputs)
        {
            index++;
            if (linked.IsCancellationRequested)
            {
                results.Add(CreateCancelled(input));
                continue;
            }

            queuedJob.Progress?.Report(new ProgressSnapshot(
                request.Id,
                ProcessingStatus.Running,
                index,
                request.Inputs.Count,
                0,
                input.FileName,
                AppStrings.Processing));

            try
            {
                Progress<double> itemProgress = new(percent => queuedJob.Progress?.Report(
                    new ProgressSnapshot(
                        request.Id,
                        ProcessingStatus.Running,
                        index,
                        request.Inputs.Count,
                        Math.Clamp(percent, 0, 100),
                        input.FileName,
                        AppStrings.Processing)));

                ProcessingItemResult itemResult = await handler.ExecuteAsync(
                    input,
                    request,
                    itemProgress,
                    linked.Token).ConfigureAwait(false);
                results.Add(itemResult);
            }
            catch (OperationCanceledException) when (linked.IsCancellationRequested)
            {
                results.Add(CreateCancelled(input));
            }
            catch (AppOperationException exception)
            {
                results.Add(new ProcessingItemResult(
                    input,
                    exception.IsSkipped ? ProcessingStatus.Skipped : ProcessingStatus.Failed,
                    null,
                    new AppError(exception.Code, exception.Message, exception.ToString())));
            }
            catch (UnauthorizedAccessException exception)
            {
                results.Add(CreateFailure(input, AppErrorCode.AccessDenied, exception));
            }
            catch (IOException exception)
            {
                results.Add(CreateFailure(input, AppErrorCode.FileLocked, exception));
            }
            catch (NotSupportedException exception)
            {
                results.Add(CreateFailure(input, AppErrorCode.UnsupportedFormat, exception));
            }
            catch (Exception exception)
            {
                string correlationId = Guid.NewGuid().ToString("N");
                logger.LogError(
                    exception,
                    "Processing item failed. Job {JobId}, correlation {CorrelationId}",
                    request.Id,
                    correlationId);
                results.Add(new ProcessingItemResult(
                    input,
                    ProcessingStatus.Failed,
                    null,
                    new AppError(
                        AppErrorCode.InternalError,
                        exception.Message,
                        exception.ToString(),
                        correlationId)));
            }
        }

        stopwatch.Stop();
        ProcessingStatus finalStatus = GetFinalStatus(results);
        queuedJob.Progress?.Report(new ProgressSnapshot(
            request.Id,
            finalStatus,
            request.Inputs.Count,
            request.Inputs.Count,
            100,
            string.Empty,
            GetStatusMessage(finalStatus)));

        var jobResult = new JobResult(request.Id, finalStatus, results, stopwatch.Elapsed);
        if (settings.Current.HistoryEnabled)
        {
            await history.RecordAsync(new HistoryEntry(
                request.OperationId,
                DateTimeOffset.UtcNow,
                finalStatus.ToString(),
                request.Inputs.Count,
                request.TargetExtension,
                (long)stopwatch.Elapsed.TotalMilliseconds), linked.Token).ConfigureAwait(false);
        }

        return jobResult;
    }

    private static ProcessingItemResult CreateCancelled(FileDescriptor input) => new(
        input,
        ProcessingStatus.Cancelled,
        null,
        new AppError(AppErrorCode.Cancelled, AppStrings.OperationCancelled));

    private static ProcessingItemResult CreateFailure(
        FileDescriptor input,
        AppErrorCode code,
        Exception exception) => new(
            input,
            ProcessingStatus.Failed,
            null,
            new AppError(code, exception.Message, exception.ToString()));

    private static ProcessingStatus GetFinalStatus(IReadOnlyCollection<ProcessingItemResult> results)
    {
        if (results.Count > 0 && results.All(item => item.Status == ProcessingStatus.Cancelled))
        {
            return ProcessingStatus.Cancelled;
        }

        if (results.Count > 0 && results.All(item => item.Status == ProcessingStatus.Skipped))
        {
            return ProcessingStatus.Skipped;
        }

        return results.Any(item => item.Status == ProcessingStatus.Failed)
            ? ProcessingStatus.Failed
            : ProcessingStatus.Succeeded;
    }

    private static string GetStatusMessage(ProcessingStatus status) => status switch
    {
        ProcessingStatus.Succeeded => AppStrings.Done,
        ProcessingStatus.Failed => AppStrings.Failed,
        ProcessingStatus.Cancelled => AppStrings.Cancel,
        ProcessingStatus.Skipped => AppStrings.Skip,
        _ => AppStrings.Processing
    };

    private sealed record QueuedJob(
        ProcessingRequest Request,
        IProgress<ProgressSnapshot>? Progress,
        CancellationToken CancellationToken,
        TaskCompletionSource<JobResult> Completion);
}
