"""
AccidentSquad outsourced civic horror commercial vertical-slice assets.

Run from Blender:
  blender --background --factory-startup --python docs/art/blender_outsourced_civic_commercial_v4.py

Output:
  Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4

The target is production-ready stylized low-poly art: municipal debt noir,
cheap, dirty, dispatch-readable, and no longer a graybox or proof-of-style
sketch.
"""

from __future__ import annotations

import math
import os
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def _find_project_root() -> Path:
    here = Path(__file__).resolve().parent
    for ancestor in (here, *here.parents):
        if (ancestor / "Assets").is_dir() and (ancestor / "ProjectSettings").is_dir():
            return ancestor
    if sys.platform == "win32":
        return Path(r"D:/AccidentSquad")
    return Path.home() / "Desktop" / "codespace" / "AccidentSquad"


OUT = _find_project_root() / "Assets" / "_Project" / "Art" / "Generated" / "OutsourcedCivicCommercial_v4"


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
        "wall": material("ASV4_old_concrete_wall", (0.430, 0.398, 0.325, 1)),
        "wall_shadow": material("ASV4_deep_green_black_wall", (0.095, 0.135, 0.112, 1)),
        "ceiling_tile": material("ASV4_smoke_stained_ceiling_tile", (0.220, 0.212, 0.180, 1)),
        "ceiling_shadow": material("ASV4_tar_black_ceiling_grid", (0.050, 0.057, 0.052, 1)),
        "floor": material("ASV4_oil_stained_concrete_floor", (0.285, 0.275, 0.240, 1)),
        "floor_line": material("ASV4_dark_concrete_grout", (0.080, 0.082, 0.072, 1)),
        "paper": material("ASV4_aged_yellowed_paper", (0.820, 0.760, 0.565, 1)),
        "paper_dark": material("ASV4_old_notice_brown_back", (0.455, 0.395, 0.265, 1)),
        "cardboard": material("ASV4_cheap_cardboard", (0.500, 0.345, 0.170, 1)),
        "wood": material("ASV4_scratched_second_hand_wood", (0.350, 0.220, 0.105, 1)),
        "metal": material("ASV4_blackened_shop_metal", (0.055, 0.063, 0.060, 1)),
        "metal_worn": material("ASV4_worn_gunmetal_green", (0.155, 0.180, 0.162, 1)),
        "black": material("ASV4_dead_rubber_black", (0.045, 0.050, 0.048, 1)),
        "terminal": material("ASV4_dispatch_terminal_green", (0.570, 0.875, 0.380, 1), 0.32),
        "exit": material("ASV4_muted_dispatch_exit_green", (0.180, 0.420, 0.235, 1), 0.22),
        "debt": material("ASV4_rust_red_debt_stamp", (0.600, 0.170, 0.125, 1), 0.02),
        "eye": material("ASV4_monster_eye_red", (1.0, 0.025, 0.0, 1), 1.4),
        "cyan": material("ASV4_sickly_fluorescent_bone", (0.790, 0.761, 0.667, 1), 0.22),
        "skin": material("ASV4_worker_skin", (0.56, 0.40, 0.30, 1)),
        "mask": material("ASV4_gas_mask_rubber", (0.082, 0.090, 0.082, 1)),
        "uniform": material("ASV4_worker_tired_fabric", (0.173, 0.196, 0.169, 1)),
        "vest": material("ASV4_sodium_safety_vest", (0.851, 0.604, 0.192, 1)),
        "helmet": material("ASV4_dirty_bone_helmet", (0.788, 0.761, 0.667, 1)),
        "glass": material("ASV4_dirty_green_black_glass", (0.100, 0.180, 0.170, 1)),
        "van_body": material("ASV4_dirty_ivory_service_van", (0.620, 0.585, 0.500, 1)),
        "van_shadow": material("ASV4_van_dead_rubber_lower_panel", (0.150, 0.150, 0.130, 1)),
        "amber": material("ASV4_old_sodium_amber_light", (0.900, 0.635, 0.190, 1), 0.38),
        "incandescent": material("ASV4_warm_incandescent_panel", (1.0, 0.910, 0.720, 1), 1.25),
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
    # Top-down plan matches the art reference: left office, right garage,
    # shared rectangular footprint, bottom office entry and bottom garage door.
    tile_floor("hq_office", (-2.55, 0.00, -0.035), 4.90, 6.40, m["floor"], m["floor_line"], coll)
    tile_floor("hq_garage", (2.75, 0.00, -0.035), 4.90, 6.40, m["wall_shadow"], m["floor_line"], coll)
    cube("hq_back_office_wall", (-2.55, 3.25, 1.35), (4.90, 0.08, 2.76), m["wall"], coll, edge=0.008)
    cube("hq_back_garage_wall", (2.75, 3.25, 1.35), (4.90, 0.08, 2.76), m["wall"], coll, edge=0.008)
    cube("hq_left_office_wall", (-5.05, 0.00, 1.35), (0.08, 6.48, 2.76), m["wall_shadow"], coll, edge=0.008)
    cube("hq_garage_outer_right_wall", (5.25, 0.00, 1.35), (0.08, 6.48, 2.76), m["wall_shadow"], coll, edge=0.008)
    cube("hq_front_office_wall_left", (-4.10, -3.25, 1.35), (1.85, 0.08, 2.76), m["wall_shadow"], coll, edge=0.008)
    cube("hq_front_office_wall_right", (-1.15, -3.25, 1.35), (1.20, 0.08, 2.76), m["wall_shadow"], coll, edge=0.008)
    cube("hq_front_garage_wall_left", (0.85, -3.25, 1.35), (1.20, 0.08, 2.76), m["wall_shadow"], coll, edge=0.008)
    cube("hq_front_garage_wall_right", (4.65, -3.25, 1.35), (1.20, 0.08, 2.76), m["wall_shadow"], coll, edge=0.008)
    # Central divider: a real wall between office and garage, with a bottom
    # passage so the route can turn from the office into the van bay.
    cube("hq_office_garage_divider_wall", (0.05, 0.90, 1.35), (0.10, 4.70, 2.76), m["wall_shadow"], coll, edge=0.008)
    cube("hq_office_garage_divider_stub", (0.05, -2.75, 1.35), (0.10, 0.90, 2.76), m["wall_shadow"], coll, edge=0.008)
    cube("hq_open_garage_header", (2.75, -3.25, 2.42), (2.45, 0.07, 0.22), m["metal"], coll, edge=0.004)
    # Roof/ceiling structure sits directly on the wall tops. It is made from
    # tiles and perimeter bands rather than one solid lid, so the top-down plan
    # stays readable while the first-person view has an actual ceiling.
    cube("hq_office_wall_cap_back", (-2.55, 3.25, 2.78), (4.95, 0.12, 0.10), m["ceiling_shadow"], coll, edge=0.002)
    cube("hq_garage_wall_cap_back", (2.75, 3.25, 2.78), (4.95, 0.12, 0.10), m["ceiling_shadow"], coll, edge=0.002)
    cube("hq_office_wall_cap_left", (-5.05, 0.00, 2.78), (0.12, 6.50, 0.10), m["ceiling_shadow"], coll, edge=0.002)
    cube("hq_garage_wall_cap_right", (5.25, 0.00, 2.78), (0.12, 6.50, 0.10), m["ceiling_shadow"], coll, edge=0.002)
    cube("hq_divider_wall_cap", (0.05, 0.90, 2.78), (0.14, 4.72, 0.10), m["ceiling_shadow"], coll, edge=0.002)
    cube("hq_office_front_ceiling_band", (-2.60, -3.25, 2.78), (4.95, 0.12, 0.10), m["ceiling_shadow"], coll, edge=0.002)
    cube("hq_garage_front_ceiling_band", (2.75, -3.25, 2.78), (4.95, 0.12, 0.10), m["ceiling_shadow"], coll, edge=0.002)
    for ix, x in enumerate((-4.10, -2.55, -1.00)):
        for iy, y in enumerate((-1.75, 0.00, 1.75)):
            cube(f"hq_office_ceiling_tile_{ix}_{iy}", (x, y, 2.73), (1.26, 1.18, 0.035), m["ceiling_tile"], coll, edge=0.002)
    for ix, x in enumerate((1.45, 2.75, 4.05)):
        for iy, y in enumerate((-1.75, 0.00, 1.75)):
            cube(f"hq_garage_ceiling_tile_{ix}_{iy}", (x, y, 2.73), (1.06, 1.18, 0.035), m["ceiling_tile"], coll, edge=0.002)

    # Dispatch computer area — items stack on a proper office-height desk so nothing floats.
    # Desk: human-scale height (0.74m top), centered at z = 0.37.
    cube("hq_dispatch_desk", (-1.55, 2.38, 0.37), (1.55, 0.62, 0.74), m["wood"], coll, edge=0.010)
    # Drawer band: visible band of darker metal at the desk front face for readable silhouette.
    cube("hq_dispatch_desk_drawer_band", (-1.55, 2.075, 0.55), (1.40, 0.014, 0.18), m["metal_worn"], coll, edge=0.002)
    # Keyboard rests on the desk top (0.74m). Bottom of keyboard at z = 0.74 + small contact gap.
    cube("hq_keyboard", (-1.55, 2.18, 0.755), (0.52, 0.18, 0.030), m["black"], coll, edge=0.002)
    cube("hq_keyboard_keys", (-1.55, 2.18, 0.772), (0.48, 0.16, 0.006), m["metal_worn"], coll, edge=0)
    # CRT sits at the back of the desk. The screen center converts to Unity
    # (-1.55, 1.085, 1.704), which the runtime interaction collider uses.
    cube("hq_crt_base", (-1.55, 1.96, 0.775), (0.68, 0.34, 0.070), m["metal"], coll, edge=0.004)
    cube("hq_crt_neck", (-1.55, 1.90, 0.855), (0.20, 0.16, 0.13), m["metal"], coll, edge=0.004)
    cube("hq_crt_body", (-1.55, 1.95, 1.045), (0.72, 0.46, 0.52), m["metal"], coll, edge=0.014)
    cube("hq_crt_back_hump", (-1.55, 2.18, 1.045), (0.62, 0.26, 0.42), m["metal_worn"], coll, edge=0.010)
    cube("hq_crt_bezel_top", (-1.55, 1.696, 1.275), (0.58, 0.026, 0.045), m["black"], coll, edge=0.002)
    cube("hq_crt_bezel_bottom", (-1.55, 1.696, 0.895), (0.58, 0.026, 0.045), m["black"], coll, edge=0.002)
    cube("hq_crt_bezel_left", (-1.85, 1.696, 1.085), (0.045, 0.026, 0.34), m["black"], coll, edge=0.002)
    cube("hq_crt_bezel_right", (-1.25, 1.696, 1.085), (0.045, 0.026, 0.34), m["black"], coll, edge=0.002)
    cube("hq_crt_screen", (-1.55, 1.704, 1.085), (0.48, 0.018, 0.32), m["terminal"], coll, edge=0.004)
    txt("hq_terminal_readout", "JOBS", (-1.55, 1.686, 1.125), (math.radians(90), 0, 0), 0.110, m["black"], coll)
    txt("hq_terminal_readout_2", "READY", (-1.55, 1.686, 0.990), (math.radians(90), 0, 0), 0.058, m["black"], coll)
    cube("hq_crt_indicator", (-1.30, 1.690, 0.825), (0.12, 0.014, 0.024), m["terminal"], coll, edge=0)
    cube("hq_crt_power_button", (-1.78, 1.690, 0.825), (0.052, 0.014, 0.052), m["debt"], coll, edge=0.002)
    cube("hq_computer_beacon_column", (-1.55, 1.54, 1.88), (0.08, 0.08, 0.70), m["terminal"], coll, edge=0.002)
    cube("hq_computer_beacon_cap", (-1.55, 1.54, 2.42), (0.42, 0.10, 0.06), m["terminal"], coll, edge=0.003)
    txt("hq_computer_label", "COMPUTER", (-1.55, 1.50, 2.26), (math.radians(90), 0, 0), 0.115, m["black"], coll)
    for i in range(4):
        cube(f"hq_crt_side_vent_{i}", (-1.92, 1.96, 0.94 + i * 0.07), (0.012, 0.20, 0.014), m["black"], coll, edge=0)
    # Receipt printer on the desk corner — touches the top.
    cube("hq_receipt_printer", (-2.18, 2.18, 0.835), (0.34, 0.26, 0.180), m["paper"], coll, edge=0.005)
    cube("hq_receipt_printer_top", (-2.18, 2.18, 0.929), (0.32, 0.24, 0.012), m["metal"], coll, edge=0)
    # Receipt curling out of the printer slot, draping forward off the desk edge.
    cube("hq_receipt_trail_a", (-2.18, 1.97, 0.745), (0.18, 0.06, 0.012), m["paper"], coll, rot=(0, math.radians(-22), 0), edge=0.001)
    cube("hq_receipt_trail_b", (-2.20, 1.86, 0.62), (0.18, 0.34, 0.010), m["paper"], coll, rot=(0, 0, math.radians(5)), edge=0.001)
    # Standing dispatch point: clear 1.1m x 0.72m player space in front of the CRT.
    cube("hq_dispatch_standing_mat", (-1.55, 1.12, 0.014), (1.10, 0.72, 0.010), m["floor_line"], coll, edge=0.001)
    cube("hq_dispatch_mat_front_tape", (-1.55, 0.77, 0.024), (0.88, 0.035, 0.006), m["terminal"], coll, edge=0)
    cube("hq_dispatch_mat_left_wear", (-1.86, 1.08, 0.026), (0.18, 0.12, 0.005), m["black"], coll, rot=(0, 0, math.radians(-8)), edge=0)
    cube("hq_dispatch_mat_right_wear", (-1.30, 1.02, 0.026), (0.16, 0.11, 0.005), m["black"], coll, rot=(0, 0, math.radians(6)), edge=0)

    # Player-readable floor plan: spawn box, green computer route, yellow van route.
    cube("hq_player_spawn_box", (-2.55, -2.45, 0.032), (0.86, 0.58, 0.012), m["amber"], coll, edge=0.001)
    cube("hq_player_spawn_box_inner", (-2.55, -2.45, 0.045), (0.64, 0.38, 0.010), m["floor"], coll, edge=0.001)
    txt("hq_spawn_label", "START", (-2.55, -2.45, 0.058), (0, 0, 0), 0.17, m["paper"], coll)
    txt("hq_use_computer_floor_text", "USE COMPUTER", (-2.05, -1.35, 0.058), (0, 0, math.radians(-8)), 0.16, m["terminal"], coll)
    cube("hq_green_route_spawn_to_computer_a", (-2.55, -1.85, 0.050), (0.06, 0.34, 0.010), m["terminal"], coll, edge=0)
    cube("hq_green_route_spawn_to_computer_b", (-2.25, -1.15, 0.050), (0.06, 0.42, 0.010), m["terminal"], coll, rot=(0, 0, math.radians(-28)), edge=0)
    cube("hq_green_route_spawn_to_computer_c", (-1.82, -0.45, 0.050), (0.06, 0.42, 0.010), m["terminal"], coll, rot=(0, 0, math.radians(-12)), edge=0)
    cube("hq_green_route_arrow_head_l", (-1.92, 0.12, 0.052), (0.26, 0.05, 0.010), m["terminal"], coll, rot=(0, 0, math.radians(35)), edge=0)
    cube("hq_green_route_arrow_head_r", (-1.66, 0.12, 0.052), (0.26, 0.05, 0.010), m["terminal"], coll, rot=(0, 0, math.radians(-35)), edge=0)

    # Company identity wall, no random poster clutter.
    cube("hq_company_panel", (-0.28, 3.16, 1.72), (0.82, 0.026, 0.46), m["black"], coll, edge=0.003)
    cube("hq_company_mark_top", (-0.28, 3.13, 1.90), (0.48, 0.012, 0.035), m["terminal"], coll, edge=0.001)
    cube("hq_company_mark_left", (-0.51, 3.13, 1.72), (0.055, 0.012, 0.34), m["terminal"], coll, edge=0.001)
    cube("hq_company_mark_right", (-0.05, 3.13, 1.72), (0.055, 0.012, 0.34), m["terminal"], coll, edge=0.001)
    cube("hq_company_mark_debt_slash", (-0.28, 3.12, 1.72), (0.07, 0.010, 0.52), m["debt"], coll, rot=(0, math.radians(-24), 0), edge=0)
    txt("hq_company_sign", "ACCIDENT SQUAD", (-2.75, 3.15, 1.94), (math.radians(90), 0, 0), 0.16, m["terminal"], coll)
    cube("hq_debt_status_card", (0.55, 3.12, 1.36), (0.38, 0.015, 0.24), m["debt"], coll, edge=0.002)
    txt("hq_debt_status_text", "DEBT\nWATCH", (0.55, 3.10, 1.37), (math.radians(90), 0, 0), 0.052, m["paper"], coll)

    # Equipment shelf — proper office-storage rack, items actually rest on plank surfaces.
    # Frame: 1.0m wide x 0.40m deep x 1.85m tall, mounted against the back wall.
    shelf_cx, shelf_cy = -3.40, 2.62
    cube("hq_equipment_shelf_frame_l", (shelf_cx - 0.50, shelf_cy, 0.925), (0.06, 0.40, 1.85), m["metal"], coll, edge=0.004)
    cube("hq_equipment_shelf_frame_r", (shelf_cx + 0.50, shelf_cy, 0.925), (0.06, 0.40, 1.85), m["metal"], coll, edge=0.004)
    cube("hq_equipment_shelf_back_panel", (shelf_cx, shelf_cy + 0.18, 0.925), (1.00, 0.020, 1.85), m["metal_worn"], coll, edge=0.002)
    # Three plank shelves: 0.45m, 0.90m, 1.35m. Plank top z = plank_z + 0.018.
    plank_zs = (0.45, 0.90, 1.35)
    for pz in plank_zs:
        cube(f"hq_equipment_shelf_plank_{pz}", (shelf_cx, shelf_cy, pz), (0.98, 0.36, 0.036), m["metal_worn"], coll, edge=0.002)
    # Plank top surface heights (used for stacking items so nothing floats).
    p0_top, p1_top, p2_top = plank_zs[0] + 0.018, plank_zs[1] + 0.018, plank_zs[2] + 0.018

    # Bottom shelf: medkit (resting on top, so item_z = p0_top + half_height).
    medkit_h = 0.12
    cube("hq_medkit", (shelf_cx - 0.30, shelf_cy - 0.05, p0_top + medkit_h * 0.5), (0.22, 0.16, medkit_h), m["paper"], coll, edge=0.004)
    cube("hq_medkit_cross_h", (shelf_cx - 0.30, shelf_cy - 0.140, p0_top + medkit_h * 0.5),
         (0.10, 0.010, 0.020), m["debt"], coll, edge=0)
    cube("hq_medkit_cross_v", (shelf_cx - 0.30, shelf_cy - 0.140, p0_top + medkit_h * 0.5),
         (0.024, 0.010, 0.070), m["debt"], coll, edge=0)
    # Battery pack (cylinder) on bottom plank.
    cyl("hq_shelf_battery", (shelf_cx + 0.05, shelf_cy - 0.08, p0_top + 0.10), 0.038, 0.20, m["cardboard"], coll, vertices=8, edge=0.001)
    cyl("hq_shelf_battery_band", (shelf_cx + 0.05, shelf_cy - 0.08, p0_top + 0.10), 0.039, 0.038, m["debt"], coll, vertices=8, edge=0)

    # Middle shelf: flashlight (lies on its side, so item half-radius = 0.045).
    cyl("hq_shelf_flashlight", (shelf_cx - 0.25, shelf_cy - 0.05, p1_top + 0.045), 0.044, 0.34, m["black"], coll,
        vertices=10, rot=(0, math.radians(90), 0), edge=0.002)
    cyl("hq_shelf_flashlight_head", (shelf_cx - 0.25 + 0.16, shelf_cy - 0.05, p1_top + 0.045), 0.052, 0.045,
        m["metal_worn"], coll, vertices=10, rot=(0, math.radians(90), 0), edge=0.001)
    # Spray can on middle shelf.
    cyl("hq_shelf_spray", (shelf_cx + 0.18, shelf_cy - 0.06, p1_top + 0.110), 0.038, 0.22, m["amber"], coll, vertices=8, edge=0.001)
    cyl("hq_shelf_spray_nozzle", (shelf_cx + 0.18, shelf_cy - 0.06, p1_top + 0.230), 0.022, 0.020, m["black"], coll, vertices=6, edge=0)

    # Top shelf: toolbox + decoy bell.
    toolbox_h = 0.14
    cube("hq_shelf_toolbox", (shelf_cx - 0.18, shelf_cy - 0.04, p2_top + toolbox_h * 0.5), (0.36, 0.20, toolbox_h), m["metal"], coll, edge=0.004)
    cube("hq_shelf_toolbox_latch", (shelf_cx - 0.18, shelf_cy - 0.14, p2_top + toolbox_h * 0.5 + 0.04), (0.06, 0.010, 0.030), m["debt"], coll, edge=0)
    cube("hq_shelf_toolbox_handle", (shelf_cx - 0.18, shelf_cy - 0.04, p2_top + toolbox_h + 0.012), (0.18, 0.024, 0.018), m["metal_worn"], coll, edge=0)
    # Tag/label on the toolbox.
    cube("hq_shelf_toolbox_tag", (shelf_cx - 0.18 + 0.10, shelf_cy - 0.14, p2_top + toolbox_h * 0.5),
         (0.060, 0.012, 0.050), m["paper"], coll, edge=0)
    # Decoy bell (small dome) on the top shelf.
    sphere("hq_shelf_decoy_bell", (shelf_cx + 0.22, shelf_cy - 0.04, p2_top + 0.080), (0.075, 0.075, 0.060), m["amber"], coll)
    cube("hq_shelf_decoy_clapper", (shelf_cx + 0.22, shelf_cy - 0.04, p2_top + 0.075), (0.010, 0.010, 0.030), m["metal"], coll, edge=0)

    # Label strip on the shelf face describing what each level is for.
    cube("hq_shelf_label_strip", (shelf_cx, shelf_cy - 0.205, 1.80), (0.84, 0.014, 0.080), m["paper"], coll, edge=0.001)
    txt("hq_shelf_label_text", "OUTSOURCED GEAR", (shelf_cx, shelf_cy - 0.215, 1.80), (math.radians(90), 0, 0), 0.052, m["black"], coll)

    # Small waiting corner: grounded, secondary, not blocking routes.
    cube("hq_sofa_base", (-4.00, -1.75, 0.20), (0.95, 0.38, 0.26), m["uniform"], coll, edge=0.006)
    cube("hq_sofa_back", (-4.00, -1.48, 0.50), (0.95, 0.07, 0.54), m["uniform"], coll, edge=0.006)
    cube("hq_waiting_table", (-3.05, -1.88, 0.24), (0.48, 0.30, 0.08), m["wood"], coll, edge=0.004)
    cube("hq_waiting_table_leg_l", (-3.36, -2.08, 0.13), (0.04, 0.04, 0.24), m["wood"], coll, edge=0.002)
    cube("hq_waiting_table_leg_r", (-2.74, -2.08, 0.13), (0.04, 0.04, 0.24), m["wood"], coll, edge=0.002)
    cube("hq_waiting_newspaper", (-3.05, -1.88, 0.292), (0.25, 0.16, 0.006), m["paper"], coll, rot=(0, 0, math.radians(-8)), edge=0)
    cube("hq_filing_cabinet", (-0.05, 2.55, 0.56), (0.38, 0.26, 0.98), m["metal_worn"], coll, edge=0.005)
    for i in range(3):
        cube(f"hq_filing_cabinet_handle_{i}", (-0.05, 2.405, 0.86 - i * 0.22), (0.22, 0.010, 0.022), m["paper_dark"], coll, edge=0)

    # Dispatch route: computer -> office passage -> garage bay -> van door.
    cube("hq_route_from_computer", (-1.58, 1.15, 0.018), (0.12, 0.85, 0.010), m["terminal"], coll, edge=0)
    cube("hq_route_to_shelf", (-2.38, 1.15, 0.020), (0.78, 0.10, 0.010), m["terminal"], coll, edge=0)
    cube("hq_route_to_garage", (-0.45, -2.30, 0.022), (1.35, 0.10, 0.010), m["amber"], coll, rot=(0, 0, math.radians(-8)), edge=0)
    cube("hq_route_van_lane", (2.75, -1.25, 0.024), (0.16, 2.20, 0.010), m["amber"], coll, edge=0)
    cube("hq_van_boarding_pad", (2.75, -1.35, 0.030), (1.55, 0.42, 0.012), m["terminal"], coll, edge=0.001)
    for i in range(7):
        cube(f"hq_yellow_route_dash_{i}", (-0.55 + i * 0.55, -2.35 + i * 0.05, 0.056), (0.24, 0.030, 0.010),
             m["amber"], coll, rot=(0, 0, math.radians(8)), edge=0)
    for i in range(9):
        cube(f"hq_garage_lane_mark_l_{i}", (1.35, -2.80 + i * 0.58, 0.054), (0.045, 0.26, 0.010), m["amber"], coll, edge=0)
        cube(f"hq_garage_lane_mark_r_{i}", (4.15, -2.80 + i * 0.58, 0.054), (0.045, 0.26, 0.010), m["amber"], coll, edge=0)

    # Bay safety curb and wheel stop, kept low so the floor plan remains readable.
    cube("hq_garage_left_floor_curb", (0.72, -1.20, 0.075), (0.10, 3.10, 0.09), m["black"], coll, edge=0.002)
    cube("hq_garage_left_guard_rail", (0.72, -1.20, 0.72), (0.08, 3.10, 0.08), m["metal"], coll, edge=0.003)
    for i, yy in enumerate((-2.65, -1.55, -0.45, 0.65)):
        cube(f"hq_garage_left_guard_post_{i}", (0.72, yy, 0.42), (0.10, 0.10, 0.72), m["metal"], coll, edge=0.003)
    cube("hq_garage_van_floor_stop", (2.75, -2.32, 0.070), (1.80, 0.10, 0.08), m["black"], coll, edge=0.002)

    # Garage is open enough to drive through, but a proper iron gate (vertical bars)
    # is partially raised — visible from inside as a clear "this is a gated bay" cue.
    # Header beam + lintel at the top of the opening.
    cube("hq_garage_gate_header", (2.75, -3.30, 2.72), (2.55, 0.18, 0.16), m["metal"], coll, edge=0.004)
    cube("hq_garage_gate_header_band", (2.75, -3.23, 2.62), (2.50, 0.020, 0.040), m["debt"], coll, edge=0)
    txt("hq_garage_gate_header_text", "BAY 01", (2.75, -3.22, 2.62), (math.radians(90), 0, 0), 0.10, m["paper"], coll)
    # Side jambs framing the opening.
    cube("hq_garage_gate_jamb_l", (1.35, -3.30, 1.30), (0.16, 0.22, 2.60), m["metal"], coll, edge=0.005)
    cube("hq_garage_gate_jamb_r", (4.15, -3.30, 1.30), (0.16, 0.22, 2.60), m["metal"], coll, edge=0.005)
    # Iron gate — vertical bars, partially raised (bars hang down from header to mid-height).
    bar_top_z = 2.62
    bar_bot_z = 1.55  # gate is half-raised, so bars hang from header down to ~chest height
    bar_z_center = (bar_top_z + bar_bot_z) * 0.5
    bar_height = bar_top_z - bar_bot_z
    for i in range(11):
        bx = 1.55 + i * 0.24
        cyl(f"hq_garage_gate_bar_{i}", (bx, -3.30, bar_z_center), 0.022, bar_height, m["metal_worn"], coll, vertices=6, edge=0.001)
    # Horizontal rails on the gate (top and bottom of the lowered section).
    cube("hq_garage_gate_rail_top", (2.75, -3.30, bar_top_z - 0.02), (2.55, 0.060, 0.045), m["metal_worn"], coll, edge=0.002)
    cube("hq_garage_gate_rail_bot", (2.75, -3.30, bar_bot_z + 0.02), (2.55, 0.060, 0.060), m["metal_worn"], coll, edge=0.002)
    # Padlock hanging from the bottom-left bar.
    cube("hq_garage_gate_lock", (1.55, -3.30, bar_bot_z - 0.05), (0.055, 0.090, 0.075), m["amber"], coll, edge=0.002)
    cyl("hq_garage_gate_lock_loop", (1.55, -3.30, bar_bot_z + 0.02), 0.018, 0.040, m["metal"], coll, vertices=6, edge=0)
    # Warning placard on the right jamb.
    cube("hq_garage_gate_warning_plate", (4.05, -3.20, 1.45), (0.22, 0.014, 0.16), m["debt"], coll, edge=0.002)
    txt("hq_garage_gate_warning_text", "GATE\nALARM", (4.05, -3.19, 1.45), (math.radians(90), 0, 0), 0.038, m["paper"], coll)
    # Ground threshold (still amber for readability), and reinforced black scuff plate.
    cube("hq_garage_threshold", (2.75, -3.08, 0.040), (2.55, 0.15, 0.030), m["amber"], coll, edge=0.001)
    cube("hq_garage_scuff_plate", (2.75, -3.28, 0.018), (2.55, 0.10, 0.012), m["black"], coll, edge=0)
    cube("hq_garage_rolled_door", (2.75, -3.42, 1.98), (2.65, 0.055, 0.42), m["metal_worn"], coll, edge=0.004)
    for i in range(4):
        cube(f"hq_garage_rolled_door_slat_{i}", (2.75, -3.46, 1.82 + i * 0.10), (2.58, 0.018, 0.018), m["metal"], coll, edge=0)
    # Overhead amber work light just inside the gate.
    cube("hq_garage_work_light", (2.75, -2.95, 2.55), (1.25, 0.045, 0.040), m["amber"], coll, edge=0.004)
    # Hazard chevrons on the floor at the gate threshold.
    for i in range(8):
        cube(f"hq_garage_hazard_mark_{i}", (1.55 + i * 0.34, -3.02, 0.052), (0.22, 0.022, 0.030),
             m["helmet"] if i % 2 == 0 else m["black"], coll, rot=(0, 0, math.radians(25 if i % 2 == 0 else -25)), edge=0)

    # Garage tool wall and supplies, matching the reference art board's right bay.
    cube("hq_garage_tool_pegboard", (5.24, -1.25, 1.48), (0.020, 0.76, 0.56), m["wood"], coll, edge=0.003)
    for i, z in enumerate((1.24, 1.42, 1.60, 1.78)):
        cube(f"hq_garage_tool_rail_{i}", (5.21, -1.25, z), (0.018, 0.70, 0.015), m["metal"], coll, edge=0)
    for i, yy in enumerate((-1.82, -1.55, -1.27, -0.98, -0.70)):
        cube(f"hq_garage_hanging_tool_{i}", (5.19, yy, 1.36 + (i % 2) * 0.24), (0.030, 0.030, 0.28), m["metal_worn"], coll,
             rot=(0, 0, math.radians(-8 + i * 4)), edge=0.001)
    cube("hq_garage_red_tool_chest", (5.02, -2.40, 0.52), (0.28, 0.36, 0.48), m["debt"], coll, edge=0.005)
    for i in range(3):
        cube(f"hq_garage_tool_chest_drawer_{i}", (4.99, -2.56, 0.38 + i * 0.13), (0.20, 0.014, 0.040), m["metal"], coll, edge=0)
    cube("hq_garage_first_aid_box", (5.25, -0.32, 1.66), (0.020, 0.22, 0.18), m["terminal"], coll, edge=0.002)
    cube("hq_garage_first_aid_cross_h", (5.23, -0.32, 1.66), (0.010, 0.12, 0.022), m["paper"], coll, edge=0)
    cube("hq_garage_first_aid_cross_v", (5.23, -0.32, 1.66), (0.010, 0.026, 0.11), m["paper"], coll, edge=0)
    cyl("hq_garage_extinguisher", (5.14, -0.05, 0.48), 0.060, 0.42, m["debt"], coll, vertices=10, edge=0.003)
    cube("hq_garage_supply_crate", (4.75, -3.05, 0.20), (0.30, 0.22, 0.20), m["cardboard"], coll, edge=0.004)
    cyl("hq_garage_oil_stain_a", (2.60, -1.80, 0.050), 0.22, 0.006, m["black"], coll, vertices=12, edge=0)

    _build_van_exterior(m, coll)
    van_center_x, van_center_y = 2.75, 0.05
    for obj in coll.objects:
        if obj.name.startswith("van_"):
            local_x, local_y = obj.location.x, obj.location.y
            # Van source model is lengthwise on Blender X. Rotate it into the
            # right-hand garage bay so its nose points toward the roll-up door,
            # matching the reference top-down plan.
            obj.location.x = van_center_x - local_y
            obj.location.y = van_center_y + local_x
            obj.rotation_euler.z += math.radians(90)

    # Bright incandescent ceiling panels: ceiling-only visual geometry. Runtime
    # colliders are hand-authored in Unity and FBX mesh colliders are disabled.
    office_panels = [
        (-4.10, -1.70, 2.66), (-2.55, -1.70, 2.66), (-1.00, -1.70, 2.66),
        (-4.10, 1.45, 2.66), (-2.55, 1.45, 2.66), (-1.00, 1.45, 2.66),
    ]
    garage_panels = [
        (1.55, -1.70, 2.80), (2.75, -1.70, 2.80), (3.95, -1.70, 2.80),
        (1.55, 1.45, 2.80), (2.75, 1.45, 2.80), (3.95, 1.45, 2.80),
    ]
    for i, (fx, fy, fz) in enumerate(office_panels):
        cube(f"hq_incandescent_panel_office_{i}_housing", (fx, fy, fz + 0.045), (0.82, 0.36, 0.045), m["metal"], coll, edge=0.003)
        cube(f"hq_incandescent_panel_office_{i}_diffuser", (fx, fy, fz), (0.74, 0.30, 0.020), m["incandescent"], coll, edge=0.001)
    for i, (fx, fy, fz) in enumerate(garage_panels):
        cube(f"hq_incandescent_panel_garage_{i}_housing", (fx, fy, fz + 0.045), (0.82, 0.36, 0.045), m["metal"], coll, edge=0.003)
        cube(f"hq_incandescent_panel_garage_{i}_diffuser", (fx, fy, fz), (0.74, 0.30, 0.020), m["incandescent"], coll, edge=0.001)
    cyl("hq_left_wall_pipe", (-4.54, 0.40, 2.38), 0.035, 4.60, m["metal_worn"], coll,
        vertices=8, rot=(math.radians(90), 0, 0), edge=0.002)
    cube("hq_fire_extinguisher_bracket", (-4.62, -1.76, 0.72), (0.035, 0.12, 0.030), m["metal"], coll, edge=0.001)
    cyl("hq_fire_extinguisher", (-4.58, -1.76, 0.52), 0.055, 0.34, m["debt"], coll, vertices=10, edge=0.003)
    return coll


