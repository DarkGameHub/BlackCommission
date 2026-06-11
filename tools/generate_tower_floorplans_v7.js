// F1/F2 floor-plan proposal v7 (SVG, zero deps) — SLAB-PARTITION MODEL.
// Every 4m cell belongs to exactly one slab: Room / Corr / Stair / Void / Open / Poche.
// Corridors are FIRST-CLASS slabs with explicit coordinates. Doors are holes punched
// in shared wall faces — no routing exists. Built-in validator: overlap, outline,
// shared-face width per door. Decisions (PM 2026-06-10): objective = 生态柱 (replaces
// 沙盘 as objective; 沙盘 stays as set dressing in SALES + stage-4 hook), F2 = show-flat
// island + open raw plate, atrium over F1 HALL, scaffold bridge = only crossing.
const fs = require('fs'), path = require('path');
const out = path.join(__dirname, '..', 'Assets/_Project/Art/Maps/Tower_EarthCoast_01/References');
fs.mkdirSync(out, { recursive: true });

// Building slab outline (both floors): x 0..44, z 0..40. Van forecourt outside south.
const OX = 0, OZ = 0, OW = 44, OD = 40;

// ---- slabs: [id, floor, x, z, w, d, kind, size, label]
// kind: Room | Corr | Stair | Void(collapse/atrium) | Bridge | Open(raw plate) | Van
const SLABS = [
  // ---------- F1 ----------
  ['VAN',     1, 14,-10, 12, 8, 'Van',  '-', '委托车/前院'],
  ['WAREHOUSE',1, 0,  0, 12, 8, 'Room', 'L', '西仓库'],
  ['LOBBY',   1, 12,  0, 12, 8, 'Room', 'L', '大堂·售楼处 ★'],
  ['PUMP',    1, 24,  0,  4, 4, 'Room', 'S', '水泵机电'],
  ['SECUR',   1, 24,  4,  4, 4, 'Room', 'S', '保安室'],
  ['WORKSHOP',1, 28,  0,  8, 8, 'Room', 'M', '工坊'],
  ['C1',      1,  0,  8, 16, 4, 'Corr', '-', '南廊·西'],
  ['C2',      1, 16,  8,  4, 8, 'Corr', '-', '门厅廊'],
  ['C3',      1, 20,  8, 24, 4, 'Corr', '-', '南廊·东'],
  ['POWER',   1,  0, 12,  4, 4, 'Room', 'S', '配电房 ⚡P-01 ★'],
  ['C4',      1,  4, 12,  4,12, 'Corr', '-', '西廊'],
  ['SAMPLE',  1,  8, 12,  4, 4, 'Room', 'S', '样品间'],
  ['STAIRB1', 1,  0, 16,  4, 8, 'Stair','-', 'B梯(暗/稳) ★'],
  ['HALL',    1, 12, 16, 12, 8, 'Room', 'L', '中央施工厅 ★(中庭挑空)'],
  ['C5',      1, 24, 12,  4,12, 'Corr', '-', '东廊'],
  ['REBAR',   1, 28, 12,  8, 8, 'Room', 'M', '钢筋堆场'],
  ['DOCK',    1, 36, 12,  8, 8, 'Room', 'M', '装卸坞·跳降着陆'],
  ['SHANTY',  1, 40, 20,  4, 4, 'Room', 'S', '民工棚'],
  ['TEMP',    1,  4, 24,  4, 4, 'Room', 'S', '临时办公(线索)'],
  ['C6',      1, 24, 24,  6, 4, 'Corr', '-', 'A梯前厅'],
  ['DORM',    1, 12, 24,  8, 8, 'Room', 'M', '宿舍(证据)'],
  ['COLLAPSE',1,  0, 28, 12,12, 'Void', '-', '塌角·露天 ★(天光)'],
  ['STAIRA1', 1, 26, 28,  4, 8, 'Stair','-', 'A梯(快/暴露) ★'],
  ['CANTEEN', 1, 12, 32,  8, 8, 'Room', 'M', '食堂'],
  ['FOREMAN', 1, 30, 32,  8, 8, 'Room', 'M', '工头办公·消防口'],
  // ---------- F2 ----------
  ['PLATE',   2,  0,  0, 44,40, 'Open', '-', ''],            // raw open plate (background)
  ['ATRIUM',  2, 12, 16, 12, 8, 'Void', '-', '中庭挑空 ★(下方=施工厅)'],
  ['BRIDGE2', 2, 12, 18, 12, 4, 'Bridge','-','脚手桥 ★(唯一跨越)'],
  ['STAIRB2', 2,  0, 16,  4, 8, 'Stair','-', 'B梯+欠款卷帘①'],
  ['STAIRA2', 2, 26, 28,  4, 8, 'Stair','-', 'A梯+卷帘②'],
  ['C7',      2, 26, 24,  4, 4, 'Corr', '-', 'A梯前厅'],
  ['SALES',   2, 24, 16,  8, 8, 'Room', 'M', '销售办公(沙盘陈设)'],
  ['SHOWFLAT',2, 32, 16,  8, 8, 'Room', 'M', '样板间·精装 ★'],
  ['TARGET',  2, 32,  8, 12, 8, 'Room', 'L', '「真实海岸」生态柱展厅 ★'],
  ['VIP',     2, 32, 24,  4, 4, 'Room', 'S', 'VIP休息室'],
  ['BALCONY', 2, 40, 16,  4, 4, 'Room', 'S', '阳台·跳降口'],
];

