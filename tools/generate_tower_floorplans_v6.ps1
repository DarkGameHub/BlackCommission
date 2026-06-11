# Tower EarthCoast 01 — F1/F2 floor-plan proposal renderer (v6)
# Sources: TowerTopologyV3.cs (graph), TowerV3WhiteboxBuilder node table (coords),
# design/levels/abandoned-tower-earth-coast-01.md (P-01 power gate, sandtable objective),
# art bible §6 (warm beacon, exit lighting). Output: References/*_Plan_v6_proposal.png
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$outDir = Join-Path $PSScriptRoot '..\Assets\_Project\Art\Maps\Tower_EarthCoast_01\References'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# id, floor, x, z, w, d, kind(Room/Junction/Stair/Van/Fire/Collapse), size(S/M/L/-), cn
$nodes = @(
  @('VAN',1,14,-8,12,8,'Van','-','委托车/前院'),
  @('LOBBY',1,12,0,12,8,'Room','L','大堂·售楼处'),
  @('WAREHOUSE',1,0,-8,12,8,'Room','L','西仓库'),
  @('POWER',1,0,10,4,4,'Room','S','配电房 ⚡P-01'),
  @('TEMP',1,4,14,4,4,'Room','S','临时办公(线索)'),
  @('SECUR',1,8,10,4,4,'Room','S','保安室'),
  @('SAMPLE',1,12,10,4,4,'Room','S','样品间'),
  @('HALL',1,12,16,12,8,'Room','L','中央施工厅'),
  @('WORKSHOP',1,24,8,8,8,'Room','M','工坊'),
  @('DOCK',1,38,16,8,8,'Room','M','装卸坞⤵着陆'),
  @('DORM',1,12,24,8,8,'Room','M','宿舍'),
  @('CANTEEN',1,12,32,8,8,'Room','M','食堂'),
  @('FOREMAN',1,22,36,8,8,'Room','M','工头办公'),
  @('REBAR',1,34,8,8,8,'Room','M','钢筋堆场'),
  @('PUMP',1,26,0,4,4,'Room','S','水泵机电'),
  @('SHANTY',1,34,24,4,4,'Room','S','民工棚'),
  @('FIRE',1,30,40,8,8,'Fire','-','消防出口'),
  @('COLLAPSE',1,0,24,12,16,'Collapse','-','塌角(露天)'),
  @('STAIRA1',1,26,28,4,8,'Stair','-','A梯(快/暴露)'),
  @('STAIRB1',1,0,16,4,8,'Stair','-','B梯(暗/稳)'),
  @('TARGET',2,48,4,12,8,'Room','L','沙盘·目标🎯'),
  @('SHOWFLAT',2,4,16,8,8,'Room','M','样板间☀暖光'),
  @('EXEC',2,32,28,8,8,'Room','M','行政套间'),
  @('MODEL',2,16,16,8,8,'Room','M','模型展厅'),
  @('SALES',2,32,16,8,8,'Room','M','销售办公'),
  @('VIP',2,44,16,8,8,'Room','M','VIP休息室'),
  @('EDGE',2,16,4,12,8,'Room','L','竖井边缘(坠落)'),
  @('BRIDGE',2,28,8,4,4,'Junction','-','脚手桥(收束)'),
  @('DANGER',2,44,28,4,4,'Room','S','危险间'),
  @('MARKET',2,0,8,4,4,'Room','S','营销储物'),
  @('BALCONY',2,60,12,4,4,'Room','S','阳台⤵跳降'),
  @('NEGOT',2,32,40,8,8,'Room','M','洽谈区'),
  @('FIN',2,8,0,4,4,'Room','S','财务室'),
  @('ARCHIVE',2,8,8,4,4,'Room','S','档案室'),
  @('TANK',2,0,0,4,4,'Room','S','水箱机房'),
  @('STAIRA2',2,26,28,4,8,'Stair','-','A梯'),
  @('STAIRB2',2,0,16,4,8,'Stair','-','B梯+欠款卷帘🔒')
)
# id, a, b, type(C=critical,F=fixed,T=toggle)
$edges = @(
  @('E-VAN','VAN','LOBBY','C'),@('E-LH','LOBBY','HALL','C'),@('E-H-SA','HALL','STAIRA1','C'),
  @('E-LPWR','LOBBY','POWER','C'),@('E-PWR-SB','POWER','STAIRB1','C'),@('E-PWR-TEMP','POWER','TEMP','C'),
  @('E-FIRE','FOREMAN','FIRE','C'),
  @('E-LSAMP','LOBBY','SAMPLE','F'),@('E-SAMP-H','SAMPLE','HALL','F'),@('E-HW','HALL','WORKSHOP','F'),
  @('E-WD','WORKSHOP','DOCK','F'),@('E-LW','LOBBY','WAREHOUSE','F'),@('E-HN','HALL','DORM','F'),
  @('E-N-FORE','DORM','FOREMAN','F'),@('E-FORE-SA','FOREMAN','STAIRA1','F'),
  @('E-SECUR-TEMP','SECUR','TEMP','F'),@('E-CANTEEN-FORE','CANTEEN','FOREMAN','F'),
  @('E-WS-REBAR','WORKSHOP','REBAR','F'),@('E-LOBBY-PUMP','LOBBY','PUMP','F'),@('E-DOCK-SHANTY','DOCK','SHANTY','F'),
  @('T1','SECUR','SAMPLE','T'),@('T2','SAMPLE','WORKSHOP','T'),@('T3','DORM','CANTEEN','T'),
  @('T4','WAREHOUSE','POWER','T'),@('T5','COLLAPSE','STAIRB1','T'),@('T6','COLLAPSE','FOREMAN','T'),
  @('T10','REBAR','DOCK','T'),@('T11','PUMP','HALL','T'),@('T12','SHANTY','FOREMAN','T'),
  @('E-SF-SB','SHOWFLAT','STAIRB2','C'),@('E-SF-MODEL','SHOWFLAT','MODEL','C'),@('E-MODEL-EDGE','MODEL','EDGE','C'),
  @('E-EDGE-BRIDGE','EDGE','BRIDGE','C'),@('E-BRIDGE-SALES','BRIDGE','SALES','C'),@('E-SALES-VIP','SALES','VIP','C'),
  @('E-VIP-TARGET','VIP','TARGET','C'),@('E-SA-EXEC','STAIRA2','EXEC','C'),@('E-EXEC-SALES','EXEC','SALES','C'),
  @('E-SF-MARKET','SHOWFLAT','MARKET','F'),@('E-MARKET-TANK','MARKET','TANK','F'),@('E-MARKET-ARCH','MARKET','ARCHIVE','F'),
  @('E-ARCH-FIN','ARCHIVE','FIN','F'),@('E-ARCH-EDGE','ARCHIVE','EDGE','F'),@('E-EXEC-NEGOT','EXEC','NEGOT','F'),
  @('E-VIP-DANGER','VIP','DANGER','F'),@('E-TARGET-BALC','TARGET','BALCONY','F'),
  @('T7','EDGE','SALES','T'),@('T8','BRIDGE','DANGER','T'),@('T9','MODEL','EXEC','T'),
  @('T13','SALES','DANGER','T'),@('T14','SHOWFLAT','EDGE','T'),@('T15','DANGER','TARGET','T'),
  @('T16','ARCHIVE','MODEL','T'),@('T17','VIP','BALCONY','T')
)

