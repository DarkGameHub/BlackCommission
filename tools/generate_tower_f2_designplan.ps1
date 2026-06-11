Add-Type -AssemblyName System.Drawing

# Tower EarthCoast 01 - FLOOR 2 redesign proposal v5 (bridge-on-backbone + carry escape).
# v5: removes the "diagram line" feeling by forcing the objective route through EDGE/BRIDGE,
#     clusters side rooms by gameplay job, and marks the two-hand沙盘 carry escape preference.
# PROPOSAL ONLY - does not touch TowerTopologyV3.cs / TowerV3WhiteboxBuilder.cs.
# PROPOSAL ONLY - does not touch TowerTopologyV3.cs / TowerV3WhiteboxBuilder.cs.

$out = 'D:/BlackCommission/Assets/_Project/Art/Maps/Tower_EarthCoast_01/References/Tower_EarthCoast_01_F2_DesignPlan_v5.png'

# palette (Municipal Debt Noir) — Light field keys into this for the per-room light swatch
$pal = @{
    amber = [System.Drawing.Color]::FromArgb(235, 170, 60)   # sodium / warm beacon / objective key light
    teal  = [System.Drawing.Color]::FromArgb(60, 130, 135)   # civic teal ambient (hub / circulation)
    red   = [System.Drawing.Color]::FromArgb(200, 55, 45)    # stamp red - danger / debt
    green = [System.Drawing.Color]::FromArgb(70, 150, 90)    # restrained dispatch green - safe
    dark  = [System.Drawing.Color]::FromArgb(70, 72, 80)     # dead rubber black - unlit back-of-house
    bridge = [System.Drawing.Color]::FromArgb(165, 125, 75)  # exposed scaffold / raw timber
}

# Name; Size; X,Z (SW corner, m); W,D ; Note(loot/role); Light(palette key); Dress(set-dressing tag)
$rooms = @(
    @{Name='SHOWFLAT';Size='M';X=4.0;Z=16.0;W=8.0;D=8.0;Note='warm beacon';Light='amber';Dress='样板间 / 暖光信标'},
    @{Name='MARKET';Size='S';X=0.0;Z=8.0;W=4.0;D=4.0;Note='low loot';Light='dark';Dress='储物 / 低压补给'},
    @{Name='TANK';Size='S';X=0.0;Z=0.0;W=4.0;D=4.0;Note='densify';Light='dark';Dress='水箱机房'},
    @{Name='ARCHIVE';Size='S';X=8.0;Z=8.0;W=4.0;D=4.0;Note='debt docs';Light='dark';Dress='资料档案 / 支线'},
    @{Name='FIN';Size='S';X=8.0;Z=0.0;W=4.0;D=4.0;Note='densify';Light='dark';Dress='财务室'},
    @{Name='MODEL';Size='M';X=16.0;Z=16.0;W=8.0;D=8.0;Note='table blocks';Light='teal';Dress='模型展厅 / 绕桌'},
    @{Name='EDGE';Size='L';X=16.0;Z=4.0;W=12.0;D=8.0;Note='shaft / fall';Light='red';Dress='竖井边缘 / 主路伤疤'},
    @{Name='BRIDGE';Size='BRIDGE';X=28.0;Z=8.0;W=4.0;D=4.0;Note='2H pinch';Light='bridge';Dress='脚手桥 / 必经窄点'},
    @{Name='EXEC';Size='M';X=32.0;Z=28.0;W=8.0;D=8.0;Note='risky entry';Light='dark';Dress='行政套间 / A梯压入'},
    @{Name='NEGOT';Size='M';X=32.0;Z=40.0;W=8.0;D=8.0;Note='densify';Light='teal';Dress='洽谈区'},
    @{Name='SALES';Size='M';X=32.0;Z=16.0;W=8.0;D=8.0;Note='2H(130)';Light='amber';Dress='销售办公 / 隔断'},
    @{Name='VIP';Size='M';X=44.0;Z=16.0;W=8.0;D=8.0;Note='sealed gate';Light='dark';Dress='VIP休息 / 封条'},
    @{Name='DANGER';Size='S';X=44.0;Z=28.0;W=4.0;D=4.0;Note='2H(95) risk';Light='red';Dress='危险间 / 近目标'},
    @{Name='TARGET';Size='L';X=48.0;Z=4.0;W=12.0;D=8.0;Note='OBJECTIVE';Light='amber';Dress='查封沙盘 / 目标·巢'},
    @{Name='BALCONY';Size='S';X=60.0;Z=12.0;W=4.0;D=4.0;Note='drop->F1';Light='green';Dress='阳台 / 高风险跳板'}
)

