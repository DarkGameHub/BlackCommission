"""
Generate a more vehicle-like low-poly AccidentSquad dispatch van.

Output:
  D:/AccidentSquad/Assets/_Project/Art/Generated/OutsourcedCivicKit_v3
"""

from __future__ import annotations

import math
from pathlib import Path

import bpy
from mathutils import Vector


OUT = Path(r"D:/AccidentSquad/Assets/_Project/Art/Generated/OutsourcedCivicKit_v3")


def clear() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for datablock in list(bpy.data.meshes):
        if datablock.users == 0:
            bpy.data.meshes.remove(datablock)
    for datablock in list(bpy.data.materials):
        if datablock.users == 0:
            bpy.data.materials.remove(datablock)


def mat(name: str, color: tuple[float, float, float, float], emission: float = 0.0) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = 0.78
        if emission:
            bsdf.inputs["Emission Color"].default_value = color
            bsdf.inputs["Emission Strength"].default_value = emission
    return material


def palette() -> dict[str, bpy.types.Material]:
    return {
        "body": mat("ASV3_van_old_warm_white", (0.72, 0.70, 0.61, 1)),
        "body_shadow": mat("ASV3_van_dirty_lower_panel", (0.48, 0.49, 0.43, 1)),
        "glass": mat("ASV3_dirty_blue_glass", (0.045, 0.12, 0.15, 1)),
        "rubber": mat("ASV3_old_black_rubber", (0.012, 0.011, 0.010, 1)),
        "metal": mat("ASV3_dead_metal", (0.08, 0.085, 0.08, 1)),
        "terminal": mat("ASV3_company_terminal_green", (0.03, 0.80, 0.34, 1), 0.25),
        "debt": mat("ASV3_debt_red", (0.55, 0.025, 0.02, 1)),
        "paper": mat("ASV3_dirty_paper", (0.78, 0.75, 0.60, 1)),
        "amber": mat("ASV3_old_amber_light", (0.95, 0.48, 0.08, 1), 0.5),
        "headlight": mat("ASV3_dull_headlight", (0.90, 0.86, 0.66, 1), 0.3),
        "red_light": mat("ASV3_rear_red_light", (0.9, 0.03, 0.02, 1), 0.3),
        "grid": mat("ASV3_preview_floor", (0.20, 0.21, 0.20, 1)),
    }


def collection(name: str) -> bpy.types.Collection:
    coll = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(coll)
    return coll


def link(obj: bpy.types.Object, coll: bpy.types.Collection) -> bpy.types.Object:
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    coll.objects.link(obj)
    return obj


def bevel(obj: bpy.types.Object, amount: float = 0.025) -> bpy.types.Object:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    modifier = obj.modifiers.new("low_poly_soft_edges", "BEVEL")
    modifier.width = amount
    modifier.segments = 1
    modifier.affect = "EDGES"
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    normals = obj.modifiers.new("weighted_normals", "WEIGHTED_NORMAL")
    bpy.ops.object.modifier_apply(modifier=normals.name)
    obj.select_set(False)
    return obj


def cube(name: str, loc, scale, material, coll, rot=(0, 0, 0), edge=0.015) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if edge:
        bevel(obj, edge)
    return link(obj, coll)


def cyl(name: str, loc, radius, depth, material, coll, vertices=16, rot=(0, 0, 0), edge=0.0) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    for poly in obj.data.polygons:
        poly.use_smooth = False
    if edge:
        bevel(obj, edge)
    return link(obj, coll)


def torus(name: str, loc, major_radius, minor_radius, material, coll, rot=(0, 0, 0)) -> bpy.types.Object:
    bpy.ops.mesh.primitive_torus_add(
        major_segments=16,
        minor_segments=4,
        major_radius=major_radius,
        minor_radius=minor_radius,
        location=loc,
        rotation=rot,
    )
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    for poly in obj.data.polygons:
        poly.use_smooth = False
    return link(obj, coll)