$byId = @{}; foreach ($n in $nodes) { $byId[$n[0]] = $n }

function Render-Floor([int]$floor, [string]$title, [string]$outFile) {
  $S = 16.0           # px per metre
  $pad = 70.0
  $minX = -12.0; $maxX = 66.0; $minZ = -14.0; $maxZ = 50.0
  $W = [int](($maxX - $minX) * $S + 2 * $pad); $H = [int](($maxZ - $minZ) * $S + 2 * $pad + 120)
  $bmp = New-Object System.Drawing.Bitmap($W, $H)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'
  $g.Clear([System.Drawing.Color]::FromArgb(244, 242, 235))   # aged paper

  function MX([double]$x) { return $pad + ($x - $minX) * $S }
  function MZ([double]$z) { return $H - 120 - $pad - ($z - $minZ) * $S }   # z up = north

  $gridPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(28,90,90,90),1)
  for ($x = [math]::Ceiling($minX/4)*4; $x -le $maxX; $x += 4) { $g.DrawLine($gridPen,(MX $x),(MZ $minZ),(MX $x),(MZ $maxZ)) }
  for ($z = [math]::Ceiling($minZ/4)*4; $z -le $maxZ; $z += 4) { $g.DrawLine($gridPen,(MX $minX),(MZ $z),(MX $maxX),(MZ $z)) }

  $fontT  = New-Object System.Drawing.Font('Microsoft YaHei',20,[System.Drawing.FontStyle]::Bold)
  $fontN  = New-Object System.Drawing.Font('Microsoft YaHei',9.5,[System.Drawing.FontStyle]::Bold)
  $fontS  = New-Object System.Drawing.Font('Microsoft YaHei',8)
  $fontE  = New-Object System.Drawing.Font('Consolas',7.5)
  $black  = [System.Drawing.Brushes]::Black
  $dim    = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(120,60,60,60))

  # edge lines first (under rooms' borders but we draw rooms semi-late; draw edges after fills)
  $fillS = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,225,230,238))
  $fillM = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,214,226,214))
  $fillL = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,235,224,205))
  $fillX = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,228,228,228))
  $fillStair = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,205,205,215))
  $fillVoid  = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255,140,140,140))
  $wallPen   = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255,45,45,45),2.5)
  $rubblePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255,120,90,60),2.5); $rubblePen.DashStyle='Dash'

  $floorNodes = $nodes | Where-Object { $_[1] -eq $floor }
  # room fills
  foreach ($n in $floorNodes) {
    $rx = MX $n[2]; $rz = MZ ($n[3] + $n[5]); $rw = $n[4]*$S; $rd = $n[5]*$S
    $fill = switch ($n[7]) { 'S' {$fillS} 'M' {$fillM} 'L' {$fillL} default {$fillX} }
    if ($n[6] -eq 'Stair') { $fill = $fillStair }
    $g.FillRectangle($fill, [single]$rx, [single]$rz, [single]$rw, [single]$rd)
  }
  # EDGE inner shaft void (F2)
  if ($floor -eq 2) {
    $g.FillRectangle($fillVoid, [single](MX 18), [single](MZ 10), [single](8*$S), [single](4*$S))
    $g.DrawString('竖井(坠落)', $fontS, [System.Drawing.Brushes]::White, [single]((MX 18)+6), [single]((MZ 10)+8))
  }

  # connection lines: port-aligned straight when spans overlap, else L elbow
  $penC = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255,180,46,34),4)
  $penF = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255,60,60,60),2.5)
  $penT = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255,90,110,90),2); $penT.DashStyle='Dash'
  foreach ($e in $edges) {
    $a = $byId[$e[1]]; $b = $byId[$e[2]]
    if ($a[1] -ne $floor -or $b[1] -ne $floor) { continue }
    $xLo=[math]::Max($a[2],$b[2]); $xHi=[math]::Min($a[2]+$a[4],$b[2]+$b[4])
    $zLo=[math]::Max($a[3],$b[3]); $zHi=[math]::Min($a[3]+$a[5],$b[3]+$b[5])
    $pen = switch ($e[3]) { 'C' {$penC} 'F' {$penF} default {$penT} }
    $acx=$a[2]+$a[4]/2.0; $acz=$a[3]+$a[5]/2.0; $bcx=$b[2]+$b[4]/2.0; $bcz=$b[3]+$b[5]/2.0
    if (($xHi-$xLo) -ge 2.8) {        # vertical straight lane
      $mx = ($xLo+$xHi)/2.0
      $g.DrawLine($pen, (MX $mx), (MZ $acz), (MX $mx), (MZ $bcz))
      $lx = (MX $mx)+3; $lz = (MZ (($acz+$bcz)/2.0))
    } elseif (($zHi-$zLo) -ge 2.8) {  # horizontal straight lane
      $mz = ($zLo+$zHi)/2.0
      $g.DrawLine($pen, (MX $acx), (MZ $mz), (MX $bcx), (MZ $mz))
      $lx = (MX (($acx+$bcx)/2.0)); $lz = (MZ $mz)-14
    } else {                           # one clean L elbow
      $g.DrawLine($pen, (MX $acx), (MZ $acz), (MX $acx), (MZ $bcz))
      $g.DrawLine($pen, (MX $acx), (MZ $bcz), (MX $bcx), (MZ $bcz))
      $lx = (MX $acx)+3; $lz = (MZ $bcz)-14
    }
    if ($e[3] -eq 'T') { $g.DrawString($e[0], $fontE, $dim, [single]$lx, [single]$lz) }
  }

  # room borders + labels on top
  foreach ($n in $floorNodes) {
    $rx = MX $n[2]; $rz = MZ ($n[3] + $n[5]); $rw = $n[4]*$S; $rd = $n[5]*$S
    $pen = if ($n[6] -eq 'Collapse') { $rubblePen } else { $wallPen }
    $g.DrawRectangle($pen, [single]$rx, [single]$rz, [single]$rw, [single]$rd)
    $label = $n[8]; $sz = if ($n[7] -ne '-') { "$($n[0])  [$($n[7])]" } else { $n[0] }
    $g.DrawString($label, $fontN, $black, [single]($rx+3), [single]($rz+2))
    $g.DrawString($sz, $fontE, $dim, [single]($rx+3), [single]($rz+18))
    if ($n[6] -eq 'Stair') {
      for ($i=1; $i -le 6; $i++) {
        $yy = $rz + $rd*$i/7.0
        $g.DrawLine($penF, [single]($rx+4), [single]$yy, [single]($rx+$rw-4), [single]$yy)
      }
    }
  }

  # scaffold drop marker
  if ($floor -eq 2) { $g.DrawString('⤵ E-DROP 单向跳降 → F1装卸坞', $fontS, $black, [single](MX 52), [single](MZ 11)) }
  if ($floor -eq 1) { $g.DrawString('⤵ 自F2阳台跳降着陆点', $fontS, $black, [single](MX 38), [single](MZ 27)) }

  # title + legend strip
  $g.DrawString($title, $fontT, $black, 24, [single]($H-112))
  $ly = $H - 74
  $g.FillRectangle($fillS, 24, $ly, 18, 14);  $g.DrawString('S 4×4', $fontS, $black, 46, [single]($ly-1))
  $g.FillRectangle($fillM, 110, $ly, 18, 14); $g.DrawString('M 8×8', $fontS, $black, 132, [single]($ly-1))
  $g.FillRectangle($fillL, 200, $ly, 18, 14); $g.DrawString('L 12×8', $fontS, $black, 222, [single]($ly-1))
  $g.FillRectangle($fillStair, 296, $ly, 18, 14); $g.DrawString('楼梯 4×8(锚定)', $fontS, $black, 318, [single]($ly-1))
  $g.DrawLine($penC, 430, [single]($ly+7), 470, [single]($ly+7)); $g.DrawString('关键路径(固定)', $fontS, $black, 476, [single]($ly-1))
  $g.DrawLine($penF, 580, [single]($ly+7), 620, [single]($ly+7)); $g.DrawString('固定通道/门', $fontS, $black, 626, [single]($ly-1))
  $g.DrawLine($penT, 720, [single]($ly+7), 760, [single]($ly+7)); $g.DrawString('随机开关 T#', $fontS, $black, 766, [single]($ly-1))
  $g.DrawString('设计依据: level GDD(P-01电力门·沙盘双手搬运·欠款卷帘) + art bible §6 + 三房型约束 | 走廊规则: 直线优先,全图禁止≥3段折叠', $fontS, $dim, 24, [single]($ly+22))

  $g.Dispose()
  $bmp.Save($outFile, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
  Write-Host "WROTE $outFile"
}

Render-Floor 1 '地球海岸壹号·烂尾预售楼 — F1 平面图 v6 (提案: 待PM批准)' (Join-Path $outDir 'Tower_EarthCoast_01_F1_Plan_v6_proposal.png')
Render-Floor 2 '地球海岸壹号·烂尾预售楼 — F2 平面图 v6 (提案: 待PM批准)' (Join-Path $outDir 'Tower_EarthCoast_01_F2_Plan_v6_proposal.png')
