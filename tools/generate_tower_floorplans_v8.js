// F1/F2 floor-plan proposal v8 (SVG, zero deps) — SLAB-PARTITION + SECTION GRAMMAR.
// PM decision 2026-06-10: 「听美术的」— corridors STAY 4m wide; spatial identity comes
// from cross-section grammar (per-class ceiling/light/prop language), not plan-width
// regrading. Plan-data fixes limited to the four-specialist convergent rules:
//   1. functionClass decoupled from size class (Dead/Hub/Corr/Node/Stair/Void/Bridge/Open)
//   2. door width <= 50% of shared wall face (4m face -> 2.0m max)
//   3. pass-through hubs: doors off-axis (no enfilade straight-shoot)
//   4. T-doors must have >=1 dead-end room end (T1/T4 rewired off the LOBBY hub)
//   5. corridor straight run > 16m requires a declared mid break node (C3)
//   6. F2 PLATE keeps a stack-free sightline corridor B-shutter -> show-flat beacon
// Geometry (slab rects) is otherwise IDENTICAL to v7. Doors = holes in shared walls.
const fs = require('fs'), path = require('path');
const out = path.join(__dirname, '..', 'Assets/_Project/Art/Maps/Tower_EarthCoast_01/References');
fs.mkdirSync(out, { recursive: true });

// Building slab outline (both floors): x 0..44, z 0..40. Van forecourt outside south.
const OX = 0, OZ = 0, OW = 44, OD = 40;

