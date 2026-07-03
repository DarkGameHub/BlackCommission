"""File Warden concept B HERO PASS - LC-fidelity push, original design.
Recipe applied: chunky rounded masses, exaggerated proportions (small heavy body,
very long thin stilts), minimal face (one glowing seal eye), smooth shading,
muted 2-tone palette + one accent, one 'wrong' detail (drawer jaw + paper tongue).
Staged render: ground plane, low 3/4 camera, rim light, contact shadow.
"""
import bpy, math
from mathutils import Vector, Euler

PREVIEW = "D:/BlackCommission/tools/rigging/preview"
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
for c in (bpy.data.meshes, bpy.data.materials, bpy.data.lights, bpy.data.cameras):
    for b in list(c): c.remove(b)

def mat(name, rgb, rough=0.85, emit=None, estr=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if b:
        b.inputs["Base Color"].default_value = (*rgb, 1.0)
        b.inputs["Roughness"].default_value = rough
        if emit:
            key = "Emission Color" if "Emission Color" in b.inputs else "Emission"
            b.inputs[key].default_value = (*emit, 1.0)
            b.inputs["Emission Strength"].default_value = estr
    return m

TEAL   = mat("Teal",   (0.075, 0.115, 0.115), 0.75)
TEALDK = mat("TealDk", (0.045, 0.07, 0.07), 0.85)
PAPER  = mat("Paper",  (0.58, 0.50, 0.35), 0.95)
EYE    = mat("Eye",    (0.62, 0.12, 0.10), 0.4, (1.0, 0.16, 0.08), 12.0)
BRASS  = mat("Brass",  (0.38, 0.30, 0.14), 0.45)
GROUND = mat("Ground", (0.09, 0.10, 0.10), 0.95)

def S(o, m, sub=0, bev=0.0):
    o.data.materials.clear(); o.data.materials.append(m)
    if bev > 0:
        md = o.modifiers.new("bv", 'BEVEL'); md.width = bev; md.segments = 4
        md2 = o.modifiers.new("ss", 'SUBSURF'); md2.levels = 1; md2.render_levels = 1
    elif sub > 0:
        md = o.modifiers.new("ss", 'SUBSURF'); md.levels = sub; md.render_levels = sub
    for pl in o.data.polygons: pl.use_smooth = True
    return o

def box(loc, sc, m, bev=0.06, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(location=loc); o = bpy.context.active_object
    o.scale = sc; o.rotation_euler = Euler([math.radians(a) for a in rot], 'XYZ')
    return S(o, m, 0, bev)

def sph(loc, sc, m):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3, radius=1, location=loc)
    o = bpy.context.active_object; o.scale = sc; return S(o, m, 1)

def limb(a, b, r1, r2, m):
    a, b = Vector(a), Vector(b); d = b - a; n = 6
    for i in range(n):
        t0, t1 = i/n, (i+1)/n
        p0, p1 = a.lerp(b, t0), a.lerp(b, t1)
        r = r1 + (r2 - r1) * (t0 + t1) / 2
        seg = p1 - p0
        bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=r, depth=seg.length*1.15, location=(p0+p1)/2)
        o = bpy.context.active_object
        o.rotation_euler = seg.to_track_quat('Z','Y').to_euler()
        S(o, m, 1)

# body: heavy rounded cabinet low over the ground between tall stilts
body = box((0, 0, 1.55), (0.46, 0.38, 0.6), TEAL, 0.12)
body.rotation_euler = Euler((math.radians(-6), 0, 0), 'XYZ')   # leans toward prey
# drawers: top shut, middle AJAR (the jaw), bottom shut
box((0, -0.40, 1.86), (0.34, 0.06, 0.115), TEALDK, 0.03, rot=(-6,0,0))
box((0, -0.50, 1.52), (0.34, 0.16, 0.12), TEALDK, 0.03, rot=(-6,0,0))
box((0, -0.42, 1.18), (0.34, 0.07, 0.115), TEALDK, 0.03, rot=(-6,0,0))
# paper tongue lolling from the ajar drawer
box((0.06, -0.66, 1.32), (0.16, 0.13, 0.014), PAPER, 0.0, rot=(-38, 3, 0))
box((0.02, -0.72, 1.18), (0.12, 0.09, 0.012), PAPER, 0.0, rot=(-52, -4, 0))
# brass handle pulls
for dz, rx in ((1.86,-6),(1.18,-6)):
    box((0, -0.475, dz), (0.16, 0.018, 0.018), BRASS, 0.01, rot=(rx,0,0))
