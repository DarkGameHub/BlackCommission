"""
Black Commission - File Warden v2 CONCEPT LINEUP (3 directions, David picks one).

Production-style reference: Lethal Company's *fidelity* — chunky low-poly with
rounded masses (bevel/subdiv), strong readable silhouette, muted palette, one
unsettling accent. Designs are ORIGINAL (no LC creature copied).

  A 「佝偻档案员」 hunched custodian: rounded hunched back, floor-length arms,
     robe fused to the base, deep cowl w/ sunken stamp-red seal eye.
  B 「柜身蜘行者」 cabinet-bodied stalker: the archive cabinet IS the torso,
     four bent stilt legs, drawer-jaw, periscope eye stalk.
  C 「纸躯圣像」 paper effigy: limbless tapered shroud like a reliquary statue,
     layered paper bands, one large seal-stamp face plate. Glides.

v2 render notes: Blender 5.1 headless — find Principled by TYPE (name lookup
fails silently → all-white), bevel for boxy parts (subsurf collapses cubes),
limbs as point-to-point cylinders.

Headless:  blender --background --python concept_file_warden_v2.py
Outputs:   preview/fw2_concept_a.png, fw2_concept_b.png, fw2_concept_c.png
"""

import bpy
import math
from mathutils import Vector, Euler

PREVIEW = "D:/BlackCommission/tools/rigging/preview"

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for c in (bpy.data.meshes, bpy.data.materials, bpy.data.lights, bpy.data.cameras):
    for b in list(c):
        c.remove(b)


def make_mat(name, rgb, rough=0.85, emit=None, estr=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = None
    for n in m.node_tree.nodes:
        if n.type == 'BSDF_PRINCIPLED':
            b = n
            break
    if b:
        b.inputs["Base Color"].default_value = (*rgb, 1.0)
        if "Roughness" in b.inputs:
            b.inputs["Roughness"].default_value = rough
        if emit is not None:
            key = "Emission Color" if "Emission Color" in b.inputs else "Emission"
            b.inputs[key].default_value = (*emit, 1.0)
            if "Emission Strength" in b.inputs:
                b.inputs["Emission Strength"].default_value = estr
    m.diffuse_color = (*rgb, 1.0)
    return m


TEAL = make_mat("Teal", (0.055, 0.09, 0.09), 0.8)
TEAL_DK = make_mat("TealDark", (0.03, 0.05, 0.05), 0.9)
PAPER = make_mat("Paper", (0.55, 0.48, 0.34), 0.95)
ROBE = make_mat("Robe", (0.13, 0.12, 0.09), 0.95)
EYE = make_mat("Eye", (0.62, 0.12, 0.10), 0.4, (1.0, 0.15, 0.08), 9.0)
BRASS = make_mat("Brass", (0.35, 0.27, 0.12), 0.5)

made = []


def smooth(o, mat, subsurf=0, shade=True, bevel=0.0):
    o.data.materials.clear()
    o.data.materials.append(mat)
    if bevel > 0.0:
        mod = o.modifiers.new("bv", 'BEVEL')
        mod.width = bevel
        mod.segments = 3
    elif subsurf > 0:
        mod = o.modifiers.new("ss", 'SUBSURF')
        mod.levels = subsurf
        mod.render_levels = subsurf
    if shade:
        for pl in o.data.polygons:
            pl.use_smooth = True
    made.append(o)
    return o


def box(loc, scale, mat, bevel=0.05, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o = bpy.context.active_object
    o.scale = scale
    o.rotation_euler = Euler([math.radians(a) for a in rot], 'XYZ')
    return smooth(o, mat, 0, True, bevel)


def sphere(loc, scale, mat=None):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3, radius=1, location=loc)
    o = bpy.context.active_object
    o.scale = scale
    return smooth(o, mat if mat is not None else TEAL, 1)


def cone(loc, r1, r2, depth, mat, verts=14, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r2, depth=depth, location=loc)
    o = bpy.context.active_object
    o.rotation_euler = Euler([math.radians(a) for a in rot], 'XYZ')
    return smooth(o, mat, 0)


def limb(a, b, r, mat):
    a, b = Vector(a), Vector(b)
    d = b - a
    bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=r, depth=d.length, location=(a + b) / 2)
    o = bpy.context.active_object
    o.rotation_euler = d.to_track_quat('Z', 'Y').to_euler()
    return smooth(o, mat, 1)


def disc(loc, r, mat, rotx=90):
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=r, depth=0.02, location=loc)
    o = bpy.context.active_object
    o.rotation_euler = Euler((math.radians(rotx), 0, 0), 'XYZ')
    return smooth(o, mat, 0)


def clear_made():
    global made
    bpy.ops.object.select_all(action='DESELECT')
    for o in made:
        o.select_set(True)
    bpy.ops.object.delete(use_global=False)
    made = []


# ── Concept A: hunched custodian ────────────────────────────────────────────
def build_a():
    cone((0, 0, 0.55), 0.55, 0.36, 1.1, ROBE)                          # robe base (grounded)
    sphere((0, 0.10, 1.30), (0.44, 0.38, 0.42), TEAL)                 # torso
    sphere((0, 0.24, 1.66), (0.32, 0.30, 0.30), TEAL_DK)              # hunch over the head
    sphere((0, -0.18, 1.52), (0.17, 0.16, 0.20), TEAL)                # skull tucked low, forward
    cone((0, -0.14, 1.80), 0.26, 0.04, 0.5, ROBE, 12, rot=(16, 0, 0))  # cowl
    disc((0, -0.33, 1.50), 0.055, EYE)                                 # sunken seal eye
    for sx in (-1, 1):
        sphere((sx * 0.40, 0.06, 1.44), (0.14, 0.13, 0.16), TEAL_DK)  # shoulder
        limb((sx * 0.46, 0.02, 1.40), (sx * 0.52, -0.14, 0.42), 0.05, TEAL)  # floor-length arm
        box((sx * 0.53, -0.16, 0.32), (0.08, 0.08, 0.11), BRASS, 0.02)       # stamp fist
    for i, (sx, sy, h) in enumerate(((-0.2, -0.34, 0.5), (0.12, -0.38, 0.4), (0.3, -0.24, 0.45))):
        box((sx, sy, 1.2 - h / 2), (0.045, 0.006, h / 2), PAPER, 0.0, rot=(3, 0, -4 + i * 4))