$specials = @(
    @{Name='STAIRA2';Size='STRUCT';X=26.0;Z=28.0;W=4.0;D=8.0;Note='down->F1 (A)';Light='teal';Dress='对齐F1 A梯 · 暴露'},
    @{Name='STAIRB2';Size='STRUCT';X=0.0;Z=16.0;W=4.0;D=8.0;Note='down->F1 (B)';Light='dark';Dress='对齐F1 B梯 · 暗'}
)

# centre of a room by name
$centre = @{}
foreach ($r in @($rooms + $specials)) {
    $cx = [double]$r.X + [double]$r.W / 2.0
    $cz = [double]$r.Z + [double]$r.D / 2.0
    $centre[$r.Name] = @($cx, $cz)
}

# edges: A,B, class (crit|fixed|toggle), optional toggle id, K = door|corr (door = narrow 2m mouth)
$edges = @(
    # Critical objective backbone: B stair teaches the floor, then the route is forced
    # through the shaft wound before entering the sales / VIP / objective zone.
    @{A='STAIRB2';B='SHOWFLAT';C='crit';K='corr'},   # B-stair (W, aligned F1) -> beacon
    @{A='SHOWFLAT';B='MODEL';C='crit';K='corr'},     # W->E spine
    @{A='MODEL';B='EDGE';C='crit';K='corr'},         # main route must touch the shaft edge
    @{A='EDGE';B='BRIDGE';C='crit';K='corr'},        # exposed 2H pinch point
    @{A='BRIDGE';B='SALES';C='crit';K='corr'},
    @{A='SALES';B='VIP';C='crit';K='corr'},          # continue E
    @{A='VIP';B='TARGET';C='crit';K='corr'},         # dogleg S to deep objective
    @{A='STAIRA2';B='EXEC';C='crit';K='corr'},       # A-stair plunges from far N
    @{A='EXEC';B='SALES';C='crit';K='corr'},         # fast/risky entry and carry return

    # fixed (always open) branches
    @{A='SHOWFLAT';B='MARKET';C='fixed';K='door'},
    @{A='MARKET';B='TANK';C='fixed';K='door'},
    @{A='MARKET';B='ARCHIVE';C='fixed';K='door'},
    @{A='ARCHIVE';B='FIN';C='fixed';K='door'},
    @{A='ARCHIVE';B='EDGE';C='fixed';K='corr'},
    @{A='EXEC';B='NEGOT';C='fixed';K='corr'},
    @{A='VIP';B='DANGER';C='fixed';K='door'},
    @{A='TARGET';B='BALCONY';C='fixed';K='door'},

    # toggle loops / shortcuts (more of them = windier, riskier; never critical)
    @{A='EDGE';B='SALES';C='toggle';T='T7';K='door'},      # shaft escape ring, bypasses BRIDGE when open
    @{A='BRIDGE';B='DANGER';C='toggle';T='T8';K='door'},   # bad idea, good story
    @{A='MODEL';B='EXEC';C='toggle';T='T9';K='corr'},      # north ring linking both stair approaches
    @{A='SALES';B='DANGER';C='toggle';T='T13';K='door'},   # loot ring near objective
    @{A='SHOWFLAT';B='EDGE';C='toggle';T='T14';K='corr'},  # west hub -> shaft loop
    @{A='DANGER';B='TARGET';C='toggle';T='T15';K='door'},  # risky 2nd objective approach
    @{A='ARCHIVE';B='MODEL';C='toggle';T='T16';K='door'},  # archive backdoor to spine
    @{A='VIP';B='BALCONY';C='toggle';T='T17';K='door'}     # chase-only drop option
)