box((0, -0.68, 1.52), (0.16, 0.018, 0.018), BRASS, 0.01, rot=(-6,0,0))
# hood cap + short neck + seal eye lantern
box((0, 0.02, 2.28), (0.34, 0.28, 0.12), TEALDK, 0.08, rot=(-8,0,0))
limb((0, -0.06, 2.34), (0, -0.22, 2.72), 0.045, 0.03, TEAL)
sph((0, -0.26, 2.78), (0.115, 0.115, 0.115), TEALDK)
bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=0.062, depth=0.03, location=(0, -0.37, 2.78))
e = bpy.context.active_object; e.rotation_euler = Euler((math.radians(90),0,0),'XYZ'); S(e, EYE, 0)
# four VERY long thin stilts (exaggerated proportion = the LC move)
for sx, sy in ((-1,-1),(1,-1),(-1,1),(1,1)):
    hip  = (sx*0.36, sy*0.26, 1.35)
    knee = (sx*0.72, sy*0.50, 1.75)     # knees ABOVE the body - spider read
    foot = (sx*0.88, sy*0.62, 0.02)
    limb(hip, knee, 0.055, 0.045, TEALDK)
    limb(knee, foot, 0.045, 0.025, TEAL)
    sph(knee, (0.075,)*3, TEALDK)
    sph((foot[0], foot[1], 0.03), (0.08, 0.08, 0.04), TEALDK)
# ground plane + staging
bpy.ops.mesh.primitive_plane_add(size=30, location=(0,0,0))
S(bpy.context.active_object, GROUND, 0)

world = bpy.data.worlds[0]; world.use_nodes = True
world.node_tree.nodes.get("Background").inputs[0].default_value = (0.055, 0.065, 0.075, 1.0)
bpy.ops.object.light_add(type='AREA', location=(2.5, -3.5, 4)); k = bpy.context.active_object
k.data.energy = 900; k.data.size = 4; k.rotation_euler = Euler((math.radians(40),0,math.radians(30)),'XYZ')
bpy.ops.object.light_add(type='SPOT', location=(-3.5, 2.5, 2.2)); r = bpy.context.active_object
r.data.energy = 1600; r.data.color = (1.0, 0.5, 0.28); r.data.spot_size = math.radians(70)
r.rotation_euler = Euler((math.radians(65), 0, math.radians(-125)), 'XYZ')
bpy.ops.object.light_add(type='POINT', location=(0, -1.4, 1.2)); f = bpy.context.active_object
f.data.energy = 60; f.data.color = (1.0, 0.3, 0.2)

bpy.ops.object.empty_add(location=(0, 0, 1.45)); tgt = bpy.context.active_object
bpy.ops.object.camera_add(location=(2.6, -3.6, 1.05)); cam = bpy.context.active_object
cam.data.lens = 42
c = cam.constraints.new(type='TRACK_TO'); c.target = tgt
c.track_axis = 'TRACK_NEGATIVE_Z'; c.up_axis = 'UP_Y'
bpy.context.scene.camera = cam
scene = bpy.context.scene
for eng in ('BLENDER_EEVEE_NEXT','BLENDER_EEVEE','BLENDER_WORKBENCH'):
    try: scene.render.engine = eng; break
    except TypeError: continue
scene.render.resolution_x = 900; scene.render.resolution_y = 900
scene.render.filepath = f"{PREVIEW}/fw2_concept_b_hero.png"
bpy.ops.render.render(write_still=True)
print("[HERO] done")
