$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sourcePath = Join-Path $repoRoot 'src\CastoPet\Assets\Runtime\Castorice\Castorice.png'
$outputPath = Join-Path $repoRoot 'src\CastoPet\Assets\AppIcon.ico'
$outputDirectory = Split-Path -Parent $outputPath
$temporaryPath = "$outputPath.tmp"
$sizes = @(256, 128, 64, 48, 32, 24, 16)
$crop = [System.Drawing.Rectangle]::new(40, 0, 240, 240)

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$source = [System.Drawing.Bitmap]::FromFile($sourcePath)
$images = [System.Collections.Generic.List[byte[]]]::new()

try {
    foreach ($size in $sizes) {
        $bitmap = [System.Drawing.Bitmap]::new(
            $size,
            $size,
            [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $stream = [System.IO.MemoryStream]::new()
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $padding = [Math]::Max(1, [Math]::Round($size * 0.035))
            $destination = [System.Drawing.Rectangle]::new(
                $padding,
                $padding,
                $size - (2 * $padding),
                $size - (2 * $padding))
            $graphics.DrawImage(
                $source,
                $destination,
                $crop,
                [System.Drawing.GraphicsUnit]::Pixel)
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            $images.Add($stream.ToArray())
        }
        finally {
            $stream.Dispose()
            $graphics.Dispose()
            $bitmap.Dispose()
        }
    }
}
finally {
    $source.Dispose()
}

$file = [System.IO.File]::Open(
    $temporaryPath,
    [System.IO.FileMode]::Create,
    [System.IO.FileAccess]::Write,
    [System.IO.FileShare]::None)
$writer = [System.IO.BinaryWriter]::new($file)
try {
    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]$images.Count)

    $offset = 6 + (16 * $images.Count)
    for ($index = 0; $index -lt $images.Count; $index++) {
        $size = $sizes[$index]
        $writer.Write([Byte]$(if ($size -eq 256) { 0 } else { $size }))
        $writer.Write([Byte]$(if ($size -eq 256) { 0 } else { $size }))
        $writer.Write([Byte]0)
        $writer.Write([Byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$images[$index].Length)
        $writer.Write([UInt32]$offset)
        $offset += $images[$index].Length
    }

    foreach ($image in $images) {
        $writer.Write($image)
    }
}
finally {
    $writer.Dispose()
    $file.Dispose()
}

Move-Item -LiteralPath $temporaryPath -Destination $outputPath -Force
Write-Host "Generated CastoPet application icon: $outputPath"
