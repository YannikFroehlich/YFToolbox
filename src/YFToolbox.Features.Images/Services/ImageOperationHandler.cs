using System.IO;
using SixLabors.ImageSharp;
using YFToolbox.Application.Contracts;
using YFToolbox.Core.Localization;
using YFToolbox.Core.Models;
using YFToolbox.Core.Processing;
using YFToolbox.Features.Images.Models;

namespace YFToolbox.Features.Images.Services;

public sealed class ImageOperationHandler(
    IImageProcessingService imageProcessing,
    IOutputPathResolver outputPathResolver,
    ITempFileService tempFiles,
    ISettingsService settings) : IOperationHandler
{
    public const string Id = "images.convert";
    private static readonly HashSet<string> SupportedExtensions =
        new([".png", ".jpg", ".jpeg", ".webp", ".bmp", ".ico"], StringComparer.OrdinalIgnoreCase);

    public string OperationId => Id;

    public bool CanHandle(ProcessingRequest request) =>
        request.OperationId == OperationId &&
        request.Inputs.All(input => input.Category == FileCategory.Image && SupportedExtensions.Contains(input.Extension));

    public async Task<ProcessingItemResult> ExecuteAsync(
        FileDescriptor input,
        ProcessingRequest request,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        var currentInfo = new FileInfo(input.FullPath);
        if (!currentInfo.Exists || currentInfo.Length != input.Size || currentInfo.LastWriteTimeUtc != input.LastWriteTime.UtcDateTime)
        {
            return Failure(input, AppErrorCode.InputChanged, AppStrings.InputChanged);
        }

        if (request.Options is not ImageConversionOptions options)
        {
            return Failure(input, AppErrorCode.InvalidInput, AppStrings.InvalidImageOptions);
        }

        var outputDirectory = outputPathResolver.ResolveOutputDirectory(
            input.FullPath,
            request.OutputDirectory,
            settings.Current);
        var output = outputPathResolver.ResolveOutputPath(
            input.FullPath,
            outputDirectory,
            request.TargetExtension,
            request.ConflictPolicy);
        if (!HasSufficientDiskSpace(output.Path, input))
        {
            return Failure(input, AppErrorCode.InsufficientDiskSpace, AppStrings.InsufficientDiskSpace);
        }

        var temporaryPath = tempFiles.CreateSiblingTempPath(output.Path);
        try
        {
            try
            {
                await imageProcessing.ProcessAsync(
                    input.FullPath,
                    temporaryPath,
                    request.TargetExtension,
                    options,
                    progress,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is UnknownImageFormatException or InvalidImageContentException)
            {
                return Failure(input, AppErrorCode.CorruptInput, AppStrings.CorruptImage);
            }
            catch (NotSupportedException exception)
            {
                return Failure(input, AppErrorCode.UnsupportedFormat, exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return Failure(input, AppErrorCode.InvalidInput, exception.Message);
            }
            cancellationToken.ThrowIfCancellationRequested();
            await tempFiles.CommitAsync(
                temporaryPath,
                output.Path,
                output.EffectiveConflictPolicy,
                cancellationToken).ConfigureAwait(false);
            return new ProcessingItemResult(input, ProcessingStatus.Succeeded, output.Path, null);
        }
        finally
        {
            tempFiles.TryDelete(temporaryPath);
        }
    }

    private static ProcessingItemResult Failure(FileDescriptor input, AppErrorCode code, string message) =>
        new(input, ProcessingStatus.Failed, null, new AppError(code, message));

    private static bool HasSufficientDiskSpace(string outputPath, FileDescriptor input)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(outputPath));
            if (string.IsNullOrWhiteSpace(root))
            {
                return true;
            }

            var estimatedDecodedBytes = input.PixelWidth is not null && input.PixelHeight is not null
                ? checked((long)input.PixelWidth.Value * input.PixelHeight.Value * 4)
                : input.Size;
            var inputReserve = input.Size > long.MaxValue / 2 ? long.MaxValue : input.Size * 2;
            const long margin = 16L * 1024 * 1024;
            var decodedReserve = estimatedDecodedBytes > long.MaxValue - margin
                ? long.MaxValue
                : estimatedDecodedBytes + margin;
            var requiredBytes = Math.Max(inputReserve, decodedReserve);
            return new DriveInfo(root).AvailableFreeSpace >= requiredBytes;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or OverflowException)
        {
            return true;
        }
    }
}