// ---- doors: [id, a, b, type C/F/T, width m]  (junction J = full-length corridor merge)
const DOORS = [
  // F1 critical
  ['D-VAN','VAN','LOBBY','C',2.8], ['D1','LOBBY','C2','C',2.8],
  ['J1','C1','C2','J',0], ['J2','C2','C3','J',0], ['J3','C1','C4','J',0],
  ['D4','C4','POWER','C',2.0], ['D5','C4','STAIRB1','C',2.0],
  ['D7','C2','HALL','C',2.8], ['D8','HALL','C5','C',2.8],
  ['J4','C3','C5','J',0], ['J5','C5','C6','J',0], ['D10','C6','STAIRA1','C',2.0],
  // F1 fixed
  ['D6','C4','TEMP','F',2.0], ['D11','C1','WAREHOUSE','F',2.8], ['D12','C1','SAMPLE','F',2.0],
  ['D13','C3','SECUR','F',2.0], ['D14','C3','WORKSHOP','F',2.8], ['D15','C3','DOCK','F',2.8],
  ['D16','C3','REBAR','F',2.8], ['D17','LOBBY','PUMP','F',2.0], ['D18','HALL','DORM','F',2.0],
  ['D19','STAIRA1','FOREMAN','F',2.0],
  // F1 toggles (9)
  ['T1','LOBBY','SECUR','T',2.0], ['T2','C4','SAMPLE','T',2.0], ['T3','DORM','CANTEEN','T',2.0],
  ['T4','LOBBY','WAREHOUSE','T',2.0], ['T5','TEMP','COLLAPSE','T',2.0],
  ['T6','COLLAPSE','CANTEEN','T',2.0], ['T10','C5','REBAR','T',2.0],
  ['T11','PUMP','WORKSHOP','T',2.0], ['T12','DOCK','SHANTY','T',2.0],
  // F2 critical
  ['D30','STAIRB2','PLATE','C',2.8], ['D31','PLATE','BRIDGE2','C',2.8],
  ['D32','BRIDGE2','SALES','C',2.8], ['D33','SALES','SHOWFLAT','C',2.8],
  ['D34','SHOWFLAT','TARGET','C',2.8],
  ['D35','STAIRA2','C7','C',2.0], ['D36','C7','SALES','C',2.8],
  // F2 toggles (2)
  ['T7','SHOWFLAT','VIP','T',2.0], ['T17','TARGET','BALCONY','T',2.0],
];

const byId = Object.fromEntries(SLABS.map(s => [s[0], s]));