# Post-pickup, two-hand沙盘 escape preference. This is not a separate required route;
# it marks the route players will want once their hands are occupied.
$returnEdges = @(
    @{A='TARGET';B='VIP';K='corr'},
    @{A='VIP';B='SALES';K='corr'},
    @{A='SALES';B='EXEC';K='corr'},
    @{A='EXEC';B='STAIRA2';K='corr'}
)

$all = @($rooms + $specials)
$minX = (($all | ForEach-Object { $_.X }) | Measure-Object -Minimum).Minimum - 4
$maxX = (($all | ForEach-Object { $_.X + $_.W }) | Measure-Object -Maximum).Maximum + 4
$minZ = (($all | ForEach-Object { $_.Z }) | Measure-Object -Minimum).Minimum - 4
$maxZ = (($all | ForEach-Object { $_.Z + $_.D }) | Measure-Object -Maximum).Maximum + 4
$scale = 21.0
$marginLeft = 60.0
$marginTop = 120.0
$legendWidth = 380.0
$imgW = [int](($maxX - $minX) * $scale + $marginLeft * 2 + $legendWidth)
$imgH = [int](($maxZ - $minZ) * $scale + $marginTop * 2)

function Convert-X([double]$x) { [float]($script:marginLeft + ($x - $script:minX) * $script:scale) }
function Convert-Z([double]$z) { [float]($script:marginTop + ($script:maxZ - $z) * $script:scale) }
function Get-Rect($item) {
    [System.Drawing.RectangleF]::new((Convert-X $item.X), (Convert-Z ($item.Z + $item.D)),
        [float]($item.W * $script:scale), [float]($item.D * $script:scale))
}

$bmp = [System.Drawing.Bitmap]::new($imgW, $imgH)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$g.Clear([System.Drawing.Color]::FromArgb(250, 248, 240))

$fontTitle = [System.Drawing.Font]::new([System.Drawing.FontFamily]::GenericSansSerif, 24.0, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$font = [System.Drawing.Font]::new([System.Drawing.FontFamily]::GenericSansSerif, 14.0, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$fontBold = [System.Drawing.Font]::new([System.Drawing.FontFamily]::GenericSansSerif, 13.0, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$fontSmall = [System.Drawing.Font]::new([System.Drawing.FontFamily]::GenericSansSerif, 11.0, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$fontItalic = [System.Drawing.Font]::new([System.Drawing.FontFamily]::GenericSansSerif, 11.0, [System.Drawing.FontStyle]::Italic, [System.Drawing.GraphicsUnit]::Pixel)

# grid
$gridPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(212, 207, 196), 1.0)
for ($x = [Math]::Floor($minX / 4) * 4; $x -le $maxX; $x += 4) { $px = Convert-X $x; $g.DrawLine($gridPen, $px, (Convert-Z $minZ), $px, (Convert-Z $maxZ)) }
for ($z = [Math]::Floor($minZ / 4) * 4; $z -le $maxZ; $z += 4) { $py = Convert-Z $z; $g.DrawLine($gridPen, (Convert-X $minX), $py, (Convert-X $maxX), $py) }

$g.DrawString('Tower EarthCoast 01 - Floor 2 Plan (REDESIGN PROPOSAL v5)', $fontTitle, [System.Drawing.Brushes]::Black, 30.0, 20.0)
$g.DrawString('v5: stair landings LOCKED over F1 (STAIRA2 @26,28; STAIRB2 @0,16). Main route now crosses EDGE/BRIDGE before SALES/VIP/TARGET.', $font, [System.Drawing.Brushes]::DimGray, 32.0, 52.0)
$g.DrawString('Red = objective approach. Orange dashed = preferred two-hand carry escape via A-stair. Green toggles make loops, not required paths.', $font, [System.Drawing.Brushes]::DimGray, 32.0, 72.0)
$g.DrawString('Per-room dot = light colour. Corner tag = set-dressing. Stamp-red = debt/danger; amber = warm/objective.', $fontItalic, [System.Drawing.Brushes]::DimGray, 32.0, 92.0)

$colors = @{
    S = [System.Drawing.Color]::FromArgb(242, 212, 107)
    M = [System.Drawing.Color]::FromArgb(127, 179, 213)
    L = [System.Drawing.Color]::FromArgb(140, 192, 132)
    BRIDGE = [System.Drawing.Color]::FromArgb(178, 136, 82)
    STRUCT = [System.Drawing.Color]::FromArgb(184, 184, 184)
}
$outline = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(35, 35, 35), 2.0)
$dash = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(90, 90, 90), 2.0)
$dash.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash

# ---- rooms ----
foreach ($item in $specials) {
    $rect = Get-Rect $item
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(125, $colors.STRUCT))
    $g.FillRectangle($brush, $rect); $g.DrawRectangle($dash, $rect.X, $rect.Y, $rect.Width, $rect.Height); $brush.Dispose()
}
foreach ($item in $rooms) {
    $rect = Get-Rect $item
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(190, $colors[$item.Size]))
    $g.FillRectangle($brush, $rect); $g.DrawRectangle($outline, $rect.X, $rect.Y, $rect.Width, $rect.Height); $brush.Dispose()
}

