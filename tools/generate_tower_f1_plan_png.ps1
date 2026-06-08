Add-Type -AssemblyName System.Drawing

$out = 'D:/BlackCommission/Assets/_Project/Art/Maps/Tower_EarthCoast_01/References/Tower_EarthCoast_01_F1_CurrentPlan.png'

$rooms = @(
    @{Name='WAREHOUSE';Size='L';X=0.00;Z=-8.00;W=12.00;D=8.00},
    @{Name='POWER';Size='S';X=0.00;Z=10.00;W=4.00;D=4.00},
    @{Name='TEMP';Size='S';X=4.00;Z=14.00;W=4.00;D=4.00},
    @{Name='SECUR';Size='S';X=8.00;Z=10.00;W=4.00;D=4.00},
    @{Name='LOBBY';Size='L';X=12.00;Z=0.00;W=12.00;D=8.00},
    @{Name='SAMPLE';Size='S';X=12.00;Z=10.00;W=4.00;D=4.00},
    @{Name='HALL';Size='L';X=12.00;Z=16.00;W=12.00;D=8.00},
    @{Name='DORM';Size='M';X=12.00;Z=24.00;W=8.00;D=8.00},
    @{Name='CANTEEN';Size='M';X=12.00;Z=32.00;W=8.00;D=8.00},
    @{Name='FOREMAN';Size='M';X=21.86;Z=35.88;W=7.99;D=7.99},
    @{Name='WORKSHOP';Size='M';X=24.00;Z=8.00;W=8.00;D=8.00},
    @{Name='PUMP';Size='S';X=26.00;Z=0.00;W=4.00;D=4.00},
    @{Name='REBAR';Size='M';X=34.00;Z=8.00;W=8.00;D=8.00},
    @{Name='DOCK';Size='M';X=34.00;Z=16.00;W=8.00;D=8.00},
    @{Name='SHANTY';Size='S';X=34.00;Z=24.00;W=4.00;D=4.00}
)

$specials = @(
    @{Name='COLLAPSE';Size='STRUCT';X=0.00;Z=23.86;W=12.14;D=16.14},
    @{Name='FIRE';Size='STRUCT';X=29.79;Z=35.79;W=8.16;D=8.16},
    @{Name='STAIRA1';Size='STRUCT';X=26.00;Z=26.41;W=4.00;D=11.19},
    @{Name='STAIRB1';Size='STRUCT';X=0.00;Z=16.00;W=4.00;D=8.00},
    @{Name='VAN';Size='STRUCT';X=10.24;Z=-8.00;W=19.80;D=8.00}
)

$all = @($rooms + $specials)
$minX = (($all | ForEach-Object { $_.X }) | Measure-Object -Minimum).Minimum - 3
$maxX = (($all | ForEach-Object { $_.X + $_.W }) | Measure-Object -Maximum).Maximum + 3
$minZ = (($all | ForEach-Object { $_.Z }) | Measure-Object -Minimum).Minimum - 3
$maxZ = (($all | ForEach-Object { $_.Z + $_.D }) | Measure-Object -Maximum).Maximum + 3
$scale = 24.0
$marginLeft = 95.0
$marginTop = 100.0
$legendWidth = 330.0
$imgW = [int](($maxX - $minX) * $scale + $marginLeft * 2 + $legendWidth)
$imgH = [int](($maxZ - $minZ) * $scale + $marginTop * 2)

function Convert-X([double]$x) {
    [float]($script:marginLeft + ($x - $script:minX) * $script:scale)
}

function Convert-Z([double]$z) {
    [float]($script:marginTop + ($script:maxZ - $z) * $script:scale)
}

function Get-Rect($item) {
    [System.Drawing.RectangleF]::new(
        (Convert-X $item.X),
        (Convert-Z ($item.Z + $item.D)),
        [float]($item.W * $script:scale),
        [float]($item.D * $script:scale)
    )
}

$bmp = [System.Drawing.Bitmap]::new($imgW, $imgH)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$g.Clear([System.Drawing.Color]::FromArgb(250, 248, 240))