// ================= VALIDATION =================
let errors = 0;
function rectsOverlap(a, b) {
  const ix = Math.min(a[2]+a[4], b[2]+b[4]) - Math.max(a[2], b[2]);
  const iz = Math.min(a[3]+a[5], b[3]+b[5]) - Math.max(a[3], b[3]);
  return ix > 0.01 && iz > 0.01;
}
// 1) no overlap among same-floor slabs (PLATE is background; BRIDGE2 lives inside ATRIUM)
for (let i = 0; i < SLABS.length; i++) for (let j = i+1; j < SLABS.length; j++) {
  const a = SLABS[i], b = SLABS[j];
  if (a[1] !== b[1]) continue;
  if (a[0] === 'PLATE' || b[0] === 'PLATE') continue;
  if ((a[0] === 'ATRIUM' && b[0] === 'BRIDGE2') || (a[0] === 'BRIDGE2' && b[0] === 'ATRIUM')) continue;
  if (rectsOverlap(a, b)) { console.error('ERROR overlap:', a[0], b[0]); errors++; }
}
// 2) inside outline (except VAN)
for (const s of SLABS) {
  if (s[0] === 'VAN') continue;
  if (s[2] < OX || s[3] < OZ || s[2]+s[4] > OX+OW || s[3]+s[5] > OZ+OD) {
    console.error('ERROR outside outline:', s[0]); errors++;
  }
}
// 3) every door has a shared face wide enough (width + 0.9 corner clearance)
function sharedFace(a, b) { // returns {axis:'x'|'z', at, lo, hi} or null
  if (Math.abs((a[2]+a[4]) - b[2]) < 0.01 || Math.abs((b[2]+b[4]) - a[2]) < 0.01) {
    const at = Math.abs((a[2]+a[4]) - b[2]) < 0.01 ? a[2]+a[4] : b[2]+b[4];
    const lo = Math.max(a[3], b[3]), hi = Math.min(a[3]+a[5], b[3]+b[5]);
    if (hi - lo > 0.01) return { axis: 'x', at, lo, hi };
  }
  if (Math.abs((a[3]+a[5]) - b[3]) < 0.01 || Math.abs((b[3]+b[5]) - a[3]) < 0.01) {
    const at = Math.abs((a[3]+a[5]) - b[3]) < 0.01 ? a[3]+a[5] : b[3]+b[5];
    const lo = Math.max(a[2], b[2]), hi = Math.min(a[2]+a[4], b[2]+b[4]);
    if (hi - lo > 0.01) return { axis: 'z', at, lo, hi };
  }
  return null;
}
for (const d of DOORS) {
  const a = byId[d[1]], b = byId[d[2]];
  if (!a || !b) { console.error('ERROR unknown slab in', d[0]); errors++; continue; }
  if (['PLATE','VAN'].includes(d[1]) || ['PLATE','VAN'].includes(d[2])) continue; // plate/forecourt: perimeter entrance, not a shared wall
  const f = sharedFace(a, b);
  if (!f) { console.error('ERROR no shared face:', d[0], d[1], '<->', d[2]); errors++; continue; }
  const need = d[3] === 'J' ? 0.5 : d[4] + 0.9;
  if (f.hi - f.lo < need) { console.error('ERROR face too narrow:', d[0], (f.hi-f.lo)+'m <', need+'m'); errors++; }
}
// 4) F1 partition completeness — uncovered cells become Poche (sealed unbuilt shell)
const poche = [];
for (let cx = OX; cx < OX+OW; cx += 4) for (let cz = OZ; cz < OZ+OD; cz += 4) {
  const c = [null, 1, cx, cz, 4, 4];
  if (!SLABS.some(s => s[1] === 1 && s[0] !== 'VAN' && rectsOverlap(s, c))) poche.push([cx, cz]);
}
console.log('F1 poche cells (sealed shell):', poche.length, '| validation errors:', errors);
if (errors) process.exit(1);

