"""
Black Commission - Echo Mold procedural model (DECEPTIVE form, v2).

ORIGINAL fungal creature (NOT a copy of any Lethal Company monster).
Design: looks like a mundane oversized fungus when idle; in Hunt the cap
splays open (segmented petals on hinges) over a dark throat with a sodium-amber
glow. Same mesh, driven by bones later -> consistent with GDD "no transformation".

Tone: Municipal Debt Noir - dead-rubber teal-black, aged ochre, amber #FF6A00.

Headless:  blender --background --python build_echo_mold.py
Outputs:   preview/echo_mold_idle_front.png, _idle_side.png, _hunt_front.png
           output/echo_mold_base.blend
"""

import bpy
import math
import mathutils
from mathutils import Vector, Euler

PREVIEW = "D:/BlackCommission/tools/rigging/preview"
OUTPUT = "D:/BlackCommission/tools/rigging/output"

# ---- reset ----------------------------------------------------------------
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for c in (bpy.data.meshes, bpy.data.materials, bpy.data.lights, bpy.data.cameras):
    for b in list(c):
        c.remove(b)


# ---- materials ------------------------------------------------------------
def make_mat(name, base_rgb, rough=0.9, emit_rgb=None, emit_strength=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*base_rgb, 1.0)
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = rough
        if emit_rgb is not None:
            key = "Emission Color" if "Emission Color" in bsdf.inputs else "Emission"
            bsdf.inputs[key].default_value = (*emit_rgb, 1.0)
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = emit_strength
    mat.diffuse_color = (*base_rgb, 1.0)
    return mat


MAT_STALK = make_mat("EM_Stalk", (0.055, 0.085, 0.085), 0.92)
MAT_CAP = make_mat("EM_Cap", (0.34, 0.27, 0.14), 0.88)
MAT_THROAT = make_mat("EM_Throat", (0.06, 0.05, 0.05), 0.95)
MAT_TENDRIL = make_mat("EM_Tendril", (0.10, 0.10, 0.09), 0.9)
EYE_BSDF_STRENGTH = 6.0
MAT_EYE = make_mat("EM_Eye", (1.0, 0.42, 0.0), 0.4, (1.0, 0.42, 0.0), EYE_BSDF_STRENGTH)

PARTS = []


def finish(obj, mat, smooth=True):
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    if smooth:
        for p in obj.data.polygons:
            p.use_smooth = True
    PARTS.append(obj)
    return obj


# ---- body -----------------------------------------------------------------
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.30, location=(0, 0, 0.14))
b = bpy.context.active_object; b.name = "EM_Base"; b.scale = (1.1, 1.1, 0.55)
finish(b, MAT_STALK)

for i in range(6):
    a = math.radians(60 * i + 15)
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.05, radius2=0.0,
                                    depth=0.42, location=(math.cos(a) * 0.24, math.sin(a) * 0.24, 0.08))
    t = bpy.context.active_object; t.name = f"EM_Root_{i}"
    t.rotation_euler = Euler((math.radians(115), 0, a + math.pi / 2), 'XYZ')
    finish(t, MAT_TENDRIL)

bpy.ops.mesh.primitive_cone_add(vertices=10, radius1=0.165, radius2=0.10,
                                depth=1.25, location=(0, 0, 0.78))
finish(bpy.context.active_object, MAT_STALK).name = "EM_Stalk"

# subtle shelf fungus (keeps the mundane read)
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.15, location=(-0.12, -0.02, 0.86))
sh = bpy.context.active_object; sh.name = "EM_Shelf"; sh.scale = (1.3, 1.0, 0.4)
sh.rotation_euler = Euler((0, math.radians(-20), 0), 'XYZ')
finish(sh, MAT_CAP)

# dark throat core + amber eye (hidden when cap closed, revealed in Hunt)
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.19, location=(0, 0, 1.50))
core = bpy.context.active_object; core.name = "EM_Throat"; core.scale = (1.0, 1.0, 0.85)
finish(core, MAT_THROAT)

bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, radius=0.075, location=(0, -0.12, 1.52))
eye = finish(bpy.context.active_object, MAT_EYE); eye.name = "EM_Eye"

# ---- cap petals (6), hinge ring at top of stalk ---------------------------
HINGE_Z, HINGE_R, PETAL_L = 1.40, 0.31, 0.46
petals = []
for i in range(6):
    az = math.radians(60 * i)
    bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=0.18, radius2=0.015, depth=PETAL_L)
    p = bpy.context.active_object; p.name = f"EM_Petal_{i}"
    p.scale = (1.3, 0.30, 1.0)
    finish(p, MAT_CAP)
    petals.append((p, az))


def pose_petal(p, az, elev_deg, inward):
    """Place petal hinged at the ring; inward<0 closes (bud), >0 splays open."""
    e = math.radians(elev_deg)
    hinge = Vector((math.cos(az) * HINGE_R, math.sin(az) * HINGE_R, HINGE_Z))
    d = Vector((math.cos(az) * math.cos(e) * inward,
                math.sin(az) * math.cos(e) * inward,
                math.sin(e)))
    p.location = hinge + d * (PETAL_L / 2.0)
    p.rotation_euler = d.to_track_quat('Z', 'Y').to_euler()