def _build_hq_infrastructure(m, coll) -> None:
    """Institutional infrastructure: lights, pipes, baseboards, exit sign, window."""

    # --- Incandescent ceiling panels (legacy helper; keep it aligned with build_hq) ---
    for i, yy in enumerate((-1.4, 1.0, 3.0)):
        cube(f"hq_infra_incandescent_panel_{i}_housing", (0, yy, 2.82), (0.85, 0.30, 0.04), m["metal"], coll, edge=0.003)
        cube(f"hq_infra_incandescent_panel_{i}_diffuser", (0, yy, 2.76), (0.78, 0.24, 0.020), m["incandescent"], coll, edge=0.001)

    # --- Baseboard molding (dark metal strip at floor level) ---
    cube("hq_baseboard_back", (0, 3.70, 0.055), (5.12, 0.04, 0.055), m["metal"], coll, edge=0.002)
    cube("hq_baseboard_left", (-5.12, 0, 0.055), (0.04, 3.72, 0.055), m["metal"], coll, edge=0.002)
    cube("hq_baseboard_right", (5.12, 0.4, 0.055), (0.04, 3.32, 0.055), m["metal"], coll, edge=0.002)

    # --- Exposed pipe along left wall ceiling ---
    cyl("hq_pipe_left_ceiling", (-4.88, 0, 2.68), 0.04, 7.2, m["metal_worn"], coll,
        vertices=8, rot=(math.radians(90), 0, 0), edge=0.002)
    cyl("hq_pipe_elbow_back", (-4.88, 3.50, 2.68), 0.05, 0.08, m["metal_worn"], coll, vertices=8, edge=0.001)
    cyl("hq_pipe_elbow_front", (-4.88, -3.20, 2.68), 0.05, 0.08, m["metal_worn"], coll, vertices=8, edge=0.001)
    cyl("hq_pipe_vertical_drain", (-4.88, -3.20, 1.34), 0.035, 2.60, m["metal_worn"], coll, vertices=8, edge=0.002)
    for i in range(3):
        cyl(f"hq_pipe_bracket_{i}", (-4.88, -2.0 + i * 2.4, 2.68), 0.06, 0.03, m["metal"], coll, vertices=8, edge=0)

    # --- Fire extinguisher on left wall near exit ---
    cube("hq_extinguisher_bracket", (-5.10, -2.40, 0.82), (0.04, 0.12, 0.035), m["metal"], coll, edge=0.001)
    cyl("hq_fire_extinguisher", (-5.06, -2.40, 0.58), 0.065, 0.42, m["debt"], coll, vertices=10, edge=0.003)
    cyl("hq_extinguisher_nozzle", (-5.06, -2.40, 0.82), 0.022, 0.08, m["metal"], coll, vertices=8, edge=0)
    cube("hq_extinguisher_label", (-5.06, -2.525, 0.58), (0.045, 0.010, 0.065), m["paper"], coll, edge=0)

    # --- Exit sign above garage opening (dispatch green, emissive) ---
    cube("hq_exit_sign_housing", (2.50, -3.30, 2.52), (0.45, 0.035, 0.12), m["black"], coll, edge=0.004)
    sign("hq_exit_sign", "EXIT", (2.50, -3.34, 2.52), (0.38, 0.018, 0.09), m["exit"], m["black"], coll, size_ratio=0.40)

    # --- Window with blinds on left wall ---
    cube("hq_window_frame", (-5.14, 1.20, 1.52), (0.035, 0.68, 0.52), m["metal"], coll, edge=0.004)
    cube("hq_window_glass", (-5.12, 1.20, 1.52), (0.015, 0.60, 0.44), m["glass"], coll, edge=0.001)
    for i in range(9):
        z = 1.30 + i * 0.05
        cube(f"hq_blind_slat_{i}", (-5.10, 1.20, z), (0.008, 0.58, 0.018), m["paper_dark"], coll, edge=0)

    # --- Ceiling panel lines (drop ceiling grid) ---
    for i in range(1, 10):
        x = -4.6 + i * 0.92
        cube(f"hq_ceiling_grid_x_{i}", (x, 0, 2.90), (0.008, 3.72, 0.012), m["metal"], coll, edge=0)
    for i in range(1, 8):
        y = -3.2 + i * 0.92
        cube(f"hq_ceiling_grid_y_{i}", (0, y, 2.90), (5.12, 0.008, 0.012), m["metal"], coll, edge=0)