# ---- path overlay (elbow routes between centres) ----
$critPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(210, 40, 40), 4.0)
$fixedPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(60, 60, 60), 2.2); $fixedPen.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
$togglePen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(40, 150, 70), 2.2); $togglePen.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dot
$carryPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(230, 135, 30), 3.2); $carryPen.DashStyle = [System.Drawing.Drawing2D.DashStyle]::DashDot
$nodeBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(35, 35, 35))

foreach ($e in $edges) {
    $a = $centre[$e.A]; $b = $centre[$e.B]
    $ax = Convert-X $a[0]; $az = Convert-Z $a[1]; $bx = Convert-X $b[0]; $bz = Convert-Z $b[1]
    $pen = switch ($e.C) { 'crit' { $critPen } 'fixed' { $fixedPen } 'toggle' { $togglePen } }
    if ([Math]::Abs($a[1]-$b[1]) -ge [Math]::Abs($a[0]-$b[0])) {
        $g.DrawLine($pen, $ax, $az, $ax, $bz); $g.DrawLine($pen, $ax, $bz, $bx, $bz)
    } else {
        $g.DrawLine($pen, $ax, $az, $bx, $az); $g.DrawLine($pen, $bx, $az, $bx, $bz)
    }
    # door tick: small perpendicular mark near B end for narrow-door links
    if ($e.K -eq 'door') {
        $g.FillEllipse([System.Drawing.Brushes]::White, [float](($ax+$bx)/2.0-3), [float](($az+$bz)/2.0-3), 6.0, 6.0)
        $g.DrawEllipse($outline, [float](($ax+$bx)/2.0-3), [float](($az+$bz)/2.0-3), 6.0, 6.0)
    }
    if ($e.C -eq 'toggle' -and $e.T) {
        $mx = ($ax + $bx) / 2.0; $mz = ($az + $bz) / 2.0
        $g.DrawString($e.T, $fontSmall, [System.Drawing.Brushes]::ForestGreen, [float]($mx-8), [float]($mz-7))
    }
}
foreach ($e in $returnEdges) {
    $a = $centre[$e.A]; $b = $centre[$e.B]
    $ax = (Convert-X $a[0]) + 5.0; $az = (Convert-Z $a[1]) + 5.0; $bx = (Convert-X $b[0]) + 5.0; $bz = (Convert-Z $b[1]) + 5.0
    if ([Math]::Abs($a[1]-$b[1]) -ge [Math]::Abs($a[0]-$b[0])) {
        $g.DrawLine($carryPen, $ax, $az, $ax, $bz); $g.DrawLine($carryPen, $ax, $bz, $bx, $bz)
    } else {
        $g.DrawLine($carryPen, $ax, $az, $bx, $az); $g.DrawLine($carryPen, $bx, $az, $bx, $bz)
    }
}
foreach ($r in @($rooms + $specials)) {
    $c = $centre[$r.Name]; $cx = Convert-X $c[0]; $cz = Convert-Z $c[1]
    $g.FillEllipse($nodeBrush, [float]($cx-3), [float]($cz-3), 6.0, 6.0)
}

