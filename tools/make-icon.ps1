# Renders package\icon.png (256x256, required by Thunderstore) with System.Drawing, in the game's palette.
param([string]$Out = (Join-Path $PSScriptRoot '..\package\icon.png'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$size = 256
$bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.Clear([System.Drawing.Color]::Transparent)

function New-RoundedRect([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = 2 * $r
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}
function C([int]$a, [int]$r, [int]$gr, [int]$b) { [System.Drawing.Color]::FromArgb($a, $r, $gr, $b) }

$cream = C 255 245 237 225
$ink = C 255 106 77 82
$track = C 255 197 182 174
$green = C 255 179 209 127
$orange = C 255 231 169 102
$red = C 255 231 128 109

# Background: the game's dark brown
$bg = New-RoundedRect 0 0 256 256 44
$bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 0, 0), (New-Object System.Drawing.Point 0, 256), (C 255 128 96 100), (C 255 84 60 65)
$g.FillPath($bgBrush, $bg)

# Speaker with sound waves
$creamBrush = New-Object System.Drawing.SolidBrush $cream
$g.FillPath($creamBrush, (New-RoundedRect 52 50 26 38 6))
$cone = New-Object System.Drawing.Drawing2D.GraphicsPath
$cone.AddPolygon(@(
    (New-Object System.Drawing.PointF 72, 54), (New-Object System.Drawing.PointF 104, 30),
    (New-Object System.Drawing.PointF 104, 108), (New-Object System.Drawing.PointF 72, 84)))
$g.FillPath($creamBrush, $cone)
$wave = New-Object System.Drawing.Pen $cream, 9
$wave.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$wave.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawArc($wave, 92, 44, 50, 50, -50, 100)
$g.DrawArc($wave, 92, 24, 90, 90, -48, 96)
$g.DrawArc($wave, 92, 4, 130, 130, -46, 92)

# One slider per player: a coloured head, the track, the fill up to a cream knob
$rows = @(
    @{ Y = 150; Color = $green; Knob = 196 },
    @{ Y = 188; Color = $orange; Knob = 122 },
    @{ Y = 226; Color = $red; Knob = 156 }
)
$knobPen = New-Object System.Drawing.Pen $ink, 4
foreach ($row in $rows) {
    $y = $row.Y
    $brush = New-Object System.Drawing.SolidBrush $row.Color
    $g.FillEllipse($brush, 26, $y - 13, 26, 26)
    $g.FillPath((New-Object System.Drawing.SolidBrush $track), (New-RoundedRect 66 ($y - 7) 166 14 7))
    $g.FillPath($brush, (New-RoundedRect 66 ($y - 7) ($row.Knob - 66) 14 7))
    $g.FillEllipse($creamBrush, $row.Knob - 14, $y - 14, 28, 28)
    $g.DrawEllipse($knobPen, $row.Knob - 14, $y - 14, 28, 28)
}

$g.Dispose()
$full = [System.IO.Path]::GetFullPath($Out)
$bmp.Save($full, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "Icon: $full"