def _build_hq_environmental_storytelling(m, coll) -> None:
    """Lived-in props: posters, clutter, stains, coat hooks, waste bin, cables."""

    # --- Motivational poster on back wall, half-covered by debt notice ---
    cube("hq_motiv_poster_bg", (-3.50, 3.66, 1.68), (0.38, 0.016, 0.26), m["paper"], coll, edge=0.003)
    txt("hq_motiv_poster_text", "WORK HARD\nPAY DEBT", (-3.50, 3.635, 1.70), (math.radians(90), 0, 0), 0.065, m["metal"], coll)
    cube("hq_motiv_debt_overlay", (-3.34, 3.64, 1.74), (0.22, 0.012, 0.14), m["debt"], coll, rot=(0, 0, math.radians(-8)), edge=0.001)
    txt("hq_motiv_overdue_stamp", "OVERDUE", (-3.34, 3.625, 1.74), (math.radians(90), 0, 0), 0.042, m["paper"], coll)

    # --- Competitor acquisition flyer near debt board ---
    cube("hq_competitor_flyer", (3.40, 3.66, 1.30), (0.17, 0.012, 0.22), m["paper"], coll, rot=(0, 0, math.radians(4)), edge=0.002)
    txt("hq_competitor_flyer_text", "SELL\nNOW?", (3.40, 3.642, 1.32), (math.radians(90), 0, 0), 0.050, m["debt"], coll)

    # --- Expired calendar on right wall ---
    cube("hq_calendar_bg", (5.10, 1.80, 1.45), (0.016, 0.22, 0.28), m["paper"], coll, edge=0.003)
    for row in range(4):
        for col in range(5):
            cube(f"hq_cal_x_{row}_{col}", (5.09, 1.66 + col * 0.065, 1.30 + row * 0.055), (0.010, 0.020, 0.015), m["debt"], coll, edge=0)

    # --- Water stain on ceiling (dark patch) ---
    cube("hq_ceiling_stain_a", (-2.2, -1.5, 2.91), (0.65, 0.45, 0.006), m["wall_shadow"], coll, edge=0.001)
    cube("hq_ceiling_stain_b", (-2.5, -1.3, 2.91), (0.35, 0.25, 0.005), m["floor"], coll, edge=0.001)

    # --- Taped floor crack near entrance ---
    cube("hq_floor_crack", (1.20, -2.80, 0.008), (0.008, 0.85, 0.008), m["floor_line"], coll, edge=0)
    cube("hq_floor_tape_a", (1.20, -2.55, 0.012), (0.12, 0.008, 0.005), m["amber"], coll, rot=(0, 0, math.radians(45)), edge=0)
    cube("hq_floor_tape_b", (1.20, -3.05, 0.012), (0.12, 0.008, 0.005), m["amber"], coll, rot=(0, 0, math.radians(-45)), edge=0)

    # --- Waste bin near desk with crumpled paper ---
    cyl("hq_waste_bin", (-0.40, 2.10, 0.18), 0.14, 0.36, m["metal_worn"], coll, vertices=10, edge=0.003)
    for i in range(4):
        cube(f"hq_crumpled_paper_{i}", (-0.40 + (i - 1.5) * 0.06, 2.10 + (i % 2) * 0.04, 0.32 + i * 0.03),
             (0.04, 0.035, 0.03), m["paper"], coll, rot=(0, 0, math.radians(15 * i)), edge=0.001)
    cube("hq_spilled_paper_floor", (-0.22, 1.85, 0.015), (0.09, 0.065, 0.005), m["paper"], coll, rot=(0, 0, math.radians(-18)), edge=0)

    # --- Donated cardboard box near filing cabinet ---
    cube("hq_donated_box", (-3.45, 3.10, 0.22), (0.28, 0.22, 0.22), m["cardboard"], coll, edge=0.004)
    cube("hq_donated_box_label", (-3.45, 2.87, 0.28), (0.16, 0.010, 0.055), m["paper"], coll, edge=0)
    txt("hq_donated_text", "DONATED", (-3.45, 2.855, 0.28), (math.radians(90), 0, 0), 0.030, m["metal"], coll)
    cube("hq_donated_box_flap_l", (-3.65, 3.10, 0.44), (0.08, 0.18, 0.015), m["cardboard"], coll, rot=(0, math.radians(25), 0), edge=0.001)
    cube("hq_donated_box_flap_r", (-3.25, 3.10, 0.44), (0.08, 0.18, 0.015), m["cardboard"], coll, rot=(0, math.radians(-20), 0), edge=0.001)

    # --- Coat hooks on left wall near entrance ---
    cube("hq_coat_rack_bar", (-5.10, -2.80, 1.52), (0.04, 0.55, 0.035), m["metal"], coll, edge=0.002)
    for i in range(3):
        cyl(f"hq_coat_hook_{i}", (-5.06, -3.00 + i * 0.22, 1.52), 0.012, 0.08, m["metal"], coll, vertices=6,
            rot=(math.radians(90), 0, 0), edge=0)
    cube("hq_hanging_jacket", (-5.02, -2.78, 1.12), (0.06, 0.18, 0.44), m["uniform"], coll, edge=0.004)
    cube("hq_hanging_vest", (-5.02, -3.00, 1.18), (0.04, 0.14, 0.32), m["vest"], coll, edge=0.003)

    # --- Power strip / cable run on floor near computer ---
    cube("hq_power_strip", (-2.10, 2.60, 0.025), (0.24, 0.06, 0.025), m["paper_dark"], coll, edge=0.002)
    for i in range(3):
        cyl(f"hq_cable_run_{i}", (-2.10 + i * 0.22, 2.80 + i * 0.18, 0.018), 0.012, 0.42, m["black"], coll,
            vertices=6, rot=(math.radians(90), 0, math.radians(30 + i * 15)), edge=0)

    # --- Wall clock (stopped) on back wall ---
    cyl("hq_wall_clock_face", (4.28, 3.66, 1.86), 0.16, 0.025, m["paper"], coll, vertices=16,
        rot=(math.radians(90), 0, 0), edge=0.002)
    cyl("hq_wall_clock_rim", (4.28, 3.68, 1.86), 0.18, 0.018, m["metal"], coll, vertices=16,
        rot=(math.radians(90), 0, 0), edge=0.001)
    cube("hq_clock_hand_hour", (4.28, 3.645, 1.90), (0.008, 0.010, 0.08), m["black"], coll,
         rot=(0, 0, math.radians(-35)), edge=0)
    cube("hq_clock_hand_minute", (4.28, 3.645, 1.92), (0.005, 0.010, 0.12), m["black"], coll,
         rot=(0, 0, math.radians(72)), edge=0)

    # --- Old landline phone on desk ---
    cube("hq_phone_base", (-0.72, 2.28, 0.655), (0.14, 0.10, 0.030), m["paper_dark"], coll, edge=0.003)
    cube("hq_phone_handset", (-0.72, 2.18, 0.695), (0.06, 0.16, 0.025), m["black"], coll, edge=0.003)
    cyl("hq_phone_earpiece", (-0.72, 2.10, 0.71), 0.028, 0.02, m["black"], coll, vertices=8,
        rot=(math.radians(90), 0, 0), edge=0)
    cyl("hq_phone_cord_coil", (-0.50, 2.28, 0.66), 0.012, 0.30, m["black"], coll, vertices=6,
        rot=(0, math.radians(75), 0), edge=0)

    # --- Paper trail from receipt printer across floor ---
    for i in range(5):
        x = -2.05 + i * 0.12
        y = 1.60 - i * 0.22
        cube(f"hq_receipt_floor_{i}", (x, y, 0.012), (0.11, 0.15, 0.005), m["paper"], coll,
             rot=(0, 0, math.radians(-5 + i * 8)), edge=0)

    # --- Small notice board on right wall ---
    cube("hq_notice_board_cork", (5.10, -0.60, 1.36), (0.018, 0.55, 0.38), m["cardboard"], coll, edge=0.004)
    cube("hq_notice_board_frame", (5.10, -0.60, 1.36), (0.022, 0.60, 0.42), m["wood"], coll, edge=0.003)
    for i in range(5):
        y = -0.82 + i * 0.11
        z = 1.28 + (i % 2) * 0.12
        cube(f"hq_pinned_note_{i}", (5.08, y, z), (0.010, 0.08, 0.06), m["paper" if i % 3 else "debt"], coll,
             rot=(0, 0, math.radians(-6 + i * 3)), edge=0)

    # --- Stacked paper on sofa arm (someone was working late) ---
    for i in range(3):
        cube(f"hq_sofa_paper_{i}", (2.62, 2.20, 0.38 + i * 0.012), (0.14, 0.10, 0.005), m["paper"], coll,
             rot=(0, 0, math.radians(-3 + i * 2)), edge=0)

    # --- Coffee mug on desk ---
    cyl("hq_coffee_mug", (-1.88, 2.00, 0.66), 0.035, 0.08, m["paper_dark"], coll, vertices=10, edge=0.002)
    cyl("hq_coffee_inside", (-1.88, 2.00, 0.70), 0.030, 0.01, m["black"], coll, vertices=10, edge=0)

    # --- Warning tape on garage threshold ---
    for i in range(10):
        x = 1.55 + i * 0.20
        cube(f"hq_garage_threshold_tape_{i}", (x, -0.60, 0.018), (0.06, 0.035, 0.008),
             m["vest"] if i % 2 == 0 else m["black"], coll, rot=(0, 0, math.radians(45 if i % 2 == 0 else -45)), edge=0)


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

    # --- Lower body ---
    cube("worker_left_boot", (x - 0.19, y, 0.08), (0.17, 0.26, 0.08), m["black"], coll, edge=0.006)
    cube("worker_right_boot", (x + 0.19, y, 0.08), (0.17, 0.26, 0.08), m["black"], coll, edge=0.006)
    cube("worker_left_leg", (x - 0.19, y, 0.52), (0.13, 0.14, 0.40), m["uniform"], coll, edge=0.008)
    cube("worker_right_leg", (x + 0.19, y, 0.52), (0.13, 0.14, 0.40), m["uniform"], coll, edge=0.008)
    cube("worker_left_knee_pad", (x - 0.19, y - 0.145, 0.52), (0.12, 0.018, 0.09), m["black"], coll, edge=0.002)
    cube("worker_right_knee_pad", (x + 0.19, y - 0.145, 0.52), (0.12, 0.018, 0.09), m["black"], coll, edge=0.002)

    # --- Torso + safety vest ---
    cube("worker_torso", (x, y, 1.12), (0.42, 0.22, 0.52), m["uniform"], coll, edge=0.012)
    cube("worker_vest_front", (x, y - 0.232, 1.14), (0.44, 0.018, 0.43), m["vest"], coll, edge=0.003)
    cube("worker_reflective_strip_a", (x, y - 0.248, 1.27), (0.36, 0.008, 0.022), m["paper"], coll, edge=0)
    cube("worker_reflective_strip_b", (x, y - 0.248, 1.03), (0.34, 0.008, 0.022), m["paper"], coll, edge=0)
    cube("worker_badge_green", (x + 0.13, y - 0.251, 1.38), (0.085, 0.006, 0.040), m["terminal"], coll, edge=0)
    txt("worker_badge_as_text", "AS", (x + 0.13, y - 0.258, 1.383), (math.radians(90), 0, 0), 0.028, m["black"], coll)
    for i, px in enumerate((-0.22, -0.07, 0.08, 0.23)):
        cube(f"worker_belt_pouch_{i}", (x + px, y - 0.225, 0.82), (0.060, 0.015, 0.070), m["cardboard"], coll, edge=0.001)

    # --- Arms ---
    cube("worker_left_arm_upper", (x - 0.50, y - 0.02, 1.26), (0.09, 0.10, 0.31), m["uniform"], coll, edge=0.006)
    cube("worker_left_arm_lower", (x - 0.53, y - 0.10, 0.84), (0.085, 0.09, 0.31), m["uniform"], coll, rot=(0, 0, math.radians(-7)), edge=0.006)
    cube("worker_right_arm_upper", (x + 0.50, y - 0.02, 1.26), (0.09, 0.10, 0.31), m["uniform"], coll, edge=0.006)
    cube("worker_right_arm_lower", (x + 0.53, y - 0.10, 0.84), (0.085, 0.09, 0.31), m["uniform"], coll, rot=(0, 0, math.radians(7)), edge=0.006)
    cube("worker_left_glove", (x - 0.55, y - 0.13, 0.62), (0.105, 0.10, 0.075), m["black"], coll, edge=0.004)
    cube("worker_right_glove", (x + 0.55, y - 0.13, 0.62), (0.105, 0.10, 0.075), m["black"], coll, edge=0.004)

    # --- Neck + head shell (mostly hidden by the mask, kept for silhouette) ---
    cube("worker_neck", (x, y, 1.50), (0.13, 0.13, 0.10), m["skin"], coll, edge=0.004)
    sphere("worker_head", (x, y, 1.72), (0.22, 0.20, 0.24), m["skin"], coll)

    # --- Full-face gas mask (front of head faces -Y so detail sits at y < 0) ---
    # The crew run filthy jobs and don't want to be recognised: the entire face is
    # covered by a rubber respirator with goggle lenses and a screw-on filter canister.
    # Rubber faceplate wrapping the front of the head.
    cube("worker_mask_faceplate", (x, y - 0.085, 1.70), (0.40, 0.26, 0.42), m["mask"], coll, edge=0.020)
    # Lower snout housing the filter screws into.
    cube("worker_mask_snout", (x, y - 0.225, 1.625), (0.20, 0.16, 0.17), m["mask"], coll, edge=0.012)
    # Chin cup.
    cube("worker_mask_chin", (x, y - 0.10, 1.565), (0.24, 0.18, 0.10), m["mask"], coll, edge=0.010)
    # Goggle rims (dark metal) + tinted glass lenses.
    cyl("worker_mask_goggle_rim_l", (x - 0.105, y - 0.232, 1.775), 0.082, 0.05, m["metal_worn"], coll,
        vertices=14, rot=(math.radians(90), 0, 0), edge=0.002)
    cyl("worker_mask_goggle_rim_r", (x + 0.105, y - 0.232, 1.775), 0.082, 0.05, m["metal_worn"], coll,
        vertices=14, rot=(math.radians(90), 0, 0), edge=0.002)
    cyl("worker_mask_lens_l", (x - 0.105, y - 0.252, 1.775), 0.066, 0.03, m["glass"], coll,
        vertices=14, rot=(math.radians(90), 0, 0), edge=0.001)
    cyl("worker_mask_lens_r", (x + 0.105, y - 0.252, 1.775), 0.066, 0.03, m["glass"], coll,
        vertices=14, rot=(math.radians(90), 0, 0), edge=0.001)
    # Filter canister screwed onto the snout, angled slightly down-forward.
    cyl("worker_mask_filter", (x, y - 0.345, 1.585), 0.072, 0.20, m["metal_worn"], coll,
        vertices=14, rot=(math.radians(78), 0, 0), edge=0.004)
    cyl("worker_mask_filter_cap", (x, y - 0.43, 1.560), 0.075, 0.03, m["black"], coll,
        vertices=14, rot=(math.radians(78), 0, 0), edge=0.002)
    cube("worker_mask_filter_band", (x, y - 0.345, 1.585), (0.155, 0.155, 0.022), m["amber"], coll,
         rot=(math.radians(78), 0, 0), edge=0)
    # Exhale valve between the lenses.
    cyl("worker_mask_exhale_valve", (x, y - 0.245, 1.695), 0.034, 0.05, m["black"], coll,
        vertices=10, rot=(math.radians(90), 0, 0), edge=0.002)
    # Head straps running from the mask edge back around the skull.
    cube("worker_mask_strap_top", (x, y + 0.02, 1.86), (0.30, 0.30, 0.030), m["black"], coll, edge=0.002)
    cube("worker_mask_strap_side_l", (x - 0.205, y - 0.02, 1.74), (0.030, 0.34, 0.10), m["black"], coll, edge=0.002)
    cube("worker_mask_strap_side_r", (x + 0.205, y - 0.02, 1.74), (0.030, 0.34, 0.10), m["black"], coll, edge=0.002)
    cube("worker_mask_strap_buckle_l", (x - 0.18, y - 0.20, 1.74), (0.040, 0.030, 0.040), m["metal_worn"], coll, edge=0.001)
    cube("worker_mask_strap_buckle_r", (x + 0.18, y - 0.20, 1.74), (0.040, 0.030, 0.040), m["metal_worn"], coll, edge=0.001)

    # --- Helmet ---
    cube("worker_helmet_cap", (x, y, 1.94), (0.30, 0.24, 0.075), m["helmet"], coll, edge=0.014)
    cube("worker_helmet_brim", (x, y - 0.20, 1.90), (0.22, 0.08, 0.022), m["helmet"], coll, edge=0.004)
    cube("worker_helmet_debt_sticker", (x + 0.09, y - 0.205, 1.935), (0.08, 0.006, 0.025), m["debt"], coll, edge=0)
    cube("worker_helmet_strap_l", (x - 0.15, y - 0.05, 1.78), (0.014, 0.012, 0.140), m["black"], coll, edge=0)
    cube("worker_helmet_strap_r", (x + 0.15, y - 0.05, 1.78), (0.014, 0.012, 0.140), m["black"], coll, edge=0)

    # --- Backpack + carried gear ---
    cube("worker_backpack", (x, y + 0.25, 1.10), (0.28, 0.10, 0.36), m["metal"], coll, edge=0.006)
    cube("worker_left_pack_strap", (x - 0.17, y - 0.232, 1.17), (0.040, 0.010, 0.40), m["black"], coll, edge=0)
    cube("worker_right_pack_strap", (x + 0.17, y - 0.232, 1.17), (0.040, 0.010, 0.40), m["black"], coll, edge=0)
    cyl("worker_flashlight", (x + 0.62, y - 0.23, 0.46), 0.044, 0.34, m["black"], coll, vertices=10, rot=(math.radians(90), 0, 0), edge=0.001)
    cube("worker_radio", (x - 0.32, y - 0.235, 1.22), (0.065, 0.018, 0.12), m["black"], coll, edge=0.002)
    cube("worker_radio_green_led", (x - 0.32, y - 0.248, 1.27), (0.030, 0.006, 0.018), m["terminal"], coll, edge=0)
    cyl("worker_radio_antenna", (x - 0.36, y - 0.242, 1.36), 0.006, 0.24, m["black"], coll, vertices=5, rot=(0, math.radians(8), 0), edge=0)
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
    cube("collector_final_notice_forehead", (x, y - 0.190, 2.62), (0.22, 0.010, 0.055), m["debt"], coll, edge=0.001)
    txt("collector_final_notice_text", "FINAL", (x, y - 0.202, 2.622), (math.radians(90), 0, 0), 0.040, m["paper"], coll)
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
    cube("collector_ledger_clip", (x - 0.96, y - 0.211, 0.78), (0.14, 0.008, 0.026), m["metal"], coll, edge=0.001)
    cube("collector_stamp_block", (x + 0.92, y - 0.13, 0.52), (0.18, 0.055, 0.10), m["black"], coll, edge=0.003)
    cube("collector_stamp_face_red", (x + 0.92, y - 0.19, 0.48), (0.16, 0.010, 0.050), m["debt"], coll, edge=0.001)
    for i in range(8):
        cube(f"collector_back_receipt_{i}", (x, y + 0.19, 2.18 - i * 0.082), (0.16, 0.014, 0.028), m["paper"], coll, edge=0.001)
    for i in range(5):
        cube(f"collector_loose_form_trail_{i}", (x + 0.25 - i * 0.09, y + 0.23 + i * 0.04, 2.10 - i * 0.16), (0.12, 0.008, 0.060), m["paper"], coll, rot=(0, 0, math.radians(12 - i * 5)), edge=0)
    for i in range(4):
        cube(f"collector_ground_invoice_shadow_{i}", (x - 0.36 + i * 0.24, y - 0.35 - (i % 2) * 0.08, 0.018),
             (0.13, 0.070, 0.004), m["paper"], coll, rot=(0, 0, math.radians(-18 + i * 11)), edge=0)
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


