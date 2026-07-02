"""
Black Commission - File Warden (档案看守) one-shot build: model + rigid-part
hierarchy + 4 baked clips + Unity FBX export.

ORIGINAL bureaucratic horror (NOT a copy of any Lethal Company monster).
A gaunt 2.2 m archive custodian: a dead-teal filing-cabinet ribcage with ajar
drawers and paper spill, a tattered aged-paper robe skirt (drags the floor -> no
legs, glides on the NavMesh), a hooded cowl with ONE stamp-red seal eye, and long
arms ending in stamp-block fists. Tone: Municipal Debt Noir - civic teal-black,
aged paper, stamp red #9E1F1A glow.

Clip frame ranges (mirrors the EchoMold pipeline; split in Unity import):
  FW_Idle    1 - 96   loop  (slow scan sway, head sweep)
  FW_Hunt  101 - 196  loop  (lean forward, arms raised, urgent sway)
  FW_Attack 201 - 224 once  (double stamp-slam)
  FW_Death  231 - 286 once  (shudder -> collapse forward, sink)

Headless:  blender --background --python build_file_warden.py
Outputs:   output/FileWarden.fbx, output/FileWarden_clips.json,
           preview/fw_idle.png, fw_hunt.png, fw_attack.png, fw_death.png
"""

import bpy
import json
import math
from mathutils import Vector, Euler

PREVIEW = "D:/BlackCommission/tools/rigging/preview"
OUTPUT = "D:/BlackCommission/tools/rigging/output"

# ---- reset ------------------------------------------------------------------
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for c in (bpy.data.meshes, bpy.data.materials, bpy.data.lights, bpy.data.cameras):
    for b in list(c):
        c.remove(b)


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


MAT_METAL = make_mat("FW_Metal", (0.045, 0.075, 0.075), 0.85)   # civic teal-black
MAT_DRAWER = make_mat("FW_Drawer", (0.09, 0.13, 0.13), 0.8)
MAT_PAPER = make_mat("FW_Paper", (0.55, 0.48, 0.34), 0.95)      # aged paper
MAT_ROBE = make_mat("FW_Robe", (0.16, 0.14, 0.10), 0.95)        # filthy paper-cloth
MAT_EYE = make_mat("FW_Eye", (0.62, 0.12, 0.10), 0.4, (1.0, 0.16, 0.10), 7.0)  # stamp red
MAT_FIST = make_mat("FW_Fist", (0.30, 0.08, 0.07), 0.7)

BODY_PARTS, HEAD_PARTS = [], []
ARM_PARTS = {-1: [], 1: []}


def part(obj, mat, bucket, smooth=False):
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    if smooth:
        for p in obj.data.polygons:
            p.use_smooth = True
    bucket.append(obj)
    return obj


# ---- body: cabinet ribcage + drawers + robe skirt + paper strips (front = -Y) --
bpy.ops.mesh.primitive_cube_add(location=(0, 0, 1.25))
torso = bpy.context.active_object; torso.name = "FW_Torso"
torso.scale = (0.30, 0.24, 0.50)
part(torso, MAT_METAL, BODY_PARTS)

# three ajar drawers on the front face (-Y), staggered
for i, (dz, out) in enumerate(((1.52, 0.10), (1.25, 0.16), (0.98, 0.06))):
    bpy.ops.mesh.primitive_cube_add(location=(0, -0.24 - out * 0.5, dz))
    d = bpy.context.active_object; d.name = f"FW_Drawer_{i}"
    d.scale = (0.24, 0.10 + out * 0.5, 0.10)
    part(d, MAT_DRAWER, BODY_PARTS)
    # paper spilling out of the middle drawer
    if i == 1:
        bpy.ops.mesh.primitive_cube_add(location=(0.05, -0.40, dz + 0.06))
        pp = bpy.context.active_object; pp.name = "FW_Spill"
        pp.scale = (0.14, 0.12, 0.012)
        pp.rotation_euler = Euler((math.radians(-14), math.radians(6), 0), 'XYZ')
        part(pp, MAT_PAPER, BODY_PARTS)

# tattered robe skirt: flared cone from waist to floor (glide skirt — hides "no legs")
bpy.ops.mesh.primitive_cone_add(vertices=10, radius1=0.42, radius2=0.26, depth=0.78,
                                location=(0, 0, 0.39))
skirt = bpy.context.active_object; skirt.name = "FW_Skirt"
part(skirt, MAT_ROBE, BODY_PARTS, smooth=True)

# shoulder yoke
bpy.ops.mesh.primitive_cube_add(location=(0, 0, 1.72))
yoke = bpy.context.active_object; yoke.name = "FW_Yoke"
yoke.scale = (0.40, 0.20, 0.07)
part(yoke, MAT_METAL, BODY_PARTS)

