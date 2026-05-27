"""
AccidentSquad outsourced civic horror art v2.

This version is meant to replace the rough blockout kit with a more usable
low-poly vertical-slice asset set. It exports both a showcase .blend/.fbx and
separate FBX files for Unity prefab creation.
"""

from __future__ import annotations

import math
from pathlib import Path

import bpy
from mathutils import Vector


OUT = Path(r"D:/AccidentSquad/Assets/_Project/Art/Generated/OutsourcedCivicKit_v2")


def clear() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for datablock in list(bpy.data.meshes):
        if datablock.users == 0:
            bpy.data.meshes.remove(datablock)
    for datablock in list(bpy.data.materials):
        if datablock.users == 0:
            bpy.data.materials.remove(datablock)


def material(name: str, color: tuple[float, float, float, float], emission: float = 0.0) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = 0.82
        if emission > 0:
            bsdf.inputs["Emission Color"].default_value = color
            bsdf.inputs["Emission Strength"].default_value = emission
    return mat


def palette() -> dict[str, bpy.types.Material]:
    return {
        "wall": material("ASV2_institutional_gray_green", (0.27, 0.34, 0.32, 1)),
        "wall_dark": material("ASV2_damp_wall_shadow", (0.12, 0.16, 0.15, 1)),
        "floor": material("ASV2_worn_vinyl_floor", (0.22, 0.23, 0.21, 1)),
        "tile": material("ASV2_old_tile_line", (0.12, 0.13, 0.12, 1)),
        "paper": material("ASV2_dirty_paper", (0.78, 0.74, 0.58, 1)),
        "cardboard": material("ASV2_cheap_cardboard", (0.40, 0.27, 0.13, 1)),
        "metal": material("ASV2_dead_metal", (0.055, 0.06, 0.06, 1)),
        "metal2": material("ASV2_worn_locker_metal", (0.32, 0.36, 0.35, 1)),
        "black": material("ASV2_flat_black", (0.01, 0.01, 0.01, 1)),
        "terminal": material("ASV2_terminal_green", (0.03, 0.85, 0.32, 1), 0.7),
        "exit": material("ASV2_exit_green", (0.05, 0.66, 0.27, 1), 0.45),
        "debt": material("ASV2_debt_red", (0.55, 0.025, 0.02, 1)),
        "eye": material("ASV2_monster_eye_red", (1.0, 0.02, 0.0, 1), 1.2),
        "cyan": material("ASV2_school_cyan_light", (0.35, 0.84, 0.78, 1), 0.45),
        "skin": material("ASV2_worker_skin", (0.56, 0.40, 0.30, 1)),
        "uniform": material("ASV2_worker_uniform_green", (0.10, 0.21, 0.16, 1)),
        "vest": material("ASV2_faded_safety_orange", (0.88, 0.42, 0.08, 1)),
        "helmet": material("ASV2_scuffed_helmet_yellow", (0.88, 0.67, 0.12, 1)),
        "glass": material("ASV2_dirty_blue_glass", (0.04, 0.11, 0.14, 1)),
        "van": material("ASV2_old_van_warm_white", (0.72, 0.70, 0.61, 1)),
        "rubber": material("ASV2_old_rubber", (0.015, 0.014, 0.012, 1)),
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
    be = obj.modifiers.new("small_style_bevel", "BEVEL")
    be.width = amount
    be.segments = 1
    be.affect = "EDGES"
    bpy.ops.object.modifier_apply(modifier=be.name)
    wn = obj.modifiers.new("weighted_normals", "WEIGHTED_NORMAL")
    bpy.ops.object.modifier_apply(modifier=wn.name)
    obj.select_set(False)


def cube(name: str, loc, scale, mat, coll, rot=(0, 0, 0), bevel_amount=0.015) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel(obj, bevel_amount)
    return link(obj, coll)


def cyl(name: str, loc, radius, depth, mat, coll, vertices=10, rot=(0, 0, 0), bevel_amount=0) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    for poly in obj.data.polygons:
        poly.use_smooth = False
    bevel(obj, bevel_amount)
    return link(obj, coll)


def sphere(name: str, loc, scale, mat, coll) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=5, radius=1, location=loc)
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
    obj = bpy.context.object
    return link(obj, coll)


def sign(name: str, body: str, loc, scale, bg, fg, coll) -> None:
    cube(name + "_plate", loc, scale, bg, coll, bevel_amount=0.006)
    txt(name + "_text", body, (loc[0], loc[1] - scale[1] - 0.012, loc[2] + 0.002), (math.radians(90), 0, 0), min(scale[0], scale[2]) * 0.35, fg, coll)