def prism_from_side_profile(
    name: str,
    points: list[tuple[float, float]],
    half_width: float,
    material: bpy.types.Material,
    coll: bpy.types.Collection,
) -> bpy.types.Object:
    verts = []
    for y in (-half_width, half_width):
        verts.extend((x, y, z) for x, z in points)

    n = len(points)
    faces = [tuple(range(n)), tuple(range(n, n * 2))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, j + n, i + n))

    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    obj.data.materials.append(material)
    coll.objects.link(obj)
    bevel(obj, 0.03)
    return obj


def side_panel(name: str, y: float, points: list[tuple[float, float]], material, coll) -> bpy.types.Object:
    verts = [(x, y, z) for x, z in points]
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(verts, [], [tuple(range(len(verts)))])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    obj.data.materials.append(material)
    coll.objects.link(obj)
    return obj


def text(name: str, body: str, loc, rot, size: float, material, coll) -> bpy.types.Object:
    bpy.ops.object.text_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.body = body
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.004
    obj.data.materials.append(material)
    bpy.ops.object.convert(target="MESH")
    return link(bpy.context.object, coll)


def make_dispatch_van(coll: bpy.types.Collection, m: dict[str, bpy.types.Material]) -> None:
    # One readable silhouette: short sloped hood, big windshield, tall cargo box, flat rear.
    profile = [
        (-1.85, 0.36),
        (-1.78, 0.62),
        (-1.45, 0.78),
        (-1.18, 1.05),
        (-0.82, 1.38),
        (1.25, 1.38),
        (1.58, 1.16),
        (1.62, 0.46),
        (1.38, 0.34),
        (-1.60, 0.34),
    ]
    prism_from_side_profile("ASV3_van_single_piece_sloped_body", profile, 0.72, m["body"], coll)

    # Lower dirty rocker panel makes it feel grounded instead of like a floating block.
    cube("ASV3_van_lower_dirty_panel_L", (-0.05, -0.735, 0.47), (1.50, 0.018, 0.10), m["body_shadow"], coll, edge=0.004)
    cube("ASV3_van_lower_dirty_panel_R", (-0.05, 0.735, 0.47), (1.50, 0.018, 0.10), m["body_shadow"], coll, edge=0.004)

    # Front bumper, grille, headlights, rear bumper.
    cube("ASV3_front_bumper", (-1.88, 0, 0.45), (0.08, 0.66, 0.09), m["metal"], coll, edge=0.01)
    cube("ASV3_rear_bumper", (1.67, 0, 0.45), (0.08, 0.66, 0.09), m["metal"], coll, edge=0.01)
    cube("ASV3_front_grille", (-1.91, 0, 0.62), (0.018, 0.32, 0.08), m["rubber"], coll, edge=0.003)
    cube("ASV3_headlight_L", (-1.92, -0.34, 0.70), (0.015, 0.10, 0.055), m["headlight"], coll, edge=0.003)
    cube("ASV3_headlight_R", (-1.92, 0.34, 0.70), (0.015, 0.10, 0.055), m["headlight"], coll, edge=0.003)
    cube("ASV3_rear_light_L", (1.705, -0.48, 0.76), (0.014, 0.075, 0.12), m["red_light"], coll, edge=0.002)
    cube("ASV3_rear_light_R", (1.705, 0.48, 0.76), (0.014, 0.075, 0.12), m["red_light"], coll, edge=0.002)

    # Glass is placed as actual sloped/side panels, so the cabin reads as a vehicle cabin.
    windshield = [
        (-1.33, -0.55, 0.86),
        (-1.02, -0.55, 1.26),
        (-1.02, 0.55, 1.26),
        (-1.33, 0.55, 0.86),
    ]
    mesh = bpy.data.meshes.new("ASV3_windshield_mesh")
    mesh.from_pydata(windshield, [], [(0, 1, 2, 3)])
    mesh.update()
    obj = bpy.data.objects.new("ASV3_sloped_front_windshield", mesh)
    obj.data.materials.append(m["glass"])
    coll.objects.link(obj)

    side_panel("ASV3_driver_window_L", -0.748, [(-1.14, 0.88), (-0.80, 1.25), (-0.34, 1.25), (-0.34, 0.89)], m["glass"], coll)
    side_panel("ASV3_driver_window_R", 0.748, [(-1.14, 0.88), (-0.80, 1.25), (-0.34, 1.25), (-0.34, 0.89)], m["glass"], coll)
    side_panel("ASV3_cargo_window_L", -0.748, [(0.02, 0.91), (0.02, 1.23), (0.62, 1.23), (0.62, 0.91)], m["glass"], coll)
    side_panel("ASV3_cargo_window_R", 0.748, [(0.02, 0.91), (0.02, 1.23), (0.62, 1.23), (0.62, 0.91)], m["glass"], coll)

    # Door and panel lines: thin dark strips communicate the van construction.
    for y in (-0.754, 0.754):
        cube(f"ASV3_front_door_cutline_{y}", (-0.28, y, 0.84), (0.012, 0.008, 0.45), m["rubber"], coll, edge=0)
        cube(f"ASV3_sliding_door_cutline_{y}", (0.72, y, 0.82), (0.012, 0.008, 0.43), m["rubber"], coll, edge=0)
        cube(f"ASV3_door_handle_front_{y}", (-0.48, y, 0.86), (0.08, 0.010, 0.025), m["metal"], coll, edge=0.002)
        cube(f"ASV3_door_handle_slide_{y}", (0.38, y, 0.86), (0.08, 0.010, 0.025), m["metal"], coll, edge=0.002)

    # Wheels, hubs, and wheel-arch rings. These are what the previous version lacked most.
    for i, x in enumerate((-1.18, 1.10)):
        for side, y in (("L", -0.78), ("R", 0.78)):
            cyl(f"ASV3_wheel_{side}_{i}", (x, y, 0.34), 0.29, 0.20, m["rubber"], coll, vertices=18, rot=(math.radians(90), 0, 0), edge=0.004)
            cyl(f"ASV3_wheel_hub_{side}_{i}", (x, y + (-0.105 if y < 0 else 0.105), 0.34), 0.12, 0.035, m["metal"], coll, vertices=10, rot=(math.radians(90), 0, 0), edge=0.002)
            torus(f"ASV3_wheel_arch_{side}_{i}", (x, y * 0.965, 0.43), 0.33, 0.018, m["metal"], coll, rot=(math.radians(90), 0, 0))

    # Roof rack with pipe and ladder gives the vehicle a utilitarian accident-response identity.
    cube("ASV3_roof_rack_front_bar", (0.10, 0, 1.52), (0.75, 0.68, 0.025), m["metal"], coll, edge=0.006)
    cube("ASV3_roof_rack_rear_bar", (0.88, 0, 1.52), (0.75, 0.68, 0.025), m["metal"], coll, edge=0.006)
    cyl("ASV3_roof_pipe_left", (0.50, -0.43, 1.60), 0.035, 1.15, m["metal"], coll, vertices=8, rot=(0, math.radians(90), 0), edge=0.002)
    cyl("ASV3_roof_pipe_right", (0.50, 0.43, 1.60), 0.035, 1.15, m["metal"], coll, vertices=8, rot=(0, math.radians(90), 0), edge=0.002)
    for x in (-0.48, 0.52, 1.28):
        for y in (-0.52, 0.52):
            cube(f"ASV3_roof_rack_mount_{x}_{y}", (x, y, 1.455), (0.035, 0.035, 0.075), m["metal"], coll, edge=0.002)
    cube("ASV3_roof_amber_beacon", (-0.55, 0, 1.54), (0.16, 0.10, 0.055), m["amber"], coll, edge=0.012)
    cube("ASV3_rear_ladder_left", (1.73, 0.38, 0.92), (0.018, 0.025, 0.45), m["metal"], coll, edge=0.002)
    cube("ASV3_rear_ladder_right", (1.73, 0.55, 0.92), (0.018, 0.025, 0.45), m["metal"], coll, edge=0.002)
    for z in (0.70, 0.92, 1.14):
        cube(f"ASV3_rear_ladder_rung_{z}", (1.74, 0.465, z), (0.016, 0.10, 0.018), m["metal"], coll, edge=0.001)

    # Branding and debt slash.
    cube("ASV3_company_green_side_patch_L", (0.46, -0.758, 0.72), (0.34, 0.012, 0.12), m["terminal"], coll, edge=0.002)
    cube("ASV3_company_green_side_patch_R", (0.46, 0.758, 0.72), (0.34, 0.012, 0.12), m["terminal"], coll, edge=0.002)
    text("ASV3_company_text_L", "AS", (0.46, -0.776, 0.72), (math.radians(90), 0, 0), 0.13, m["rubber"], coll)
    text("ASV3_company_text_R", "AS", (0.46, 0.776, 0.72), (math.radians(90), 0, math.radians(180)), 0.13, m["rubber"], coll)
    cube("ASV3_debt_slash_L", (1.05, -0.760, 0.78), (0.25, 0.012, 0.038), m["debt"], coll, rot=(0, 0, math.radians(-10)), edge=0.001)
    cube("ASV3_debt_slash_R", (1.05, 0.760, 0.78), (0.25, 0.012, 0.038), m["debt"], coll, rot=(0, 0, math.radians(-10)), edge=0.001)

    # Dents and taped repair panels, but kept few so the silhouette stays clean.
    cube("ASV3_dented_patch_rear", (1.28, -0.762, 1.02), (0.18, 0.010, 0.08), m["body_shadow"], coll, edge=0.002)
    cube("ASV3_taped_repair_front", (-0.76, -0.764, 0.58), (0.18, 0.010, 0.035), m["paper"], coll, edge=0.001)