# hanging paper strips (front + sides)
for i, (sx, sy, h) in enumerate(((-0.22, -0.26, 0.55), (0.10, -0.28, 0.42),
                                 (0.30, -0.18, 0.50), (-0.34, -0.05, 0.38))):
    bpy.ops.mesh.primitive_cube_add(location=(sx, sy, 1.55 - h * 0.5))
    st = bpy.context.active_object; st.name = f"FW_Strip_{i}"
    st.scale = (0.045, 0.006, h * 0.5)
    st.rotation_euler = Euler((math.radians(3), 0, math.radians(-6 + i * 4)), 'XYZ')
    part(st, MAT_PAPER, BODY_PARTS)

# ---- head: hooded cowl + single seal eye (child of body) ---------------------
bpy.ops.mesh.primitive_cone_add(vertices=12, radius1=0.20, radius2=0.05, depth=0.42,
                                location=(0, 0.02, 2.06))
cowl = bpy.context.active_object; cowl.name = "FW_Cowl"
cowl.rotation_euler = Euler((math.radians(8), 0, 0), 'XYZ')
part(cowl, MAT_ROBE, HEAD_PARTS, smooth=True)

bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.14, location=(0, -0.02, 1.94))
skull = bpy.context.active_object; skull.name = "FW_Skull"
skull.scale = (1.0, 0.9, 1.1)
part(skull, MAT_METAL, HEAD_PARTS, smooth=True)

bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.055, depth=0.03,
                                    location=(0, -0.155, 1.94))
eye = bpy.context.active_object; eye.name = "FW_Eye"
eye.rotation_euler = Euler((math.radians(90), 0, 0), 'XYZ')
part(eye, MAT_EYE, HEAD_PARTS)

# ---- arms: long hanging arms with stamp-block fists (children of pivots) -----
for sx in (-1, 1):
    bpy.ops.mesh.primitive_cube_add(location=(sx * 0.40, 0, 1.30))
    arm = bpy.context.active_object; arm.name = f"FW_Arm_{sx}"
    arm.scale = (0.055, 0.055, 0.34)
    part(arm, MAT_METAL, ARM_PARTS[sx])

    bpy.ops.mesh.primitive_cube_add(location=(sx * 0.40, 0, 0.90))
    fist = bpy.context.active_object; fist.name = f"FW_Fist_{sx}"
    fist.scale = (0.11, 0.11, 0.09)
    part(fist, MAT_FIST, ARM_PARTS[sx])


