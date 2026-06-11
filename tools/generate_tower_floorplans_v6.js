// F1/F2 floor-plan proposal renderer (SVG, zero deps).
// Sources: TowerTopologyV3.cs (graph+CN names), TowerV3WhiteboxBuilder node table (coords),
// design/levels/abandoned-tower-earth-coast-01.md, art bible section 6.
const fs = require('fs'), path = require('path');
const out = path.join(__dirname, '..', 'Assets/_Project/Art/Maps/Tower_EarthCoast_01/References');
fs.mkdirSync(out, { recursive: true });
const N = [ // id,floor,x,z,w,d,kind,size,cn
['VAN',1,14,-8,12,8,'Van','-','委托车/前院'],['LOBBY',1,12,0,12,8,'Room','L','大堂·售楼处'],
['WAREHOUSE',1,0,-8,12,8,'Room','L','西仓库'],['POWER',1,0,10,4,4,'Room','S','配电房 ⚡P-01'],
['TEMP',1,4,14,4,4,'Room','S','临时办公(线索)'],['SECUR',1,8,10,4,4,'Room','S','保安室'],
['SAMPLE',1,12,10,4,4,'Room','S','样品间'],['HALL',1,12,16,12,8,'Room','L','中央施工厅'],
['WORKSHOP',1,24,8,8,8,'Room','M','工坊'],['DOCK',1,38,16,8,8,'Room','M','装卸坞·跳降着陆'],
['DORM',1,12,24,8,8,'Room','M','宿舍'],['CANTEEN',1,12,32,8,8,'Room','M','食堂'],
['FOREMAN',1,22,36,8,8,'Room','M','工头办公'],['REBAR',1,34,8,8,8,'Room','M','钢筋堆场'],
['PUMP',1,26,0,4,4,'Room','S','水泵机电'],['SHANTY',1,34,24,4,4,'Room','S','民工棚'],
['FIRE',1,30,40,8,8,'Fire','-','消防出口'],['COLLAPSE',1,0,24,12,16,'Collapse','-','塌角(露天)'],
['STAIRA1',1,26,28,4,8,'Stair','-','A梯(快/暴露)'],['STAIRB1',1,0,16,4,8,'Stair','-','B梯(暗/稳)'],
['TARGET',2,48,4,12,8,'Room','L','沙盘·目标'],['SHOWFLAT',2,4,16,8,8,'Room','M','样板间·暖光'],
['EXEC',2,32,28,8,8,'Room','M','行政套间'],['MODEL',2,16,16,8,8,'Room','M','模型展厅'],
['SALES',2,32,16,8,8,'Room','M','销售办公'],['VIP',2,44,16,8,8,'Room','M','VIP休息室'],
['EDGE',2,16,4,12,8,'Room','L','竖井边缘(坠落)'],['BRIDGE',2,28,8,4,4,'Junction','-','脚手桥(收束)'],
['DANGER',2,44,28,4,4,'Room','S','危险间'],['MARKET',2,0,8,4,4,'Room','S','营销储物'],
['BALCONY',2,60,12,4,4,'Room','S','阳台·跳降口'],['NEGOT',2,32,40,8,8,'Room','M','洽谈区'],
['FIN',2,8,0,4,4,'Room','S','财务室'],['ARCHIVE',2,8,8,4,4,'Room','S','档案室'],
['TANK',2,0,0,4,4,'Room','S','水箱机房'],['STAIRA2',2,26,28,4,8,'Stair','-','A梯'],
['STAIRB2',2,0,16,4,8,'Stair','-','B梯+欠款卷帘']];
const E = [
['E-VAN','VAN','LOBBY','C','R'],['E-LH','LOBBY','HALL','C','R'],['E-H-SA','HALL','STAIRA1','C','R'],
['E-LPWR','LOBBY','POWER','C','R'],['E-PWR-SB','POWER','STAIRB1','C','R'],['E-PWR-TEMP','POWER','TEMP','C','D'],
['E-FIRE','FOREMAN','FIRE','C','D'],['E-LSAMP','LOBBY','SAMPLE','F','D'],['E-SAMP-H','SAMPLE','HALL','F','R'],
['E-HW','HALL','WORKSHOP','F','R'],['E-WD','WORKSHOP','DOCK','F','D'],['E-LW','LOBBY','WAREHOUSE','F','R'],
['E-HN','HALL','DORM','F','R'],['E-N-FORE','DORM','FOREMAN','F','R'],['E-FORE-SA','FOREMAN','STAIRA1','F','R'],
['E-SECUR-TEMP','SECUR','TEMP','F','D'],['E-CANTEEN-FORE','CANTEEN','FOREMAN','F','R'],
['E-WS-REBAR','WORKSHOP','REBAR','F','R'],['E-LOBBY-PUMP','LOBBY','PUMP','F','D'],['E-DOCK-SHANTY','DOCK','SHANTY','F','D'],
['T1','SECUR','SAMPLE','T','D'],['T2','SAMPLE','WORKSHOP','T','R'],['T3','DORM','CANTEEN','T','R'],
['T4','WAREHOUSE','POWER','T','D'],['T5','COLLAPSE','STAIRB1','T','R'],['T6','COLLAPSE','FOREMAN','T','R'],
['T10','REBAR','DOCK','T','D'],['T11','PUMP','HALL','T','D'],['T12','SHANTY','FOREMAN','T','R'],
['E-SF-SB','SHOWFLAT','STAIRB2','C','R'],['E-SF-MODEL','SHOWFLAT','MODEL','C','R'],['E-MODEL-EDGE','MODEL','EDGE','C','R'],
['E-EDGE-BRIDGE','EDGE','BRIDGE','C','R'],['E-BRIDGE-SALES','BRIDGE','SALES','C','R'],['E-SALES-VIP','SALES','VIP','C','R'],
['E-VIP-TARGET','VIP','TARGET','C','R'],['E-SA-EXEC','STAIRA2','EXEC','C','R'],['E-EXEC-SALES','EXEC','SALES','C','R'],
['E-SF-MARKET','SHOWFLAT','MARKET','F','D'],['E-MARKET-TANK','MARKET','TANK','F','D'],['E-MARKET-ARCH','MARKET','ARCHIVE','F','D'],
['E-ARCH-FIN','ARCHIVE','FIN','F','D'],['E-ARCH-EDGE','ARCHIVE','EDGE','F','R'],['E-EXEC-NEGOT','EXEC','NEGOT','F','R'],
['E-VIP-DANGER','VIP','DANGER','F','D'],['E-TARGET-BALC','TARGET','BALCONY','F','D'],
['T7','EDGE','SALES','T','D'],['T8','BRIDGE','DANGER','T','D'],['T9','MODEL','EXEC','T','R'],
['T13','SALES','DANGER','T','D'],['T14','SHOWFLAT','EDGE','T','R'],['T15','DANGER','TARGET','T','D'],
['T16','ARCHIVE','MODEL','T','D'],['T17','VIP','BALCONY','T','D']];
const byId = Object.fromEntries(N.map(n => [n[0], n]));
const S = 16, PAD = 70, MINX = -12, MAXX = 66, MINZ = -14, MAXZ = 50;
const W = (MAXX - MINX) * S + 2 * PAD, H = (MAXZ - MINZ) * S + 2 * PAD + 110;
const mx = x => PAD + (x - MINX) * S;
const mz = z => H - 110 - PAD - (z - MINZ) * S;
const FILL = { S: '#e1e6ee', M: '#d6e2d6', L: '#ebe0cd', '-': '#e4e4e4' };
function render(floor, title, file) {
  let s2 = ''; let s = '<svg xmlns="http://www.w3.org/2000/svg" width="' + W + '" height="' + H + '" font-family="Microsoft YaHei,sans-serif">';
  s += '<rect width="100%" height="100%" fill="#f4f2eb"/>';
  for (let x = Math.ceil(MINX / 4) * 4; x <= MAXX; x += 4) s += '<line x1="' + mx(x) + '" y1="' + mz(MINZ) + '" x2="' + mx(x) + '" y2="' + mz(MAXZ) + '" stroke="#5a5a5a" stroke-opacity=".12"/>';
  for (let z = Math.ceil(MINZ / 4) * 4; z <= MAXZ; z += 4) s += '<line x1="' + mx(MINX) + '" y1="' + mz(z) + '" x2="' + mx(MAXX) + '" y2="' + mz(z) + '" stroke="#5a5a5a" stroke-opacity=".12"/>';
  const fn = N.filter(n => n[1] === floor);
  for (const n of fn) {
    const fill = n[6] === 'Stair' ? '#cdcdd7' : FILL[n[7]];
    s += '<rect x="' + mx(n[2]) + '" y="' + mz(n[3] + n[5]) + '" width="' + n[4] * S + '" height="' + n[5] * S + '" fill="' + fill + '"/>';
  }
  if (floor === 2) {
    s += '<rect x="' + mx(18) + '" y="' + mz(10) + '" width="' + 8 * S + '" height="' + 4 * S + '" fill="#8c8c8c"/>';
    s += '<text x="' + (mx(18) + 6) + '" y="' + (mz(10) + 18) + '" fill="#fff" font-size="11">竖井(坠落)</text>';
  }
  const gaps = [];   // door/corridor openings punched over walls later
  const marks = [];  // toggle labels + red centerlines, drawn on top
  for (const e of E) {
    const a = byId[e[1]], b = byId[e[2]];
    if (a[1] !== floor || b[1] !== floor) continue;
    const xLo = Math.max(a[2], b[2]), xHi = Math.min(a[2] + a[4], b[2] + b[4]);
    const zLo = Math.max(a[3], b[3]), zHi = Math.min(a[3] + a[5], b[3] + b[5]);
    const tog = e[3] === 'T', crit = e[3] === 'C';
    const doorW = e[4] === 'D' ? 2.0 : 2.8, half = doorW / 2;
    const bandFill = tog ? 'fill="#cfc9b8" fill-opacity=".75"' : 'fill="#cfc9b8"';
    const bandStroke = tog ? 'stroke="#5a6e5a" stroke-width="1.5" stroke-dasharray="6,5"' : 'stroke="#2d2d2d" stroke-width="1.5"';
    function band(x0, z0, x1, z1) {
      if (x1 - x0 < 0.05 || z1 - z0 < 0.05) return;
      s2 += '<rect x="' + mx(x0) + '" y="' + mz(z1) + '" width="' + (x1 - x0) * S + '" height="' + (z1 - z0) * S + '" ' + bandFill + ' ' + bandStroke + '/>';
    }
    function gap(cx, cz, vertWall) { // white opening punched across a wall
      const gw = vertWall ? 0.8 : doorW, gd = vertWall ? doorW : 0.8;
      gaps.push('<rect x="' + mx(cx - gw / 2) + '" y="' + mz(cz + gd / 2) + '" width="' + gw * S + '" height="' + gd * S + '" fill="' + (tog ? '#e8c87a' : '#f4f2eb') + '"' + (tog ? ' stroke="#5a6e5a" stroke-dasharray="4,3"' : '') + (crit ? ' stroke="#b42e22" stroke-width="2"' : '') + '/>');
    }
    const acx = a[2] + a[4] / 2, acz = a[3] + a[5] / 2, bcx = b[2] + b[4] / 2, bcz = b[3] + b[5] / 2;
    let lbl;
    if (xHi - xLo >= 2.8) {            // shared vertical lane
      const m = (xLo + xHi) / 2;
      const lo = Math.min(a[3] + a[5], b[3] + b[5]), hi = Math.max(a[3], b[3]); // gap between rooms
      band(m - half, lo, m + half, hi);
      const aEdge = acz < bcz ? a[3] + a[5] : a[3], bEdge = acz < bcz ? b[3] : b[3] + b[5];
      gap(m, aEdge, false); gap(m, bEdge, false);
      if (crit) marks.push('<line x1="' + mx(m) + '" y1="' + mz(acz) + '" x2="' + mx(m) + '" y2="' + mz(bcz) + '" stroke="#b42e22" stroke-width="3"/>');
      lbl = [mx(m) + 6, mz((aEdge + bEdge) / 2)];
    } else if (zHi - zLo >= 2.8) {     // shared horizontal lane
      const m = (zLo + zHi) / 2;
      const lo = Math.min(a[2] + a[4], b[2] + b[4]), hi = Math.max(a[2], b[2]);
      band(lo, m - half, hi, m + half);
      const aEdge = acx < bcx ? a[2] + a[4] : a[2], bEdge = acx < bcx ? b[2] : b[2] + b[4];
      gap(aEdge, m, true); gap(bEdge, m, true);
      if (crit) marks.push('<line x1="' + mx(acx) + '" y1="' + mz(m) + '" x2="' + mx(bcx) + '" y2="' + mz(m) + '" stroke="#b42e22" stroke-width="3"/>');
      lbl = [mx((aEdge + bEdge) / 2), mz(m) - 8];
    } else {                            // one clean L
      const aEdgeZ = bcz > acz ? a[3] + a[5] : a[3];
      const bEdgeX = acx < bcx ? b[2] : b[2] + b[4];
      band(acx - half, Math.min(aEdgeZ, bcz - half), acx + half, Math.max(aEdgeZ, bcz + half));
      band(Math.min(acx - half, bEdgeX), bcz - half, Math.max(acx + half, bEdgeX), bcz + half);
      gap(acx, aEdgeZ, false); gap(bEdgeX, bcz, true);
      if (crit) marks.push('<polyline points="' + mx(acx) + ',' + mz(acz) + ' ' + mx(acx) + ',' + mz(bcz) + ' ' + mx(bcx) + ',' + mz(bcz) + '" fill="none" stroke="#b42e22" stroke-width="3"/>');
      lbl = [mx(acx) + 6, mz(bcz) - 8];
    }
    if (tog) marks.push('<text x="' + lbl[0] + '" y="' + lbl[1] + '" font-size="10" fill="#3c3c3c" fill-opacity=".8" font-family="Consolas">' + e[0] + '</text>');
  }
  let s2tmp = ''; // (bands already appended into s2 via closure)
  s += s2;
  for (const n of fn) {
    const x = mx(n[2]), y = mz(n[3] + n[5]), w = n[4] * S, h = n[5] * S;
    const stroke = n[6] === 'Collapse' ? 'stroke="#785a3c" stroke-dasharray="7,5"' : 'stroke="#2d2d2d"';
    s += '<rect x="' + x + '" y="' + y + '" width="' + w + '" height="' + h + '" fill="none" ' + stroke + ' stroke-width="2.5"/>';
    s += '<text x="' + (x + 4) + '" y="' + (y + 15) + '" font-size="12.5" font-weight="bold">' + n[8] + '</text>';
    s += '<text x="' + (x + 4) + '" y="' + (y + 29) + '" font-size="10" fill="#3c3c3c" fill-opacity=".7" font-family="Consolas">' + n[0] + (n[7] !== '-' ? '  [' + n[7] + ']' : '') + '</text>';
    if (n[6] === 'Stair') for (let i = 1; i <= 6; i++) s += '<line x1="' + (x + 4) + '" y1="' + (y + h * i / 7) + '" x2="' + (x + w - 4) + '" y2="' + (y + h * i / 7) + '" stroke="#3c3c3c" stroke-width="1.5"/>';
  }
  for (const g2 of gaps) s += g2;
  for (const m2 of marks) s += m2;
  if (floor === 2) s += '<text x="' + mx(49) + '" y="' + mz(11.5) + '" font-size="12">E-DROP 单向跳降 → F1装卸坞</text>';
  if (floor === 1) s += '<text x="' + mx(37.5) + '" y="' + mz(26.5) + '" font-size="12">自F2阳台跳降着陆</text>';
  s += '<text x="24" y="' + (H - 78) + '" font-size="24" font-weight="bold">' + title + '</text>';
  const ly = H - 48;
  let lx = 24;
  const leg = [['#e1e6ee', 'S 4×4'], ['#d6e2d6', 'M 8×8'], ['#ebe0cd', 'L 12×8'], ['#cdcdd7', '楼梯4×8(锚定)']];
  for (const [c, t] of leg) {
    s += '<rect x="' + lx + '" y="' + (ly - 12) + '" width="18" height="14" fill="' + c + '" stroke="#888"/><text x="' + (lx + 24) + '" y="' + ly + '" font-size="12">' + t + '</text>';
    lx += 26 + t.length * 12 + 22;
  }
  s += '<line x1="' + lx + '" y1="' + (ly - 5) + '" x2="' + (lx + 38) + '" y2="' + (ly - 5) + '" stroke="#b42e22" stroke-width="4"/><text x="' + (lx + 44) + '" y="' + ly + '" font-size="12">关键路径(固定)</text>';
  lx += 160;
  s += '<line x1="' + (lx + 40) + '" y1="' + (ly - 5) + '" x2="' + (lx + 78) + '" y2="' + (ly - 5) + '" stroke="#3c3c3c" stroke-width="2.5"/><text x="' + (lx + 84) + '" y="' + ly + '" font-size="12">固定通道/门</text>';
  s += '<line x1="' + (lx + 210) + '" y1="' + (ly - 5) + '" x2="' + (lx + 248) + '" y2="' + (ly - 5) + '" stroke="#5a6e5a" stroke-width="2" stroke-dasharray="6,5"/><text x="' + (lx + 254) + '" y="' + ly + '" font-size="12">随机开关 T#</text>';
  s += '<text x="24" y="' + (H - 14) + '" font-size="11" fill="#555">设计依据: level GDD (P-01电力门3秒长按 · 沙盘双手搬运0.55× · 欠款卷帘) + Art Bible §6 + 三房型约束 | 走廊规则: 共享投影带=直线, 其余至多一个直角弯, 全图禁止≥3段折叠</text>';
  s += '</svg>';
  fs.writeFileSync(path.join(out, file), s);
  console.log('WROTE', file);
}
render(1, '地球海岸壹号·烂尾预售楼 — F1 平面图 v6（提案·待PM批准）', 'Tower_EarthCoast_01_F1_Plan_v6_proposal.svg');
render(2, '地球海岸壹号·烂尾预售楼 — F2 平面图 v6（提案·待PM批准）', 'Tower_EarthCoast_01_F2_Plan_v6_proposal.svg');