// ---- slabs: [id, floor, x, z, w, d, kind, size, label]  (geometry unchanged from v7)
const SLABS = [
  // ---------- F1 ----------
  ['VAN',     1, 14,-10, 12, 8, 'Van',  '-', '委托车/前院'],
  ['WAREHOUSE',1, 0,  0, 12, 8, 'Room', 'L', '西仓库'],
  ['LOBBY',   1, 12,  0, 12, 8, 'Room', 'L', '大堂·售楼处 ★'],
  ['PUMP',    1, 24,  0,  4, 4, 'Room', 'S', '水泵机电'],
  ['SECUR',   1, 24,  4,  4, 4, 'Room', 'S', '保安室'],
  ['WORKSHOP',1, 28,  0,  8, 8, 'Room', 'M', '工坊'],
  ['C1',      1,  0,  8, 16, 4, 'Corr', '-', '南廊·西'],
  ['C2',      1, 16,  8,  4, 8, 'Corr', '-', '门厅路口'],
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

// ---- functionClass: gameplay descriptor, DECOUPLED from kind (geometry) / size.
// Rule V8-C3: a Room with >=2 critical doors MUST be Hub (transit, zero loot, high headroom cue).
const FC_OVERRIDE = { LOBBY:'Hub', HALL:'Hub', SALES:'Hub', SHOWFLAT:'Hub', C2:'Node', C6:'Node', C7:'Node' };
const FC_DEFAULT = { Room:'Dead', Corr:'Corr', Stair:'Stair', Void:'Void', Bridge:'Bridge', Open:'Open', Van:'Van' };
const fcOf = s => FC_OVERRIDE[s[0]] || FC_DEFAULT[s[6]];

// ---- per-slab section note (art-led ceiling grammar; corridors carry it in the legend)
const CEIL = {
  LOBBY: '顶:裸龙骨4.5m', HALL: '顶:挑空7–8m', SALES: '顶:吊顶完好2.8m',
  SHOWFLAT: '顶:精装吊顶2.8m', TARGET: '顶:3.2m+展台', STAIRB1: '井道通高5.6m', STAIRA1: '井道通高5.6m',
};

// ---- doors: [id, a, b, type C/F/T, width m, offset?]  (J = full-length corridor merge)
// offset shifts the door center along the shared face from the face midpoint (meters).
const DOORS = [
  // F1 critical — 4m-face doors capped at 2.0m (V8-C1); D-VAN offset +3 breaks the
  // D-VAN/D1 enfilade through the LOBBY hub (V8-C2)
  ['D-VAN','VAN','LOBBY','C',2.8, 3], ['D1','LOBBY','C2','C',2.0],
  ['J1','C1','C2','J',0], ['J2','C2','C3','J',0], ['J3','C1','C4','J',0],
  ['D4','C4','POWER','C',2.0], ['D5','C4','STAIRB1','C',2.0,-2],
  ['D7','C2','HALL','C',2.0], ['D8','HALL','C5','C',2.8],
  ['J4','C3','C5','J',0], ['J5','C5','C6','J',0], ['D10','C6','STAIRA1','C',2.0,-0.55],
  // F1 fixed
  ['D6','C4','TEMP','F',2.0], ['D11','C1','WAREHOUSE','F',2.8], ['D12','C1','SAMPLE','F',2.0],
  ['D13','C3','SECUR','F',2.0], ['D14','C3','WORKSHOP','F',2.8], ['D15','C3','DOCK','F',2.8],
  ['D16','C3','REBAR','F',2.8], ['D17','LOBBY','PUMP','F',2.0], ['D18','HALL','DORM','F',2.0],
  ['D19','STAIRA1','FOREMAN','F',2.0],
  // Backbone anchors promoted from toggles T3/T12 (V8-C7: CANTEEN/SHANTY must stay
  // reachable with every toggle closed — same fix V3 applied via added anchors).
  ['D20','DORM','CANTEEN','F',2.0], ['D21','DOCK','SHANTY','F',2.0],
  // F1 toggles (7) — T1/T4 rewired off the LOBBY hub (V8-C5: >=1 end must be a dead room)
  ['T1','SECUR','WORKSHOP','T',2.0], ['T2','C4','SAMPLE','T',2.0],
  ['T4','C1','WAREHOUSE','T',2.0,-4], ['T5','TEMP','COLLAPSE','T',2.0],
  ['T6','COLLAPSE','CANTEEN','T',2.0], ['T10','C5','REBAR','T',2.0],
  ['T11','PUMP','WORKSHOP','T',2.0],
  // F2 critical — D32 capped 2.0 (4m face); D33 offset -2 breaks the bridge->sales->showflat axis
  ['D30','STAIRB2','PLATE','C',2.8,2], ['D31','PLATE','BRIDGE2','C',2.8],
  ['D32','BRIDGE2','SALES','C',2.0], ['D33','SALES','SHOWFLAT','C',2.8,-2],
  ['D34','SHOWFLAT','TARGET','C',2.8],
  ['D35','STAIRA2','C7','C',2.0,0.55], ['D36','C7','SALES','C',2.0],
  // F2 toggles (2)
  ['T7','SHOWFLAT','VIP','T',2.0], ['T17','TARGET','BALCONY','T',2.0],
];

// ---- corridor mid-break nodes (V8-C4: straight run > 16m needs a declared break)
const BREAKS = [{ slab: 'C3', x: 30, z: 10, label: '廊中节点:指向牌+工程灯' }];

// ---- F2 material stacks (decorative cover, fixed) + sightline corridor (V8-C6)
// Stacks keep clear of the sightline corridor AND of the collapse sky shaft (x0..12,z28..40).
const STACKS = [[3,3],[8,6],[16,4],[7,26],[14,35],[16,30],[20,34],[33,34],[39,30],[21,8],[14,11]];
const SIGHT = { x: 4, z: 18, w: 28, d: 4 }; // B-shutter exit -> show-flat beacon, stack-free
const SKYHOLE = { x: 0, z: 28, w: 12, d: 12 }; // 塌角上空: the F2 plate is collapsed through here

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
function doorCenter(d) { // {axis, at, pos} mirroring doorMark; null for J/unplaceable
  if (d[3] === 'J') return null;
  if (d[0] === 'D-VAN') return { axis: 'z', at: 0, pos: 18 + (d[5] || 0) };
  if (['PLATE','VAN'].includes(d[1]) || ['PLATE','VAN'].includes(d[2])) {
    const s = ['PLATE','VAN'].includes(d[1]) ? byId[d[2]] : byId[d[1]];
    if (s[0] === 'STAIRB2') return { axis: 'x', at: s[2]+s[4], pos: s[3]+s[5]/2 };
    if (s[0] === 'BRIDGE2') return { axis: 'x', at: s[2],      pos: s[3]+s[5]/2 };
    return null;
  }
  const f = sharedFace(byId[d[1]], byId[d[2]]);
  if (!f) return null;
  return { axis: f.axis, at: f.at, pos: (f.lo + f.hi) / 2 + (d[5] || 0) };
}
// 3) every door fits its shared face: width + 0.9 corner clearance, offset respected,
//    and door width <= 50% of the face (V8-C1)
for (const d of DOORS) {
  const a = byId[d[1]], b = byId[d[2]];
  if (!a || !b) { console.error('ERROR unknown slab in', d[0]); errors++; continue; }
  if (['PLATE','VAN'].includes(d[1]) || ['PLATE','VAN'].includes(d[2])) continue; // perimeter entrance
  const f = sharedFace(a, b);
  if (!f) { console.error('ERROR no shared face:', d[0], d[1], '<->', d[2]); errors++; continue; }
  if (d[3] === 'J') { if (f.hi - f.lo < 0.5) { console.error('ERROR junction too narrow:', d[0]); errors++; } continue; }
  const face = f.hi - f.lo, c = (f.lo + f.hi) / 2 + (d[5] || 0);
  if (d[4] > face * 0.5 + 0.01) { console.error('ERROR V8-C1 door > 50% of face:', d[0], d[4]+'m on '+face+'m'); errors++; }
  if (c - d[4]/2 < f.lo + 0.45 || c + d[4]/2 > f.hi - 0.45) { console.error('ERROR door off face:', d[0]); errors++; }
}
// 4) V8-C2: no enfilade through a Hub — two critical doors on parallel opposite faces
//    of the same hub must have centers >= 2m apart
for (const s of SLABS) {
  if (fcOf(s) !== 'Hub') continue;
  const ds = DOORS.filter(d => d[3] === 'C' && (d[1] === s[0] || d[2] === s[0])).map(doorCenter).filter(Boolean);
  for (let i = 0; i < ds.length; i++) for (let j = i+1; j < ds.length; j++) {
    if (ds[i].axis !== ds[j].axis || Math.abs(ds[i].at - ds[j].at) < 0.01) continue;
    if (Math.abs(ds[i].pos - ds[j].pos) < 2 - 0.01) {
      console.error('ERROR V8-C2 enfilade through hub', s[0], '(door centers', ds[i].pos, ds[j].pos + ')'); errors++;
    }
  }
}
// 5) V8-C3: Room kind with >=2 critical doors must be functionClass Hub
for (const s of SLABS) {
  if (s[6] !== 'Room') continue;
  const c = DOORS.filter(d => d[3] === 'C' && (d[1] === s[0] || d[2] === s[0])).length;
  if (c >= 2 && fcOf(s) !== 'Hub') { console.error('ERROR V8-C3 pass-through room not Hub:', s[0]); errors++; }
}
// 6) V8-C4: corridor straight run > 16m requires a declared break node
for (const s of SLABS) {
  if (fcOf(s) !== 'Corr') continue;
  if (Math.max(s[4], s[5]) > 16 && !BREAKS.some(b => b.slab === s[0])) {
    console.error('ERROR V8-C4 corridor run > 16m without break:', s[0]); errors++;
  }
}
// 7) V8-C5: every toggle door has at least one dead-room end (<=1 non-toggle door)
const nonTCount = {};
for (const d of DOORS) if (d[3] === 'C' || d[3] === 'F') { nonTCount[d[1]] = (nonTCount[d[1]]||0)+1; nonTCount[d[2]] = (nonTCount[d[2]]||0)+1; }
for (const d of DOORS) {
  if (d[3] !== 'T') continue;
  const dead = id => { const s = byId[id]; return (fcOf(s) === 'Dead' || fcOf(s) === 'Void') && (nonTCount[id]||0) <= 1; };
  if (!dead(d[1]) && !dead(d[2])) { console.error('ERROR V8-C5 toggle between non-dead ends:', d[0], d[1], d[2]); errors++; }
}
// 8) V8-C6: F2 sightline corridor (B-shutter -> beacon) free of material stacks;
//    stacks also must not float over the collapse sky shaft (no plate there).
for (const [px, pz] of STACKS) {
  if (px+1.6 > SIGHT.x && px < SIGHT.x+SIGHT.w && pz+1.2 > SIGHT.z && pz < SIGHT.z+SIGHT.d) {
    console.error('ERROR V8-C6 stack blocks sightline corridor:', px, pz); errors++;
  }
  if (px+1.6 > SKYHOLE.x && px < SKYHOLE.x+SKYHOLE.w && pz+1.2 > SKYHOLE.z && pz < SKYHOLE.z+SKYHOLE.d) {
    console.error('ERROR stack floats over collapse sky shaft:', px, pz); errors++;
  }
}
// 9) V8-C7: backbone reachability — every Room slab except seed-gated VIP/BALCONY must be
//    reachable from VAN with all toggles closed (stairs included as virtual edges).
{
  const adj = {};
  const link = (a, b) => { (adj[a] = adj[a] || []).push(b); (adj[b] = adj[b] || []).push(a); };
  for (const d of DOORS) if (d[3] !== 'T') link(d[1], d[2]);
  link('STAIRB1', 'STAIRB2'); link('STAIRA1', 'STAIRA2');
  const seen = new Set(['VAN']); const q = ['VAN'];
  while (q.length) { const n = q.shift(); for (const m of adj[n] || []) if (!seen.has(m)) { seen.add(m); q.push(m); } }
  for (const s of SLABS) {
    if (s[6] !== 'Room' || s[0] === 'VIP' || s[0] === 'BALCONY') continue;
    if (!seen.has(s[0])) { console.error('ERROR V8-C7 room not backbone-reachable:', s[0]); errors++; }
  }
}
// 9) F1 partition completeness — uncovered cells become Poche (sealed unbuilt shell)
const poche = [];
for (let cx = OX; cx < OX+OW; cx += 4) for (let cz = OZ; cz < OZ+OD; cz += 4) {
  const c = [null, 1, cx, cz, 4, 4];
  if (!SLABS.some(s => s[1] === 1 && s[0] !== 'VAN' && rectsOverlap(s, c))) poche.push([cx, cz]);
}
console.log('F1 poche cells (sealed shell):', poche.length, '| validation errors:', errors);
if (errors) process.exit(1);

// ================= RENDER =================
const S = 16, PAD = 70, MINX = -2, MAXX = 46, MINZ = -12, MAXZ = 42;
const W = (MAXX - MINX) * S + 2 * PAD, H = (MAXZ - MINZ) * S + 2 * PAD + 150;
const mx = x => PAD + (x - MINX) * S;
const mz = z => H - 150 - PAD - (z - MINZ) * S;
const FILL = { S: '#e1e6ee', M: '#d6e2d6', L: '#ebe0cd' };
function rect(x, z, w, d, attrs) {
  return '<rect x="' + mx(x) + '" y="' + mz(z + d) + '" width="' + w * S + '" height="' + d * S + '" ' + attrs + '/>';
}
function txt(x, z, t, sz, attrs) {
  return '<text x="' + mx(x) + '" y="' + mz(z) + '" font-size="' + (sz||11) + '" ' + (attrs||'') + '>' + t + '</text>';
}
function doorMark(d) { // SVG for one door gap on the shared face (offset-aware)
  const a = byId[d[1]], b = byId[d[2]];
  if (['PLATE','VAN'].includes(d[1]) || ['PLATE','VAN'].includes(d[2])) {
    const s = ['PLATE','VAN'].includes(d[1]) ? b : a;
    if (s[0] === 'STAIRB2') return gapRect(s[2]+s[4], s[3]+s[5]/2, 'x', d);   // shutter onto plate, east face
    if (s[0] === 'BRIDGE2') return gapRect(s[2], s[3]+s[5]/2, 'x', d);        // bridge west threshold
    if (s[0] === 'LOBBY')   return gapRect(18 + (d[5]||0), s[3], 'z', d);     // main entrance, south face, off-axis
    return '';
  }
  const f = sharedFace(a, b);
  if (d[3] === 'J') { // corridor junction: merge fill over full shared edge
    const gw = 0.5;
    if (f.axis === 'x') return rect(f.at - gw/2, f.lo + 0.2, gw, (f.hi - f.lo) - 0.4, 'fill="#cfc9b8"');
    return rect(f.lo + 0.2, f.at - gw/2, (f.hi - f.lo) - 0.4, gw, 'fill="#cfc9b8"');
  }
  const m = (f.lo + f.hi) / 2 + (d[5] || 0);
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

// corridor section glyphs: dashed cable-tray centerline + pilaster ticks every 4m (art rhythm)
function corrGlyphs(n) {
  let s = '';
  const horiz = n[4] >= n[5];
  if (horiz) {
    s += '<line x1="' + mx(n[2]+0.6) + '" y1="' + mz(n[3]+n[5]/2) + '" x2="' + mx(n[2]+n[4]-0.6) + '" y2="' + mz(n[3]+n[5]/2) + '" stroke="#8d8678" stroke-width="1.2" stroke-dasharray="6,4"/>';
    for (let x = n[2]+4; x < n[2]+n[4]-0.1; x += 4) {
      s += rect(x-0.15, n[3], 0.3, 0.5, 'fill="#8d8678"') + rect(x-0.15, n[3]+n[5]-0.5, 0.3, 0.5, 'fill="#8d8678"');
    }
  } else {
    s += '<line x1="' + mx(n[2]+n[4]/2) + '" y1="' + mz(n[3]+0.6) + '" x2="' + mx(n[2]+n[4]/2) + '" y2="' + mz(n[3]+n[5]-0.6) + '" stroke="#8d8678" stroke-width="1.2" stroke-dasharray="6,4"/>';
    for (let z = n[3]+4; z < n[3]+n[5]-0.1; z += 4) {
      s += rect(n[2], z-0.15, 0.5, 0.3, 'fill="#8d8678"') + rect(n[2]+n[4]-0.5, z-0.15, 0.5, 0.3, 'fill="#8d8678"');
    }
  }
  return s;
}
// node glyph: floor-marking crosshair (路口地面标线)
function nodeGlyph(n) {
  const cx = n[2]+n[4]/2, cz = n[3]+n[5]/2, r = 0.9;
  return '<circle cx="' + mx(cx) + '" cy="' + mz(cz) + '" r="' + r*S + '" fill="none" stroke="#7a7263" stroke-width="1.5" stroke-dasharray="3,3"/>'
       + '<line x1="' + mx(cx-r) + '" y1="' + mz(cz) + '" x2="' + mx(cx+r) + '" y2="' + mz(cz) + '" stroke="#7a7263" stroke-width="1.2"/>'
       + '<line x1="' + mx(cx) + '" y1="' + mz(cz-r) + '" x2="' + mx(cx) + '" y2="' + mz(cz+r) + '" stroke="#7a7263" stroke-width="1.2"/>';
}

function render(floor, title, file) {
  let s = '<svg xmlns="http://www.w3.org/2000/svg" width="' + W + '" height="' + H + '" font-family="Microsoft YaHei,sans-serif">';
  s += '<rect width="100%" height="100%" fill="#f4f2eb"/>';
  s += '<defs><pattern id="hatch" width="8" height="8" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">'
     + '<line x1="0" y1="0" x2="0" y2="8" stroke="#8d8678" stroke-width="2"/></pattern></defs>';
  for (let x = MINX + 2; x <= MAXX; x += 4) s += '<line x1="' + mx(x) + '" y1="' + mz(MINZ) + '" x2="' + mx(x) + '" y2="' + mz(MAXZ) + '" stroke="#5a5a5a" stroke-opacity=".1"/>';
  for (let z = MINZ + 2; z <= MAXZ; z += 4) s += '<line x1="' + mx(MINX) + '" y1="' + mz(z) + '" x2="' + mx(MAXX) + '" y2="' + mz(z) + '" stroke="#5a5a5a" stroke-opacity=".1"/>';

  if (floor === 2) { // raw open plate as base
    s += rect(OX, OZ, OW, OD, 'fill="#ded8c9"');
    // Collapse sky shaft: the corner that fell through both floors (gives F1 its skylight).
    s += rect(SKYHOLE.x, SKYHOLE.z, SKYHOLE.w, SKYHOLE.d, 'fill="#efe9da" stroke="#785a3c" stroke-width="2" stroke-dasharray="7,5"');
    s += txt(SKYHOLE.x + 0.4, SKYHOLE.z + SKYHOLE.d - 1.1, '塌角上空(楼板塌穿·无板·临边坠落)', 10.5, 'fill="#785a3c"');
    // V8-C6 sightline corridor first (under the stacks layer visually, none overlap anyway)
    s += rect(SIGHT.x, SIGHT.z, SIGHT.w, SIGHT.d, 'fill="#e8e2d2" stroke="#c98a2d" stroke-width="1.5" stroke-dasharray="6,5"');
    s += txt(SIGHT.x + 0.4, SIGHT.z + 0.9, '视线走廊:自B梯卷帘口直见样板岛冷光(料堆禁区·V8-C6)', 10.5, 'fill="#8a6420"');
    for (const [px, pz] of STACKS) s += rect(px, pz, 1.6, 1.2, 'fill="#b9b3a4" stroke="#8d8678"');
    s += txt(2, 11.5, '毛坯开放板 — 料堆/立柱掩体 · 防尘布 · 黑暗(需手电/光锚)', 12, 'fill="#6a6354"');
    s += txt(13, 38.6, '北沿:临边防护缺失(坠落风险标识)', 10.5, 'fill="#8a4030"');
  }
  // F1 poche (sealed shell cells)
  if (floor === 1) for (const [px, pz] of poche) s += rect(px, pz, 4, 4, 'fill="url(#hatch)" fill-opacity="0.55"');

  const fl = SLABS.filter(n => n[1] === floor && n[0] !== 'PLATE');
  for (const n of fl) { // fills (functionClass-aware)
    const fc = fcOf(n);
    let fill;
    if (n[6] === 'Stair') fill = '#cdcdd7';
    else if (n[6] === 'Void') fill = n[0] === 'ATRIUM' ? '#3a3733' : '#efe9da';
    else if (fc === 'Hub') fill = '#e9d5ae';
    else if (fc === 'Node') fill = '#c4bda9';
    else if (n[6] === 'Corr') fill = '#cfc9b8';
    else if (n[6] === 'Bridge') fill = '#a89878';
    else if (n[6] === 'Van') fill = '#e4e4e4';
    else fill = FILL[n[7]];
    s += rect(n[2], n[3], n[4], n[5], 'fill="' + fill + '"');
  }
  for (const d of DOORS) { const a = byId[d[1]]; if (a[1] !== floor && byId[d[2]][1] !== floor) continue; if (d[3] === 'J') s += doorMark(d); }
  for (const n of fl) { // section glyphs + outlines + labels
    const fc = fcOf(n);
    if (fc === 'Corr') s += corrGlyphs(n);
    if (fc === 'Node') s += nodeGlyph(n);
    const dash = n[6] === 'Void' ? (n[0] === 'COLLAPSE' ? 'stroke-dasharray="7,5" stroke="#785a3c"' : 'stroke="#2d2d2d"') : 'stroke="#2d2d2d"';
    s += rect(n[2], n[3], n[4], n[5], 'fill="none" ' + dash + ' stroke-width="2.5"');
    const white = n[0] === 'ATRIUM';
    s += txt(n[2] + 0.25, n[3] + n[5] - 1.1, n[8], 12.5, 'font-weight="bold"' + (white ? ' fill="#f4f2eb"' : ''));
    const tag = n[0] + (n[7] !== '-' ? ' [' + n[7] + ']' : '') + (fc === 'Hub' ? ' ·枢纽' : fc === 'Node' ? ' ·路口' : '');
    s += txt(n[2] + 0.25, n[3] + n[5] - 2, tag, 10, 'fill="' + (white ? '#d8d4c8' : '#3c3c3c') + '" fill-opacity=".75" font-family="Consolas"');
    if (CEIL[n[0]]) s += txt(n[2] + 0.25, n[3] + n[5] - 2.9, CEIL[n[0]], 9.5, 'fill="#6a6354"');
    if (n[6] === 'Stair') { const x = mx(n[2]), y = mz(n[3]+n[5]), w = n[4]*S, h = n[5]*S;
      for (let i = 1; i <= 6; i++) s += '<line x1="' + (x+4) + '" y1="' + (y + h*i/7) + '" x2="' + (x+w-4) + '" y2="' + (y + h*i/7) + '" stroke="#3c3c3c" stroke-width="1.5"/>'; }
    if (n[6] === 'Bridge') { const x = mx(n[2]), y = mz(n[3]+n[5]), w = n[4]*S, h = n[5]*S;
      for (let i = 1; i <= 11; i++) s += '<line x1="' + (x + w*i/12) + '" y1="' + (y+3) + '" x2="' + (x + w*i/12) + '" y2="' + (y+h-3) + '" stroke="#6a5a3a" stroke-width="1.2"/>'; }
  }
  // corridor mid-break nodes
  for (const b of BREAKS) {
    const n = byId[b.slab];
    if (n[1] !== floor) continue;
    s += rect(b.x-0.3, n[3]+0.2, 0.6, 0.6, 'fill="#7a7263"') + rect(b.x-0.3, n[3]+n[5]-0.8, 0.6, 0.6, 'fill="#7a7263"');
    s += lightDot(b.x, b.z, '#e8a33d');
    s += txt(b.x + 0.7, b.z + 0.6, b.label, 10, 'fill="#6a6354"');
  }
  for (const d of DOORS) { const a = byId[d[1]]; if ((a[1] !== floor && byId[d[2]][1] !== floor) || d[3] === 'J') continue; s += doorMark(d); }

  if (floor === 1) {
    // critical paths: B-route (power then stair B) + A-route (hall to stair A); entrance now off-axis at x=21
    s += poly([[21,-6],[21,4],[18,4],[18,10],[10,10],[6,10],[6,14],[2,14]], RED);
    s += poly([[6,16],[6,20],[2,20]], RED);
    s += poly([[18,12],[18,20],[26,20],[26,26],[28,26],[28,30]], RED);
    s += txt(15.5, -5.4, '↑ 进楼(偏轴防对穿)', 11, 'fill="#b42e22"');
    s += '<circle ' + 'cx="' + mx(41.5) + '" cy="' + mz(16.5) + '" r="' + 1.6*S + '" fill="none" stroke="#c98a2d" stroke-width="2" stroke-dasharray="5,4"/>';
    s += txt(33.2, 21.2, '自F2阳台跳降着陆', 10.5, 'fill="#8a6420"');
    s += txt(31.2, 40.7, '▲ 消防口(单向撤离)', 10.5, 'fill="#8a4030"');
    s += rect(32, 39.55, 2.8, 0.9, 'fill="#f7f5ef" stroke="#8a4030" stroke-width="1.5"'); // fire exit gap
    s += txt(12.3, 17, '↑中庭挑空:仰视可见脚手桥与样板岛灯光', 10.5, 'fill="#6a6354"');
    s += lightDot(17, 6.5, '#e8a33d', '值班台残灯(暖·自D1/C2方向可见)');
    s += lightDot(1, 15.2, '#c43a2a', 'P-01悬挂红灯(自门外C4可见)');
    s += lightDot(1.4, 22.8, '#d9b25a', 'B梯钠灯频闪');
    s += lightDot(6, 38, '#eef0f4', '塌角天光(指北)');
    s += txt(4.6, 11.2, '值班台暖光向南渗透→返程导向', 9.5, 'fill="#8a6420"');
    s += txt(28.6, 36.8, 'A梯:外墙未浇筑,透光', 10, 'fill="#555"');
  } else {
    // critical: B-shutter -> plate -> bridge -> island -> target; A: stair -> C7 -> sales
    s += poly([[4,20],[12,20]], RED);
    s += poly([[12,20],[24,20],[27,20],[30,18],[33,18],[33,12],[38,12]], RED);
    s += poly([[28,32],[28,26],[28,24]], RED);
    s += poly([[42,16],[42,14]], AMB);
    s += txt(38.7, 13.4, 'E-DROP 单向跳降 → F1装卸坞', 10.5, 'fill="#8a6420"');
    s += txt(4.4, 24.6, '卷帘①(欠款/断电锁定)', 10.5, 'fill="#8a4030"');
    s += txt(25.5, 36.8, '卷帘②(断电锁定)', 10.5, 'fill="#8a4030"');
    s += txt(32.2, 6.6, '封闭幕墙(岛外不可进入)', 10, 'fill="#6a6354"');
    s += txt(24.3, 14.9, '沙盘居中挡视线·D33偏轴→进厅须转向', 9.5, 'fill="#6a6354"');
    s += lightDot(36, 22, '#e8f0f4', '样板岛冷光(全图灯塔)');
    s += lightDot(35, 13.5, '#7fd4c0', '生态柱微光(变异藻荧光)');
    s += lightDot(27.5, 21.5, '#e8a33d', '销售台暖灯(北墙)+沙盘陈设');
    // monster seed positions
    for (const [px, pz, t] of [[40,10,'巢'], [26,17,''], [23.2,21,'']])
      s += '<g><circle cx="' + mx(px) + '" cy="' + mz(pz) + '" r="8" fill="none" stroke="#8a3030" stroke-width="2"/><text x="' + (mx(px)-5) + '" y="' + (mz(pz)+4) + '" font-size="10" fill="#8a3030">⚠</text></g>'
         + (t ? txt(px+0.7, pz-0.4, t, 10, 'fill="#8a3030"') : '');
    s += txt(13, 14.2, '⚠=感染监理 seed随机起始位(巢=生态柱展厅)', 10.5, 'fill="#8a3030"');
  }
  // outline + van arrow
  s += rect(OX, OZ, OW, OD, 'fill="none" stroke="#1d1d1d" stroke-width="4"');
  if (floor === 1) s += rect(14, -10, 12, 8, 'fill="none" stroke="#2d2d2d" stroke-width="2.5" stroke-dasharray="2,3"');

  s += '<text x="24" y="' + (H - 116) + '" font-size="24" font-weight="bold">' + title + '</text>';
  let lx = 24; const ly = H - 84;
  const leg = [['#e1e6ee','S 4×4'], ['#d6e2d6','M 8×8'], ['#ebe0cd','L 12×8'], ['#e9d5ae','枢纽(穿行·零战利品)'], ['#cfc9b8','走廊(桥架压顶)'], ['#c4bda9','路口节点'], ['#cdcdd7','楼梯(锚定)'], ['url(#hatch)','封死毛坯'], ['#3a3733','中庭挑空']];
  for (const [c, t] of leg) {
    s += '<rect x="' + lx + '" y="' + (ly-12) + '" width="18" height="14" fill="' + c + '" stroke="#888"/><text x="' + (lx+23) + '" y="' + ly + '" font-size="12">' + t + '</text>';
    lx += 26 + t.length * 12 + 16;
  }
  let l2 = 24; const ly2 = H - 56;
  s += '<line x1="' + l2 + '" y1="' + (ly2-5) + '" x2="' + (l2+38) + '" y2="' + (ly2-5) + '" stroke="#b42e22" stroke-width="3"/><text x="' + (l2+44) + '" y="' + ly2 + '" font-size="12">关键路径(固定)</text>'; l2 += 170;
  s += '<rect x="' + l2 + '" y="' + (ly2-13) + '" width="16" height="11" fill="#e8c87a" stroke="#5a6e5a" stroke-dasharray="4,3"/><text x="' + (l2+22) + '" y="' + ly2 + '" font-size="12">随机开关门 T#(共9)</text>'; l2 += 190;
  s += '<rect x="' + l2 + '" y="' + (ly2-13) + '" width="16" height="11" fill="#f7f5ef" stroke="#2d2d2d"/><text x="' + (l2+22) + '" y="' + ly2 + '" font-size="12">固定门/洞口</text>'; l2 += 140;
  s += '<circle cx="' + (l2+8) + '" cy="' + (ly2-7) + '" r="6" fill="#e8a33d"/><text x="' + (l2+20) + '" y="' + ly2 + '" font-size="12">光锚</text>'; l2 += 80;
  s += '<text x="' + l2 + '" y="' + ly2 + '" font-size="12">★ 地标</text>';
  s += '<text x="24" y="' + (H - 32) + '" font-size="11" fill="#555">截面语法(美术主导): 走廊=4m宽+桥架/管线压顶(感知2.2–2.4m)+柱距4m节奏 | 枢纽=高净空/挑空+多门可见+零战利品 | 死端房=单一光锚+功能陈设+门框收缩 | 楼梯=井道通高+专属光色 | 毛坯板=无顶柱阵+灯塔导航 | 路口=地面标线+指向牌</text>';
  s += '<text x="24" y="' + (H - 12) + '" font-size="11" fill="#555">v8修订: functionClass与尺寸类解耦(LOBBY/HALL/SALES/SHOWFLAT=枢纽) | 门宽≤墙面50%(4m墙→2.0m: D1/D7/D32/D36) | 枢纽门位偏轴防对穿(D-VAN+3, D33−2) | T1/T4移出大堂枢纽 | T3/T12升为固定门D20/D21(全关连通·V8-C7) | C3廊中节点 | F2视线走廊护航灯塔 | 塌角上空无板(F1天光成立) | VIP/阳台=seed奖励房</text>';
  s += '</svg>';
  fs.writeFileSync(path.join(out, file), s);
  console.log('WROTE', file);
}
render(1, '地球海岸壹号·烂尾预售楼 — F1 平面图 v8(提案·截面语法/枢纽分级·待PM批准)', 'Tower_EarthCoast_01_F1_Plan_v8_proposal.svg');
render(2, '地球海岸壹号·烂尾预售楼 — F2 平面图 v8(提案·样板岛+中庭·截面语法·待PM批准)', 'Tower_EarthCoast_01_F2_Plan_v8_proposal.svg');