# ── Concept B: cabinet-bodied stalker ───────────────────────────────────────
def build_b():
    box((0, 0, 1.35), (0.42, 0.34, 0.55), TEAL, 0.08)                  # cabinet torso, rounded edges
    for i, dz in enumerate((1.66, 1.35, 1.04)):
        out = 0.12 if i == 1 else 0.05
        box((0, -0.34 - out, dz), (0.32, 0.08 + out, 0.11), TEAL_DK, 0.03)  # drawer rows (mid ajar)
    box((0.08, -0.52, 1.0), (0.15, 0.11, 0.012), PAPER, 0.0, rot=(-20, 4, 0))  # paper tongue
    cone((0, 0, 2.0), 0.32, 0.2, 0.25, TEAL_DK, 12)                    # shoulder cap
    limb((0, -0.02, 2.05), (0, -0.14, 2.5), 0.035, TEAL)               # eye stalk
    sphere((0, -0.16, 2.52), (0.09, 0.09, 0.09), TEAL_DK)
    disc((0, -0.25, 2.52), 0.055, EYE)                                 # periscope seal eye
    for sx, sy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):                # four bent stilts, connected
        hip = (sx * 0.34, sy * 0.24, 0.92)
        knee = (sx * 0.56, sy * 0.40, 0.52)
        foot = (sx * 0.64, sy * 0.47, 0.04)
        limb(hip, knee, 0.05, TEAL_DK)
        limb(knee, foot, 0.042, TEAL)
        sphere(knee, (0.065, 0.065, 0.065), TEAL_DK)
        sphere((foot[0], foot[1], 0.04), (0.08, 0.08, 0.045), TEAL_DK)


# ── Concept C: paper effigy ─────────────────────────────────────────────────
def build_c():
    cone((0, 0, 1.0), 0.5, 0.22, 2.0, ROBE, 16)                        # tapered shroud body
    sphere((0, 0, 2.1), (0.24, 0.22, 0.3), TEAL_DK)                   # head mass
    cone((0, 0, 2.42), 0.26, 0.02, 0.5, ROBE, 12)                      # spire crown
    for i, z in enumerate((0.45, 0.8, 1.15, 1.5)):                     # layered paper bands
        r = 0.52 - i * 0.075
        bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=r, depth=0.09, location=(0, 0, z))
        band = bpy.context.active_object
        band.rotation_euler = Euler((math.radians(2 + i), 0, math.radians(i * 9)), 'XYZ')
        smooth(band, PAPER, 1)
    box((0, -0.235, 1.98), (0.13, 0.02, 0.16), BRASS, 0.02)            # face plate
    disc((0, -0.27, 1.98), 0.075, EYE)                                 # big seal-stamp face
    for sx in (-1, 1):                                                 # arms folded flush (hunt-only)
        box((sx * 0.34, -0.12, 1.35), (0.04, 0.1, 0.32), TEAL_DK, 0.02, rot=(0, 0, sx * 6))


# ── stage & render ──────────────────────────────────────────────────────────
world = bpy.data.worlds[0]
world.use_nodes = True
world.node_tree.nodes.get("Background").inputs[0].default_value = (0.10, 0.115, 0.125, 1.0)
bpy.ops.object.light_add(type='SUN', location=(3, -4, 6))
k = bpy.context.active_object
k.data.energy = 3.5
k.rotation_euler = Euler((math.radians(55), 0, math.radians(35)), 'XYZ')
bpy.ops.object.light_add(type='AREA', location=(-4, -3, 2.5))
fill = bpy.context.active_object
fill.data.energy = 150.0
fill.data.size = 6.0
bpy.ops.object.light_add(type='SPOT', location=(0, -5, 0.6))
rim = bpy.context.active_object
rim.data.energy = 300.0
rim.data.color = (1.0, 0.45, 0.25)
rim.rotation_euler = Euler((math.radians(80), 0, 0), 'XYZ')

bpy.ops.object.empty_add(location=(0, 0, 1.25))
tgt = bpy.context.active_object
bpy.ops.object.camera_add(location=(1.9, -4.6, 1.5))
cam = bpy.context.active_object
c = cam.constraints.new(type='TRACK_TO')
c.target = tgt
c.track_axis = 'TRACK_NEGATIVE_Z'
c.up_axis = 'UP_Y'
bpy.context.scene.camera = cam

scene = bpy.context.scene
for eng in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE', 'BLENDER_WORKBENCH'):
    try:
        scene.render.engine = eng
        break
    except TypeError:
        continue
scene.render.resolution_x = 720
scene.render.resolution_y = 900

for name, fn in (("fw2_concept_a", build_a), ("fw2_concept_b", build_b), ("fw2_concept_c", build_c)):
    fn()
    scene.render.filepath = f"{PREVIEW}/{name}.png"
    bpy.ops.render.render(write_still=True)
    print(f"[FW2] wrote {name}.png")
    clear_made()

print("[FW2] DONE")