# ---- ART LAYER: per-room light swatch + set-dressing tag ----
foreach ($item in @($rooms + $specials)) {
    $rect = Get-Rect $item
    # light swatch (top-right corner of room)
    if ($item.Light) {
        $lc = $pal[$item.Light]
        $sw = [System.Drawing.SolidBrush]::new($lc)
        $g.FillEllipse($sw, [float]($rect.X + $rect.Width - 16), [float]($rect.Y + 5), 11.0, 11.0)
        $g.DrawEllipse([System.Drawing.Pens]::Black, [float]($rect.X + $rect.Width - 16), [float]($rect.Y + 5), 11.0, 11.0)
        $sw.Dispose()
    }
}

# special art markers
# EDGE: stamp-red hazard lip (red dashed rect over the shaft border)
$edgeItem = $rooms | Where-Object { $_.Name -eq 'EDGE' }
$er = Get-Rect $edgeItem
$hazPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(200, 55, 45), 3.0); $hazPen.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
$g.DrawRectangle($hazPen, $er.X + 3, $er.Y + 3, $er.Width - 6, $er.Height - 6)
$g.DrawString('坠落危险', $fontSmall, [System.Drawing.Brushes]::Firebrick, [float]($er.X + 8), [float]($er.Y + $er.Height - 18))

# MODEL: central table blocks the straight line for both normal travel and 2H carry.
$modelItem = $rooms | Where-Object { $_.Name -eq 'MODEL' }
$mr = Get-Rect $modelItem
$tableBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(120, 90, 65))
$g.FillRectangle($tableBrush, [float]($mr.X + $mr.Width * 0.32), [float]($mr.Y + $mr.Height * 0.35), [float]($mr.Width * 0.36), [float]($mr.Height * 0.30))
$g.DrawRectangle([System.Drawing.Pens]::SaddleBrown, [float]($mr.X + $mr.Width * 0.32), [float]($mr.Y + $mr.Height * 0.35), [float]($mr.Width * 0.36), [float]($mr.Height * 0.30))
$tableBrush.Dispose()

# BRIDGE: mark it as the deliberate two-hand pinch point.
$bridgeItem = $rooms | Where-Object { $_.Name -eq 'BRIDGE' }
$br = Get-Rect $bridgeItem
$railPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(110, 70, 35), 2.0)
$g.DrawLine($railPen, $br.X + 4, $br.Y + 6, $br.X + $br.Width - 4, $br.Y + 6)
$g.DrawLine($railPen, $br.X + 4, $br.Y + $br.Height - 6, $br.X + $br.Width - 4, $br.Y + $br.Height - 6)
$g.DrawString('2H窄点', $fontSmall, [System.Drawing.Brushes]::SaddleBrown, [float]($br.X + 3), [float]($br.Y + $br.Height - 18))

# TARGET: 查封 seizure stamp box
$tgtItem = $rooms | Where-Object { $_.Name -eq 'TARGET' }
$tr = Get-Rect $tgtItem
$stampPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(200, 55, 45), 2.5)
$g.DrawRectangle($stampPen, [float]($tr.X + 8), [float]($tr.Y + 8), 56.0, 22.0)
$g.DrawString('查封 SEIZED', $fontSmall, [System.Drawing.Brushes]::Firebrick, [float]($tr.X + 11), [float]($tr.Y + 11))

# seal-notice tags on VIP / EXEC doors
foreach ($nm in @('VIP','EXEC')) {
    $it = $rooms | Where-Object { $_.Name -eq $nm }
    $rr = Get-Rect $it
    $g.DrawString('封条', $fontSmall, [System.Drawing.Brushes]::Firebrick, [float]($rr.X + 6), [float]($rr.Y + $rr.Height - 18))
}

