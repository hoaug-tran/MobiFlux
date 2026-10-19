Add-Type -AssemblyName System.Drawing

function Generate-MobiFluxPng([int]$size, [string]$outputPath) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $scale = $size / 64.0

    # Draw left curve: from (14, 50) up to (14, 23), curve via (14, 12), (23, 12) to (32, 28)
    $pLeft = New-Object System.Drawing.Drawing2D.GraphicsPath
    $pLeft.AddLine((14.0 * $scale), (50.0 * $scale), (14.0 * $scale), (23.0 * $scale))
    $pLeft.AddBezier((14.0 * $scale), (23.0 * $scale), (14.0 * $scale), (12.0 * $scale), (23.0 * $scale), (12.0 * $scale), (32.0 * $scale), (28.0 * $scale))

    # Gradient for left path: Cyan #38BDF8 to Electric Blue #0284C7
    $penLeft = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 56, 189, 248)), (6.0 * $scale)
    $penLeft.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penLeft.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penLeft.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $g.DrawPath($penLeft, $pLeft)

    # Draw right curve: from (32, 28) curve via (41, 12), (50, 12) to (50, 23), then line to (50, 50)
    $pRight = New-Object System.Drawing.Drawing2D.GraphicsPath
    $pRight.AddBezier((32.0 * $scale), (28.0 * $scale), (41.0 * $scale), (12.0 * $scale), (50.0 * $scale), (12.0 * $scale), (50.0 * $scale), (23.0 * $scale))
    $pRight.AddLine((50.0 * $scale), (23.0 * $scale), (50.0 * $scale), (50.0 * $scale))

    # Gradient for right path: Sky Blue #0284C7 to Indigo #6366F1
    $penRight = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 99, 102, 241)), (6.0 * $scale)
    $penRight.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penRight.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penRight.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $g.DrawPath($penRight, $pRight)

    # Center routing nexus: Outer luminous glow & solid bright center
    $glowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(120, 56, 189, 248))
    $glowR = 8.0 * $scale
    $g.FillEllipse($glowBrush, (32.0 * $scale - $glowR), (28.0 * $scale - $glowR), ($glowR * 2), ($glowR * 2))

    $coreBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
    $coreR = 4.0 * $scale
    $g.FillEllipse($coreBrush, (32.0 * $scale - $coreR), (28.0 * $scale - $coreR), ($coreR * 2), ($coreR * 2))

    # Endpoint dots
    $dotLeft = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 56, 189, 248))
    $dotRight = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 129, 140, 248))
    $dotR = 3.5 * $scale
    $g.FillEllipse($dotLeft, (14.0 * $scale - $dotR), (50.0 * $scale - $dotR), ($dotR * 2), ($dotR * 2))
    $g.FillEllipse($dotRight, (50.0 * $scale - $dotR), (50.0 * $scale - $dotR), ($dotR * 2), ($dotR * 2))

    $bmp.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)

    $penLeft.Dispose()
    $penRight.Dispose()
    $pLeft.Dispose()
    $pRight.Dispose()
    $glowBrush.Dispose()
    $coreBrush.Dispose()
    $dotLeft.Dispose()
    $dotRight.Dispose()
    $g.Dispose()
    $bmp.Dispose()
}

$logoPng = "src\MobiFlux.Desktop\Assets\logo.png"
Generate-MobiFluxPng 256 $logoPng
Write-Host "Generated $logoPng successfully"

# Generate multi-res ICO from PNG frames
$icoPath = "src\MobiFlux.Desktop\Assets\app.ico"
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngBytesList = @()

foreach ($s in $sizes) {
    $tempFile = [System.IO.Path]::GetTempFileName() + ".png"
    Generate-MobiFluxPng $s $tempFile
    $bytes = [System.IO.File]::ReadAllBytes($tempFile)
    [System.IO.File]::Delete($tempFile)
    $pngBytesList += ,@($s, $bytes)
}

$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter $fs

# ICONDIR header: idReserved (0), idType (1 = ICO), idCount
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$pngBytesList.Count)

$offset = 6 + ($pngBytesList.Count * 16)

# ICONDIRENTRY entries
foreach ($item in $pngBytesList) {
    $s = $item[0]
    $data = $item[1]

    $w = if ($s -ge 256) { 0 } else { [byte]$s }
    $h = if ($s -ge 256) { 0 } else { [byte]$s }

    $bw.Write([byte]$w)
    $bw.Write([byte]$h)
    $bw.Write([byte]0) # Color count
    $bw.Write([byte]0) # Reserved
    $bw.Write([uint16]1) # Color planes
    $bw.Write([uint16]32) # Bits per pixel
    $bw.Write([uint32]$data.Length) # Image size in bytes
    $bw.Write([uint32]$offset) # Offset of image data
    $offset += $data.Length
}

# Image data (PNG streams)
foreach ($item in $pngBytesList) {
    $bw.Write($item[1])
}

$bw.Close()
$fs.Close()
Write-Host "Generated $icoPath successfully"