def _build_van_exterior(m, coll) -> None:
    """Prisoner-transport van exterior: boxy institutional body, no rear windows."""

    # --- Body: taller, boxier than the old sloped van ---
    # Cab profile (front section, slightly sloped hood)
    cab = [
        (-2.05, 0.34), (-1.95, 0.56), (-1.72, 0.72), (-1.40, 0.88),
        (-1.10, 1.48), (-0.60, 1.48), (-0.60, 0.34),
    ]
    prism("van_cab_body", cab, 0.76, m["van_body"], coll)

    # Rear box: flat roof, vertical walls — institutional transport shape
    cube("van_rear_box", (0.45, 0, 0.92), (2.10, 0.76, 0.58), m["van_body"], coll, edge=0.010)
    cube("van_rear_box_top", (0.45, 0, 1.48), (2.10, 0.76, 0.04), m["van_body"], coll, edge=0.006)

    # Side panels (blank, no windows in prisoner section)
    cube("van_side_panel_L", (0.45, -0.775, 0.92), (2.08, 0.020, 0.56), m["van_body"], coll, edge=0.004)
    cube("van_side_panel_R", (0.45, 0.775, 0.92), (2.08, 0.020, 0.56), m["van_body"], coll, edge=0.004)
    cube("van_lower_strip_L", (0.45, -0.765, 0.44), (2.08, 0.016, 0.08), m["van_shadow"], coll, edge=0.003)
    cube("van_lower_strip_R", (0.45, 0.765, 0.44), (2.08, 0.016, 0.08), m["van_shadow"], coll, edge=0.003)

    # Cab side windows (small, only in driver section)
    cube("van_cab_window_L", (-1.12, -0.78, 1.12), (0.32, 0.014, 0.18), m["glass"], coll, edge=0.002)
    cube("van_cab_window_R", (-1.12, 0.78, 1.12), (0.32, 0.014, 0.18), m["glass"], coll, edge=0.002)
    cube("van_windshield", (-1.42, 0, 1.18), (0.014, 0.52, 0.22), m["glass"], coll, edge=0.003)

    # Bumpers
    cube("van_front_bumper", (-2.08, 0, 0.44), (0.08, 0.68, 0.085), m["metal"], coll, edge=0.006)
    cube("van_rear_bumper", (1.72, 0, 0.44), (0.08, 0.68, 0.085), m["metal"], coll, edge=0.006)
    cube("van_front_grille", (-2.10, 0, 0.62), (0.014, 0.34, 0.08), m["black"], coll, edge=0)
    cube("van_left_mirror_arm", (-1.32, -0.86, 1.08), (0.18, 0.012, 0.012), m["metal"], coll, rot=(0, 0, math.radians(-8)), edge=0)
    cube("van_right_mirror_arm", (-1.32, 0.86, 1.08), (0.18, 0.012, 0.012), m["metal"], coll, rot=(0, 0, math.radians(8)), edge=0)
    cube("van_left_mirror", (-1.20, -0.94, 1.10), (0.08, 0.018, 0.12), m["glass"], coll, edge=0.002)
    cube("van_right_mirror", (-1.20, 0.94, 1.10), (0.08, 0.018, 0.12), m["glass"], coll, edge=0.002)

    # Headlights + housings on the nose (the van had none — looked unfinished).
    for side, hy in (("L", -0.46), ("R", 0.46)):
        cube(f"van_headlight_housing_{side}", (-2.05, hy, 0.60), (0.05, 0.20, 0.16), m["metal"], coll, edge=0.003)
        cube(f"van_headlight_lens_{side}", (-2.085, hy, 0.60), (0.02, 0.16, 0.12), m["amber"], coll, edge=0.002)
        cube(f"van_turn_signal_{side}", (-2.07, hy + (0.13 if hy < 0 else -0.13), 0.49), (0.03, 0.06, 0.05),
             m["vest"], coll, edge=0.001)
    # Taillights on the rear doors.
    for side, ty in (("L", -0.40), ("R", 0.40)):
        cube(f"van_taillight_{side}", (1.77, ty, 0.62), (0.02, 0.13, 0.11), m["debt"], coll, edge=0.002)
        cube(f"van_reverse_light_{side}", (1.77, ty, 0.50), (0.02, 0.11, 0.04), m["paper"], coll, edge=0.001)
    # Rear plate + plate light.
    cube("van_rear_plate", (1.78, 0, 0.50), (0.014, 0.30, 0.10), m["paper"], coll, edge=0.001)
    txt("van_rear_plate_text", "AS-04", (1.792, 0, 0.50), (math.radians(90), 0, math.radians(180)), 0.055, m["black"], coll)

    # Rear doors (closed from outside, heavy institutional look)
    cube("van_rear_door_L", (1.74, -0.34, 0.92), (0.04, 0.34, 0.56), m["metal_worn"], coll, edge=0.006)
    cube("van_rear_door_R", (1.74, 0.34, 0.92), (0.04, 0.34, 0.56), m["metal_worn"], coll, edge=0.006)
    cube("van_rear_door_seam", (1.74, 0, 0.92), (0.045, 0.015, 0.56), m["black"], coll, edge=0)
    for i, z in enumerate((0.64, 0.93, 1.22)):
        cube(f"van_rear_hinge_L_{i}", (1.785, -0.66, z), (0.030, 0.045, 0.070), m["metal"], coll, edge=0.001)
        cube(f"van_rear_hinge_R_{i}", (1.785, 0.66, z), (0.030, 0.045, 0.070), m["metal"], coll, edge=0.001)
    cube("van_rear_door_handle_L", (1.78, -0.16, 0.88), (0.020, 0.06, 0.035), m["metal"], coll, edge=0.002)
    cube("van_rear_door_handle_R", (1.78, 0.16, 0.88), (0.020, 0.06, 0.035), m["metal"], coll, edge=0.002)
    # Small wire-mesh window slit in rear door
    cube("van_rear_window_slit", (1.76, 0, 1.18), (0.010, 0.22, 0.06), m["glass"], coll, edge=0.001)
    for i in range(5):
        cube(f"van_rear_mesh_{i}", (1.77, -0.10 + i * 0.05, 1.18), (0.008, 0.005, 0.058), m["metal"], coll, edge=0)

    # Company branding
    cube("van_company_patch_L", (0.45, -0.790, 0.78), (0.34, 0.010, 0.12), m["terminal"], coll, edge=0.001)
    cube("van_company_patch_R", (0.45, 0.790, 0.78), (0.34, 0.010, 0.12), m["terminal"], coll, edge=0.001)
    txt("van_company_text_L", "AS", (0.45, -0.806, 0.78), (math.radians(90), 0, 0), 0.13, m["black"], coll)
    txt("van_company_text_R", "AS", (0.45, 0.806, 0.78), (math.radians(90), 0, math.radians(180)), 0.13, m["black"], coll)
    cube("van_service_label_L", (-0.12, -0.792, 0.62), (0.40, 0.009, 0.050), m["paper"], coll, edge=0)
    cube("van_service_label_R", (-0.12, 0.792, 0.62), (0.40, 0.009, 0.050), m["paper"], coll, edge=0)
    txt("van_service_text_L", "CIVIC JOBS", (-0.12, -0.808, 0.62), (math.radians(90), 0, 0), 0.052, m["black"], coll)
    txt("van_service_text_R", "CIVIC JOBS", (-0.12, 0.808, 0.62), (math.radians(90), 0, math.radians(180)), 0.052, m["black"], coll)
    for i, x in enumerate((-0.42, 0.05, 0.52, 0.99)):
        cube(f"van_side_dent_L_{i}", (x, -0.796, 1.05 - (i % 2) * 0.18), (0.20, 0.006, 0.035), m["van_shadow"], coll,
             rot=(0, 0, math.radians(-5 + i * 3)), edge=0)
        cube(f"van_side_dent_R_{i}", (x, 0.796, 1.05 - (i % 2) * 0.18), (0.20, 0.006, 0.035), m["van_shadow"], coll,
             rot=(0, 0, math.radians(5 - i * 3)), edge=0)
    cube("van_debt_slash_L", (1.04, -0.792, 0.85), (0.25, 0.010, 0.035), m["debt"], coll, rot=(0, 0, math.radians(-10)), edge=0)
    cube("van_debt_slash_R", (1.04, 0.792, 0.85), (0.25, 0.010, 0.035), m["debt"], coll, rot=(0, 0, math.radians(-10)), edge=0)

    # Wheels
    for i, x in enumerate((-1.28, 1.10)):
        for side, y in (("L", -0.82), ("R", 0.82)):
            cyl(f"van_wheel_{side}_{i}", (x, y, 0.34), 0.29, 0.20, m["black"], coll, vertices=18,
                rot=(math.radians(90), 0, 0), edge=0.003)
            cyl(f"van_wheel_hub_{side}_{i}", (x, y + (-0.105 if y < 0 else 0.105), 0.34), 0.115, 0.030, m["metal"],
                coll, vertices=10, rot=(math.radians(90), 0, 0), edge=0.001)

    # Roof rack and amber beacon
    cube("van_roof_rack_front", (0.10, 0, 1.56), (0.75, 0.66, 0.024), m["metal"], coll, edge=0.004)
    cube("van_roof_rack_rear", (0.88, 0, 1.56), (0.75, 0.66, 0.024), m["metal"], coll, edge=0.004)
    cyl("van_roof_pipe_left", (0.50, -0.45, 1.64), 0.034, 1.12, m["metal"], coll, vertices=8,
        rot=(0, math.radians(90), 0), edge=0.001)
    cyl("van_roof_pipe_right", (0.50, 0.45, 1.64), 0.034, 1.12, m["metal"], coll, vertices=8,
        rot=(0, math.radians(90), 0), edge=0.001)
    cube("van_roof_amber_beacon", (-0.55, 0, 1.58), (0.16, 0.09, 0.050), m["amber"], coll, edge=0.006)
    cube("van_rear_green_beacon", (1.76, 0, 1.24), (0.014, 0.15, 0.038), m["terminal"], coll, edge=0.001)


