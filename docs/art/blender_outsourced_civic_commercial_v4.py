"""
AccidentSquad outsourced civic horror commercial vertical-slice assets.

Run on Windows from Blender:
  blender --background --factory-startup --python D:/AccidentSquad/docs/art/blender_outsourced_civic_commercial_v4.py

Output:
  D:/AccidentSquad/Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4

The target is production-ready stylized low-poly art: municipal debt noir,
cheap, dirty, dispatch-readable, and no longer a graybox or proof-of-style
sketch.
"""

from __future__ import annotations

import math
from pathlib import Path

import bpy
from mathutils import Vector


OUT = Path(r"D:/AccidentSquad/Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4")


def clear() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in list(bpy.data.materials):
        if block.users == 0:
            bpy.data.materials.remove(block)


def material(name: str, color: tuple[float, float, float, float], emission: float = 0.0) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = 0.88
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = 0.0
        if emission > 0:
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = color
            if "Emission Strength" in bsdf.inputs:
                bsdf.inputs["Emission Strength"].default_value = emission
    return mat


def palette() -> dict[str, bpy.types.Material]:
    return {
        "wall": material("ASV4_civic_teal_wall", (0.184, 0.310, 0.294, 1)),
        "wall_shadow": material("ASV4_deep_civic_teal_shadow", (0.090, 0.141, 0.133, 1)),
        "floor": material("ASV4_dead_rubber_civic_floor", (0.137, 0.157, 0.145, 1)),
        "floor_line": material("ASV4_dead_rubber_grout", (0.067, 0.078, 0.075, 1)),
        "paper": material("ASV4_aged_paper", (0.839, 0.784, 0.608, 1)),
        "paper_dark": material("ASV4_old_notice_back", (0.525, 0.478, 0.345, 1)),
        "cardboard": material("ASV4_cheap_cardboard", (0.451, 0.314, 0.165, 1)),
        "wood": material("ASV4_second_hand_wood", (0.290, 0.192, 0.098, 1)),
        "metal": material("ASV4_dead_rubber_metal", (0.067, 0.078, 0.075, 1)),
        "metal_worn": material("ASV4_worn_civic_teal_metal", (0.125, 0.208, 0.196, 1)),
        "black": material("ASV4_dead_rubber_black", (0.067, 0.078, 0.075, 1)),
        "terminal": material("ASV4_dispatch_green", (0.482, 0.812, 0.541, 1), 0.24),
        "exit": material("ASV4_muted_dispatch_exit_green", (0.133, 0.349, 0.227, 1), 0.18),
        "debt": material("ASV4_stamp_red_debt", (0.761, 0.227, 0.169, 1), 0.02),
        "eye": material("ASV4_monster_eye_red", (1.0, 0.025, 0.0, 1), 1.4),
        "cyan": material("ASV4_sickly_fluorescent_bone", (0.790, 0.761, 0.667, 1), 0.22),
        "skin": material("ASV4_worker_skin", (0.56, 0.40, 0.30, 1)),
        "uniform": material("ASV4_worker_tired_fabric", (0.173, 0.196, 0.169, 1)),
        "vest": material("ASV4_sodium_safety_vest", (0.851, 0.604, 0.192, 1)),
        "helmet": material("ASV4_dirty_bone_helmet", (0.788, 0.761, 0.667, 1)),
        "glass": material("ASV4_dirty_green_black_glass", (0.110, 0.235, 0.243, 1)),
        "van_body": material("ASV4_civic_fleet_teal_van_body", (0.184, 0.310, 0.294, 1)),
        "van_shadow": material("ASV4_van_dead_rubber_lower_panel", (0.137, 0.157, 0.145, 1)),
        "amber": material("ASV4_old_sodium_amber_light", (0.851, 0.604, 0.192, 1), 0.32),
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


def bevel(obj: bpy.types.Object, amount: float) -> None:
    if amount <= 0:
        return
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    mod = obj.modifiers.new("ASV4_single_segment_bevel", "BEVEL")
    mod.width = amount
    mod.segments = 1
    mod.affect = "EDGES"
    bpy.ops.object.modifier_apply(modifier=mod.name)
    normals = obj.modifiers.new("ASV4_weighted_normals", "WEIGHTED_NORMAL")
    bpy.ops.object.modifier_apply(modifier=normals.name)
    obj.select_set(False)


def cube(name: str, loc, scale, mat, coll, rot=(0, 0, 0), edge: float = 0.01) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel(obj, edge)
    return link(obj, coll)


def cyl(name: str, loc, radius: float, depth: float, mat, coll, vertices: int = 12, rot=(0, 0, 0), edge: float = 0.0) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    for poly in obj.data.polygons:
        poly.use_smooth = False
    bevel(obj, edge)
    return link(obj, coll)


def sphere(name: str, loc, scale, mat, coll) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for poly in obj.data.polygons:
        poly.use_smooth = False
    return link(obj, coll)


def txt(name: str, body: str, loc, rot, size: float, mat, coll) -> bpy.types.Object:
    bpy.ops.object.text_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.body = body
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.004
    obj.data.materials.append(mat)
    bpy.ops.object.convert(target="MESH")
    return link(bpy.context.object, coll)


def sign(name: str, body: str, loc, scale, bg, fg, coll, size_ratio: float = 0.32) -> None:
    cube(name + "_plate", loc, scale, bg, coll, edge=0.004)
    txt(name + "_text", body, (loc[0], loc[1] - scale[1] - 0.012, loc[2] + 0.002), (math.radians(90), 0, 0), min(scale[0], scale[2]) * size_ratio, fg, coll)


def tile_floor(prefix: str, center, half_x: float, half_y: float, mat, grout, coll) -> None:
    cube(prefix + "_floor", center, (half_x, half_y, 0.025), mat, coll, edge=0.002)
    for i in range(1, int(half_x * 2)):
        x = center[0] - half_x + i
        cube(f"{prefix}_grout_x_{i}", (x, center[1], center[2] + 0.028), (0.006, half_y, 0.002), grout, coll, edge=0)
    for i in range(1, int(half_y * 2)):
        y = center[1] - half_y + i
        cube(f"{prefix}_grout_y_{i}", (center[0], y, center[2] + 0.029), (half_x, 0.006, 0.002), grout, coll, edge=0)


def prism(name: str, points: list[tuple[float, float]], half_width: float, mat, coll) -> bpy.types.Object:
    verts = []
    for y in (-half_width, half_width):
        verts.extend((x, y, z) for x, z in points)
    n = len(points)
    faces = [tuple(reversed(range(n))), tuple(range(n, n * 2))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, j + n, i + n))
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    obj.data.materials.append(mat)
    coll.objects.link(obj)
    bevel(obj, 0.025)
    return obj