def add_preview_floor(coll: bpy.types.Collection, m: dict[str, bpy.types.Material]) -> None:
    cube("ASV3_preview_floor", (0, 0, -0.035), (2.4, 1.25, 0.025), m["grid"], coll, edge=0.002)
    for x in (-1.6, -0.8, 0, 0.8, 1.6):
        cube(f"ASV3_preview_floor_x_{x}", (x, 0, -0.005), (0.004, 1.25, 0.002), m["metal"], coll, edge=0)
    for y in (-0.8, 0, 0.8):
        cube(f"ASV3_preview_floor_y_{y}", (0, y, -0.004), (2.4, 0.004, 0.002), m["metal"], coll, edge=0)


def lights_camera() -> None:
    bpy.ops.object.light_add(type="AREA", location=(-3.0, -4.0, 4.5))
    key = bpy.context.object
    key.name = "ASV3_preview_large_softbox"
    key.data.energy = 450
    key.data.size = 4
    bpy.ops.object.light_add(type="POINT", location=(-0.55, 0, 1.95))
    beacon = bpy.context.object
    beacon.name = "ASV3_preview_beacon_glow"
    beacon.data.energy = 60
    beacon.data.color = (1.0, 0.45, 0.08)

    cam_loc = Vector((3.8, -5.1, 2.35))
    target = Vector((0.0, 0.0, 0.82))
    bpy.ops.object.camera_add(location=cam_loc)
    cam = bpy.context.object
    cam.name = "ASV3_preview_camera"
    cam.rotation_euler = (target - cam_loc).to_track_quat("-Z", "Y").to_euler()
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 4.4
    bpy.context.scene.camera = cam


def export(coll: bpy.types.Collection) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "ASV3_Second_Hand_Dispatch_Van.blend"))
    bpy.ops.object.select_all(action="DESELECT")
    for obj in coll.objects:
        if obj.type == "MESH":
            obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=str(OUT / "ASV3_Second_Hand_Dispatch_Van.fbx"),
        use_selection=True,
        apply_unit_scale=True,
        object_types={"MESH"},
        add_leaf_bones=False,
        use_mesh_modifiers=True,
    )
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1600
    bpy.context.scene.render.resolution_y = 900
    bpy.context.scene.render.filepath = str(OUT / "ASV3_Second_Hand_Dispatch_Van_preview.png")
    bpy.ops.render.render(write_still=True)


def main() -> None:
    clear()
    bpy.context.scene.unit_settings.system = "METRIC"
    m = palette()
    coll = collection("ASV3_Second_Hand_Dispatch_Van")
    preview = collection("ASV3_Preview_Only_Not_Exported")
    make_dispatch_van(coll, m)
    add_preview_floor(preview, m)
    lights_camera()
    export(coll)


if __name__ == "__main__":
    main()
