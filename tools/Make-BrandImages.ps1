# Draws the PaperEngineer Shell icon and installer images from code, so every size comes from one design.
# Run from the repository root: powershell -ExecutionPolicy Bypass -File tools\Make-BrandImages.ps1
param([string]$Root = (Split-Path $PSScriptRoot -Parent))

Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'

$teal   = [Drawing.Color]::FromArgb(255, 14, 92, 99)
$tealDk = [Drawing.Color]::FromArgb(255, 9, 64, 70)
$paper  = [Drawing.Color]::FromArgb(255, 250, 248, 242)
$fold   = [Drawing.Color]::FromArgb(255, 205, 214, 212)
$line   = [Drawing.Color]::FromArgb(255, 120, 146, 148)
$orange = [Drawing.Color]::FromArgb(255, 240, 138, 36)
$navy   = [Drawing.Color]::FromArgb(255, 31, 56, 92)

function New-RoundRect([single]$x, [single]$y, [single]$w, [single]$h, [single]$r) {
    $p = New-Object Drawing.Drawing2D.GraphicsPath
    $d = 2 * $r
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    $p
}

# The mark: a teal tile holding a model sheet with a folded corner and an orange version tab.
function Draw-Mark([Drawing.Graphics]$g, [single]$x, [single]$y, [single]$s) {
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $u = $s / 32.0

    $tile = New-RoundRect $x $y $s $s (6 * $u)
    $brush = New-Object Drawing.Drawing2D.LinearGradientBrush ([Drawing.PointF]::new($x, $y)), ([Drawing.PointF]::new($x, $y + $s)), $teal, $tealDk
    $g.FillPath($brush, $tile)

    # Sheet with the top-right corner folded.
    $sx = $x + 8 * $u; $sy = $y + 5 * $u; $sw = 16 * $u; $sh = 22 * $u; $f = 6 * $u
    $sheet = [Drawing.PointF[]]@(
        [Drawing.PointF]::new($sx, $sy),
        [Drawing.PointF]::new($sx + $sw - $f, $sy),
        [Drawing.PointF]::new($sx + $sw, $sy + $f),
        [Drawing.PointF]::new($sx + $sw, $sy + $sh),
        [Drawing.PointF]::new($sx, $sy + $sh))
    $g.FillPolygon((New-Object Drawing.SolidBrush $paper), $sheet)
    $corner = [Drawing.PointF[]]@(
        [Drawing.PointF]::new($sx + $sw - $f, $sy),
        [Drawing.PointF]::new($sx + $sw - $f, $sy + $f),
        [Drawing.PointF]::new($sx + $sw, $sy + $f))
    $g.FillPolygon((New-Object Drawing.SolidBrush $fold), $corner)

    # Plan lines on the sheet (left out at 16 px, where they only blur).
    if ($s -ge 24) {
        $pen = New-Object Drawing.Pen $line, ([Math]::Max(1.0, 1.6 * $u))
        foreach ($ly in 11, 15, 19) {
            $len = if ($ly -eq 11) { 7 } else { 10 }
            $g.DrawLine($pen, $sx + 3 * $u, $y + $ly * $u, $sx + (3 + $len) * $u, $y + $ly * $u)
        }
    }

    # Version tab across the lower-right of the sheet.
    $tab = New-RoundRect ($x + 15 * $u) ($y + 20 * $u) (13 * $u) (8 * $u) (2 * $u)
    $g.FillPath((New-Object Drawing.SolidBrush $orange), $tab)
    if ($s -ge 32) {
        $pen = New-Object Drawing.Pen $paper, (1.8 * $u)
        $pen.StartCap = 'Round'; $pen.EndCap = 'Round'; $pen.LineJoin = 'Round'
        # A small "v" for version.
        $g.DrawLines($pen, [Drawing.PointF[]]@(
            [Drawing.PointF]::new($x + 19 * $u, $y + 22.3 * $u),
            [Drawing.PointF]::new($x + 21.5 * $u, $y + 25.7 * $u),
            [Drawing.PointF]::new($x + 24 * $u, $y + 22.3 * $u)))
    }
}

function Save-Icon([int]$size, [string]$path) {
    $bmp = New-Object Drawing.Bitmap $size, $size, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.Clear([Drawing.Color]::Transparent)
    Draw-Mark $g 0 0 $size
    $g.Dispose()
    $bmp.Save($path, [Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

$images = Join-Path $Root 'sources\images'
$installer = Join-Path $Root 'sources\installer'
foreach ($size in 16, 32, 120, 256) {
    Save-Icon $size (Join-Path $images "PaperEngineerShell_$size.png")
}

# Installer banner (493x58): white strip with the mark on the right.
$banner = New-Object Drawing.Bitmap 493, 58, ([Drawing.Imaging.PixelFormat]::Format24bppRgb)
$g = [Drawing.Graphics]::FromImage($banner)
$g.Clear([Drawing.Color]::White)
Draw-Mark $g 437 7 44
$g.Dispose()
$banner.Save((Join-Path $installer 'installer-banner.bmp'), [Drawing.Imaging.ImageFormat]::Bmp)
$banner.Dispose()

# Installer background (493x312): the left panel carries the mark and the product name.
$bg = New-Object Drawing.Bitmap 493, 312, ([Drawing.Imaging.PixelFormat]::Format24bppRgb)
$g = [Drawing.Graphics]::FromImage($bg)
$g.Clear([Drawing.Color]::White)
Draw-Mark $g 34 56 96
$g.TextRenderingHint = 'AntiAliasGridFit'
$font = New-Object Drawing.Font 'Segoe UI Semibold', 13
$g.DrawString('PaperEngineer', $font, (New-Object Drawing.SolidBrush $navy), 28, 168)
$g.DrawString('Shell', $font, (New-Object Drawing.SolidBrush $navy), 28, 192)
$g.Dispose()
$bg.Save((Join-Path $installer 'installer-background.bmp'), [Drawing.Imaging.ImageFormat]::Bmp)
$bg.Dispose()

Write-Host "Brand images written to $images and $installer"
