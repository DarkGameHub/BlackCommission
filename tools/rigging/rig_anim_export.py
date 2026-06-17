"""
Black Commission - Echo Mold rig pass 2: bake 4 animations onto the rigid-part
hierarchy and export a single Unity-ready FBX (Generic rig). Locomotion is NOT
a clip - the NavMesh moves the root; Idle/Hunt play in place.

Clip frame ranges (split these in Unity's FBX import 'Clips'):
  EM_Idle    1 - 96    loop   (closed cap, gentle sway, faint breathe)
  EM_Hunt   101 - 196  loop   (cap open, arms forward, aggressive sway)
  EM_Attack 201 - 224  once   (wind back -> lunge -> settle)
  EM_Death  231 - 286  once   (shudder -> collapse forward -> wilt)

Headless:  blender --background --python rig_anim_export.py
Outputs:   output/EchoMold.fbx, output/EchoMold_clips.json,
           preview/anim_attack.png, preview/anim_death.png
"""

import bpy
import json
import math
from mathutils import Vector, Euler, Quaternion, Matrix

OUTPUT = "D:/BlackCommission/tools/rigging/output"
PREVIEW = "D:/BlackCommission/tools/rigging/preview"

bpy.ops.wm.open_mainfile(filepath=f"{OUTPUT}/echo_mold_rigged.blend")


def obj(n):
    return bpy.data.objects.get(n)


root = obj("EchoMold")
stalk = obj("EchoMold_Stalk")
head = obj("EchoMold_Head")
petal_pivots = []
HINGE_R, HINGE_Z = 0.31, 1.40
for i in range(6):
    az = math.radians(60 * i)
    tangent = Vector((-math.sin(az), math.cos(az), 0.0))
    Y = Vector((0, 0, 1)).cross(tangent).normalized()
    Z = tangent.cross(Y).normalized()
    rest_q = Matrix((tangent, Y, Z)).transposed().to_quaternion()
    piv = obj(f"EchoMold_PetalPiv_{i}")
    piv.rotation_mode = 'QUATERNION'
    petal_pivots.append((piv, rest_q))
arm_pivots = {-1: obj("EchoMold_ArmPiv_L"), 1: obj("EchoMold_ArmPiv_R")}
for p in arm_pivots.values():
    p.rotation_mode = 'XYZ'
stalk.rotation_mode = 'XYZ'
head.rotation_mode = 'XYZ'

THETA_OPEN = math.radians(-152)
ARM_DEPLOY = math.radians(-95)


def set_pose(openness, deploy, head_pitch, tiltx, tilty):
    for (piv, rest_q) in petal_pivots:
        piv.rotation_quaternion = rest_q @ Quaternion((1, 0, 0), openness * THETA_OPEN)
    for sx, piv in arm_pivots.items():
        piv.rotation_euler = Euler((deploy * ARM_DEPLOY, 0, 0), 'XYZ')
    head.rotation_euler = Euler((head_pitch, 0, 0), 'XYZ')
    stalk.rotation_euler = Euler((tiltx, tilty, 0), 'XYZ')


def key(f, openness, deploy, head_deg, tiltx_deg, tilty_deg):
    set_pose(openness, deploy, math.radians(head_deg),
             math.radians(tiltx_deg), math.radians(tilty_deg))
    for (piv, _) in petal_pivots:
        piv.keyframe_insert("rotation_quaternion", frame=f)
    for piv in arm_pivots.values():
        piv.keyframe_insert("rotation_euler", frame=f)
    head.keyframe_insert("rotation_euler", frame=f)
    stalk.keyframe_insert("rotation_euler", frame=f)


# ---- EM_Idle  (1-96 loop): closed, gentle sway + faint breathe -------------
key(1,  0.00, 0.0, 0,  2.0,  0.0)
key(24, 0.03, 0.0, 1,  1.0,  1.5)
key(48, 0.00, 0.0, 0,  2.5,  0.0)
key(72, 0.03, 0.0, -1, 1.0, -1.5)
key(96, 0.00, 0.0, 0,  2.0,  0.0)

# ---- EM_Hunt  (101-196 loop): open, arms out, aggressive sway --------------
key(101, 1.00, 1.00, -12, 0.0,  3.0)
key(130, 0.96, 0.92, -10, -2.0, 0.0)
key(160, 1.00, 1.00, -14, 0.0, -3.0)
key(196, 1.00, 1.00, -12, 0.0,  3.0)