# dispatch-green SAFE markers at SHOWFLAT / STAIRB2 / BALCONY
$greenBrush = [System.Drawing.SolidBrush]::new($pal['green'])
foreach ($nm in @('SHOWFLAT','BALCONY')) {
    $it = $rooms | Where-Object { $_.Name -eq $nm }
    $rr = Get-Rect $it
    $g.DrawString('▲安全', $fontSmall, $greenBrush, [float]($rr.X + 6), [float]($rr.Y + $rr.Height - 18))
}
$execItem = $rooms | Where-Object { $_.Name -eq 'EXEC' }
$exr = Get-Rect $execItem
$g.DrawString('2H快撤 -> A梯', $fontSmall, [System.Drawing.Brushes]::DarkOrange, [float]($exr.X + 6), [float]($exr.Y + 6))

# ---- labels: name + role note + set-dressing tag ----
$cf = [System.Drawing.StringFormat]::new(); $cf.Alignment = [System.Drawing.StringAlignment]::Center; $cf.LineAlignment = [System.Drawing.StringAlignment]::Center
foreach ($item in @($specials + $rooms)) {
    $rect = Get-Rect $item
    $note = if ($item.Note) { "`n$($item.Note)" } else { '' }
    $g.DrawString("$($item.Name)$note", $fontBold, [System.Drawing.Brushes]::Black, $rect, $cf)
    # set-dressing tag along the bottom-centre
    if ($item.Dress) {
        $tagRect = [System.Drawing.RectangleF]::new($rect.X, [float]($rect.Y + $rect.Height - 16), $rect.Width, 14.0)
        $g.DrawString($item.Dress, $fontItalic, [System.Drawing.Brushes]::DimGray, $tagRect, $cf)
    }
}

# stair down + balcony drop markers
$bc = $centre['BALCONY']; $bx = Convert-X $bc[0]; $bz = Convert-Z $bc[1]
$g.DrawString('drop->F1 DOCK', $fontSmall, [System.Drawing.Brushes]::SaddleBrown, [float]($bx-30), [float]($bz+16))
foreach ($s in $specials) {
    $c = $centre[$s.Name]; $cx = Convert-X $c[0]; $cz = Convert-Z $c[1]
    $g.DrawString('v 下到F1', $fontSmall, [System.Drawing.Brushes]::RoyalBlue, [float]($cx-18), [float]($cz+16))
}

# North arrow
$arrowPen = [System.Drawing.Pen]::new([System.Drawing.Color]::Black, 3.0)
$ax = $imgW - $legendWidth - 40; $ay = 110
$g.DrawLine($arrowPen, $ax, $ay + 45, $ax, $ay); $g.DrawLine($arrowPen, $ax, $ay, $ax - 9, $ay + 15); $g.DrawLine($arrowPen, $ax, $ay, $ax + 9, $ay + 15)
$g.DrawString('N', $fontBold, [System.Drawing.Brushes]::Black, [float]($ax - 5), [float]($ay - 22))

# ---- legend ----
$lx = $imgW - $legendWidth + 30; $ly = 110
$legendBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(235, 255, 255, 255))
$g.FillRectangle($legendBrush, $lx - 18, $ly - 40, 340, 560)
$g.DrawRectangle([System.Drawing.Pens]::Gray, $lx - 18, $ly - 40, 340, 560)
$g.DrawString('Legend', $fontBold, [System.Drawing.Brushes]::Black, [float]$lx, [float]($ly - 28))