def build_hq(m) -> bpy.types.Collection:
    coll = collection("ASV4_HQ_Rundown_Commission_Office")
    tile_floor("hq", (0, 0, -0.035), 5.2, 3.8, m["floor"], m["floor_line"], coll)
    cube("hq_back_corrugated_wall", (0, 3.78, 1.48), (5.2, 0.08, 1.48), m["wall"], coll, edge=0.008)
    cube("hq_left_wall", (-5.2, 0, 1.48), (0.08, 3.8, 1.48), m["wall_shadow"], coll, edge=0.008)
    cube("hq_right_wall", (5.2, 0.4, 1.48), (0.08, 3.4, 1.48), m["wall_shadow"], coll, edge=0.008)
    cube("hq_ceiling_tiles", (0, 0, 2.92), (5.12, 3.72, 0.035), m["metal"], coll, edge=0.003)

    cube("hq_counter_front", (-1.35, 2.72, 0.267), (1.45, 0.32, 0.58), m["wood"], coll, edge=0.008)
    cube("hq_counter_top", (-1.35, 2.40, 0.585), (1.52, 0.14, 0.055), m["metal"], coll, edge=0.004)
    cube("hq_crt_base", (-1.35, 2.20, 0.640), (0.52, 0.24, 0.040), m["metal"], coll, edge=0.004)
    cube("hq_crt_neck", (-1.35, 2.20, 0.710), (0.10, 0.10, 0.13), m["metal"], coll, edge=0.004)
    cube("hq_crt_body", (-1.35, 2.22, 0.875), (0.42, 0.26, 0.22), m["metal"], coll, edge=0.012)
    cube("hq_crt_screen", (-1.35, 1.93, 0.875), (0.30, 0.022, 0.13), m["terminal"], coll, edge=0.004)
    txt("hq_terminal_readout", "JOBS  DEBT  SHOP", (-1.35, 1.905, 0.985), (math.radians(90), 0, 0), 0.064, m["terminal"], coll)
    cube("hq_keyboard_keys", (-1.35, 2.16, 0.645), (0.48, 0.15, 0.018), m["black"], coll, edge=0.002)
    for i in range(6):
        cube(f"hq_keyboard_key_row_{i}", (-1.56 + i * 0.082, 2.02, 0.670), (0.022, 0.011, 0.005), m["paper_dark"], coll, edge=0)
    cube("hq_receipt_printer", (-1.92, 2.18, 0.665), (0.30, 0.20, 0.11), m["paper"], coll, edge=0.004)
    cube("hq_receipt_trail_a", (-2.02, 1.98, 0.630), (0.18, 0.26, 0.012), m["paper"], coll, edge=0.001)
    cube("hq_receipt_trail_b", (-2.04, 1.72, 0.630), (0.18, 0.24, 0.010), m["paper"], coll, rot=(0, 0, math.radians(7)), edge=0.001)

    cube("hq_debt_board", (2.05, 3.70, 1.56), (1.28, 0.035, 0.78), m["debt"], coll, edge=0.005)
    txt("hq_debt_board_text", "OVERDUE\nTAKEOVER\nPRESSURE", (2.05, 3.655, 1.58), (math.radians(90), 0, 0), 0.15, m["paper"], coll)
    for i in range(14):
        cube(f"hq_wall_notice_{i}", (0.86 + (i % 7) * 0.35, 3.64, 0.58 + (i // 7) * 0.28), (0.11, 0.010, 0.070), m["paper" if i % 5 else "debt"], coll, edge=0.001)

    cube("hq_company_mark_backplate", (0.10, 3.70, 1.82), (0.78, 0.024, 0.44), m["black"], coll, edge=0.003)
    cube("hq_company_mark_top", (0.10, 3.665, 1.98), (0.48, 0.012, 0.035), m["terminal"], coll, edge=0.001)
    cube("hq_company_mark_left", (-0.13, 3.665, 1.82), (0.055, 0.012, 0.34), m["terminal"], coll, edge=0.001)
    cube("hq_company_mark_right", (0.33, 3.665, 1.82), (0.055, 0.012, 0.34), m["terminal"], coll, edge=0.001)
    cube("hq_company_mark_debt_slash", (0.10, 3.650, 1.82), (0.07, 0.010, 0.52), m["debt"], coll, rot=(0, math.radians(-24), 0), edge=0)

    cube("hq_equipment_shelf_left_upright", (-4.56, 3.42, 0.79), (0.08, 0.16, 1.54), m["metal"], coll, edge=0.006)
    cube("hq_equipment_shelf_right_upright", (-3.74, 3.42, 0.79), (0.08, 0.16, 1.54), m["metal"], coll, edge=0.006)
    cube("hq_equipment_shelf_back_brace", (-4.15, 3.48, 1.52), (0.86, 0.06, 0.08), m["metal"], coll, edge=0.003)
    for z in (0.36, 0.84, 1.32):
        cube(f"hq_equipment_shelf_plank_{z}", (-4.15, 3.18, z), (0.90, 0.10, 0.035), m["metal_worn"], coll, edge=0.002)
    cube("hq_medkit", (-4.58, 3.02, 0.50), (0.17, 0.13, 0.12), m["paper"], coll, edge=0.004)
    cube("hq_medkit_cross_h", (-4.58, 2.875, 0.50), (0.10, 0.009, 0.018), m["debt"], coll, edge=0)
    cube("hq_medkit_cross_v", (-4.58, 2.875, 0.50), (0.025, 0.009, 0.065), m["debt"], coll, edge=0)
    cyl("hq_spray_can", (-4.16, 2.99, 0.52), 0.060, 0.26, m["cyan"], coll, vertices=10, edge=0.002)
    cyl("hq_decoy_bell", (-4.48, 2.99, 0.88), 0.075, 0.08, m["cardboard"], coll, vertices=12, edge=0.002)
    cyl("hq_flashlight", (-3.73, 2.99, 1.00), 0.050, 0.36, m["black"], coll, vertices=10, rot=(0, math.radians(90), 0), edge=0.002)

    cube("hq_garage_floor_lane", (2.50, -1.82, 0.010), (2.00, 1.18, 0.018), m["wall_shadow"], coll, edge=0.002)
    for i in range(8):
        stripe = cube(f"hq_garage_hazard_chevron_{i}", (1.55 + i * 0.28, -0.75, 0.045), (0.17, 0.018, 0.035), m["helmet"] if i % 2 == 0 else m["black"], coll, rot=(0, 0, math.radians(24 if i % 2 == 0 else -24)), edge=0)
        stripe.name = f"hq_garage_hazard_chevron_{i}"
    for i in range(7):
        cube(f"hq_rollup_door_panel_{i}", (2.50, -3.72, 0.48 + i * 0.26), (1.72, 0.050, 0.095), m["metal"], coll, edge=0.002)
    cube("hq_green_departure_beacon", (2.50, -3.62, 2.30), (0.22, 0.035, 0.055), m["terminal"], coll, edge=0.006)
    cube("hq_garage_amber_work_light", (2.50, -3.54, 2.12), (1.10, 0.035, 0.045), m["amber"], coll, edge=0.004)
    cube("hq_garage_flood_lamp_housing", (2.50, -3.48, 2.22), (1.25, 0.055, 0.055), m["black"], coll, edge=0.004)

    cube("hq_sofa_contact_shadow", (3.42, 2.04, -0.010), (1.05, 0.40, 0.018), m["black"], coll, edge=0.001)
    cube("hq_sofa_base", (3.42, 2.04, 0.158), (0.95, 0.34, 0.36), m["uniform"], coll, edge=0.006)
    cube("hq_sofa_back", (3.42, 2.32, 0.388), (0.95, 0.08, 0.82), m["uniform"], coll, edge=0.006)
    cube("hq_missing_cushion_dark", (3.05, 2.04, 0.338), (0.24, 0.25, 0.030), m["black"], coll, edge=0.002)
    cube("hq_office_chair_seat", (-1.35, 1.52, 0.30), (0.46, 0.38, 0.08), m["uniform"], coll, edge=0.004)
    cube("hq_office_chair_back", (-1.35, 1.68, 0.58), (0.46, 0.08, 0.48), m["uniform"], coll, edge=0.004)
    cube("hq_office_chair_stem", (-1.35, 1.52, 0.118), (0.07, 0.07, 0.28), m["metal"], coll, edge=0.003)
    cube("hq_filing_cabinet_contact_shadow", (-2.75, 3.32, -0.010), (0.52, 0.40, 0.018), m["black"], coll, edge=0.001)
    cube("hq_filing_cabinet_grounded", (-2.75, 3.32, 0.578), (0.44, 0.34, 1.20), m["metal_worn"], coll, edge=0.005)
    for i in range(3):
        cube(f"hq_filing_cabinet_handle_{i}", (-2.75, 3.13, 0.91 - i * 0.26), (0.24, 0.010, 0.026), m["paper_dark"], coll, edge=0)
    for i in range(8):
        x = -4.45 + (i % 2) * 0.50
        z = 0.35 + (i // 2) * 0.33
        cube(f"hq_archive_box_{i}", (x, -2.65, z), (0.22, 0.20, 0.15), m["cardboard"], coll, edge=0.003)
        cube(f"hq_archive_label_{i}", (x, -2.865, z + 0.02), (0.13, 0.010, 0.040), m["paper"], coll, edge=0)
    sign("hq_company_sign", "ACCIDENT SQUAD", (0, 3.72, 2.40), (1.25, 0.020, 0.13), m["black"], m["terminal"], coll)
    return coll


def build_school_map(m) -> bpy.types.Collection:
    coll = collection("ASV4_School_Lost_Item_Map")
    tile_floor("school_hall", (0, 8.5, -0.035), 2.45, 5.65, m["floor"], m["floor_line"], coll)
    cube("school_left_hall_wall", (-2.45, 8.5, 1.42), (0.08, 5.65, 1.42), m["wall"], coll, edge=0.006)
    cube("school_right_hall_wall", (2.45, 8.5, 1.42), (0.08, 5.65, 1.42), m["wall"], coll, edge=0.006)
    cube("school_hall_ceiling", (0, 8.5, 2.78), (2.40, 5.58, 0.040), m["black"], coll, edge=0.002)
    for i, yy in enumerate([4.3, 6.8, 9.3, 11.8]):
        cube(f"school_fluorescent_fixture_{i}", (0, yy, 2.62), (0.76, 0.045, 0.035), m["cyan"], coll, edge=0.004)
    for i in range(10):
        yy = 3.75 + i * 0.95
        cube(f"school_locker_body_{i}", (-2.30, yy, 0.94), (0.13, 0.34, 0.80), m["metal_worn"], coll, edge=0.004)
        cube(f"school_locker_handle_{i}", (-2.405, yy + 0.10, 0.94), (0.010, 0.030, 0.18), m["black"], coll, edge=0)
        cube(f"school_locker_overdue_{i}", (-2.41, yy - 0.06, 1.30), (0.010, 0.11, 0.045), m["debt"], coll, edge=0)

    for side, x, yy in (("left", -4.85, 6.75), ("right", 4.85, 9.8)):
        tile_floor(f"school_{side}_classroom", (x, yy, -0.032), 1.70, 1.48, m["floor"], m["floor_line"], coll)
        cube(f"school_{side}_back_wall", (x, yy + 1.48, 1.18), (1.70, 0.07, 1.18), m["wall"], coll, edge=0.006)
        cube(f"school_{side}_outer_wall", (x + (-1.70 if x < 0 else 1.70), yy, 1.18), (0.07, 1.48, 1.18), m["wall_shadow"], coll, edge=0.006)
        cube(f"school_{side}_blackboard", (x, yy + 1.405, 1.30), (0.90, 0.018, 0.28), m["uniform"], coll, edge=0.001)
        for row in range(2):
            for col in range(3):
                px = x - 0.72 + col * 0.72
                py = yy - 0.48 + row * 0.55
                cube(f"school_{side}_desk_{row}_{col}", (px, py, 0.42), (0.22, 0.17, 0.10), m["wood"], coll, edge=0.004)
                cube(f"school_{side}_chair_{row}_{col}", (px, py - 0.24, 0.27), (0.15, 0.11, 0.12), m["metal"], coll, edge=0.002)
        for i in range(7):
            cube(f"school_{side}_paper_clutter_{i}", (x - 1.0 + i * 0.30, yy - 1.0 + (i % 2) * 0.20, 0.052), (0.12, 0.075, 0.006), m["paper"], coll, rot=(0, 0, math.radians(-10 + i * 5)), edge=0)

    tile_floor("school_debt_room", (4.92, 12.05, -0.032), 1.48, 1.24, m["floor"], m["floor_line"], coll)
    cube("school_debt_room_back_wall", (4.92, 13.30, 1.18), (1.48, 0.07, 1.18), m["wall_shadow"], coll, edge=0.006)
    cube("school_debt_room_counter", (4.92, 12.36, 0.52), (1.02, 0.20, 0.22), m["metal"], coll, edge=0.005)
    sign("school_homework_debt_banner", "HOMEWORK\nDEBT\nOFFICE", (4.92, 13.22, 1.82), (1.05, 0.018, 0.17), m["debt"], m["paper"], coll, size_ratio=0.30)
    for i in range(12):
        cube(f"school_debt_notice_{i}", (4.25 + (i % 4) * 0.34, 13.16, 0.76 + (i // 4) * 0.22), (0.11, 0.010, 0.065), m["debt"] if i % 5 == 0 else m["paper"], coll, edge=0)

    cube("school_exit_floor_pad", (0, 3.12, 0.018), (1.22, 0.36, 0.010), m["exit"], coll, edge=0.002)
    sign("school_exit_wall_sign", "EXIT", (0, 2.92, 1.78), (0.60, 0.018, 0.15), m["exit"], m["black"], coll)
    cube("school_objective_table", (-4.86, 7.05, 0.42), (0.55, 0.34, 0.12), m["wood"], coll, edge=0.004)
    cube("school_objective_notebook_cover", (-4.74, 7.05, 0.56), (0.18, 0.13, 0.015), m["helmet"], coll, edge=0.002)
    cube("school_objective_notebook_label", (-4.79, 7.01, 0.578), (0.070, 0.045, 0.004), m["paper"], coll, edge=0)
    cube("school_objective_notebook_stamp", (-4.65, 6.95, 0.580), (0.062, 0.018, 0.004), m["debt"], coll, edge=0)
    return coll


def build_worker(m) -> bpy.types.Collection:
    coll = collection("ASV4_Worker_Cheap_Outsourced_Uniform")
    x, y = 0.0, 0.0
    cube("worker_left_boot", (x - 0.19, y, 0.08), (0.17, 0.26, 0.08), m["black"], coll, edge=0.006)
    cube("worker_right_boot", (x + 0.19, y, 0.08), (0.17, 0.26, 0.08), m["black"], coll, edge=0.006)
    cube("worker_left_leg", (x - 0.19, y, 0.52), (0.13, 0.14, 0.40), m["uniform"], coll, edge=0.008)
    cube("worker_right_leg", (x + 0.19, y, 0.52), (0.13, 0.14, 0.40), m["uniform"], coll, edge=0.008)
    cube("worker_torso", (x, y, 1.12), (0.42, 0.22, 0.52), m["uniform"], coll, edge=0.012)
    cube("worker_vest_front", (x, y - 0.232, 1.14), (0.44, 0.018, 0.43), m["vest"], coll, edge=0.003)
    cube("worker_reflective_strip_a", (x, y - 0.248, 1.27), (0.36, 0.008, 0.022), m["paper"], coll, edge=0)
    cube("worker_reflective_strip_b", (x, y - 0.248, 1.03), (0.34, 0.008, 0.022), m["paper"], coll, edge=0)
    cube("worker_badge_green", (x + 0.13, y - 0.251, 1.38), (0.085, 0.006, 0.040), m["terminal"], coll, edge=0)
    cube("worker_left_arm_upper", (x - 0.50, y - 0.02, 1.26), (0.09, 0.10, 0.31), m["uniform"], coll, edge=0.006)
    cube("worker_left_arm_lower", (x - 0.53, y - 0.10, 0.84), (0.085, 0.09, 0.31), m["uniform"], coll, rot=(0, 0, math.radians(-7)), edge=0.006)
    cube("worker_right_arm_upper", (x + 0.50, y - 0.02, 1.24), (0.09, 0.10, 0.33), m["uniform"], coll, edge=0.006)
    cube("worker_right_arm_lower", (x + 0.56, y - 0.14, 0.78), (0.085, 0.09, 0.34), m["uniform"], coll, rot=(0, 0, math.radians(8)), edge=0.006)
    cube("worker_left_glove", (x - 0.55, y - 0.13, 0.49), (0.105, 0.10, 0.075), m["black"], coll, edge=0.004)
    cube("worker_right_glove", (x + 0.58, y - 0.18, 0.42), (0.105, 0.10, 0.075), m["black"], coll, edge=0.004)
    sphere("worker_head", (x, y, 1.72), (0.22, 0.19, 0.24), m["skin"], coll)
    cube("worker_helmet_cap", (x, y, 1.94), (0.30, 0.24, 0.075), m["helmet"], coll, edge=0.014)
    cube("worker_helmet_brim", (x, y - 0.20, 1.90), (0.22, 0.08, 0.022), m["helmet"], coll, edge=0.004)
    cube("worker_backpack", (x, y + 0.25, 1.10), (0.28, 0.10, 0.36), m["metal"], coll, edge=0.006)
    cube("worker_left_pack_strap", (x - 0.17, y - 0.232, 1.17), (0.040, 0.010, 0.40), m["black"], coll, edge=0)
    cube("worker_right_pack_strap", (x + 0.17, y - 0.232, 1.17), (0.040, 0.010, 0.40), m["black"], coll, edge=0)
    cyl("worker_flashlight", (x + 0.62, y - 0.23, 0.46), 0.044, 0.34, m["black"], coll, vertices=10, rot=(math.radians(90), 0, 0), edge=0.001)
    for i, px in enumerate((-0.22, -0.07, 0.08, 0.23)):
        cube(f"worker_belt_pouch_{i}", (x + px, y - 0.225, 0.82), (0.060, 0.015, 0.070), m["cardboard"], coll, edge=0.001)
    return coll


def build_monster(m) -> bpy.types.Collection:
    coll = collection("ASV4_Monster_Homework_Debt_Collector")
    x, y = 0.0, 0.0
    cube("collector_long_feet", (x, y, 0.08), (0.58, 0.30, 0.08), m["black"], coll, edge=0.006)
    cube("collector_lower_coat", (x, y, 0.80), (0.42, 0.22, 0.66), m["debt"], coll, edge=0.010)
    cube("collector_upper_coat", (x, y, 1.56), (0.52, 0.24, 0.70), m["debt"], coll, edge=0.010)
    cube("collector_coat_tail_left", (x - 0.22, y + 0.04, 0.32), (0.13, 0.06, 0.34), m["debt"], coll, rot=(0, 0, math.radians(-6)), edge=0.004)
    cube("collector_coat_tail_right", (x + 0.22, y + 0.04, 0.32), (0.13, 0.06, 0.34), m["debt"], coll, rot=(0, 0, math.radians(6)), edge=0.004)
    cube("collector_paper_collar", (x, y - 0.19, 2.00), (0.44, 0.022, 0.16), m["paper"], coll, edge=0.004)
    sphere("collector_receipt_head", (x, y, 2.40), (0.24, 0.18, 0.34), m["paper"], coll)
    cube("collector_eye_bar_glow", (x, y - 0.188, 2.47), (0.32, 0.006, 0.058), m["eye"], coll, edge=0.008)
    cube("collector_eye_left", (x - 0.10, y - 0.196, 2.47), (0.045, 0.010, 0.026), m["black"], coll, edge=0)
    cube("collector_eye_right", (x + 0.10, y - 0.196, 2.47), (0.045, 0.010, 0.026), m["black"], coll, edge=0)
    cube("collector_left_upper_arm", (x - 0.58, y, 1.44), (0.085, 0.10, 0.54), m["debt"], coll, edge=0.006)
    cube("collector_right_upper_arm", (x + 0.58, y, 1.44), (0.085, 0.10, 0.54), m["debt"], coll, edge=0.006)
    cube("collector_left_lower_arm", (x - 0.80, y - 0.08, 0.78), (0.075, 0.085, 0.66), m["debt"], coll, edge=0.005)
    cube("collector_right_lower_arm", (x + 0.80, y - 0.08, 0.78), (0.075, 0.085, 0.66), m["debt"], coll, edge=0.005)
    cube("collector_ledger", (x - 0.96, y - 0.17, 0.56), (0.20, 0.032, 0.27), m["black"], coll, edge=0.004)
    cube("collector_ledger_label", (x - 0.96, y - 0.204, 0.65), (0.13, 0.006, 0.044), m["paper"], coll, edge=0)
    txt("collector_ledger_text", "LATE", (x - 0.96, y - 0.214, 0.652), (math.radians(90), 0, 0), 0.043, m["debt"], coll)
    for i in range(8):
        cube(f"collector_back_receipt_{i}", (x, y + 0.19, 2.18 - i * 0.082), (0.16, 0.014, 0.028), m["paper"], coll, edge=0.001)
    for i in range(5):
        cube(f"collector_loose_form_trail_{i}", (x + 0.25 - i * 0.09, y + 0.23 + i * 0.04, 2.10 - i * 0.16), (0.12, 0.008, 0.060), m["paper"], coll, rot=(0, 0, math.radians(12 - i * 5)), edge=0)
    return coll


def build_notebook(m) -> bpy.types.Collection:
    coll = collection("ASV4_Missing_Homework_Notebook")
    x, y = 0.0, 0.0
    cube("notebook_cover", (x, y, 0.075), (0.34, 0.24, 0.030), m["helmet"], coll, edge=0.004)
    cube("notebook_pages", (x + 0.025, y + 0.018, 0.045), (0.32, 0.225, 0.015), m["paper"], coll, edge=0.002)
    cube("notebook_name_label", (x - 0.065, y - 0.030, 0.109), (0.15, 0.082, 0.005), m["paper"], coll, edge=0.001)
    txt("notebook_hw_text", "HW", (x - 0.065, y - 0.030, 0.118), (0, 0, 0), 0.065, m["black"], coll)
    cube("notebook_overdue_stamp", (x + 0.115, y - 0.120, 0.112), (0.110, 0.024, 0.005), m["debt"], coll, edge=0)
    for i in range(5):
        cyl(f"notebook_spiral_{i}", (x - 0.30, y - 0.13 + i * 0.065, 0.114), 0.010, 0.010, m["black"], coll, vertices=8, rot=(math.radians(90), 0, 0))
    cube("notebook_parent_note", (x + 0.10, y + 0.08, 0.116), (0.105, 0.052, 0.005), m["paper"], coll, edge=0)
    return coll


def build_van(m) -> bpy.types.Collection:
    coll = collection("ASV4_Second_Hand_Dispatch_Van")
    body = [
        (-1.85, 0.34), (-1.75, 0.62), (-1.44, 0.78), (-1.14, 1.07),
        (-0.82, 1.38), (1.22, 1.38), (1.60, 1.14), (1.64, 0.46),
        (1.38, 0.34)
    ]
    prism("van_body_sloped_one_piece", body, 0.72, m["van_body"], coll)
    cube("van_closed_side_skin_L", (-0.04, -0.745, 0.86), (3.05, 0.018, 0.72), m["van_body"], coll, edge=0.004)
    cube("van_closed_side_skin_R", (-0.04, 0.745, 0.86), (3.05, 0.018, 0.72), m["van_body"], coll, edge=0.004)
    cube("van_lower_dirty_panel_L", (-0.06, -0.735, 0.47), (1.48, 0.016, 0.095), m["van_shadow"], coll, edge=0.003)
    cube("van_lower_dirty_panel_R", (-0.06, 0.735, 0.47), (1.48, 0.016, 0.095), m["van_shadow"], coll, edge=0.003)
    cube("van_front_bumper", (-1.88, 0, 0.45), (0.08, 0.66, 0.085), m["metal"], coll, edge=0.006)
    cube("van_rear_bumper", (1.68, 0, 0.45), (0.08, 0.66, 0.085), m["metal"], coll, edge=0.006)
    cube("van_front_grille", (-1.91, 0, 0.62), (0.014, 0.33, 0.075), m["black"], coll, edge=0)
    cube("van_company_patch_L", (0.45, -0.757, 0.72), (0.34, 0.010, 0.12), m["terminal"], coll, edge=0.001)
    cube("van_company_patch_R", (0.45, 0.757, 0.72), (0.34, 0.010, 0.12), m["terminal"], coll, edge=0.001)
    txt("van_company_text_L", "AS", (0.45, -0.773, 0.72), (math.radians(90), 0, 0), 0.13, m["black"], coll)
    txt("van_company_text_R", "AS", (0.45, 0.773, 0.72), (math.radians(90), 0, math.radians(180)), 0.13, m["black"], coll)
    cube("van_service_label_L", (-0.12, -0.758, 0.58), (0.40, 0.009, 0.050), m["paper"], coll, edge=0)
    cube("van_service_label_R", (-0.12, 0.758, 0.58), (0.40, 0.009, 0.050), m["paper"], coll, edge=0)
    txt("van_service_text_L", "CIVIC JOBS", (-0.12, -0.774, 0.58), (math.radians(90), 0, 0), 0.052, m["black"], coll)
    txt("van_service_text_R", "CIVIC JOBS", (-0.12, 0.774, 0.58), (math.radians(90), 0, math.radians(180)), 0.052, m["black"], coll)
    cube("van_debt_slash_L", (1.04, -0.758, 0.79), (0.25, 0.010, 0.035), m["debt"], coll, rot=(0, 0, math.radians(-10)), edge=0)
    cube("van_debt_slash_R", (1.04, 0.758, 0.79), (0.25, 0.010, 0.035), m["debt"], coll, rot=(0, 0, math.radians(-10)), edge=0)
    for i, x in enumerate((-1.18, 1.10)):
        for side, y in (("L", -0.78), ("R", 0.78)):
            cyl(f"van_wheel_{side}_{i}", (x, y, 0.34), 0.29, 0.20, m["black"], coll, vertices=18, rot=(math.radians(90), 0, 0), edge=0.003)
            cyl(f"van_wheel_hub_{side}_{i}", (x, y + (-0.105 if y < 0 else 0.105), 0.34), 0.115, 0.030, m["metal"], coll, vertices=10, rot=(math.radians(90), 0, 0), edge=0.001)
    cube("van_roof_rack_front", (0.10, 0, 1.52), (0.75, 0.66, 0.024), m["metal"], coll, edge=0.004)
    cube("van_roof_rack_rear", (0.88, 0, 1.52), (0.75, 0.66, 0.024), m["metal"], coll, edge=0.004)
    cyl("van_roof_pipe_left", (0.50, -0.43, 1.60), 0.034, 1.12, m["metal"], coll, vertices=8, rot=(0, math.radians(90), 0), edge=0.001)
    cyl("van_roof_pipe_right", (0.50, 0.43, 1.60), 0.034, 1.12, m["metal"], coll, vertices=8, rot=(0, math.radians(90), 0), edge=0.001)
    cube("van_roof_amber_beacon", (-0.55, 0, 1.54), (0.16, 0.09, 0.050), m["amber"], coll, edge=0.006)
    cube("van_rear_green_beacon", (1.704, 0, 1.20), (0.014, 0.15, 0.038), m["terminal"], coll, edge=0.001)
    return coll


def lights_camera() -> None:
    bpy.ops.object.light_add(type="AREA", location=(1.5, -8.0, 7.0))
    light = bpy.context.object
    light.name = "ASV4_preview_large_softbox"
    light.data.energy = 900
    light.data.size = 7
    bpy.ops.object.light_add(type="POINT", location=(0, 1.8, 2.0))
    glow = bpy.context.object
    glow.name = "ASV4_preview_terminal_glow"
    glow.data.energy = 90
    glow.data.color = (0.18, 1.0, 0.48)
    cam_loc = Vector((8.0, -15.0, 7.5))
    target = Vector((0.0, 3.8, 1.0))
    bpy.ops.object.camera_add(location=cam_loc)
    cam = bpy.context.object
    cam.name = "ASV4_preview_camera"
    cam.rotation_euler = (target - cam_loc).to_track_quat("-Z", "Y").to_euler()
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 16.0
    bpy.context.scene.camera = cam


def select_collection(coll: bpy.types.Collection) -> list[bpy.types.Object]:
    bpy.ops.object.select_all(action="DESELECT")
    objs: list[bpy.types.Object] = []

    def walk(c: bpy.types.Collection) -> None:
        for obj in c.objects:
            if obj.type == "MESH":
                obj.select_set(True)
                objs.append(obj)
        for child in c.children:
            walk(child)

    walk(coll)
    if objs:
        bpy.context.view_layer.objects.active = objs[0]
    return objs


def origin_offset(objs: list[bpy.types.Object]) -> Vector:
    if not objs:
        return Vector((0, 0, 0))
    corners = [obj.matrix_world @ Vector(corner) for obj in objs for corner in obj.bound_box]
    low = Vector((min(c.x for c in corners), min(c.y for c in corners), min(c.z for c in corners)))
    high = Vector((max(c.x for c in corners), max(c.y for c in corners), max(c.z for c in corners)))
    return Vector(((low.x + high.x) * 0.5, (low.y + high.y) * 0.5, low.z))


def export_collection(coll: bpy.types.Collection, filename: str) -> None:
    objs = select_collection(coll)
    offset = origin_offset(objs)
    for obj in objs:
        obj.location -= offset
    try:
        bpy.ops.export_scene.fbx(
            filepath=str(OUT / f"{filename}.fbx"),
            use_selection=True,
            apply_unit_scale=True,
            object_types={"MESH"},
            add_leaf_bones=False,
            use_mesh_modifiers=True,
            axis_forward="-Z",
            axis_up="Y",
        )
    finally:
        for obj in objs:
            obj.location += offset


def export_all(assets: dict[str, bpy.types.Collection]) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "accidentsquad_outsourced_civic_commercial_v4.blend"))
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(OUT / "accidentsquad_outsourced_civic_commercial_v4_showcase.fbx"),
        use_selection=True,
        apply_unit_scale=True,
        object_types={"MESH", "LIGHT", "CAMERA"},
        add_leaf_bones=False,
        use_mesh_modifiers=True,
        axis_forward="-Z",
        axis_up="Y",
    )
    for filename, coll in assets.items():
        export_collection(coll, filename)
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1600
    bpy.context.scene.render.resolution_y = 900
    bpy.context.scene.render.filepath = str(OUT / "accidentsquad_outsourced_civic_commercial_v4_preview.png")
    bpy.ops.render.render(write_still=True)


def main() -> None:
    clear()
    bpy.context.scene.unit_settings.system = "METRIC"
    m = palette()
    assets = {
        "ASV4_HQ_Rundown_Commission_Office": build_hq(m),
        "ASV4_School_Lost_Item_Map": build_school_map(m),
        "ASV4_Worker_Cheap_Outsourced_Uniform": build_worker(m),
        "ASV4_Monster_Homework_Debt_Collector": build_monster(m),
        "ASV4_Missing_Homework_Notebook": build_notebook(m),
        "ASV4_Second_Hand_Dispatch_Van": build_van(m),
    }
    lights_camera()
    export_all(assets)


if __name__ == "__main__":
    main()
