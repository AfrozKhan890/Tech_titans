Add-Type -AssemblyName System.Drawing

function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
    $radius = [int]($size * 0.24)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d - 1, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d - 1, $rect.Bottom - $d - 1, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d - 1, $d, $d, 90, 90)
    $path.CloseFigure()

    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point(0, 0)),
        (New-Object System.Drawing.Point($size, $size)),
        [System.Drawing.Color]::FromArgb(255, 122, 143, 58),
        [System.Drawing.Color]::FromArgb(255, 68, 83, 34))
    $g.SetClip($path)
    $g.FillRectangle($brush, 0, 0, $size, $size)
    $g.ResetClip()
    $path.Dispose(); $brush.Dispose()

    $s = $size / 64.0
    $P = { param($x, $y) New-Object System.Drawing.PointF([float]($x * $s), [float]($y * $s)) }

    $berry = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 222, 128, 43))
    $g.FillEllipse($berry, [float](13.5 * $s), [float](11.5 * $s), [float](11 * $s), [float](11 * $s))
    $berry.Dispose()

    $leaf = New-Object System.Drawing.Drawing2D.GraphicsPath
    $leaf.AddBezier((&$P 12 50), (&$P 12 31), (&$P 25 19), (&$P 52 16))
    $leaf.AddBezier((&$P 52 16), (&$P 54 39), (&$P 42 52), (&$P 24 53))
    $leaf.CloseFigure()
    $leafBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 216, 233, 131))
    $g.FillPath($leafBrush, $leaf)
    $leafBrush.Dispose(); $leaf.Dispose()

    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 68, 83, 34), [float](4 * $s))
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $midrib = [System.Drawing.PointF[]]@((&$P 16 47), (&$P 24 35), (&$P 34 26), (&$P 47 19))
    $g.DrawCurve($pen, $midrib)
    $pen.Dispose(); $g.Dispose()
    return $bmp
}

$target = Join-Path $PSScriptRoot '../wwwroot/favicon.ico'
$sizes = @(16, 24, 32, 48)
$pngs = @{}
foreach ($size in $sizes) {
    $bmp = New-IconBitmap $size
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs[$size] = $ms.ToArray()
    $ms.Dispose(); $bmp.Dispose()
}

$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($out)
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$sizes.Count)
$offset = 6 + (16 * $sizes.Count)
foreach ($size in $sizes) {
    $data = $pngs[$size]
    $bw.Write([Byte]$size); $bw.Write([Byte]$size)
    $bw.Write([Byte]0); $bw.Write([Byte]0); $bw.Write([UInt32]0)
    $bw.Write([UInt32]$data.Length); $bw.Write([UInt32]$offset)
    $offset += $data.Length
}
foreach ($size in $sizes) { $bw.Write($pngs[$size]) }
$bw.Flush()
[System.IO.File]::WriteAllBytes($target, $out.ToArray())
$bw.Dispose(); $out.Dispose()
Write-Output "favicon.ico written: $((Get-Item $target).Length) bytes"
