"""
Generate an original AccidentSquad outsourced-civic-horror kit.

Run from Blender:
  blender --background --factory-startup --python D:/AccidentSquad/docs/art/blender_outsourced_civic_horror_kit.py

Output:
  D:/AccidentSquad/Assets/_Project/Art/Generated/OutsourcedCivicKit
"""

from __future__ import annotations

import math
from pathlib import Path

import bpy
from mathutils import Vector

import sys
sys.path.append(str(Path(__file__).parent))
import blender_retro_industrial_kit as kit


OUTPUT_DIR = Path(r"D:/AccidentSquad/Assets/_Project/Art/Generated/OutsourcedCivicKit")
BLEND_NAME = "accidentsquad_outsourced_civic_horror_kit.blend"
FBX_NAME = "accidentsquad_outsourced_civic_horror_kit.fbx"
PREVIEW_NAME = "accidentsquad_outsourced_civic_horror_preview.png"


def mats() -> dict[str, bpy.types.Material]:
    return {
        "wall": kit.make_mat("AS_institutional_gray_green", (0.30, 0.36, 0.35, 1)),
        "floor": kit.make_mat("AS_dirty_tile_floor", (0.22, 0.24, 0.23, 1)),
        "paper": kit.make_mat("AS_dirty_paper", (0.78, 0.75, 0.60, 1)),
        "cardboard": kit.make_mat("AS_cheap_cardboard", (0.43, 0.30, 0.16, 1)),
        "dead_metal": kit.make_mat("AS_dead_metal", (0.05, 0.055, 0.055, 1)),
        "terminal": kit.make_mat("AS_terminal_green", (0.03, 0.85, 0.36, 1)),
        "exit": kit.make_mat("AS_exit_green", (0.04, 0.70, 0.30, 1)),
        "debt": kit.make_mat("AS_debt_red", (0.62, 0.03, 0.025, 1)),
        "eye": kit.make_mat("AS_monster_eye_red", (1.0, 0.03, 0.0, 1)),
        "cyan": kit.make_mat("AS_school_cold_cyan", (0.42, 0.86, 0.82, 1)),
        "cloth": kit.make_mat("AS_cheap_uniform_green", (0.12, 0.24, 0.18, 1)),
        "vest": kit.make_mat("AS_faded_safety_vest", (0.88, 0.45, 0.10, 1)),
        "skin": kit.make_mat("AS_muted_skin", (0.62, 0.45, 0.34, 1)),
        "glass": kit.make_mat("AS_dirty_window_glass", (0.07, 0.12, 0.15, 1)),
        "van": kit.make_mat("AS_second_hand_van_white", (0.72, 0.70, 0.62, 1)),
        "black": kit.make_mat("AS_flat_black", (0.01, 0.01, 0.01, 1)),
    }