def floor_grid(prefix: str, x0: float, y0: float, w: float, d: float, z: float, coll, m) -> None:
    cube(prefix + "_floor_base", (x0, y0, z), (w, d, 0.035), m["floor"], coll, bevel_amount=0.003)
    for i in range(1, int(w * 2)):
        x = x0 - w + i
        cube(f"{prefix}_tile_x_{i}", (x, y0, z + 0.038), (0.006, d, 0.003), m["tile"], coll, bevel_amount=0)
    for i in range(1, int(d * 2)):
        y = y0 - d + i
        cube(f"{prefix}_tile_y_{i}", (x0, y, z + 0.039), (w, 0.006, 0.003), m["tile"], coll, bevel_amount=0)


def build_hq(m) -> bpy.types.Collection:
    coll = collection("ASV2_HQ_Rundown_Commission_Office")
    floor_grid("hq", 0, 0, 4.6, 3.2, -0.04, coll, m)
    cube("hq_back_wall", (0, 3.2, 1.45), (4.6, 0.10, 1.45), m["wall"], coll, bevel_amount=0.01)
    cube("hq_left_wall", (-4.6, 0, 1.45), (0.10, 3.2, 1.45), m["wall"], coll, bevel_amount=0.01)
    cube("hq_right_half_wall", (4.6, 0.85, 1.45), (0.10, 2.35, 1.45), m["wall"], coll, bevel_amount=0.01)
    cube("hq_entry_door_frame", (4.57, -2.20, 1.35), (0.13, 0.62, 1.35), m["metal"], coll)
    cube("hq_entry_door_panel", (4.48, -2.20, 1.15), (0.055, 0.48, 1.08), m["wall_dark"], coll)
    cube("hq_ceiling_panels", (0, 0, 2.82), (4.55, 3.15, 0.035), m["black"], coll, bevel_amount=0.004)

    cube("hq_main_desk", (-1.35, 1.82, 0.48), (1.18, 0.48, 0.48), m["cardboard"], coll)
    cube("hq_desk_top_metal_edge", (-1.35, 1.45, 0.98), (1.26, 0.08, 0.045), m["metal"], coll)
    cube("hq_crt_monitor_body", (-1.35, 1.36, 1.22), (0.42, 0.26, 0.29), m["metal"], coll)
    cube("hq_crt_screen", (-1.35, 1.075, 1.23), (0.31, 0.02, 0.18), m["terminal"], coll, bevel_amount=0.006)
    cube("hq_keyboard", (-1.35, 1.26, 1.00), (0.42, 0.15, 0.025), m["black"], coll, bevel_amount=0.004)
    txt("hq_terminal_words", "JOBS  DEBT  SHOP", (-1.35, 1.04, 1.52), (math.radians(90), 0, 0), 0.11, m["terminal"], coll)

    cube("hq_cable_1", (-0.80, 1.58, 0.08), (0.035, 0.62, 0.025), m["black"], coll, rot=(0, 0, math.radians(16)), bevel_amount=0.004)
    cube("hq_equipment_shelf", (-3.55, 2.82, 1.0), (0.74, 0.20, 0.92), m["metal"], coll)
    for z in (0.55, 1.05, 1.55):
        cube(f"hq_shelf_plank_{z}", (-3.55, 2.52, z), (0.86, 0.10, 0.035), m["metal2"], coll)
    cube("hq_medkit_box", (-3.95, 2.36, 0.76), (0.18, 0.14, 0.12), m["paper"], coll)
    cube("hq_medkit_cross_h", (-3.95, 2.20, 0.76), (0.11, 0.012, 0.018), m["debt"], coll, bevel_amount=0.002)
    cube("hq_medkit_cross_v", (-3.95, 2.20, 0.76), (0.026, 0.013, 0.07), m["debt"], coll, bevel_amount=0.002)
    cyl("hq_spray_can", (-3.55, 2.34, 0.78), 0.065, 0.27, m["cyan"], coll, vertices=8)
    cyl("hq_flashlight", (-3.13, 2.35, 1.23), 0.055, 0.38, m["black"], coll, vertices=8, rot=(0, math.radians(90), 0))

    cube("hq_debt_board", (1.95, 3.08, 1.42), (1.20, 0.035, 0.75), m["debt"], coll, bevel_amount=0.006)
    txt("hq_debt_board_text", "OVERDUE\nTAKEOVER\nPRESSURE", (1.95, 3.035, 1.43), (math.radians(90), 0, 0), 0.17, m["paper"], coll)
    for i in range(10):
        x = 0.95 + (i % 5) * 0.42
        z = 0.68 + (i // 5) * 0.28
        cube(f"hq_debt_notice_{i}", (x, 3.025, z), (0.14, 0.010, 0.075), m["paper"], coll, bevel_amount=0.002)

    cube("hq_sofa_base", (2.65, -1.95, 0.34), (0.88, 0.34, 0.20), m["uniform"], coll)
    cube("hq_sofa_back", (2.65, -2.28, 0.72), (0.88, 0.08, 0.40), m["uniform"], coll)
    cube("hq_missing_cushion_dark", (2.28, -1.95, 0.58), (0.23, 0.25, 0.030), m["black"], coll)
    sign("hq_logo_sign", "ACCIDENT SQUAD", (0, 3.08, 2.32), (1.15, 0.025, 0.14), m["black"], m["terminal"], coll)
    return coll


def build_school(m) -> bpy.types.Collection:
    coll = collection("ASV2_School_Lost_Item_Hallway")
    y = 8.5
    floor_grid("school", 0, y, 2.25, 5.4, -0.035, coll, m)
    cube("school_left_wall", (-2.25, y, 1.42), (0.09, 5.4, 1.42), m["wall"], coll)
    cube("school_right_wall", (2.25, y, 1.42), (0.09, 5.4, 1.42), m["wall"], coll)
    cube("school_ceiling", (0, y, 2.78), (2.20, 5.35, 0.04), m["black"], coll)
    for i, yy in enumerate([y - 4.3, y - 1.45, y + 1.4, y + 4.25]):
        cube(f"school_fluorescent_{i}", (0, yy, 2.63), (0.68, 0.045, 0.035), m["cyan"], coll, bevel_amount=0.006)
    for i in range(8):
        yy = y - 4.3 + i * 1.10
        cube(f"school_locker_{i}", (-2.12, yy, 0.94), (0.16, 0.36, 0.82), m["metal2"], coll)
        cube(f"school_locker_vent_{i}", (-2.23, yy - 0.08, 1.16), (0.012, 0.14, 0.018), m["black"], coll, bevel_amount=0)
        cube(f"school_overdue_sticker_{i}", (-2.235, yy + 0.09, 1.34), (0.012, 0.12, 0.05), m["debt"], coll, bevel_amount=0.002)
    for i, yy in enumerate([y - 2.9, y + 0.35, y + 3.55]):
        cube(f"school_classroom_door_{i}", (2.16, yy, 1.05), (0.07, 0.52, 1.05), m["wall_dark"], coll)
        cube(f"school_door_window_{i}", (2.08, yy - 0.18, 1.55), (0.018, 0.18, 0.16), m["glass"], coll)
    sign("school_debt_office", "HOMEWORK\nDEBT\nOFFICE", (2.10, y + 2.05, 1.78), (0.018, 0.32, 0.25), m["debt"], m["paper"], coll)
    sign("school_exit_sign", "EXIT", (0, y - 5.28, 1.78), (0.62, 0.020, 0.16), m["exit"], m["black"], coll)
    return coll


def build_worker(m) -> bpy.types.Collection:
    coll = collection("ASV2_Worker_Cheap_Outsourced_Uniform")
    x, y = -7.2, 0.0
    cube("worker_left_boot", (x - 0.18, y, 0.08), (0.16, 0.25, 0.08), m["rubber"], coll)
    cube("worker_right_boot", (x + 0.18, y, 0.08), (0.16, 0.25, 0.08), m["rubber"], coll)
    cube("worker_left_leg", (x - 0.18, y, 0.52), (0.14, 0.15, 0.40), m["uniform"], coll)
    cube("worker_right_leg", (x + 0.18, y, 0.52), (0.14, 0.15, 0.40), m["uniform"], coll)
    cube("worker_torso", (x, y, 1.12), (0.40, 0.23, 0.52), m["uniform"], coll)
    cube("worker_vest_front", (x, y - 0.245, 1.14), (0.43, 0.024, 0.43), m["vest"], coll, bevel_amount=0.005)
    cube("worker_reflective_strip", (x, y - 0.273, 1.23), (0.36, 0.012, 0.028), m["paper"], coll, bevel_amount=0.002)
    cube("worker_left_arm", (x - 0.48, y - 0.02, 1.02), (0.105, 0.12, 0.48), m["uniform"], coll)
    cube("worker_right_arm", (x + 0.48, y - 0.02, 1.02), (0.105, 0.12, 0.48), m["uniform"], coll)
    sphere("worker_head", (x, y, 1.72), (0.22, 0.20, 0.24), m["skin"], coll)
    cube("worker_helmet_cap", (x, y, 1.95), (0.29, 0.23, 0.08), m["helmet"], coll)
    cube("worker_helmet_brim", (x, y - 0.20, 1.91), (0.22, 0.08, 0.025), m["helmet"], coll)
    cyl("worker_hand_flashlight", (x + 0.60, y - 0.18, 0.68), 0.045, 0.34, m["black"], coll, vertices=8, rot=(math.radians(90), 0, 0))
    cube("worker_backpack", (x, y + 0.27, 1.10), (0.28, 0.11, 0.35), m["metal"], coll)
    return coll


def build_collector(m) -> bpy.types.Collection:
    coll = collection("ASV2_Monster_Homework_Debt_Collector")
    x, y = -10.5, 0.0
    cube("collector_long_feet", (x, y, 0.08), (0.54, 0.28, 0.08), m["black"], coll)
    cube("collector_lower_coat", (x, y, 0.86), (0.40, 0.22, 0.70), m["debt"], coll)
    cube("collector_upper_coat", (x, y, 1.62), (0.50, 0.24, 0.70), m["debt"], coll)
    cube("collector_paper_collar", (x, y - 0.19, 2.03), (0.42, 0.025, 0.16), m["paper"], coll, bevel_amount=0.004)
    sphere("collector_receipt_head", (x, y, 2.42), (0.25, 0.19, 0.36), m["paper"], coll)
    cube("collector_eye_left", (x - 0.09, y - 0.195, 2.47), (0.045, 0.013, 0.028), m["eye"], coll, bevel_amount=0.002)
    cube("collector_eye_right", (x + 0.09, y - 0.195, 2.47), (0.045, 0.013, 0.028), m["eye"], coll, bevel_amount=0.002)
    cube("collector_receipt_mouth", (x, y - 0.20, 2.31), (0.17, 0.010, 0.022), m["black"], coll, bevel_amount=0.002)
    cube("collector_left_upper_arm", (x - 0.58, y, 1.45), (0.09, 0.10, 0.55), m["debt"], coll)
    cube("collector_right_upper_arm", (x + 0.58, y, 1.45), (0.09, 0.10, 0.55), m["debt"], coll)
    cube("collector_left_lower_arm", (x - 0.78, y - 0.06, 0.82), (0.08, 0.09, 0.66), m["debt"], coll)
    cube("collector_right_lower_arm", (x + 0.78, y - 0.06, 0.82), (0.08, 0.09, 0.66), m["debt"], coll)
    cube("collector_ledger", (x - 0.94, y - 0.15, 0.58), (0.19, 0.035, 0.25), m["black"], coll)
    cube("collector_ledger_label", (x - 0.94, y - 0.19, 0.63), (0.12, 0.010, 0.045), m["paper"], coll, bevel_amount=0.002)
    txt("collector_ledger_text", "LATE", (x - 0.94, y - 0.205, 0.635), (math.radians(90), 0, 0), 0.045, m["debt"], coll)
    for i in range(6):
        cube(f"collector_back_receipt_{i}", (x, y + 0.18, 2.18 - i * 0.095), (0.16, 0.018, 0.030), m["paper"], coll, bevel_amount=0.002)
    return coll


def build_van(m) -> bpy.types.Collection:
    coll = collection("ASV2_Second_Hand_Dispatch_Van")
    x, y = 7.2, 0.0
    cube("van_main_body", (x, y, 0.66), (1.55, 0.70, 0.48), m["van"], coll)
    cube("van_cabin", (x - 0.76, y, 1.08), (0.58, 0.66, 0.40), m["van"], coll)
    cube("van_front_slope_hint", (x - 1.28, y, 0.86), (0.18, 0.62, 0.28), m["van"], coll, rot=(0, math.radians(0), 0), bevel_amount=0.02)
    cube("van_windshield", (x - 1.05, y - 0.37, 1.15), (0.28, 0.018, 0.15), m["glass"], coll)
    cube("van_side_window", (x - 0.62, y - 0.39, 1.15), (0.22, 0.018, 0.15), m["glass"], coll)
    cube("van_company_patch", (x + 0.30, y - 0.72, 0.86), (0.36, 0.018, 0.13), m["terminal"], coll)
    txt("van_as_text", "AS", (x + 0.30, y - 0.744, 0.865), (math.radians(90), 0, 0), 0.12, m["black"], coll)
    cube("van_debt_slash", (x + 0.86, y - 0.72, 0.92), (0.28, 0.018, 0.045), m["debt"], coll)
    cube("van_roof_rack", (x + 0.25, y, 1.42), (0.86, 0.58, 0.035), m["black"], coll)
    for i, wx in enumerate([x - 0.95, x + 0.95]):
        cyl(f"van_wheel_left_{i}", (wx, y - 0.74, 0.34), 0.23, 0.16, m["rubber"], coll, vertices=12, rot=(math.radians(90), 0, 0))
        cyl(f"van_wheel_right_{i}", (wx, y + 0.74, 0.34), 0.23, 0.16, m["rubber"], coll, vertices=12, rot=(math.radians(90), 0, 0))
    return coll


def build_notebook(m) -> bpy.types.Collection:
    coll = collection("ASV2_Missing_Homework_Notebook")
    x, y = -7.2, 2.55
    cube("notebook_cover", (x, y, 0.07), (0.36, 0.25, 0.035), m["helmet"], coll, bevel_amount=0.006)
    cube("notebook_pages", (x + 0.025, y + 0.018, 0.045), (0.34, 0.24, 0.018), m["paper"], coll, bevel_amount=0.003)
    cube("notebook_name_label", (x - 0.06, y - 0.03, 0.108), (0.16, 0.09, 0.008), m["paper"], coll, bevel_amount=0.002)
    txt("notebook_text", "HW", (x - 0.06, y - 0.03, 0.118), (0, 0, 0), 0.07, m["black"], coll)
    cube("notebook_overdue_stamp", (x + 0.12, y - 0.12, 0.111), (0.12, 0.030, 0.008), m["debt"], coll, bevel_amount=0.001)
    return coll


def lights_and_camera() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -5.2, 7.2))
    area = bpy.context.object
    area.name = "ASV2_preview_softbox"
    area.data.energy = 700
    area.data.size = 7
    bpy.ops.object.light_add(type="POINT", location=(-10.5, -1.0, 2.2))
    red = bpy.context.object
    red.name = "ASV2_collector_red_warning_light"
    red.data.energy = 110
    red.data.color = (1.0, 0.03, 0.02)
    cam_loc = Vector((7.5, -13.5, 7.5))
    target = Vector((0.0, 2.7, 1.0))
    bpy.ops.object.camera_add(location=cam_loc)
    cam = bpy.context.object
    cam.rotation_euler = (target - cam_loc).to_track_quat("-Z", "Y").to_euler()
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 15.5
    bpy.context.scene.camera = cam