# ---- EM_Attack (201-224 once): wind back -> lunge -> settle -----------------
key(201, 1.00, 1.00, -12, 0.0,  0.0)   # = hunt
key(206, 0.95, 0.80, -8,  8.0,  0.0)   # wind back
key(212, 1.06, 1.15, -18, -27.0, 0.0)  # LUNGE forward (-X tilt = toward -Y prey)
key(217, 1.04, 1.10, -16, -22.0, 0.0)  # contact hold
key(224, 1.00, 1.00, -12, 0.0,  0.0)   # settle back toward hunt

# ---- EM_Death (231-286 once): shudder -> collapse forward -> wilt ----------
key(231, 1.00, 1.00, -12, 0.0,  0.0)
key(245, 0.80, 0.90, -6,  5.0,  3.0)   # shudder
key(262, 0.40, 0.35, 18, -42.0, 6.0)   # buckle forward, head flops
key(286, 0.12, 0.10, 32, -68.0, 4.0)   # wilted, folded toward ground

bpy.context.scene.frame_start = 1
bpy.context.scene.frame_end = 286

# linear-ish loops: leave default bezier (fine for organic sway)

# save an animated .blend so the GUI can scrub all 4 clips
bpy.ops.wm.save_as_mainfile(filepath=f"{OUTPUT}/echo_mold_animated.blend")
print("[ANIM] saved echo_mold_animated.blend")

# ---- export FBX ------------------------------------------------------------
bpy.ops.object.select_all(action='DESELECT')
sel = [root] + list(root.children_recursive)
for o in sel:
    o.select_set(True)
bpy.context.view_layer.objects.active = root

fbx_path = f"{OUTPUT}/EchoMold.fbx"
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
print(f"[ANIM] exported {fbx_path}")

clips = {
    "fbx": "EchoMold.fbx",
    "rig": "Generic",
    "clips": [
        {"name": "EM_Idle", "start": 1, "end": 96, "loop": True},
        {"name": "EM_Hunt", "start": 101, "end": 196, "loop": True},
        {"name": "EM_Attack", "start": 201, "end": 224, "loop": False},
        {"name": "EM_Death", "start": 231, "end": 286, "loop": False},
    ],
}
with open(f"{OUTPUT}/EchoMold_clips.json", "w") as fh:
    json.dump(clips, fh, indent=2)
print("[ANIM] wrote EchoMold_clips.json")

# ---- verify the new poses by rendering attack peak + death end -------------
world = bpy.data.worlds[0]
world.use_nodes = True
world.node_tree.nodes.get("Background").inputs[0].default_value = (0.16, 0.18, 0.20, 1.0)
bpy.ops.object.light_add(type='SUN', location=(3, -4, 6))
k = bpy.context.active_object; k.data.energy = 3.2
k.rotation_euler = Euler((math.radians(55), 0, math.radians(35)), 'XYZ')
bpy.ops.object.light_add(type='AREA', location=(-4, -3, 2.5))
bpy.context.active_object.data.energy = 120.0
bpy.context.active_object.data.size = 5.0
CZ = 0.98
bpy.ops.object.empty_add(location=(0, 0, CZ)); tgt = bpy.context.active_object
bpy.ops.object.camera_add(location=(0, -6, CZ)); cam = bpy.context.active_object
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.7
c = cam.constraints.new(type='TRACK_TO'); c.target = tgt
c.track_axis = 'TRACK_NEGATIVE_Z'; c.up_axis = 'UP_Y'
bpy.context.scene.camera = cam
scene = bpy.context.scene
for eng in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE', 'BLENDER_WORKBENCH'):
    try:
        scene.render.engine = eng; break
    except TypeError:
        continue
scene.render.resolution_x = 640
scene.render.resolution_y = 900

for fr, name in ((212, "anim_attack"), (286, "anim_death")):
    scene.frame_set(fr)
    scene.render.filepath = f"{PREVIEW}/{name}.png"
    bpy.ops.render.render(write_still=True)
    print(f"[ANIM] wrote {name}.png @f{fr}")

print("[ANIM] DONE")