def text(
    name: str,
    body: str,
    loc: tuple[float, float, float],
    rot: tuple[float, float, float],
    size: float,
    mat: bpy.types.Material,
    coll: bpy.types.Collection,
) -> bpy.types.Object:
    bpy.ops.object.text_add(location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.body = body
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.003
    obj.data.materials.append(mat)
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.object
    kit.move_to_collection(obj, coll)
    return obj


def poster(name: str, body: str, loc: tuple[float, float, float], w: float, h: float, mat_bg, mat_text, coll) -> None:
    kit.cube(name + "_paper", loc, (w, 0.015, h), mat_bg, coll)
    text(name + "_text", body, (loc[0], loc[1] - 0.018, loc[2] + 0.002), (math.radians(90), 0, 0), min(w, h) * 0.22, mat_text, coll)


def hq(root, m) -> None:
    coll = kit.new_collection("01_HQ_broke_commission_office", root)
    y = 0.0
    kit.cube("hq_floor_dirty_carpet", (0, y, -0.05), (4.6, 3.8, 0.05), m["floor"], coll)
    kit.cube("hq_back_wall", (0, y + 3.8, 1.45), (4.6, 0.10, 1.45), m["wall"], coll)
    kit.cube("hq_left_wall", (-4.6, y, 1.45), (0.10, 3.8, 1.45), m["wall"], coll)
    kit.cube("hq_right_wall", (4.6, y, 1.45), (0.10, 3.8, 1.45), m["wall"], coll)
    kit.cube("hq_low_ceiling_shadow", (0, y, 2.92), (4.6, 3.8, 0.04), m["black"], coll)

    kit.cube("office_computer_desk", (-1.5, y + 1.65, 0.48), (1.15, 0.45, 0.48), m["cardboard"], coll)
    kit.cube("office_crt_case", (-1.5, y + 1.22, 1.15), (0.42, 0.24, 0.28), m["dead_metal"], coll)
    kit.cube("office_terminal_screen_green", (-1.5, y + 0.97, 1.17), (0.32, 0.025, 0.18), m["terminal"], coll)
    kit.cube("office_keyboard_slab", (-1.5, y + 1.15, 0.95), (0.42, 0.14, 0.025), m["black"], coll)
    text("terminal_label", "JOB / DEBT / SHOP", (-1.5, y + 0.93, 1.46), (math.radians(90), 0, 0), 0.12, m["terminal"], coll)

    kit.cube("debt_wall_board", (1.85, y + 3.68, 1.45), (1.35, 0.035, 0.82), m["debt"], coll)
    text("debt_board_text", "OVERDUE\nTAKEOVER\nPRESSURE", (1.85, y + 3.63, 1.45), (math.radians(90), 0, 0), 0.19, m["paper"], coll)
    for i in range(8):
        x = 0.75 + (i % 4) * 0.42
        z = 0.86 + (i // 4) * 0.38
        kit.cube(f"debt_notice_{i+1}", (x, y + 3.61, z), (0.16, 0.012, 0.11), m["paper"], coll)

    kit.cube("equipment_shelf_back", (-3.45, y + 3.35, 1.05), (0.75, 0.16, 0.95), m["dead_metal"], coll)
    for z in [0.55, 1.05, 1.55]:
        kit.cube(f"equipment_shelf_plank_{z}", (-3.45, y + 3.15, z), (0.82, 0.10, 0.04), m["dead_metal"], coll)
    kit.cube("gear_medkit", (-3.85, y + 3.05, 0.77), (0.18, 0.12, 0.12), m["paper"], coll)
    kit.cube("gear_medkit_cross", (-3.85, y + 2.91, 0.77), (0.10, 0.012, 0.025), m["debt"], coll)
    kit.cylinder("gear_spray_can", (-3.42, y + 3.04, 0.78), 0.07, 0.28, m["cyan"], coll, vertices=8)
    kit.cylinder("gear_flashlight", (-3.08, y + 3.05, 1.24), 0.055, 0.34, m["black"], coll, vertices=8, rotation=(0, math.radians(90), 0))
    poster("hq_company_sign", "ACCIDENT SQUAD", (0, y + 3.62, 2.28), 1.25, 0.20, m["black"], m["terminal"], coll)

    kit.cube("broken_sofa_base", (2.7, y - 1.95, 0.35), (0.9, 0.32, 0.22), m["cloth"], coll)
    kit.cube("broken_sofa_back", (2.7, y - 2.25, 0.75), (0.9, 0.09, 0.42), m["cloth"], coll)
    kit.cube("missing_cushion_shadow", (2.35, y - 1.95, 0.61), (0.25, 0.26, 0.035), m["black"], coll)


def school_hall(root, m) -> None:
    coll = kit.new_collection("02_school_lost_item_hallway", root)
    y = 8.0
    kit.cube("school_hall_floor", (0, y, -0.04), (2.3, 5.0, 0.04), m["floor"], coll)
    kit.cube("school_left_wall", (-2.3, y, 1.35), (0.08, 5.0, 1.35), m["wall"], coll)
    kit.cube("school_right_wall", (2.3, y, 1.35), (0.08, 5.0, 1.35), m["wall"], coll)
    kit.cube("school_ceiling", (0, y, 2.7), (2.3, 5.0, 0.04), m["black"], coll)

    for i in range(7):
        ly = y - 4.0 + i * 1.25
        kit.cube(f"locker_bank_{i+1}", (-2.18, ly, 0.92), (0.16, 0.42, 0.82), m["dead_metal"], coll)
        kit.cube(f"locker_overdue_slip_{i+1}", (-2.285, ly - 0.10, 1.18), (0.012, 0.14, 0.055), m["debt"], coll)

    for i, sy in enumerate([y - 3.1, y - 0.8, y + 1.5, y + 3.8]):
        kit.cube(f"fluorescent_cyan_{i+1}", (0, sy, 2.58), (0.75, 0.045, 0.04), m["cyan"], coll)
        bpy.ops.object.light_add(type="POINT", location=(0, sy, 2.35))
        light = bpy.context.object
        light.name = f"school_cyan_pool_light_{i+1}"
        light.data.color = (0.42, 0.86, 0.82)
        light.data.energy = 45
        light.data.shadow_soft_size = 2.0
        kit.move_to_collection(light, coll)

    poster("classroom_sign", "CLASS 3-B", (2.21, y - 1.4, 1.75), 0.16, 0.28, m["paper"], m["black"], coll)
    poster("debt_office_sign", "HOMEWORK\nDEBT\nOFFICE", (2.21, y + 2.2, 1.65), 0.18, 0.42, m["debt"], m["paper"], coll)
    kit.cube("school_exit_marker", (0, y - 4.92, 1.65), (0.65, 0.03, 0.20), m["exit"], coll)
    text("school_exit_text", "EXIT", (0, y - 4.96, 1.66), (math.radians(90), 0, 0), 0.18, m["black"], coll)


def worker(root, m) -> None:
    coll = kit.new_collection("03_outsourced_worker", root)
    x, y = -7.5, 0.0
    kit.cube("worker_boots", (x, y, 0.08), (0.44, 0.24, 0.08), m["black"], coll)
    kit.cube("worker_legs", (x, y, 0.56), (0.34, 0.18, 0.46), m["cloth"], coll)
    kit.cube("worker_torso_uniform", (x, y, 1.18), (0.46, 0.25, 0.55), m["cloth"], coll)
    kit.cube("worker_safety_vest", (x, y - 0.26, 1.22), (0.50, 0.035, 0.42), m["vest"], coll)
    kit.cube("worker_vest_reflective_bar", (x, y - 0.30, 1.28), (0.40, 0.018, 0.035), m["paper"], coll)
    kit.sphere("worker_head", (x, y, 1.79), (0.23, 0.22, 0.25), m["skin"], coll)
    kit.cube("worker_cheap_helmet", (x, y, 2.03), (0.29, 0.24, 0.09), m["vest"], coll)
    kit.cube("worker_arm_L", (x - 0.48, y, 1.05), (0.11, 0.12, 0.52), m["cloth"], coll)
    kit.cube("worker_arm_R", (x + 0.48, y, 1.05), (0.11, 0.12, 0.52), m["cloth"], coll)
    kit.cylinder("worker_flashlight_hand", (x + 0.62, y - 0.12, 0.72), 0.045, 0.35, m["black"], coll, vertices=8, rotation=(math.radians(90), 0, 0))
    kit.cube("worker_name_badge", (x - 0.13, y - 0.285, 1.36), (0.10, 0.012, 0.04), m["paper"], coll)


def homework_debt_collector(root, m) -> None:
    coll = kit.new_collection("04_homework_debt_collector", root)
    x, y = -10.8, 0.0
    kit.cube("collector_long_feet", (x, y, 0.08), (0.62, 0.32, 0.08), m["black"], coll)
    kit.cube("collector_red_coat_lower", (x, y, 0.85), (0.44, 0.24, 0.72), m["debt"], coll)
    kit.cube("collector_red_coat_upper", (x, y, 1.65), (0.54, 0.25, 0.74), m["debt"], coll)
    kit.cube("collector_paper_collar", (x, y - 0.18, 2.05), (0.42, 0.03, 0.18), m["paper"], coll)
    kit.sphere("collector_head_receipt_mask", (x, y, 2.44), (0.26, 0.20, 0.38), m["paper"], coll)
    kit.cube("collector_eye_L", (x - 0.10, y - 0.205, 2.50), (0.055, 0.016, 0.035), m["eye"], coll)
    kit.cube("collector_eye_R", (x + 0.10, y - 0.205, 2.50), (0.055, 0.016, 0.035), m["eye"], coll)
    kit.cube("collector_mouth_receipt_slot", (x, y - 0.21, 2.35), (0.20, 0.014, 0.025), m["black"], coll)
    kit.cube("collector_long_arm_L", (x - 0.72, y - 0.02, 1.22), (0.10, 0.10, 0.95), m["debt"], coll)
    kit.cube("collector_long_arm_R", (x + 0.72, y - 0.02, 1.22), (0.10, 0.10, 0.95), m["debt"], coll)
    kit.cube("collector_ledger", (x - 0.93, y - 0.13, 0.76), (0.19, 0.05, 0.26), m["black"], coll)
    kit.cube("collector_ledger_label", (x - 0.93, y - 0.19, 0.82), (0.13, 0.012, 0.05), m["paper"], coll)
    text("collector_label", "PAY\nLATE", (x - 0.93, y - 0.205, 0.83), (math.radians(90), 0, 0), 0.045, m["debt"], coll)
    for i in range(5):
        kit.cube(f"collector_receipt_tail_{i+1}", (x, y + 0.18, 2.18 - i * 0.10), (0.16, 0.018, 0.035), m["paper"], coll)


def notebook(root, m) -> None:
    coll = kit.new_collection("05_missing_homework_notebook", root)
    x, y = -7.5, 3.0
    kit.cube("notebook_yellow_cover", (x, y, 0.06), (0.36, 0.26, 0.04), m["vest"], coll)
    kit.cube("notebook_white_label", (x - 0.05, y - 0.02, 0.105), (0.17, 0.10, 0.012), m["paper"], coll)
    text("notebook_label_text", "HOMEWORK", (x - 0.05, y - 0.02, 0.121), (0, 0, 0), 0.04, m["black"], coll)
    for i in range(4):
        kit.cube(f"notebook_page_stack_{i+1}", (x + 0.48 + i * 0.13, y + 0.02, 0.035), (0.09, 0.13, 0.012), m["paper"], coll)
    kit.cube("notebook_red_overdue_stamp", (x + 0.10, y - 0.13, 0.112), (0.12, 0.035, 0.012), m["debt"], coll)


def van(root, m) -> None:
    coll = kit.new_collection("06_second_hand_dispatch_van", root)
    x, y = 8.0, 0.0
    kit.cube("van_body_dented", (x, y, 0.65), (1.65, 0.76, 0.50), m["van"], coll)
    kit.cube("van_cabin_box", (x - 0.72, y, 1.10), (0.62, 0.72, 0.43), m["van"], coll)
    kit.cube("van_windshield_dirty", (x - 1.08, y - 0.42, 1.17), (0.30, 0.03, 0.18), m["glass"], coll)
    kit.cube("van_side_window", (x - 0.58, y - 0.44, 1.17), (0.24, 0.025, 0.18), m["glass"], coll)
    kit.cube("van_company_green_patch", (x + 0.38, y - 0.79, 0.83), (0.42, 0.025, 0.16), m["terminal"], coll)
    text("van_company_text", "AS", (x + 0.38, y - 0.82, 0.84), (math.radians(90), 0, 0), 0.16, m["black"], coll)
    kit.cube("van_debt_red_slash", (x + 0.95, y - 0.80, 0.92), (0.34, 0.025, 0.055), m["debt"], coll)
    kit.cube("van_roof_rack", (x + 0.35, y, 1.44), (0.92, 0.64, 0.045), m["black"], coll)
    kit.cylinder("van_roof_pipe_L", (x + 0.35, y - 0.30, 1.52), 0.035, 1.0, m["dead_metal"], coll, vertices=8, rotation=(0, math.radians(90), 0))
    kit.cylinder("van_roof_pipe_R", (x + 0.35, y + 0.30, 1.52), 0.035, 1.0, m["dead_metal"], coll, vertices=8, rotation=(0, math.radians(90), 0))
    for i, wx in enumerate([x - 1.05, x + 1.05]):
        kit.cylinder(f"van_wheel_L_{i+1}", (wx, y - 0.82, 0.35), 0.25, 0.18, m["black"], coll, vertices=10, rotation=(math.radians(90), 0, 0))
        kit.cylinder(f"van_wheel_R_{i+1}", (wx, y + 0.82, 0.35), 0.25, 0.18, m["black"], coll, vertices=10, rotation=(math.radians(90), 0, 0))


def lighting_camera() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -8, 7))
    area = bpy.context.object
    area.name = "AS_preview_soft_overhead"
    area.data.energy = 600
    area.data.size = 7
    bpy.ops.object.light_add(type="POINT", location=(-10.8, -1.2, 2.4))
    red = bpy.context.object
    red.name = "AS_collector_eye_warning_light"
    red.data.energy = 90
    red.data.color = (1.0, 0.04, 0.02)
    camera_loc = Vector((7.8, -15.0, 8.5))
    target = Vector((0.0, 3.0, 1.0))
    bpy.ops.object.camera_add(location=camera_loc)
    camera = bpy.context.object
    camera.rotation_euler = (target - camera_loc).to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 17
    bpy.context.scene.camera = camera


def export() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_DIR / BLEND_NAME))
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT_DIR / FBX_NAME),
        use_selection=True,
        apply_unit_scale=True,
        object_types={"MESH", "LIGHT", "CAMERA"},
        add_leaf_bones=False,
    )
    bpy.context.scene.render.filepath = str(OUTPUT_DIR / PREVIEW_NAME)
    bpy.context.scene.render.resolution_x = 1600
    bpy.context.scene.render.resolution_y = 900
    bpy.ops.render.render(write_still=True)


def main() -> None:
    kit.reset_scene()
    bpy.context.scene.unit_settings.system = "METRIC"
    root = kit.new_collection("AccidentSquad_OutsourcedCivicHorrorKit")
    m = mats()
    hq(root, m)
    school_hall(root, m)
    worker(root, m)
    homework_debt_collector(root, m)
    notebook(root, m)
    van(root, m)
    lighting_camera()
    export()


if __name__ == "__main__":
    main()
