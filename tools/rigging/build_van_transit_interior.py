"""Blender 5.x: build a believable high-roof work-van transit cabin at real scale."""
import bpy
import math
import mathutils
import os

OUT = "D:/BlackCommission/Assets/Resources/GeneratedArt"
SOURCE = "D:/BlackCommission/Assets/_Project/Art/Props/Van"


def reset():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def mat(name, color, roughness=0.72, metallic=0.0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = color
    m.use_nodes = True
    p = m.node_tree.nodes.get("Principled BSDF")
    if p:
        p.inputs["Base Color"].default_value = color
        p.inputs["Roughness"].default_value = roughness
        p.inputs["Metallic"].default_value = metallic
    return m


def box(name, loc, size, material, bevel=0.015, parent=None):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o = bpy.context.object
    o.name = name
    o.scale = (size[0] / 2, size[1] / 2, size[2] / 2)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        b = o.modifiers.new("PressedEdges", "BEVEL")
        b.width = bevel
        b.segments = 2
    o.data.materials.append(material)
    if parent: o.parent = parent
    return o


def cyl(name, loc, radius, depth, material, rot=(0, math.pi / 2, 0), parent=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=20, radius=radius, depth=depth, location=loc, rotation=rot)
    o = bpy.context.object
    o.name = name
    o.data.materials.append(material)
    if parent: o.parent = parent
    return o


reset()
os.makedirs(OUT, exist_ok=True)
os.makedirs(SOURCE, exist_ok=True)

paint = mat("DirtyWarmGreySteel", (0.19, 0.205, 0.195, 1))
dark = mat("RubberFloor", (0.035, 0.041, 0.039, 1), 0.92)
rib = mat("ExposedSteelRibs", (0.27, 0.285, 0.275, 1), 0.55, 0.15)
ply = mat("SealedPlywoodPanels", (0.23, 0.19, 0.135, 1), 0.88)
seat = mat("WornBlackVinyl", (0.045, 0.052, 0.049, 1), 0.82)
strap = mat("OxbloodRestraint", (0.25, 0.065, 0.05, 1), 0.78)
lamp = mat("WarmLEDLens", (0.92, 0.83, 0.62, 1), 0.35)
paper = mat("AgedSafetyPaper", (0.72, 0.68, 0.55, 1), 0.9)
red = mat("StampRed", (0.58, 0.08, 0.045, 1), 0.82)

root = bpy.data.objects.new("BC_VanTransitInterior", None)
bpy.context.collection.objects.link(root)

# True high-roof cargo envelope: 3.40m long x 1.78m wide x 1.93m high.
box("Interior_Floor", (0, 0, 0.035), (3.40, 1.70, 0.07), dark, 0.01, root)
box("Interior_Ceiling", (0, 0, 1.90), (3.40, 1.54, 0.06), paint, 0.02, root)
box("Interior_WallL", (0, -0.865, 1.00), (3.40, 0.05, 1.78), paint, 0.02, root)
box("Interior_WallR", (0, 0.865, 1.00), (3.40, 0.05, 1.78), paint, 0.02, root)
box("Interior_Bulkhead", (-1.68, 0, 1.00), (0.06, 1.72, 1.86), paint, 0.018, root)
box("Interior_WallRear", (1.68, 0, 1.00), (0.06, 1.72, 1.86), paint, 0.018, root)

# Pressed ribs and recessed lower wall panels make the shell read as a vehicle, not a room.
for i, x in enumerate((-1.42, -0.94, -0.46, 0.02, 0.50, 0.98, 1.46)):
    box(f"RoofRib_{i}", (x, 0, 1.855), (0.055, 1.70, 0.055), rib, 0.009, root)
    box(f"WallRibL_{i}", (x, -0.835, 1.00), (0.055, 0.055, 1.72), rib, 0.009, root)
    box(f"WallRibR_{i}", (x, 0.835, 1.00), (0.055, 0.055, 1.72), rib, 0.009, root)
for side in (-1, 1):
    for i, x in enumerate((-1.18, -0.55, 0.08, 0.71, 1.30)):
        box(f"PlyPanel_{side}_{i}", (x, side * 0.815, 0.67), (0.54, 0.025, 0.72), ply, 0.012, root)

# Wheel arches constrain the lower width just like the 1.35m real wheelhouse measurement.
for side in (-1, 1):
    box(f"WheelArch_{side}", (0.76, side * 0.715, 0.29), (0.82, 0.32, 0.46), dark, 0.07, root)

# Two wall-mounted benches, deliberately short of the rear doors and bulkhead.
for side in (-1, 1):
    z = side * 0.68
    box(f"BenchSeat_{side}", (0, z, 0.43), (2.35, 0.36, 0.13), seat, 0.045, root)
    box(f"BenchBack_{side}", (0, side * 0.81, 0.78), (2.35, 0.10, 0.60), seat, 0.04, root)
    for x in (-0.95, -0.32, 0.32, 0.95):
        box(f"SeatBracket_{side}_{x}", (x, side * 0.72, 0.22), (0.055, 0.055, 0.38), rib, 0.008, root)
    for x in (-0.72, 0.72):
        box(f"SeatBelt_{side}_{x}", (x, side * 0.805, 0.82), (0.05, 0.018, 0.55), strap, 0.006, root)

# Flush lights and side handrails: nothing crosses the player's view or hangs in the aisle.
for x in (-0.72, 0.72):
    box(f"CeilingLED_{x}", (x, 0, 1.858), (0.72, 0.12, 0.018), lamp, 0.01, root)
for side in (-1, 1):
    cyl(f"Handrail_{side}", (0, side * 0.76, 1.58), 0.018, 2.45, rib, parent=root)

# Rear double-door seam, hinges and bulkhead service hatch.
box("RearDoorSeam", (1.645, 0, 1.00), (0.018, 0.025, 1.72), dark, 0.002, root)
for side in (-1, 1):
    for y in (0.45, 1.42):
        box(f"RearHinge_{side}_{y}", (1.638, side * 0.70, y), (0.03, 0.11, 0.13), rib, 0.008, root)
box("BulkheadHatch", (-1.642, 0, 1.02), (0.025, 0.62, 0.48), dark, 0.025, root)
box("CompanyPlate", (-1.625, 0, 1.42), (0.015, 0.48, 0.16), paper, 0.008, root)
box("CompanyStamp", (-1.615, 0.13, 1.41), (0.012, 0.10, 0.06), red, 0.004, root)

# Low-profile practical details.
box("FirstAidBox", (-1.15, 0.80, 1.28), (0.30, 0.08, 0.25), dark, 0.025, root)
box("SafetyNotice", (-0.92, -0.82, 1.30), (0.34, 0.02, 0.25), paper, 0.006, root)
box("SafetyStamp", (-0.84, -0.805, 1.23), (0.10, 0.012, 0.055), red, 0.003, root)

# Apply modifiers for predictable FBX rendering.
bpy.context.view_layer.objects.active = next(o for o in bpy.context.scene.objects if o.type == "MESH")
bpy.ops.object.select_all(action="DESELECT")
for o in bpy.context.scene.objects:
    if o.type == "MESH": o.select_set(True)
bpy.ops.object.convert(target="MESH")

blend_path = os.path.join(SOURCE, "BC_VanTransitInterior.blend")
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
bpy.ops.export_scene.fbx(
    filepath=os.path.join(OUT, "BC_VanTransitInterior.fbx"),
    use_selection=False,
    apply_scale_options="FBX_SCALE_UNITS",
    add_leaf_bones=False,
    bake_anim=False,
    mesh_smooth_type="FACE",
    axis_forward="-Z",
    axis_up="Y",
)

# Review render (added after save/export, so cameras and lights do not pollute the source asset).
bpy.ops.object.camera_add(location=(1.42, 0.0, 1.35))
camera = bpy.context.object
camera.data.lens = 20
camera.rotation_euler = (mathutils.Vector((-0.45, 0, 0.85)) - camera.location).to_track_quat("-Z", "Y").to_euler()
bpy.context.scene.camera = camera
bpy.ops.object.light_add(type="AREA", location=(0, -0.3, 2.8))
bpy.context.object.data.energy = 650
bpy.context.object.data.size = 3.5
bpy.ops.object.light_add(type="AREA", location=(-2.4, 1.8, 1.5))
bpy.context.object.data.energy = 300
bpy.context.object.data.color = (0.65, 0.72, 0.78)
bpy.context.object.data.size = 2.5
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 960
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.filepath = "D:/BlackCommission/tools/rigging/preview/van_transit_interior.png"
scene.world.color = (0.012, 0.014, 0.013)
bpy.ops.render.render(write_still=True)
print("Built", blend_path)
