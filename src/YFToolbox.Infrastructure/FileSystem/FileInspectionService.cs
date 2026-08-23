using FileSignatures;
using SixLabors.ImageSharp;
using YFToolbox.Application.Contracts;
using YFToolbox.Core.Models;

namespace YFToolbox.Infrastructure.FileSystem;

public sealed class FileInspectionService(IFileTypeDetector detector) : IFileInspectionService
{
    public Task<FileDescriptor> InspectAsync(string path, CancellationToken cancellationToken = default) =>
        detector.DetectAsync(path, cancellationToken);

    public async Task<IReadOnlyList<FileDescriptor>> InspectManyAsync(
        IEnumerable<string> paths,
        CancellationToken cancellationToken = default)
    {
        var results = new List<FileDescriptor>();
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await InspectAsync(path, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }
}

public sealed class FileTypeDetector : IFileTypeDetector
{
    private static readonly IReadOnlyDictionary<string, (string MimeType, FileCategory Category)> ExtensionMap =
        new Dictionary<string, (string, FileCategory)>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = ("image/png", FileCategory.Image),
            [".jpg"] = ("image/jpeg", FileCategory.Image),
            [".jpeg"] = ("image/jpeg", FileCategory.Image),
            [".webp"] = ("image/webp", FileCategory.Image),
            [".bmp"] = ("image/bmp", FileCategory.Image),
            [".ico"] = ("image/x-icon", FileCategory.Image),
            [".pdf"] = ("application/pdf", FileCategory.Pdf),
            [".mp3"] = ("audio/mpeg", FileCategory.Audio),
            [".wav"] = ("audio/wav", FileCategory.Audio),
            [".flac"] = ("audio/flac", FileCategory.Audio),
            [".m4a"] = ("audio/mp4", FileCategory.Audio),
            [".ogg"] = ("audio/ogg", FileCategory.Audio),
            [".mp4"] = ("video/mp4", FileCategory.Video),
            [".mkv"] = ("video/x-matroska", FileCategory.Video),
            [".webm"] = ("video/webm", FileCategory.Video),
            [".mov"] = ("video/quicktime", FileCategory.Video),
            [".avi"] = ("video/x-msvideo", FileCategory.Video)
        };

    private readonly IFileFormatInspector _inspector = new FileFormatInspector();

    public async Task<FileDescriptor> DetectAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath) || Directory.Exists(fullPath))
        {
            throw new FileNotFoundException("The input file does not exist.", fullPath);
        }

        return await Task.Run(() => Detect(fullPath, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    private FileDescriptor Detect(string fullPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var info = new FileInfo(fullPath);
        var extension = info.Extension.ToLowerInvariant();
        ExtensionMap.TryGetValue(extension, out var extensionType);

        string? signatureMime = null;
        string? signatureExtension = null;
        using (var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            8_192,
            FileOptions.SequentialScan))
        {
            var format = _inspector.DetermineFileFormat(stream);
            signatureMime = format?.MediaType;
            signatureExtension = format?.Extension;
        }

        var mimeType = signatureMime ?? extensionType.MimeType ?? "application/octet-stream";
        var category = GetCategory(mimeType, extensionType.Category);
        var hasSignature = signatureMime is not null;
        var extensionMatches = !hasSignature || MatchesExtension(extension, signatureExtension, signatureMime);
        var confidence = hasSignature ? DetectionConfidence.Signature :
            extensionType.MimeType is not null ? DetectionConfidence.ExtensionOnly : DetectionConfidence.Unknown;
        int? width = null;
        int? height = null;
        int? frames = null;
        AppErrorCode? inspectionError = null;

        if (category == FileCategory.Image)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var imageInfo = Image.Identify(fullPath);
                width = imageInfo.Width;
                height = imageInfo.Height;
                frames = imageInfo.FrameMetadataCollection.Count;
                mimeType = imageInfo.Metadata.DecodedImageFormat?.DefaultMimeType ?? mimeType;
                confidence = DetectionConfidence.DecoderVerified;
                extensionMatches = MatchesExtension(extension, imageInfo.Metadata.DecodedImageFormat?.FileExtensions.FirstOrDefault(), mimeType);
            }
            catch (UnknownImageFormatException)
            {
                confidence = hasSignature ? DetectionConfidence.Signature : DetectionConfidence.ExtensionOnly;
            }
            catch (InvalidImageContentException)
            {
                inspectionError = AppErrorCode.CorruptInput;
                confidence = hasSignature ? DetectionConfidence.Signature : DetectionConfidence.ExtensionOnly;
            }
        }

        return new FileDescriptor(
            fullPath,
            info.Name,
            extension,
            info.Length,
            info.LastWriteTimeUtc,
            mimeType,
            category,
            confidence,
            extensionMatches,
            width,
            height,
            frames,
            inspectionError);
    }

    private static FileCategory GetCategory(string mimeType, FileCategory fallback) => mimeType switch
    {
        var value when value.StartsWith("image/", StringComparison.OrdinalIgnoreCase) => FileCategory.Image,
        var value when value.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) => FileCategory.Audio,
        var value when value.StartsWith("video/", StringComparison.OrdinalIgnoreCase) => FileCategory.Video,
        "application/pdf" => FileCategory.Pdf,
        _ => fallback
    };

    private static bool MatchesExtension(string actual, string? detected, string? mimeType)
    {
        var normalized = detected?.TrimStart('.').ToLowerInvariant();
        var actualNormalized = actual.TrimStart('.').ToLowerInvariant();
        if (mimeType == "image/jpeg")
        {
            return actualNormalized is "jpg" or "jpeg";
        }

        if (mimeType == "image/x-icon" || mimeType == "image/vnd.microsoft.icon")
        {
            return actualNormalized == "ico";
        }

        return normalized is null || actualNormalized == normalized;
    }
}
