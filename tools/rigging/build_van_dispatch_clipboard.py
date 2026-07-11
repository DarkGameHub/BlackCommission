"""Blender 5.x: original low-poly wall-mounted van dispatch clipboard."""
import bpy
import mathutils
import os

ART = "D:/BlackCommission/Assets/_Project/Art/Props/WorkOrder"
RESOURCE = "D:/BlackCommission/Assets/Resources/WorkOrder"
PREVIEW = "D:/BlackCommission/tools/rigging/preview/van_dispatch_clipboard.png"


def reset():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def mat(name, color, roughness=0.9):
    m = bpy.data.materials.new(name)
    m.diffuse_color = color
    m.roughness = roughness
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
    return m


def cube(name, loc, scale, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new("WornEdge", "BEVEL")
        mod.width = bevel
        mod.segments = 1
    obj.data.materials.append(material)
    return obj


reset()
dead = mat("DeadSteel", (0.055, 0.065, 0.058, 1))
steel = mat("OxidizedClip", (0.21, 0.23, 0.20, 1), 0.72)
paper = mat("DirtyDispatchPaper", (0.58, 0.56, 0.45, 1))
ink = mat("FadedRibbonInk", (0.035, 0.045, 0.037, 1))
stamp = mat("RejectedStamp", (0.35, 0.055, 0.035, 1))
rust = mat("EdgeRust", (0.24, 0.10, 0.035, 1))

parts = [
    cube("BackPlate", (0, 0.018, 0), (0.25, 0.018, 0.34), dead, 0.012),
    cube("Paper", (0, -0.006, -0.015), (0.215, 0.004, 0.295), paper, 0.003),
    cube("TopClip", (0, -0.026, 0.285), (0.085, 0.022, 0.045), steel, 0.008),
    cube("ClipJaw", (0, -0.052, 0.245), (0.075, 0.010, 0.018), steel, 0.004),
    cube("RustLeft", (-0.247, -0.003, -0.13), (0.006, 0.006, 0.09), rust),
    cube("Stamp", (0.11, -0.014, -0.20), (0.065, 0.003, 0.025), stamp),
]

# Tractor holes and fixed printed lines are geometry, not screen UI.
for side in (-1, 1):
    parts.append(cube(f"FeedStrip_{side}", (side * 0.198, -0.013, -0.015),
                      (0.008, 0.003, 0.275), ink))
    for row in range(9):
        parts.append(cube(f"FeedHole_{side}_{row}",
                          (side * 0.198, -0.020, -0.245 + row * 0.06),
                          (0.004, 0.002, 0.008), paper))

for row, width in enumerate((0.145, 0.175, 0.12, 0.18, 0.16, 0.10, 0.17, 0.135)):
    parts.append(cube(f"PrintLine_{row}", (-0.02, -0.014, 0.19 - row * 0.053),
                      (width, 0.002, 0.005), ink))

bpy.ops.object.select_all(action="DESELECT")
for obj in parts:
    obj.select_set(True)
os.makedirs(RESOURCE, exist_ok=True)
bpy.ops.export_scene.fbx(filepath=os.path.join(RESOURCE, "VanDispatchClipboard.fbx"),
    use_selection=True, apply_scale_options="FBX_SCALE_UNITS", add_leaf_bones=False,
    bake_anim=False, mesh_smooth_type="FACE", axis_forward="-Z", axis_up="Y")

# Visual QA render.
bpy.ops.object.camera_add(location=(0.95, -1.75, 0.75))
cam = bpy.context.object
cam.rotation_euler = (mathutils.Vector((0, 0, 0)) - cam.location).to_track_quat("-Z", "Y").to_euler()
bpy.context.scene.camera = cam
bpy.ops.object.light_add(type="AREA", location=(0.8, -1.0, 1.7))
bpy.context.object.data.energy = 520
bpy.context.object.data.size = 2.0
bpy.ops.object.light_add(type="AREA", location=(-1.0, 0.4, 0.4))
bpy.context.object.data.energy = 180
bpy.context.object.data.color = (0.42, 0.52, 0.43)
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 720
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = PREVIEW
scene.world.color = (0.01, 0.012, 0.01)
bpy.ops.render.render(write_still=True)

os.makedirs(ART, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ART, "VanDispatchClipboard.blend"))
backup = os.path.join(ART, "VanDispatchClipboard.blend1")
if os.path.exists(backup):
    os.remove(backup)
print("Built wall-mounted van dispatch clipboard")