// ================= RENDER =================
const S = 16, PAD = 70, MINX = -2, MAXX = 46, MINZ = -12, MAXZ = 42;
const W = (MAXX - MINX) * S + 2 * PAD, H = (MAXZ - MINZ) * S + 2 * PAD + 130;
const mx = x => PAD + (x - MINX) * S;
const mz = z => H - 130 - PAD - (z - MINZ) * S;
const FILL = { S: '#e1e6ee', M: '#d6e2d6', L: '#ebe0cd' };
function rect(x, z, w, d, attrs) {
  return '<rect x="' + mx(x) + '" y="' + mz(z + d) + '" width="' + w * S + '" height="' + d * S + '" ' + attrs + '/>';
}
function txt(x, z, t, sz, attrs) {
  return '<text x="' + mx(x) + '" y="' + mz(z) + '" font-size="' + (sz||11) + '" ' + (attrs||'') + '>' + t + '</text>';
}
function doorMark(d) { // returns SVG for one door gap on the shared face
  const a = byId[d[1]], b = byId[d[2]];
  if (['PLATE','VAN'].includes(d[1]) || ['PLATE','VAN'].includes(d[2])) {
    const s = ['PLATE','VAN'].includes(d[1]) ? b : a;
    if (s[0] === 'STAIRB2') return gapRect(s[2]+s[4], s[3]+s[5]/2, 'x', d);   // shutter onto plate, east face
    if (s[0] === 'BRIDGE2') return gapRect(s[2], s[3]+s[5]/2, 'x', d);        // bridge west threshold
    if (s[0] === 'LOBBY')   return gapRect(s[2]+s[4]/2 - 2, s[3], 'z', d);    // main entrance, south face
    return '';
  }
  const f = sharedFace(a, b);
  if (d[3] === 'J') { // corridor junction: merge fill over full shared edge
    const gw = 0.5;
    if (f.axis === 'x') return rect(f.at - gw/2, f.lo + 0.2, gw, (f.hi - f.lo) - 0.4, 'fill="#cfc9b8"');
    return rect(f.lo + 0.2, f.at - gw/2, (f.hi - f.lo) - 0.4, gw, 'fill="#cfc9b8"');
  }
  const m = (f.lo + f.hi) / 2;
  return gapRect(f.axis === 'x' ? f.at : m, f.axis === 'x' ? m : f.at, f.axis, d);
}
function gapRect(cx, cz, axis, d) {
  const w = d[4], t = 0.9;
  const gx = axis === 'x' ? cx - t/2 : cx - w/2, gz = axis === 'x' ? cz - w/2 : cz - t/2;
  const gw = axis === 'x' ? t : w, gd = axis === 'x' ? w : t;
  const fill = d[3] === 'T' ? '#e8c87a' : '#f7f5ef';
  const stroke = d[3] === 'T' ? 'stroke="#5a6e5a" stroke-width="1.5" stroke-dasharray="4,3"'
              : d[3] === 'C' ? 'stroke="#b42e22" stroke-width="2"' : 'stroke="#2d2d2d" stroke-width="1"';
  let s = rect(gx, gz, gw, gd, 'fill="' + fill + '" ' + stroke);
  if (d[3] === 'T') s += txt(gx + gw + 0.2, gz + 0.2, d[0], 10, 'fill="#3c3c3c" font-family="Consolas"');
  return s;
}
function lightDot(x, z, color, label) {
  let s = '<circle cx="' + mx(x) + '" cy="' + mz(z) + '" r="6" fill="' + color + '" stroke="#2d2d2d" stroke-width="0.8" fill-opacity="0.9"/>';
  if (label) s += txt(x + 0.6, z - 0.5, label, 10, 'fill="#555"');
  return s;
}
function poly(pts, attrs) {
  return '<polyline points="' + pts.map(p => mx(p[0]) + ',' + mz(p[1])).join(' ') + '" fill="none" ' + attrs + '/>';
}
const RED = 'stroke="#b42e22" stroke-width="3" stroke-linejoin="round"';
const AMB = 'stroke="#c98a2d" stroke-width="2.5" stroke-dasharray="8,5" stroke-linejoin="round"';

