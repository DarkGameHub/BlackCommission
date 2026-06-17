"""
Black Commission - Echo Mold rig pass 1: build rigid-part hierarchy + hinge
pivots, then render the rig posed to IDLE (closed) and HUNT (open) to verify
the pivots actually drive the petals/arms before baking animations.

Hierarchy (Generic rig for Unity):
  EchoMold (root empty)
   |- Base            (bulb + roots; grounded)
   |- Stalk           (stalk + shelf; pivots at base -> sway/lean)
       |- Head        (throat core + eye; pivots at neck -> look/lock)
       |- PetalPiv_0..5 -> Petal mesh   (hinge open/close)
       |- ArmPiv_L/R    -> Arm mesh      (deploy forward)

Headless:  blender --background --python rig_build.py
"""

import bpy
import math
import mathutils
from mathutils import Vector, Euler, Quaternion, Matrix

OUTPUT = "D:/BlackCommission/tools/rigging/output"
PREVIEW = "D:/BlackCommission/tools/rigging/preview"

bpy.ops.wm.open_mainfile(filepath=f"{OUTPUT}/echo_mold_base.blend")


def obj(name):
    return bpy.data.objects.get(name)


def join(target_name, names, new_name):
    objs = [obj(n) for n in names if obj(n)]
    if not objs:
        return None
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    tgt = obj(target_name)
    bpy.context.view_layer.objects.active = tgt
    bpy.ops.object.join()
    tgt.name = new_name
    return tgt


# ---- merge into logical animated groups -----------------------------------
base = join("EM_Base", ["EM_Base"] + [f"EM_Root_{i}" for i in range(6)], "EchoMold_Base")
stalk = join("EM_Stalk", ["EM_Stalk", "EM_Shelf"], "EchoMold_Stalk")
head = join("EM_Throat", ["EM_Throat", "EM_Eye"], "EchoMold_Head")
petals = [obj(f"EM_Petal_{i}") for i in range(6)]
arms = {-1: obj("EM_Arm_-1"), 1: obj("EM_Arm_1")}

# remove the leftover lights/cam/empty from the base file (keep a clean scene)
for n in list(bpy.data.objects):
    if n.type in ('LIGHT', 'CAMERA', 'EMPTY'):
        bpy.data.objects.remove(n, do_unlink=True)


def set_origin(o, loc):
    bpy.context.scene.cursor.location = Vector(loc)
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')


def make_empty(name, loc, quat=None, parent=None):
    e = bpy.data.objects.new(name, None)
    e.empty_display_size = 0.12
    bpy.context.collection.objects.link(e)
    e.location = Vector(loc)
    e.rotation_mode = 'QUATERNION'
    if quat is not None:
        e.rotation_quaternion = quat
    if parent is not None:
        bpy.context.view_layer.update()
        e.parent = parent
        e.matrix_parent_inverse = parent.matrix_world.inverted()
    return e


def parent_keep(child, parent):
    bpy.context.view_layer.update()
    child.parent = parent
    child.matrix_parent_inverse = parent.matrix_world.inverted()


# ---- root + base + stalk + head -------------------------------------------
root = make_empty("EchoMold", (0, 0, 0))
root.empty_display_size = 0.3

parent_keep(base, root)

set_origin(stalk, (0, 0, 0.30))           # stalk pivots from its base
stalk.rotation_mode = 'XYZ'
parent_keep(stalk, root)

set_origin(head, (0, 0, 1.40))            # head pivots at the neck
head.rotation_mode = 'XYZ'
parent_keep(head, stalk)

# ---- petal hinge pivots ----------------------------------------------------
HINGE_R = 0.31
HINGE_Z = 1.40
THETA_OPEN = math.radians(-152)           # closed -> open swing about hinge axis
petal_pivots = []
for i, p in enumerate(petals):
    az = math.radians(60 * i)
    tangent = Vector((-math.sin(az), math.cos(az), 0.0))   # hinge axis
    up = Vector((0, 0, 1))
    Y = up.cross(tangent).normalized()
    Z = tangent.cross(Y).normalized()
    rest_q = Matrix((tangent, Y, Z)).transposed().to_quaternion()
    hinge = (math.cos(az) * HINGE_R, math.sin(az) * HINGE_R, HINGE_Z)
    piv = make_empty(f"EchoMold_PetalPiv_{i}", hinge, quat=rest_q, parent=stalk)
    parent_keep(p, piv)
    petal_pivots.append((piv, rest_q))

# ---- arm pivots ------------------------------------------------------------
arm_pivots = {}
for sx, a in arms.items():
    piv = make_empty(f"EchoMold_ArmPiv_{'R' if sx > 0 else 'L'}",
                     (0.06 * sx, 0.12, 1.12), parent=stalk)
    piv.rotation_mode = 'XYZ'
    parent_keep(a, piv)
    arm_pivots[sx] = piv

ARM_DEPLOY = math.radians(-95)            # pitch arm from down -> forward


# ---- pose driver -----------------------------------------------------------
def set_pose(openness=0.0, deploy=0.0, head_pitch=0.0, stalk_tilt=(0, 0)):
    for (piv, rest_q) in petal_pivots:
        piv.rotation_quaternion = rest_q @ Quaternion((1, 0, 0), openness * THETA_OPEN)
    for sx, piv in arm_pivots.items():
        piv.rotation_euler = Euler((deploy * ARM_DEPLOY, 0, 0), 'XYZ')
    head.rotation_euler = Euler((head_pitch, 0, 0), 'XYZ')
    stalk.rotation_euler = Euler((stalk_tilt[0], stalk_tilt[1], 0), 'XYZ')


# ---- lighting / camera / render -------------------------------------------
world = bpy.data.worlds[0] if bpy.data.worlds else bpy.data.worlds.new("W")
bpy.context.scene.world = world
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
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.5
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


def render(path, loc):
    cam.location = loc
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print(f"[RIG] wrote {path}")


# IDLE pose (closed, slight lean)
set_pose(openness=0.0, deploy=0.0, head_pitch=0.0, stalk_tilt=(math.radians(2), 0))
render(f"{PREVIEW}/rig_idle_front.png", (0, -6, CZ))

# HUNT pose (open, arms forward, head up)
set_pose(openness=1.0, deploy=1.0, head_pitch=math.radians(-12), stalk_tilt=(0, 0))
render(f"{PREVIEW}/rig_hunt_front.png", (0, -6, CZ))
render(f"{PREVIEW}/rig_hunt_side.png", (6, 0, CZ))

# leave in idle and save rigged blend
set_pose(openness=0.0, deploy=0.0, head_pitch=0.0, stalk_tilt=(0, 0))
bpy.ops.wm.save_as_mainfile(filepath=f"{OUTPUT}/echo_mold_rigged.blend")
print("[RIG] saved echo_mold_rigged.blend")
print("[RIG] DONE")
