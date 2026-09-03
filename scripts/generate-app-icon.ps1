<#
    Generates the YF Toolbox application icon: a rounded gradient tile
    (cyan -> blue -> purple, matching Resources/Brand.xaml's
    YFLogoGradientBrush) with a simple toolbox glyph, rendered at every
    standard Windows icon resolution and packed into a single .ico file.

    This is a one-off asset-generation script, not part of the build.
#>
Add-Type -AssemblyName System.Drawing

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$outDir = "src/YFToolbox.App/Assets"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function New-IconFrame([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded-square background tile, gradient cyan -> blue -> purple.
    $radius = [Math]::Max(2, $size * 0.22)
    $rect = New-Object System.Drawing.RectangleF 0, 0, $size, $size
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point 0, 0),
        (New-Object System.Drawing.Point $size, $size),
        [System.Drawing.Color]::FromArgb(255, 0x22, 0xD3, 0xEE),
        [System.Drawing.Color]::FromArgb(255, 0x8B, 0x5C, 0xF6))
    $blend = New-Object System.Drawing.Drawing2D.ColorBlend
    $blend.Colors = @(
        [System.Drawing.Color]::FromArgb(255, 0x22, 0xD3, 0xEE),
        [System.Drawing.Color]::FromArgb(255, 0x2F, 0x6F, 0xED),
        [System.Drawing.Color]::FromArgb(255, 0x8B, 0x5C, 0xF6)
    )
    $blend.Positions = @(0.0, 0.5, 1.0)
    $brush.InterpolationColors = $blend
    $g.FillPath($brush, $path)

    # Toolbox glyph: flat carry handle + wide body + near-top seam + latches, in white.
    $cx = $size / 2.0
    $boxW = $size * 0.60
    $boxH = $size * 0.32
    $boxLeft = $cx - $boxW / 2.0
    $boxTop = $size * 0.52
    $boxRadius = [Math]::Max(1, $size * 0.05)

    # Below ~24px the seam/latch detail just muddies into a blob, so tiny
    # sizes get a bolder, simplified silhouette (handle + body only).
    $simplified = $size -le 24

    $glyphBrush = [System.Drawing.Brushes]::White
    $glyphPenWidth = [Math]::Max(1.0, $size * (& { if ($simplified) { 0.075 } else { 0.05 } }))
    $glyphPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, $glyphPenWidth)
    $glyphPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $glyphPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $glyphPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    # Handle: a short, flat carry-handle bracket sitting just above the box.
    $handleW = $size * 0.32
    $handleH = $size * 0.10
    $handleLeft = $cx - $handleW / 2.0
    $handleRight = $cx + $handleW / 2.0
    $handleTop = $boxTop - $handleH * 1.15
    $handlePath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $handlePath.AddArc($handleLeft, $handleTop, $handleW, $handleH * 2, 180, 180)
    $g.DrawPath($glyphPen, $handlePath)
    $g.DrawLine($glyphPen, $handleLeft, $handleTop + $handleH, $handleLeft, $boxTop + ($size * 0.015))
    $g.DrawLine($glyphPen, $handleRight, $handleTop + $handleH, $handleRight, $boxTop + ($size * 0.015))

    # Body.
    $bodyRect = New-Object System.Drawing.RectangleF $boxLeft, $boxTop, $boxW, $boxH
    $bodyPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $bd = $boxRadius * 2
    $bodyPath.AddArc($bodyRect.X, $bodyRect.Y, $bd, $bd, 180, 90)
    $bodyPath.AddArc($bodyRect.Right - $bd, $bodyRect.Y, $bd, $bd, 270, 90)
    $bodyPath.AddArc($bodyRect.Right - $bd, $bodyRect.Bottom - $bd, $bd, $bd, 0, 90)
    $bodyPath.AddArc($bodyRect.X, $bodyRect.Bottom - $bd, $bd, $bd, 90, 90)
    $bodyPath.CloseFigure()
    $g.FillPath($glyphBrush, $bodyPath)

    if (-not $simplified) {
        # Lid seam near the top of the case, with two latches offset toward the
        # edges (rectangular, not circular, so it reads as a case, not a lock).
        $seamY = $boxTop + $boxH * 0.24
        $seamPen = New-Object System.Drawing.Pen($brush, [Math]::Max(1.0, $size * 0.03))
        $g.DrawLine($seamPen, $boxLeft + $size * 0.03, $seamY, $boxLeft + $boxW - $size * 0.03, $seamY)
        $latchW = $size * 0.045
        $latchH = $size * 0.06
        $latchOffsetX = $boxW * 0.27
        $g.FillRectangle($brush, ($cx - $latchOffsetX - $latchW / 2), ($seamY - $latchH / 2), $latchW, $latchH)
        $g.FillRectangle($brush, ($cx + $latchOffsetX - $latchW / 2), ($seamY - $latchH / 2), $latchW, $latchH)
    }

    $g.Dispose()
    return $bmp
}

$pngBlobs = @()
foreach ($size in $sizes) {
    $bmp = New-IconFrame $size
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBlobs += ,@{ Size = $size; Bytes = $ms.ToArray() }
    if ($size -eq 256) {
        $bmp.Save("$outDir/YFToolboxIcon.png", [System.Drawing.Imaging.ImageFormat]::Png)
    }
    $bmp.Dispose()
}

# Pack every frame into a single multi-resolution .ico (PNG-compressed frames).
$icoPath = "$outDir/YFToolbox.ico"
$fs = New-Object System.IO.FileStream $icoPath, ([System.IO.FileMode]::Create)
$bw = New-Object System.IO.BinaryWriter $fs

$bw.Write([UInt16]0)      # reserved
$bw.Write([UInt16]1)      # type = icon
$bw.Write([UInt16]$pngBlobs.Count)

$headerSize = 6 + (16 * $pngBlobs.Count)
$offset = $headerSize
foreach ($blob in $pngBlobs) {
    $dim = if ($blob.Size -ge 256) { 0 } else { $blob.Size }
    $bw.Write([Byte]$dim)            # width
    $bw.Write([Byte]$dim)            # height
    $bw.Write([Byte]0)               # color count
    $bw.Write([Byte]0)               # reserved
    $bw.Write([UInt16]1)             # planes
    $bw.Write([UInt16]32)            # bit count
    $bw.Write([UInt32]$blob.Bytes.Length)
    $bw.Write([UInt32]$offset)
    $offset += $blob.Bytes.Length
}
foreach ($blob in $pngBlobs) {
    $bw.Write($blob.Bytes)
}
$bw.Flush()
$bw.Dispose()
$fs.Dispose()

"Wrote $icoPath ($($pngBlobs.Count) frames) and $outDir/YFToolboxIcon.png"