def select_collection(coll: bpy.types.Collection) -> list[bpy.types.Object]:
    bpy.ops.object.select_all(action="DESELECT")
    objs: list[bpy.types.Object] = []
    def collect(c: bpy.types.Collection) -> None:
        for obj in c.objects:
            if obj.type == "MESH":
                obj.select_set(True)
                objs.append(obj)
        for child in c.children:
            collect(child)
    collect(coll)
    if objs:
        bpy.context.view_layer.objects.active = objs[0]
    return objs


def export_collection(coll: bpy.types.Collection, name: str) -> None:
    select_collection(coll)
    bpy.ops.export_scene.fbx(
        filepath=str(OUT / f"{name}.fbx"),
        use_selection=True,
        apply_unit_scale=True,
        object_types={"MESH"},
        add_leaf_bones=False,
        use_mesh_modifiers=True,
    )


def export_all(collections: dict[str, bpy.types.Collection]) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "accidentsquad_outsourced_civic_horror_v2.blend"))
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(OUT / "accidentsquad_outsourced_civic_horror_v2_showcase.fbx"),
        use_selection=True,
        apply_unit_scale=True,
        object_types={"MESH", "LIGHT", "CAMERA"},
        add_leaf_bones=False,
        use_mesh_modifiers=True,
    )
    for filename, coll in collections.items():
        export_collection(coll, filename)
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1600
    bpy.context.scene.render.resolution_y = 900
    bpy.context.scene.render.filepath = str(OUT / "accidentsquad_outsourced_civic_horror_v2_preview.png")
    bpy.ops.render.render(write_still=True)


def main() -> None:
    clear()
    bpy.context.scene.unit_settings.system = "METRIC"
    m = palette()
    assets = {
        "ASV2_HQ_Rundown_Commission_Office": build_hq(m),
        "ASV2_School_Lost_Item_Hallway": build_school(m),
        "ASV2_Worker_Cheap_Outsourced_Uniform": build_worker(m),
        "ASV2_Monster_Homework_Debt_Collector": build_collector(m),
        "ASV2_Second_Hand_Dispatch_Van": build_van(m),
        "ASV2_Missing_Homework_Notebook": build_notebook(m),
    }
    lights_and_camera()
    export_all(assets)


if __name__ == "__main__":
    main()