def set_cap(open_state):
    for p, az in petals:
        if open_state:
            pose_petal(p, az, -18, 1.0)     # splayed out & down
        else:
            pose_petal(p, az, 46, -1.0)     # closed / solid-looking cap (flatter)


# arm tendrils (tucked down when idle, reach forward in hunt)
arms = []
for sx in (-1, 1):
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.05, radius2=0.0,
                                    depth=0.7, location=(0.12 * sx, 0.02, 1.0))
    arm = bpy.context.active_object; arm.name = f"EM_Arm_{sx}"
    finish(arm, MAT_TENDRIL); arms.append((arm, sx))


def set_arms(hunt):
    for arm, sx in arms:
        if hunt:
            # reach forward (-Y) and slightly down, toward prey
            arm.location = (0.12 * sx, -0.12, 1.12)
            arm.rotation_euler = Euler((math.radians(100), 0, math.radians(18 * sx)), 'XYZ')
        else:
            # hidden: hang straight down behind the stalk (+Y, away from front cam)
            arm.location = (0.05 * sx, 0.13, 0.80)
            arm.rotation_euler = Euler((math.radians(180), 0, 0), 'XYZ')


# ---- lighting / world -----------------------------------------------------
world = bpy.data.worlds[0] if bpy.data.worlds else bpy.data.worlds.new("W")
bpy.context.scene.world = world; world.use_nodes = True
bg = world.node_tree.nodes.get("Background")
if bg:
    bg.inputs[0].default_value = (0.16, 0.18, 0.20, 1.0)

bpy.ops.object.light_add(type='SUN', location=(3, -4, 6))
k = bpy.context.active_object; k.data.energy = 3.2
k.rotation_euler = Euler((math.radians(55), 0, math.radians(35)), 'XYZ')
bpy.ops.object.light_add(type='AREA', location=(-4, -3, 2.5))
bpy.context.active_object.data.energy = 120.0
bpy.context.active_object.data.size = 5.0

# ---- camera ---------------------------------------------------------------
CENTER_Z = 0.98
bpy.ops.object.empty_add(location=(0, 0, CENTER_Z)); tgt = bpy.context.active_object
bpy.ops.object.camera_add(location=(0, -6, CENTER_Z)); cam = bpy.context.active_object
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.5
con = cam.constraints.new(type='TRACK_TO'); con.target = tgt
con.track_axis = 'TRACK_NEGATIVE_Z'; con.up_axis = 'UP_Y'
bpy.context.scene.camera = cam

scene = bpy.context.scene
for eng in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE', 'BLENDER_WORKBENCH'):
    try:
        scene.render.engine = eng; break
    except TypeError:
        continue
print(f"[ECHO_MOLD] engine={scene.render.engine}")
if scene.render.engine == 'BLENDER_WORKBENCH':
    scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_cavity = True
scene.render.resolution_x = 640
scene.render.resolution_y = 900


def render(path, loc):
    cam.location = loc; scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print(f"[ECHO_MOLD] wrote {path}")


# IDLE (deceptive: closed cap, tucked arms, dim eye)
set_cap(False); set_arms(False)
def set_eye(strength):
    bsdf = MAT_EYE.node_tree.nodes.get("Principled BSDF")
    if bsdf and "Emission Strength" in bsdf.inputs:
        bsdf.inputs["Emission Strength"].default_value = strength
set_eye(1.2)
render(f"{PREVIEW}/echo_mold_idle_front.png", (0, -6, CENTER_Z))
render(f"{PREVIEW}/echo_mold_idle_side.png", (6, 0, CENTER_Z))

# HUNT reveal (cap splayed, arms forward, eye glaring)
set_cap(True); set_arms(True); set_eye(9.0)
render(f"{PREVIEW}/echo_mold_hunt_front.png", (0, -6, CENTER_Z))

# leave the file in IDLE pose for the rig pass
set_cap(False); set_arms(False); set_eye(EYE_BSDF_STRENGTH)
bpy.ops.wm.save_as_mainfile(filepath=f"{OUTPUT}/echo_mold_base.blend")

# bbox report
mins = Vector((1e9,) * 3); maxs = Vector((-1e9,) * 3); vt = 0
for o in PARTS:
    vt += len(o.data.vertices)
    for cc in o.bound_box:
        w = o.matrix_world @ Vector(cc)
        for j in range(3):
            mins[j] = min(mins[j], w[j]); maxs[j] = max(maxs[j], w[j])
dd = maxs - mins
print(f"[ECHO_MOLD] parts={len(PARTS)} verts={vt} bbox W={dd.x:.2f} D={dd.y:.2f} H={dd.z:.2f}")
print("[ECHO_MOLD] DONE")