function render(floor, title, file) {
  let s = '<svg xmlns="http://www.w3.org/2000/svg" width="' + W + '" height="' + H + '" font-family="Microsoft YaHei,sans-serif">';
  s += '<rect width="100%" height="100%" fill="#f4f2eb"/>';
  s += '<defs><pattern id="hatch" width="8" height="8" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">'
     + '<line x1="0" y1="0" x2="0" y2="8" stroke="#8d8678" stroke-width="2"/></pattern></defs>';
  for (let x = MINX + 2; x <= MAXX; x += 4) s += '<line x1="' + mx(x) + '" y1="' + mz(MINZ) + '" x2="' + mx(x) + '" y2="' + mz(MAXZ) + '" stroke="#5a5a5a" stroke-opacity=".1"/>';
  for (let z = MINZ + 2; z <= MAXZ; z += 4) s += '<line x1="' + mx(MINX) + '" y1="' + mz(z) + '" x2="' + mx(MAXX) + '" y2="' + mz(z) + '" stroke="#5a5a5a" stroke-opacity=".1"/>';

  if (floor === 2) { // raw open plate as base
    s += rect(OX, OZ, OW, OD, 'fill="#ded8c9"');
    // scattered material-stack cover (decorative, fixed positions)
    for (const [px, pz] of [[3,3],[8,6],[16,4],[6,28],[5,35],[16,30],[20,34],[33,34],[39,30],[21,8],[14,11]])
      s += rect(px, pz, 1.6, 1.2, 'fill="#b9b3a4" stroke="#8d8678"');
    s += txt(2, 11.5, '毛坯开放板 — 料堆/立柱掩体 · 防尘布 · 黑暗(需手电/光锚)', 12, 'fill="#6a6354"');
    s += txt(13, 38.6, '北沿:临边防护缺失(坠落风险标识)', 10.5, 'fill="#8a4030"');
  }
  // F1 poche (sealed shell cells)
  if (floor === 1) for (const [px, pz] of poche) s += rect(px, pz, 4, 4, 'fill="url(#hatch)" fill-opacity="0.55"');

  const fl = SLABS.filter(n => n[1] === floor && n[0] !== 'PLATE');
  for (const n of fl) { // fills
    let fill;
    if (n[6] === 'Stair') fill = '#cdcdd7';
    else if (n[6] === 'Void') fill = n[0] === 'ATRIUM' ? '#3a3733' : '#efe9da';
    else if (n[6] === 'Corr') fill = '#cfc9b8';
    else if (n[6] === 'Bridge') fill = '#a89878';
    else if (n[6] === 'Van') fill = '#e4e4e4';
    else fill = FILL[n[7]];
    s += rect(n[2], n[3], n[4], n[5], 'fill="' + fill + '"');
  }
  for (const d of DOORS) { const a = byId[d[1]]; if (a[1] !== floor && byId[d[2]][1] !== floor) continue; if (d[3] === 'J') s += doorMark(d); }
  for (const n of fl) { // outlines + labels
    const dash = n[6] === 'Void' ? (n[0] === 'COLLAPSE' ? 'stroke-dasharray="7,5" stroke="#785a3c"' : 'stroke="#2d2d2d"') : 'stroke="#2d2d2d"';
    s += rect(n[2], n[3], n[4], n[5], 'fill="none" ' + dash + ' stroke-width="2.5"');
    const white = n[0] === 'ATRIUM';
    s += txt(n[2] + 0.25, n[3] + n[5] - 1.1, n[8], 12.5, 'font-weight="bold"' + (white ? ' fill="#f4f2eb"' : ''));
    s += txt(n[2] + 0.25, n[3] + n[5] - 2, n[0] + (n[7] !== '-' ? '  [' + n[7] + ']' : ''), 10, 'fill="' + (white ? '#d8d4c8' : '#3c3c3c') + '" fill-opacity=".75" font-family="Consolas"');
    if (n[6] === 'Stair') { const x = mx(n[2]), y = mz(n[3]+n[5]), w = n[4]*S, h = n[5]*S;
      for (let i = 1; i <= 6; i++) s += '<line x1="' + (x+4) + '" y1="' + (y + h*i/7) + '" x2="' + (x+w-4) + '" y2="' + (y + h*i/7) + '" stroke="#3c3c3c" stroke-width="1.5"/>'; }
    if (n[6] === 'Bridge') { const x = mx(n[2]), y = mz(n[3]+n[5]), w = n[4]*S, h = n[5]*S;
      for (let i = 1; i <= 11; i++) s += '<line x1="' + (x + w*i/12) + '" y1="' + (y+3) + '" x2="' + (x + w*i/12) + '" y2="' + (y+h-3) + '" stroke="#6a5a3a" stroke-width="1.2"/>'; }
  }
  for (const d of DOORS) { const a = byId[d[1]]; if ((a[1] !== floor && byId[d[2]][1] !== floor) || d[3] === 'J') continue; s += doorMark(d); }

  if (floor === 1) {
    // critical paths: B-route (power then stair B) + A-route (hall to stair A)
    s += poly([[20,-6],[20,4],[18,4],[18,10],[10,10],[6,10],[6,14],[2,14]], RED);
    s += poly([[6,16],[6,20],[2,20]], RED);
    s += poly([[18,12],[18,20],[26,20],[26,26],[28,26],[28,30]], RED);
    s += txt(14.5, -5.4, '↑ 进楼', 11, 'fill="#b42e22"');
    s += '<circle ' + 'cx="' + mx(41.5) + '" cy="' + mz(16.5) + '" r="' + 1.6*S + '" fill="none" stroke="#c98a2d" stroke-width="2" stroke-dasharray="5,4"/>';
    s += txt(33.2, 21.2, '自F2阳台跳降着陆', 10.5, 'fill="#8a6420"');
    s += txt(31.2, 40.7, '▲ 消防口(单向撤离)', 10.5, 'fill="#8a4030"');
    s += rect(32, 39.55, 2.8, 0.9, 'fill="#f7f5ef" stroke="#8a4030" stroke-width="1.5"'); // fire exit gap
    s += txt(12.3, 17, '↑中庭挑空:仰视可见脚手桥与样板岛灯光', 10.5, 'fill="#6a6354"');
    s += lightDot(13.2, 6.8, '#e8a33d', '值班台残灯(暖)');
    s += lightDot(1, 15.2, '#c43a2a', 'P-01红色指示灯');
    s += lightDot(1.4, 22.8, '#d9b25a', 'B梯钠灯频闪');
    s += lightDot(6, 38, '#eef0f4', '塌角天光(指北)');
    s += txt(28.6, 36.8, 'A梯:外墙未浇筑,透光', 10, 'fill="#555"');
  } else {
    // critical: B-shutter -> plate -> bridge -> island -> target; A: stair -> C7 -> sales
    s += poly([[4,20],[12,20]], RED);
    s += poly([[12,20],[24,20],[28,20],[28,18],[33,18],[33,12],[38,12]], RED);
    s += poly([[28,32],[28,26],[28,24]], RED);
    s += poly([[42,16],[42,14]], AMB);
    s += txt(38.7, 13.4, 'E-DROP 单向跳降 → F1装卸坞', 10.5, 'fill="#8a6420"');
    s += txt(4.4, 24.6, '卷帘①(欠款/断电锁定)', 10.5, 'fill="#8a4030"');
    s += txt(25.5, 36.8, '卷帘②(断电锁定)', 10.5, 'fill="#8a4030"');
    s += txt(32.2, 6.6, '封闭幕墙(岛外不可进入)', 10, 'fill="#6a6354"');
    s += lightDot(36, 22, '#e8f0f4', '样板岛冷光(全图灯塔)');
    s += lightDot(35, 13.5, '#7fd4c0', '生态柱微光(变异藻荧光)');
    s += lightDot(27.5, 22.5, '#e8a33d', '销售台暖灯+沙盘陈设');
    // monster seed positions
    for (const [px, pz, t] of [[40,10,'巢'], [26,17,''], [23.2,21,'']])
      s += '<g><circle cx="' + mx(px) + '" cy="' + mz(pz) + '" r="8" fill="none" stroke="#8a3030" stroke-width="2"/><text x="' + (mx(px)-5) + '" y="' + (mz(pz)+4) + '" font-size="10" fill="#8a3030">⚠</text></g>'
         + (t ? txt(px+0.7, pz-0.4, t, 10, 'fill="#8a3030"') : '');
    s += txt(13, 14.2, '⚠=感染监理 seed随机起始位(巢=生态柱展厅)', 10.5, 'fill="#8a3030"');
  }
  // outline + van arrow
  s += rect(OX, OZ, OW, OD, 'fill="none" stroke="#1d1d1d" stroke-width="4"');
  if (floor === 1) s += rect(14, -10, 12, 8, 'fill="none" stroke="#2d2d2d" stroke-width="2.5" stroke-dasharray="2,3"');

  s += '<text x="24" y="' + (H - 96) + '" font-size="24" font-weight="bold">' + title + '</text>';
  let lx = 24; const ly = H - 64;
  const leg = [['#e1e6ee','S 4×4'], ['#d6e2d6','M 8×8'], ['#ebe0cd','L 12×8'], ['#cfc9b8','走廊(一等公民)'], ['#cdcdd7','楼梯(锚定)'], ['url(#hatch)','封死毛坯'], ['#ded8c9','开放毛坯板'], ['#3a3733','中庭挑空']];
  for (const [c, t] of leg) {
    s += '<rect x="' + lx + '" y="' + (ly-12) + '" width="18" height="14" fill="' + c + '" stroke="#888"/><text x="' + (lx+23) + '" y="' + ly + '" font-size="12">' + t + '</text>';
    lx += 26 + t.length * 12 + 16;
  }
  let l2 = 24; const ly2 = H - 36;
  s += '<line x1="' + l2 + '" y1="' + (ly2-5) + '" x2="' + (l2+38) + '" y2="' + (ly2-5) + '" stroke="#b42e22" stroke-width="3"/><text x="' + (l2+44) + '" y="' + ly2 + '" font-size="12">关键路径(固定)</text>'; l2 += 170;
  s += '<rect x="' + l2 + '" y="' + (ly2-13) + '" width="16" height="11" fill="#e8c87a" stroke="#5a6e5a" stroke-dasharray="4,3"/><text x="' + (l2+22) + '" y="' + ly2 + '" font-size="12">随机开关门 T#(共11)</text>'; l2 += 190;
  s += '<rect x="' + l2 + '" y="' + (ly2-13) + '" width="16" height="11" fill="#f7f5ef" stroke="#2d2d2d"/><text x="' + (l2+22) + '" y="' + ly2 + '" font-size="12">固定门/洞口</text>'; l2 += 140;
  s += '<circle cx="' + (l2+8) + '" cy="' + (ly2-7) + '" r="6" fill="#e8a33d"/><text x="' + (l2+20) + '" y="' + ly2 + '" font-size="12">光锚</text>'; l2 += 80;
  s += '<text x="' + l2 + '" y="' + ly2 + '" font-size="12">★ 地标</text>';
  s += '<text x="24" y="' + (H - 12) + '" font-size="11" fill="#555">v7 楼板分区模型:每格∈{房间,走廊,楼梯核心,中庭/塌角,封死毛坯};走廊为显式坐标一等公民,门=共享墙打洞,无寻路 | 目标物=「真实海岸」生态柱(双手0.55×);沙盘留作销售办公陈设(四阶段黑色委托钩子) | 三房型+楼梯锚定不变</text>';
  s += '</svg>';
  fs.writeFileSync(path.join(out, file), s);
  console.log('WROTE', file);
}
render(1, '地球海岸壹号·烂尾预售楼 — F1 平面图 v7(提案·楼板分区·待PM批准)', 'Tower_EarthCoast_01_F1_Plan_v7_proposal.svg');
render(2, '地球海岸壹号·烂尾预售楼 — F2 平面图 v7(提案·样板岛+中庭·待PM批准)', 'Tower_EarthCoast_01_F2_Plan_v7_proposal.svg');