def _build_van_interior(m, coll, prefix: str = "van") -> None:
    """Prisoner compartment interior: cage, benches, driver silhouette, grab bars."""

    # --- Interior floor ---
    cube(f"{prefix}_int_floor", (0.45, 0, 0.36), (2.00, 0.68, 0.02), m["floor"], coll, edge=0.002)

    # --- Interior ceiling ---
    cube(f"{prefix}_int_ceiling", (0.45, 0, 1.44), (2.00, 0.68, 0.02), m["metal"], coll, edge=0.002)

    # --- Interior wall panels (scratched teal metal) ---
    cube(f"{prefix}_int_wall_L", (0.45, -0.68, 0.92), (2.00, 0.02, 0.56), m["wall"], coll, edge=0.003)
    cube(f"{prefix}_int_wall_R", (0.45, 0.68, 0.92), (2.00, 0.02, 0.56), m["wall"], coll, edge=0.003)

    # --- Cage partition (vertical bars between cab and prisoner area) ---
    cube(f"{prefix}_cage_frame_top", (-0.55, 0, 1.42), (0.04, 0.68, 0.04), m["metal"], coll, edge=0.004)
    cube(f"{prefix}_cage_frame_bot", (-0.55, 0, 0.42), (0.04, 0.68, 0.04), m["metal"], coll, edge=0.004)
    cube(f"{prefix}_cage_frame_L", (-0.55, -0.66, 0.92), (0.04, 0.04, 0.56), m["metal"], coll, edge=0.004)
    cube(f"{prefix}_cage_frame_R", (-0.55, 0.66, 0.92), (0.04, 0.04, 0.56), m["metal"], coll, edge=0.004)
    for i in range(7):
        y = -0.54 + i * 0.18
        cyl(f"{prefix}_cage_bar_{i}", (-0.55, y, 0.92), 0.012, 1.0, m["metal"], coll, vertices=6, edge=0)

    # --- Driver silhouette (dark menacing shape visible through cage) ---
    cube(f"{prefix}_driver_torso", (-1.30, 0, 0.88), (0.24, 0.20, 0.36), m["black"], coll, edge=0.008)
    sphere(f"{prefix}_driver_head", (-1.30, -0.02, 1.28), (0.14, 0.12, 0.16), m["black"], coll)
    cube(f"{prefix}_driver_cap", (-1.30, -0.04, 1.42), (0.18, 0.15, 0.04), m["black"], coll, edge=0.006)
    cube(f"{prefix}_driver_cap_brim", (-1.30, -0.14, 1.38), (0.12, 0.06, 0.015), m["black"], coll, edge=0.003)
    # Arms reaching to steering wheel
    cube(f"{prefix}_driver_arm_L", (-1.52, -0.18, 0.86), (0.16, 0.06, 0.08), m["black"], coll,
         rot=(0, 0, math.radians(-15)), edge=0.004)
    cube(f"{prefix}_driver_arm_R", (-1.52, 0.18, 0.86), (0.16, 0.06, 0.08), m["black"], coll,
         rot=(0, 0, math.radians(15)), edge=0.004)
    # Steering wheel silhouette
    cyl(f"{prefix}_steering_wheel", (-1.64, 0, 0.86), 0.12, 0.015, m["black"], coll, vertices=12,
        rot=(math.radians(70), 0, 0), edge=0)

    # --- Left bench (facing right) ---
    cube(f"{prefix}_bench_L_seat", (0.50, -0.52, 0.48), (1.50, 0.18, 0.04), m["metal_worn"], coll, edge=0.004)
    cube(f"{prefix}_bench_L_back", (0.50, -0.64, 0.80), (1.50, 0.04, 0.30), m["metal_worn"], coll, edge=0.003)
    # Exposed bolt heads
    for i in range(4):
        cyl(f"{prefix}_bench_L_bolt_{i}", (0.50 - 0.60 + i * 0.40, -0.66, 0.52), 0.012, 0.015, m["metal"], coll,
            vertices=6, rot=(math.radians(90), 0, 0), edge=0)

    # --- Right bench (facing left) ---
    cube(f"{prefix}_bench_R_seat", (0.50, 0.52, 0.48), (1.50, 0.18, 0.04), m["metal_worn"], coll, edge=0.004)
    cube(f"{prefix}_bench_R_back", (0.50, 0.64, 0.80), (1.50, 0.04, 0.30), m["metal_worn"], coll, edge=0.003)
    for i in range(4):
        cyl(f"{prefix}_bench_R_bolt_{i}", (0.50 - 0.60 + i * 0.40, 0.66, 0.52), 0.012, 0.015, m["metal"], coll,
            vertices=6, rot=(math.radians(90), 0, 0), edge=0)

    # --- Ceiling grab bars ---
    cyl(f"{prefix}_grab_bar_L", (0.45, -0.32, 1.38), 0.018, 1.60, m["metal"], coll, vertices=8,
        rot=(0, math.radians(90), 0), edge=0.001)
    cyl(f"{prefix}_grab_bar_R", (0.45, 0.32, 1.38), 0.018, 1.60, m["metal"], coll, vertices=8,
        rot=(0, math.radians(90), 0), edge=0.001)
    # Bar mounting brackets
    for bar_y in (-0.32, 0.32):
        for bx in (-0.30, 0.45, 1.20):
            cube(f"{prefix}_grab_mount_{bar_y:.0f}_{bx:.0f}", (bx, bar_y, 1.42), (0.03, 0.03, 0.04), m["metal"],
                 coll, edge=0.002)

    # --- Interior fluorescent strip (dim amber) ---
    cube(f"{prefix}_int_light_housing", (0.45, 0, 1.42), (0.85, 0.05, 0.025), m["metal"], coll, edge=0.002)
    cube(f"{prefix}_int_light_tube", (0.45, 0, 1.39), (0.72, 0.025, 0.012), m["amber"], coll, edge=0.001)
    cube(f"{prefix}_mission_locker", (1.16, 0, 0.88), (0.18, 0.54, 0.32), m["metal"], coll, edge=0.004)
    for i, z in enumerate((0.72, 0.86, 1.00, 1.14)):
        cube(f"{prefix}_locker_slot_{i}", (1.04, -0.010, z), (0.018, 0.48, 0.045), m["black"], coll, edge=0.001)
        cube(f"{prefix}_locker_label_{i}", (1.02, -0.26 + i * 0.17, z), (0.020, 0.055, 0.018), m["paper"], coll, edge=0)

    # --- Floor scuff marks ---
    for i in range(5):
        cube(f"{prefix}_scuff_{i}", (0.10 + i * 0.30, -0.15 + (i % 2) * 0.30, 0.37), (0.10, 0.04, 0.003),
             m["floor_line"], coll, rot=(0, 0, math.radians(-12 + i * 6)), edge=0)

    # --- Rear door frame / threshold (from inside) ---
    cube(f"{prefix}_rear_threshold", (1.56, 0, 0.40), (0.04, 0.68, 0.04), m["metal"], coll, edge=0.003)