# ---- join into rig groups, set pivots ----------------------------------------
def join(objs, new_name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    objs[0].name = new_name
    return objs[0]


def set_origin(o, loc):
    bpy.context.scene.cursor.location = Vector(loc)
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')


body = join(BODY_PARTS, "FileWarden_Body")
head = join(HEAD_PARTS, "FileWarden_Head")
arm_l = join(ARM_PARTS[-1], "FileWarden_Arm_L")
arm_r = join(ARM_PARTS[1], "FileWarden_Arm_R")

set_origin(body, (0, 0, 0.55))    # waist pivot → lean/sway
set_origin(head, (0, 0, 1.82))    # neck pivot
set_origin(arm_l, (-0.40, 0, 1.64))  # shoulder pivots
set_origin(arm_r, (0.40, 0, 1.64))

root = bpy.data.objects.new("FileWarden", None)
root.empty_display_size = 0.15
bpy.context.collection.objects.link(root)

body.parent = root
for o in (head, arm_l, arm_r):
    o.parent = body
    o.matrix_parent_inverse = body.matrix_world.inverted()

for o in (body, head, arm_l, arm_r):
    o.rotation_mode = 'XYZ'

# ---- bake the 4 clips ---------------------------------------------------------
scene = bpy.context.scene
scene.frame_start = 1
scene.frame_end = 286
BODY_Z = body.location.z


def key(frame, body_yaw=0.0, body_pitch=0.0, body_z=0.0,
        head_yaw=0.0, head_pitch=0.0, arm=0.0, arm_asym=0.0):
    """Pose everything (degrees) and keyframe at frame. arm = shoulder X-rotation
    (negative raises the arms forward toward -Y); arm_asym staggers L vs R."""
    scene.frame_set(frame)
    body.rotation_euler = Euler((math.radians(body_pitch), 0, math.radians(body_yaw)), 'XYZ')
    body.location = Vector((0, 0, BODY_Z + body_z))
    head.rotation_euler = Euler((math.radians(head_pitch), 0, math.radians(head_yaw)), 'XYZ')
    arm_l.rotation_euler = Euler((math.radians(arm + arm_asym), 0, 0), 'XYZ')
    arm_r.rotation_euler = Euler((math.radians(arm - arm_asym), 0, 0), 'XYZ')
    for o in (body, head, arm_l, arm_r):
        o.keyframe_insert(data_path="rotation_euler", frame=frame)
    body.keyframe_insert(data_path="location", frame=frame)


# FW_Idle 1-96 loop: weary scan — body sway, head sweep, arms dead-hang
key(1,  body_yaw=-5, body_z=0.00, head_yaw=22, arm=0)
key(24, body_yaw=0,  body_z=0.015, head_yaw=8, arm=-2)
key(48, body_yaw=5,  body_z=0.00, head_yaw=-22, arm=0)
key(72, body_yaw=0,  body_z=0.015, head_yaw=-6, arm=-2)
key(96, body_yaw=-5, body_z=0.00, head_yaw=22, arm=0)

# FW_Hunt 101-196 loop: locked on — lean forward, arms half-raised, urgent sway
key(101, body_pitch=-13, body_yaw=-4, head_pitch=6, arm=-58, arm_asym=6)
key(125, body_pitch=-15, body_yaw=3, body_z=0.02, head_pitch=8, arm=-64, arm_asym=-6)
key(148, body_pitch=-13, body_yaw=5, head_pitch=6, arm=-58, arm_asym=6)
key(172, body_pitch=-15, body_yaw=-3, body_z=0.02, head_pitch=8, arm=-64, arm_asym=-6)
key(196, body_pitch=-13, body_yaw=-4, head_pitch=6, arm=-58, arm_asym=6)

# FW_Attack 201-224 once: wind up high, double stamp-slam, settle
key(201, body_pitch=-12, head_pitch=4, arm=-60)
key(208, body_pitch=6, body_z=0.05, head_pitch=-8, arm=-135)   # wind up, rear back
key(214, body_pitch=-26, body_z=-0.03, head_pitch=14, arm=-6)  # SLAM
key(219, body_pitch=-20, head_pitch=10, arm=-18)
key(224, body_pitch=-13, head_pitch=6, arm=-58)

# FW_Death 231-286 once: shudder, then collapse forward and sink into the robe
key(231, body_yaw=0, arm=-20)
key(238, body_yaw=10, head_yaw=18, arm=-30)
key(244, body_yaw=-12, head_yaw=-20, arm=-10)
key(250, body_yaw=8, head_yaw=10, arm=-24)
key(262, body_pitch=-55, body_z=-0.12, head_pitch=25, arm=-40)
key(274, body_pitch=-82, body_z=-0.30, head_pitch=32, arm=-52)
key(286, body_pitch=-85, body_z=-0.34, head_pitch=32, arm=-52)

# ---- save + export -------------------------------------------------------------
bpy.ops.wm.save_as_mainfile(filepath=f"{OUTPUT}/file_warden_animated.blend")

bpy.ops.object.select_all(action='SELECT')
fbx_path = f"{OUTPUT}/FileWarden.fbx"
bpy.ops.export_scene.fbx(
    filepath=fbx_path,
    use_selection=True,
    object_types={'EMPTY', 'MESH'},
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_NONE',
    axis_forward='-Z', axis_up='Y',
    mesh_smooth_type='FACE',
    add_leaf_bones=False,
    bake_anim=True,
    bake_anim_use_all_actions=False,
    bake_anim_use_nla_strips=False,
    bake_anim_step=1.0,
    bake_anim_simplify_factor=0.0,
)
print(f"[FW] exported {fbx_path}")

clips = {
    "fbx": "FileWarden.fbx",
    "rig": "Generic",
    "clips": [
        {"name": "FW_Idle", "start": 1, "end": 96, "loop": True},
        {"name": "FW_Hunt", "start": 101, "end": 196, "loop": True},
        {"name": "FW_Attack", "start": 201, "end": 224, "loop": False},
        {"name": "FW_Death", "start": 231, "end": 286, "loop": False},
    ],
}
with open(f"{OUTPUT}/FileWarden_clips.json", "w") as fh:
    json.dump(clips, fh, indent=2)
print("[FW] wrote FileWarden_clips.json")

# ---- preview renders ------------------------------------------------------------
world = bpy.data.worlds[0]
world.use_nodes = True
world.node_tree.nodes.get("Background").inputs[0].default_value = (0.16, 0.18, 0.20, 1.0)
bpy.ops.object.light_add(type='SUN', location=(3, -4, 6))
k = bpy.context.active_object; k.data.energy = 3.2
k.rotation_euler = Euler((math.radians(55), 0, math.radians(35)), 'XYZ')
bpy.ops.object.light_add(type='AREA', location=(-4, -3, 2.5))
bpy.context.active_object.data.energy = 120.0
bpy.context.active_object.data.size = 5.0
CZ = 1.15
bpy.ops.object.empty_add(location=(0, 0, CZ)); tgt = bpy.context.active_object
bpy.ops.object.camera_add(location=(0, -6, CZ)); cam = bpy.context.active_object
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 3.0
c = cam.constraints.new(type='TRACK_TO'); c.target = tgt
c.track_axis = 'TRACK_NEGATIVE_Z'; c.up_axis = 'UP_Y'
bpy.context.scene.camera = cam
for eng in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE', 'BLENDER_WORKBENCH'):
    try:
        scene.render.engine = eng; break
    except TypeError:
        continue
scene.render.resolution_x = 640
scene.render.resolution_y = 900

for fr, name in ((48, "fw_idle"), (125, "fw_hunt"), (214, "fw_attack"), (286, "fw_death")):
    scene.frame_set(fr)
    scene.render.filepath = f"{PREVIEW}/{name}.png"
    bpy.ops.render.render(write_still=True)
    print(f"[FW] wrote {name}.png @f{fr}")

print("[FW] DONE")