$fontTitle = [System.Drawing.Font]::new([System.Drawing.FontFamily]::GenericSansSerif, 24.0, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$font = [System.Drawing.Font]::new([System.Drawing.FontFamily]::GenericSansSerif, 14.0, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$fontBold = [System.Drawing.Font]::new([System.Drawing.FontFamily]::GenericSansSerif, 14.0, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$fontSmall = [System.Drawing.Font]::new([System.Drawing.FontFamily]::GenericSansSerif, 12.0, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)

$gridPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(212, 207, 196), 1.0)
for ($x = [Math]::Floor($minX / 4) * 4; $x -le $maxX; $x += 4) {
    $px = Convert-X $x
    $g.DrawLine($gridPen, $px, (Convert-Z $minZ), $px, (Convert-Z $maxZ))
}
for ($z = [Math]::Floor($minZ / 4) * 4; $z -le $maxZ; $z += 4) {
    $py = Convert-Z $z
    $g.DrawLine($gridPen, (Convert-X $minX), $py, (Convert-X $maxX), $py)
}

$g.DrawString('Tower EarthCoast 01 - Floor 1 Plan', $fontTitle, [System.Drawing.Brushes]::Black, 30.0, 24.0)
$g.DrawString('Actual PNG preview. Color = room size, generated from saved scene room footprints.', $font, [System.Drawing.Brushes]::DimGray, 32.0, 55.0)

$colors = @{
    S = [System.Drawing.Color]::FromArgb(242, 212, 107)
    M = [System.Drawing.Color]::FromArgb(127, 179, 213)
    L = [System.Drawing.Color]::FromArgb(140, 192, 132)
    STRUCT = [System.Drawing.Color]::FromArgb(184, 184, 184)
}
$outline = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(35, 35, 35), 2.0)
$dash = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(90, 90, 90), 2.0)
$dash.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash

foreach ($item in $specials) {
    $rect = Get-Rect $item
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(125, $colors.STRUCT))
    $g.FillRectangle($brush, $rect)
    $g.DrawRectangle($dash, $rect.X, $rect.Y, $rect.Width, $rect.Height)
    $brush.Dispose()
}

foreach ($item in $rooms) {
    $rect = Get-Rect $item
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(190, $colors[$item.Size]))
    $g.FillRectangle($brush, $rect)
    $g.DrawRectangle($outline, $rect.X, $rect.Y, $rect.Width, $rect.Height)
    $brush.Dispose()
}

$centerFormat = [System.Drawing.StringFormat]::new()
$centerFormat.Alignment = [System.Drawing.StringAlignment]::Center
$centerFormat.LineAlignment = [System.Drawing.StringAlignment]::Center
foreach ($item in @($specials + $rooms)) {
    $rect = Get-Rect $item
    $label = "$($item.Name)`n$($item.Size)"
    $g.DrawString($label, $fontBold, [System.Drawing.Brushes]::Black, $rect, $centerFormat)
}

$arrowPen = [System.Drawing.Pen]::new([System.Drawing.Color]::Black, 3.0)
$ax = $imgW - $legendWidth - 50
$ay = 95
$g.DrawLine($arrowPen, $ax, $ay + 45, $ax, $ay)
$g.DrawLine($arrowPen, $ax, $ay, $ax - 9, $ay + 15)
$g.DrawLine($arrowPen, $ax, $ay, $ax + 9, $ay + 15)
$g.DrawString('N', $fontBold, [System.Drawing.Brushes]::Black, [float]($ax - 5), [float]($ay - 25))

$lx = $imgW - $legendWidth + 35
$ly = 95
$legendBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(235, 255, 255, 255))
$g.FillRectangle($legendBrush, $lx - 18, $ly - 40, 270, 245)
$g.DrawRectangle([System.Drawing.Pens]::Gray, $lx - 18, $ly - 40, 270, 245)
$g.DrawString('Legend', $fontBold, [System.Drawing.Brushes]::Black, [float]$lx, [float]($ly - 28))

$legendItems = @(
    @{K='S';T='Small <= 25 sqm'},
    @{K='M';T='Medium <= 80 sqm'},
    @{K='L';T='Large > 80 sqm'},
    @{K='STRUCT';T='Stair / Van / Fire / Collapse'}
)
$yy = $ly + 8
foreach ($legendItem in $legendItems) {
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(190, $colors[$legendItem.K]))
    $g.FillRectangle($brush, $lx, $yy, 24, 16)
    $g.DrawRectangle([System.Drawing.Pens]::Black, $lx, $yy, 24, 16)
    $g.DrawString("$($legendItem.K): $($legendItem.T)", $fontSmall, [System.Drawing.Brushes]::Black, [float]($lx + 34), [float]($yy + 1))
    $brush.Dispose()
    $yy += 31
}
$g.DrawString('Counts: S=6, M=6, L=3', $fontSmall, [System.Drawing.Brushes]::DimGray, [float]$lx, [float]($yy + 12))
$g.DrawString('Grid: 4m', $fontSmall, [System.Drawing.Brushes]::DimGray, [float]$lx, [float]($yy + 32))

$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)

$g.Dispose()
$bmp.Dispose()
$fontTitle.Dispose()
$font.Dispose()
$fontBold.Dispose()
$fontSmall.Dispose()
$gridPen.Dispose()
$outline.Dispose()
$dash.Dispose()
$arrowPen.Dispose()
$legendBrush.Dispose()
$centerFormat.Dispose()

Write-Host "Wrote $out size=${imgW}x${imgH}"