$legendItems = @(
    @{K='S';T='Small room <= 25 sqm'},
    @{K='M';T='Medium room <= 80 sqm'},
    @{K='L';T='Large room > 80 sqm'},
    @{K='BRIDGE';T='Bridge / two-hand pinch'},
    @{K='STRUCT';T='Stair (down to F1)'}
)
$yy = $ly + 6
foreach ($li in $legendItems) {
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(190, $colors[$li.K]))
    $g.FillRectangle($brush, $lx, $yy, 24, 16); $g.DrawRectangle([System.Drawing.Pens]::Black, $lx, $yy, 24, 16)
    $g.DrawString($li.T, $fontSmall, [System.Drawing.Brushes]::Black, [float]($lx + 34), [float]($yy + 1)); $brush.Dispose(); $yy += 26
}
$yy += 4
$g.DrawString('Routes:', $fontBold, [System.Drawing.Brushes]::Black, [float]$lx, [float]$yy); $yy += 20
$g.DrawLine($critPen, $lx, $yy + 7, $lx + 28, $yy + 7); $g.DrawString('Critical objective backbone', $fontSmall, [System.Drawing.Brushes]::Black, [float]($lx + 36), [float]$yy); $yy += 22
$g.DrawLine($fixedPen, $lx, $yy + 7, $lx + 28, $yy + 7); $g.DrawString('Fixed (always open) branch', $fontSmall, [System.Drawing.Brushes]::Black, [float]($lx + 36), [float]$yy); $yy += 22
$g.DrawLine($togglePen, $lx, $yy + 7, $lx + 28, $yy + 7); $g.DrawString('Toggle loop / shortcut (T#)', $fontSmall, [System.Drawing.Brushes]::Black, [float]($lx + 36), [float]$yy); $yy += 22
$g.DrawLine($carryPen, $lx, $yy + 7, $lx + 28, $yy + 7); $g.DrawString('Two-hand carry escape preference', $fontSmall, [System.Drawing.Brushes]::Black, [float]($lx + 36), [float]$yy); $yy += 22
$g.FillEllipse([System.Drawing.Brushes]::White, $lx + 9, $yy + 2, 11.0, 11.0); $g.DrawEllipse($outline, $lx + 9, $yy + 2, 11.0, 11.0)
$g.DrawString('= narrow door (2m), else 2.8m corridor', $fontSmall, [System.Drawing.Brushes]::Black, [float]($lx + 36), [float]$yy); $yy += 26

$g.DrawString('Art layer - light colour:', $fontBold, [System.Drawing.Brushes]::Black, [float]$lx, [float]$yy); $yy += 20
$artItems = @(
    @{K='amber';T='Sodium amber - beacon / objective key'},
    @{K='teal';T='Civic teal - hub / circulation'},
    @{K='red';T='Stamp red - danger / debt seizure'},
    @{K='green';T='Dispatch green - safe / extract'},
    @{K='dark';T='Dead rubber - unlit back-of-house'},
    @{K='bridge';T='Scaffold bridge - exposed raw timber'}
)
foreach ($ai in $artItems) {
    $sw = [System.Drawing.SolidBrush]::new($pal[$ai.K])
    $g.FillEllipse($sw, $lx + 6, $yy + 2, 13.0, 13.0); $g.DrawEllipse([System.Drawing.Pens]::Black, $lx + 6, $yy + 2, 13.0, 13.0)
    $g.DrawString($ai.T, $fontSmall, [System.Drawing.Brushes]::Black, [float]($lx + 34), [float]$yy); $sw.Dispose(); $yy += 24
}
$yy += 6
$g.DrawString('Loops: T7/T14 shaft rings, T8/T13/T15 danger', $fontSmall, [System.Drawing.Brushes]::DimGray, [float]$lx, [float]$yy); $yy += 16
$g.DrawString('rings, T9/T16 navigation rings, T17 drop option.', $fontSmall, [System.Drawing.Brushes]::DimGray, [float]$lx, [float]$yy); $yy += 22
$g.DrawString('Identity beats: dead sales theatre,', $fontSmall, [System.Drawing.Brushes]::DimGray, [float]$lx, [float]$yy); $yy += 16
$g.DrawString('seized sand-table objective, debt seals.', $fontSmall, [System.Drawing.Brushes]::DimGray, [float]$lx, [float]$yy); $yy += 22
$g.DrawString('Rooms: L=2, M=6, S=6, Bridge=1, Stairs=2. Grid 4m.', $fontSmall, [System.Drawing.Brushes]::DimGray, [float]$lx, [float]$yy)

$greenBrush.Dispose()
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Host "Wrote $out size=${imgW}x${imgH}"