def build_van(m) -> bpy.types.Collection:
    coll = collection("ASV4_Second_Hand_Dispatch_Van")
    _build_van_exterior(m, coll)
    _build_van_interior(m, coll)
    return coll


def build_van_transit_interior(m) -> bpy.types.Collection:
    """Rear compartment only — used by Unity for the 3D transit camera view."""
    coll = collection("ASV4_Van_Transit_Interior")
    _build_van_interior(m, coll, prefix="transit")

    # Rear doors modeled ajar (slightly open, showing dark exterior)
    cube("transit_rear_door_L", (1.62, -0.38, 0.92), (0.04, 0.30, 0.56), m["metal_worn"], coll,
         rot=(0, 0, math.radians(18)), edge=0.005)
    cube("transit_rear_door_R", (1.62, 0.38, 0.92), (0.04, 0.30, 0.56), m["metal_worn"], coll,
         rot=(0, 0, math.radians(-18)), edge=0.005)

    # Side window slits (narrow, for parallax exterior view)
    cube("transit_window_slit_L", (0.45, -0.70, 1.18), (0.65, 0.015, 0.06), m["glass"], coll, edge=0.001)
    cube("transit_window_slit_R", (0.45, 0.70, 1.18), (0.65, 0.015, 0.06), m["glass"], coll, edge=0.001)
    # Wire mesh over slits
    for i in range(8):
        x = 0.14 + i * 0.08
        cyl(f"transit_mesh_L_{i}", (x, -0.71, 1.18), 0.004, 0.058, m["metal"], coll, vertices=4, edge=0)
        cyl(f"transit_mesh_R_{i}", (x, 0.71, 1.18), 0.004, 0.058, m["metal"], coll, vertices=4, edge=0)

    # Seat position empties (for Unity player placement — exported as tiny cubes)
    for i, (x, y, rot_z) in enumerate([
        (0.15, -0.42, 0), (0.85, -0.42, 0),       # left bench, facing right
        (0.15, 0.42, math.pi), (0.85, 0.42, math.pi),  # right bench, facing left
    ]):
        cube(f"transit_seat_marker_{i}", (x, y, 0.52), (0.02, 0.02, 0.02), m["exit"], coll,
             rot=(0, 0, rot_z), edge=0)

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
    # The HQ collection is exported at raw Blender coordinates so that Unity-side
    # overlays (interaction collider for the dispatch CRT, walkable floors, lights,
    # van boarding trigger) can use the Blender script's source-of-truth positions
    # without compensating for a recentering offset. Other collections (worker, van,
    # notebook, etc.) still get recentered so their FBX origin sits at the asset's
    # bottom-center, which is convenient for character / prop placement.
    recenter = "HQ" not in coll.name
    offset = origin_offset(objs) if recenter else Vector((0, 0, 0))
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


