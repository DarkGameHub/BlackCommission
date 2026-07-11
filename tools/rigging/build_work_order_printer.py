"""Blender 5.x: build a production-ready compact dot-matrix printer cart and dispatch sheet."""
import bpy
import math
import mathutils
import os

ROOT = "D:/BlackCommission/Assets/_Project/Art/Props/WorkOrder"


def reset():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def material(name, rgba):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = rgba
    mat.roughness = 0.9
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = rgba
        bsdf.inputs["Roughness"].default_value = 0.9
    return mat


def cube(name, location, scale, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new("WornEdges", "BEVEL")
        mod.width = bevel
        mod.segments = 1
    obj.data.materials.append(mat)
    return obj


def cylinder(name, location, radius, depth, mat, rotation=(math.pi / 2, 0, 0), vertices=20):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location,
                                       rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new("SoftEdges", "BEVEL")
    bevel.width = 0.006
    bevel.segments = 2
    return obj


def export_selected(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_scale_options="FBX_SCALE_UNITS",
        add_leaf_bones=False,
        bake_anim=True,
        mesh_smooth_type="FACE",
        axis_forward="-Z",
        axis_up="Y",
    )


def render_preview(path, target=(0, 0, 0.72)):
    bpy.ops.object.camera_add(location=(2.2, 3.2, 1.85))
    camera = bpy.context.object
    camera.rotation_euler = (mathutils.Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = camera
    bpy.ops.object.light_add(type="AREA", location=(1.8, 1.4, 3.1))
    bpy.context.object.data.energy = 850
    bpy.context.object.data.shape = "DISK"
    bpy.context.object.data.size = 3.0
    bpy.ops.object.light_add(type="AREA", location=(-2.0, -0.5, 1.8))
    bpy.context.object.data.energy = 420
    bpy.context.object.data.color = (0.35, 0.55, 0.58)
    bpy.context.object.data.size = 2.0
    bpy.ops.mesh.primitive_plane_add(size=8, location=(0, 0, -0.01))
    bpy.context.object.data.materials.append(dark)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 720
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = path
    scene.world.color = (0.015, 0.018, 0.017)
    bpy.ops.render.render(write_still=True)


reset()
dark = material("PowderCoatBlack", (0.035, 0.043, 0.041, 1))
shell = material("WarmGreyABS", (0.29, 0.28, 0.235, 1))
metal = material("GalvanizedSteel", (0.24, 0.255, 0.245, 1))
paper = material("AgedContinuousPaper", (0.72, 0.69, 0.56, 1))
ink = material("RibbonInk", (0.045, 0.047, 0.041, 1))
red = material("JamLamp", (0.62, 0.045, 0.025, 1))
amber = material("PowerLamp", (0.82, 0.50, 0.08, 1))
rust = material("EdgeRust", (0.25, 0.09, 0.035, 1))
glass = material("SmokedCover", (0.075, 0.095, 0.085, 0.58))
glass.diffuse_color = (0.075, 0.095, 0.085, 0.58)
glass.surface_render_method = "DITHERED"

# Compact 1980s tractor-feed printer on a visually light service cart.
parts = []
# Cart: 0.68 x 0.50 m footprint, open centre keeps it from reading as another cabinet.
parts += [
    cube("CartTop", (0, 0, 0.73), (0.34, 0.25, 0.025), metal, 0.012),
    cube("CartShelf", (0, 0, 0.28), (0.31, 0.22, 0.018), metal, 0.008),
    cube("Leg_FL", (-0.30, 0.21, 0.48), (0.018, 0.018, 0.24), dark, 0.005),
    cube("Leg_FR", (0.30, 0.21, 0.48), (0.018, 0.018, 0.24), dark, 0.005),
    cube("Leg_BL", (-0.30, -0.21, 0.48), (0.018, 0.018, 0.24), dark, 0.005),
    cube("Leg_BR", (0.30, -0.21, 0.48), (0.018, 0.018, 0.24), dark, 0.005),
    cube("PaperBox", (0, -0.01, 0.39), (0.22, 0.16, 0.09), paper, 0.012),
]
for x in (-0.30, 0.30):
    for y in (-0.21, 0.21):
        parts.append(cylinder(f"Caster_{x}_{y}", (x, y, 0.12), 0.045, 0.035, dark,
                              rotation=(math.pi / 2, 0, 0), vertices=16))

# Printer body sits from 0.755 to about 1.08 m; proportions match desktop dot-matrix hardware.
parts += [
    cube("PrinterLowerShell", (0, 0.005, 0.855), (0.305, 0.225, 0.10), shell, 0.025),
    cube("PrinterUpperShell", (0, -0.045, 0.985), (0.29, 0.16, 0.055), shell, 0.020),
    cube("FrontControlBezel", (0, 0.222, 0.90), (0.285, 0.018, 0.055), dark, 0.008),
    cube("SmokedRibbonCover", (0, 0.055, 1.025), (0.225, 0.12, 0.025), glass, 0.015),
    cube("RearPaperGuide", (0, -0.205, 1.08), (0.27, 0.018, 0.14), metal, 0.008),
    cube("OutputPaper", (0, 0.205, 1.10), (0.255, 0.006, 0.16), paper, 0.002),
    cube("TearBar", (0, 0.233, 1.005), (0.285, 0.018, 0.016), metal, 0.004),
    cylinder("Platen", (0, 0.135, 1.015), 0.027, 0.54, dark, rotation=(0, math.pi / 2, 0)),
    cylinder("PlatenKnob_L", (-0.325, 0.135, 1.015), 0.042, 0.035, dark, rotation=(0, math.pi / 2, 0)),
    cylinder("PlatenKnob_R", (0.325, 0.135, 1.015), 0.042, 0.035, dark, rotation=(0, math.pi / 2, 0)),
    cube("AssetPlate", (-0.17, 0.244, 0.91), (0.075, 0.005, 0.018), metal, 0.002),
    cube("PowerButton", (0.21, 0.245, 0.915), (0.025, 0.008, 0.018), amber, 0.003),
    cube("JamButton", (0.265, 0.245, 0.915), (0.018, 0.008, 0.018), red, 0.003),
    cube("WearFront", (-0.285, 0.243, 0.845), (0.012, 0.005, 0.035), rust, 0.001),
]

# Control keys, cooling slots, paper perforations and printed lines hold up in close view.
for i in range(5):
    parts.append(cube(f"ControlKey_{i}", (-0.09 + i * 0.045, 0.245, 0.91),
                      (0.015, 0.007, 0.012), metal, 0.002))
for i in range(7):
    parts.append(cube(f"Vent_{i}", (-0.18 + i * 0.06, -0.211, 0.91),
                      (0.018, 0.004, 0.005), dark, 0.0))
for side in (-1, 1):
    for i in range(7):
        parts.append(cube(f"FeedHole_{side}_{i}", (side * 0.235, 0.212, 1.005 + i * 0.034),
                          (0.007, 0.008, 0.005), ink, 0.0))
for i, width in enumerate((0.17, 0.20, 0.13, 0.18)):
    parts.append(cube(f"PrintedLine_{i}", (-0.025, 0.214, 1.065 + i * 0.035),
                      (width, 0.008, 0.003), ink, 0.0))

bpy.ops.object.select_all(action="DESELECT")
for obj in parts:
    obj.select_set(True)
export_selected(os.path.join(ROOT, "WorkOrderPrinter.fbx"))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "WorkOrderPrinter.blend"))
render_preview("D:/BlackCommission/tools/rigging/preview/work_order_printer.png", target=(0, 0, 0.62))

# Separate carry sheet asset, with slightly raised feed strips and ink bars.
reset()
paper = material("AgedContinuousPaper", (0.66, 0.63, 0.47, 1))
ink = material("FadedInk", (0.055, 0.06, 0.05, 1))
sheet = [cube("WorkOrderSheet", (0, 0, 0), (0.215, 0.17, 0.004), paper)]
for row, width in enumerate((0.16, 0.12, 0.18, 0.14, 0.17, 0.10)):
    sheet.append(cube(f"InkLine_{row}", (-0.025, -0.11 + row * 0.038, 0.006),
                      (width, 0.006, 0.0015), ink))
for side in (-1, 1):
    sheet.append(cube(f"FeedStrip_{side}", (side * 0.195, 0, 0.006), (0.012, 0.16, 0.002), ink))
bpy.ops.object.select_all(action="SELECT")
export_selected(os.path.join(ROOT, "WorkOrderSheet.fbx"))

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "WorkOrderSheet.blend"))
for backup in ("WorkOrderPrinter.blend1", "WorkOrderSheet.blend1"):
    backup_path = os.path.join(ROOT, backup)
    if os.path.exists(backup_path):
        os.remove(backup_path)
print("Built work-order printer assets in", ROOT)
