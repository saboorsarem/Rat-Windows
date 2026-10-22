# Draws the "R" app icon and packs src/Rat/app.ico (PNG-compressed entries, 16-256 px).
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$outFile = Join-Path $root 'src/Rat/app.ico'
$sizes = 16, 24, 32, 48, 64, 128, 256
$entries = @()

foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear([System.Drawing.Color]::Transparent)

    $r = [Math]::Max(3, [int]($s * 0.22))
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, 2 * $r, 2 * $r, 180, 90)
    $path.AddArc($s - 2 * $r, 0, 2 * $r, 2 * $r, 270, 90)
    $path.AddArc($s - 2 * $r, $s - 2 * $r, 2 * $r, 2 * $r, 0, 90)
    $path.AddArc(0, $s - 2 * $r, 2 * $r, 2 * $r, 90, 90)
    $path.CloseFigure()
    $bg = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 17, 19, 24))
    $g.FillPath($bg, $path)

    $penWidth = [Math]::Max(1.5, $s * 0.06)
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 76, 141, 255), [float]$penWidth)
    $m = [int]($s * 0.17)
    $g.DrawEllipse($pen, $m, $m, $s - 2 * $m, $s - 2 * $m)

    $fontSize = [float]($s * 0.46)
    $font = New-Object System.Drawing.Font('Segoe UI', $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = [System.Drawing.StringAlignment]::Center
    $fmt.LineAlignment = [System.Drawing.StringAlignment]::Center
    $rect = New-Object System.Drawing.RectangleF(0, 0, $s, $s)
    $g.DrawString('R', $font, [System.Drawing.Brushes]::White, $rect, $fmt)

    $font.Dispose(); $pen.Dispose(); $bg.Dispose(); $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $entries += , @($s, $ms.ToArray())
}

$msOut = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter($msOut)
$w.Write([UInt16]0)
$w.Write([UInt16]1)
$w.Write([UInt16]$entries.Count)
$offset = 6 + 16 * $entries.Count
foreach ($e in $entries) {
    $w.Write([byte]($e[0] % 256))
    $w.Write([byte]($e[0] % 256))
    $w.Write([byte]0)
    $w.Write([byte]0)
    $w.Write([UInt16]1)
    $w.Write([UInt16]32)
    $w.Write([UInt32]$e[1].Length)
    $w.Write([UInt32]$offset)
    $offset += $e[1].Length
}
foreach ($e in $entries) { $w.Write([byte[]]$e[1]) }
$w.Flush()
[System.IO.File]::WriteAllBytes($outFile, $msOut.ToArray())
$w.Dispose()
$msOut.Dispose()
Write-Host "wrote $outFile ($([Math]::Round((Get-Item $outFile).Length / 1kb)) KB)"