def build_first_person_gloves(m) -> bpy.types.Collection:
    """Low-poly first-person gloved hands (~300 tris). Origin at wrist center."""
    coll = collection("ASV4_FirstPerson_Gloves")

    # Right hand
    rx, ry = 0.22, 0.0

    # forearm + sleeve cuff
    cube("fp_right_forearm", (rx, ry, -0.12), (0.065, 0.055, 0.22), m["uniform"], coll, edge=0.004)
    cube("fp_right_cuff", (rx, ry, -0.01), (0.072, 0.062, 0.04), m["uniform"], coll, edge=0.002)

    # palm
    cube("fp_right_palm", (rx, ry, 0.10), (0.06, 0.04, 0.09), m["black"], coll, edge=0.006)

    # thumb
    cube("fp_right_thumb_base", (rx - 0.045, ry - 0.02, 0.06), (0.022, 0.02, 0.04), m["black"], coll,
         rot=(0, 0, math.radians(25)), edge=0.003)
    cube("fp_right_thumb_tip", (rx - 0.065, ry - 0.025, 0.09), (0.018, 0.018, 0.03), m["black"], coll,
         rot=(0, 0, math.radians(15)), edge=0.002)

    # fingers
    finger_x_offsets = [-0.024, -0.008, 0.008, 0.024]
    finger_lengths = [0.026, 0.032, 0.031, 0.024]
    finger_base_z = 0.135
    for i, (fx, fl) in enumerate(zip(finger_x_offsets, finger_lengths)):
        cube(f"fp_right_finger_{i}_base", (rx + fx, ry - 0.010, finger_base_z), (0.018, 0.020, 0.018), m["black"], coll, edge=0.002)
        cube(f"fp_right_finger_{i}_mid", (rx + fx, ry - 0.012, finger_base_z + fl * 0.35), (0.015, 0.017, fl * 0.35), m["black"], coll, edge=0.002)
        cube(f"fp_right_finger_{i}_tip", (rx + fx, ry - 0.016, finger_base_z + fl * 0.62), (0.013, 0.014, fl * 0.22), m["black"], coll, edge=0.001)

    # knuckle ridge
    cube("fp_right_knuckle_ridge", (rx, ry - 0.025, 0.145), (0.058, 0.008, 0.012), m["black"], coll, edge=0.002)

    # Left hand — mirror of right
    lx = -rx

    cube("fp_left_forearm", (lx, ry, -0.12), (0.065, 0.055, 0.22), m["uniform"], coll, edge=0.004)
    cube("fp_left_cuff", (lx, ry, -0.01), (0.072, 0.062, 0.04), m["uniform"], coll, edge=0.002)
    cube("fp_left_palm", (lx, ry, 0.10), (0.06, 0.04, 0.09), m["black"], coll, edge=0.006)

    cube("fp_left_thumb_base", (lx + 0.045, ry - 0.02, 0.06), (0.022, 0.02, 0.04), m["black"], coll,
         rot=(0, 0, math.radians(-25)), edge=0.003)
    cube("fp_left_thumb_tip", (lx + 0.065, ry - 0.025, 0.09), (0.018, 0.018, 0.03), m["black"], coll,
         rot=(0, 0, math.radians(-15)), edge=0.002)

    for i, (fx, fl) in enumerate(zip(finger_x_offsets, finger_lengths)):
        cube(f"fp_left_finger_{i}_base", (lx - fx, ry - 0.010, finger_base_z), (0.018, 0.020, 0.018), m["black"], coll, edge=0.002)
        cube(f"fp_left_finger_{i}_mid", (lx - fx, ry - 0.012, finger_base_z + fl * 0.35), (0.015, 0.017, fl * 0.35), m["black"], coll, edge=0.002)
        cube(f"fp_left_finger_{i}_tip", (lx - fx, ry - 0.016, finger_base_z + fl * 0.62), (0.013, 0.014, fl * 0.22), m["black"], coll, edge=0.001)

    cube("fp_left_knuckle_ridge", (lx, ry - 0.025, 0.145), (0.058, 0.008, 0.012), m["black"], coll, edge=0.002)

    # wristwatch strap hint on left wrist
    cube("fp_left_watch_strap", (lx, ry - 0.032, 0.0), (0.032, 0.006, 0.035), m["metal"], coll, edge=0.001)

    return coll


