using YFToolbox.Core.Models;

namespace YFToolbox.Features.Images.Models;

public sealed record ImageConversionOptions(
    int? Width = null,
    int? Height = null,
    bool LockAspectRatio = true,
    bool AllowUpscale = false,
    int RotationDegrees = 0,
    bool FlipHorizontal = false,
    bool FlipVertical = false,
    QualityPreset QualityPreset = QualityPreset.Balanced,
    bool PreserveMetadata = true,
    bool AllowLargeImage = false,
    string JpegBackground = "#FFFFFF");
