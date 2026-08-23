namespace YFToolbox.Core.Models;

public sealed record FileDescriptor(
    string FullPath,
    string FileName,
    string Extension,
    long Size,
    DateTimeOffset LastWriteTime,
    string MimeType,
    FileCategory Category,
    DetectionConfidence Confidence,
    bool ExtensionMatchesContent,
    int? PixelWidth = null,
    int? PixelHeight = null,
    int? FrameCount = null,
    AppErrorCode? InspectionError = null)
{
    public string DisplaySize => Size switch
    {
        >= 1_073_741_824 => $"{Size / 1_073_741_824d:0.##} GB",
        >= 1_048_576 => $"{Size / 1_048_576d:0.##} MB",
        >= 1_024 => $"{Size / 1_024d:0.##} KB",
        _ => $"{Size} B"
    };

    public string Dimensions => PixelWidth is not null && PixelHeight is not null
        ? $"{PixelWidth} × {PixelHeight}"
        : "—";
}

public sealed record ToolDescriptor(
    string Id,
    string NameResourceKey,
    FileCategory Category,
    IReadOnlySet<string> SupportedExtensions,
    int Priority,
    bool AvailableForUnknown = false);