def build_flashlight(m) -> bpy.types.Collection:
    """Low-poly handheld flashlight — ~150 tris. Origin at grip center."""
    coll = collection("ASV4_Item_Flashlight")

    # Main body — rubber grip tube
    cyl("fl_body", (0.0, 0.0, 0.0), 0.032, 0.22, m["black"], coll, vertices=8, edge=0.003)
    # Head — wider reflector housing
    cyl("fl_head", (0.0, 0.0, 0.135), 0.048, 0.055, m["metal_worn"], coll, vertices=8, edge=0.004)
    # Lens face — emissive amber
    cyl("fl_lens", (0.0, 0.0, 0.162), 0.038, 0.008, m["amber"], coll, vertices=8)
    # Tail cap
    cyl("fl_tail", (0.0, 0.0, -0.118), 0.035, 0.012, m["metal_worn"], coll, vertices=8, edge=0.002)
    # Rubber grip texture ridges
    for i in range(4):
        z = -0.06 + i * 0.028
        cyl(f"fl_grip_{i}", (0.0, 0.0, z), 0.034, 0.006, m["black"], coll, vertices=8)
    # Pocket clip
    cube("fl_clip", (0.038, 0.0, 0.02), (0.006, 0.005, 0.14), m["metal"], coll, edge=0.001)

    return coll


def build_battery(m) -> bpy.types.Collection:
    """Low-poly AA battery — ~80 tris. Origin at center."""
    coll = collection("ASV4_Item_Battery")

    # Main body — amber/cardboard label
    cyl("bat_body", (0.0, 0.0, 0.0), 0.028, 0.10, m["cardboard"], coll, vertices=8, edge=0.002)
    # Positive terminal (nub top)
    cyl("bat_pos", (0.0, 0.0, 0.056), 0.010, 0.008, m["metal"], coll, vertices=6)
    # Negative terminal (flat bottom cap)
    cyl("bat_neg", (0.0, 0.0, -0.054), 0.028, 0.006, m["metal"], coll, vertices=8)
    # Warning label band — stamp red
    cyl("bat_label_band", (0.0, 0.0, 0.015), 0.0285, 0.032, m["debt"], coll, vertices=8)

    return coll


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
        "ASV4_Van_Transit_Interior": build_van_transit_interior(m),
        "ASV4_FirstPerson_Gloves": build_first_person_gloves(m),
        "ASV4_Item_Flashlight": build_flashlight(m),
        "ASV4_Item_Battery": build_battery(m),
    }
    lights_camera()
    export_all(assets)


if __name__ == "__main__":
    main()
