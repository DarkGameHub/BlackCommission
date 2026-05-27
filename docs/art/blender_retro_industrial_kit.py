"""
Generate an original low-poly retro industrial horror asset kit for AccidentSquad.

How to use:
1. Open Blender.
2. Run this script from Blender's Scripting tab.
3. It creates a .blend file and an FBX export under:
   D:/AccidentSquad/Assets/_Project/Art/Generated/RetroIndustrialKit

The models are intentionally blocky MVP art direction assets:
- agency office/reception module
- worker character
- tall monster
- compact response vehicle
- corridor/street map modules
"""

from __future__ import annotations

import math
import os
from pathlib import Path

import bpy


OUTPUT_DIR = Path(r"D:/AccidentSquad/Assets/_Project/Art/Generated/RetroIndustrialKit")
BLEND_NAME = "accidentsquad_retro_industrial_kit.blend"
FBX_NAME = "accidentsquad_retro_industrial_kit.fbx"


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)
    for block in list(bpy.data.materials):
        if block.users == 0:
            bpy.data.materials.remove(block)


def make_mat(name: str, color: tuple[float, float, float, float], roughness: float = 0.85) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = 0.0
    return mat


def assign(obj: bpy.types.Object, mat: bpy.types.Material) -> bpy.types.Object:
    obj.data.materials.append(mat)
    return obj


def cube(
    name: str,
    loc: tuple[float, float, float],
    scale: tuple[float, float, float],
    mat: bpy.types.Material,
    collection: bpy.types.Collection,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    assign(obj, mat)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to_collection(obj, collection)
    return obj


def cylinder(
    name: str,
    loc: tuple[float, float, float],
    radius: float,
    depth: float,
    mat: bpy.types.Material,
    collection: bpy.types.Collection,
    vertices: int = 8,
    rotation: tuple[float, float, float] = (0, 0, 0),
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    assign(obj, mat)
    shade_flat(obj)
    move_to_collection(obj, collection)
    return obj


def cone(
    name: str,
    loc: tuple[float, float, float],
    radius1: float,
    radius2: float,
    depth: float,
    mat: bpy.types.Material,
    collection: bpy.types.Collection,
    vertices: int = 6,
    rotation: tuple[float, float, float] = (0, 0, 0),
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius1,
        radius2=radius2,
        depth=depth,
        location=loc,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    assign(obj, mat)
    shade_flat(obj)
    move_to_collection(obj, collection)
    return obj


def sphere(
    name: str,
    loc: tuple[float, float, float],
    scale: tuple[float, float, float],
    mat: bpy.types.Material,
    collection: bpy.types.Collection,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=4, radius=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    assign(obj, mat)
    shade_flat(obj)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to_collection(obj, collection)
    return obj


def shade_flat(obj: bpy.types.Object) -> None:
    if hasattr(obj.data, "polygons"):
        for poly in obj.data.polygons:
            poly.use_smooth = False


def move_to_collection(obj: bpy.types.Object, collection: bpy.types.Collection) -> None:
    for coll in obj.users_collection:
        coll.objects.unlink(obj)
    collection.objects.link(obj)


def new_collection(name: str, parent: bpy.types.Collection | None = None) -> bpy.types.Collection:
    coll = bpy.data.collections.new(name)
    (parent or bpy.context.scene.collection).children.link(coll)
    return coll


def create_materials() -> dict[str, bpy.types.Material]:
    return {
        "concrete": make_mat("AS_concrete_cold_gray", (0.29, 0.31, 0.31, 1)),
        "dark": make_mat("AS_dark_charcoal", (0.045, 0.05, 0.055, 1)),
        "metal": make_mat("AS_worn_metal", (0.42, 0.43, 0.41, 1)),
        "yellow": make_mat("AS_hazard_yellow", (0.95, 0.68, 0.12, 1)),
        "orange": make_mat("AS_faded_rescue_orange", (0.85, 0.28, 0.11, 1)),
        "red": make_mat("AS_alarm_red", (0.58, 0.04, 0.035, 1)),
        "green": make_mat("AS_monitor_green", (0.10, 0.72, 0.33, 1)),
        "glass": make_mat("AS_dirty_blue_glass", (0.08, 0.16, 0.20, 1)),
        "skin": make_mat("AS_muted_skin", (0.62, 0.46, 0.35, 1)),
        "cloth": make_mat("AS_worker_suit_desat_orange", (0.72, 0.31, 0.10, 1)),
        "monster": make_mat("AS_monster_sick_red", (0.46, 0.02, 0.04, 1)),
        "bone": make_mat("AS_bone_mask", (0.78, 0.73, 0.62, 1)),
    }


def add_agency_module(root: bpy.types.Collection, mat: dict[str, bpy.types.Material]) -> None:
    coll = new_collection("01_agency_office_module", root)
    ox, oy = 0.0, 0.0

    cube("agency_floor_8x8", (ox, oy, -0.05), (4.0, 4.0, 0.05), mat["concrete"], coll)
    cube("agency_back_wall", (ox, oy + 4.0, 1.45), (4.0, 0.12, 1.45), mat["concrete"], coll)
    cube("agency_left_wall", (ox - 4.0, oy, 1.45), (0.12, 4.0, 1.45), mat["concrete"], coll)
    cube("agency_right_wall", (ox + 4.0, oy, 1.45), (0.12, 4.0, 1.45), mat["concrete"], coll)
    cube("agency_ceiling_shadow_panel", (ox, oy, 2.95), (4.0, 4.0, 0.04), mat["dark"], coll)

    cube("agency_front_left_wall", (ox - 2.6, oy - 4.0, 1.45), (1.4, 0.12, 1.45), mat["concrete"], coll)
    cube("agency_front_right_wall", (ox + 2.6, oy - 4.0, 1.45), (1.4, 0.12, 1.45), mat["concrete"], coll)
    cube("agency_front_header", (ox, oy - 4.0, 2.55), (4.0, 0.12, 0.35), mat["concrete"], coll)
    cube("agency_double_door_left", (ox - 0.55, oy - 4.08, 1.0), (0.5, 0.05, 1.0), mat["metal"], coll)
    cube("agency_double_door_right", (ox + 0.55, oy - 4.08, 1.0), (0.5, 0.05, 1.0), mat["metal"], coll)
    cube("agency_door_window_left", (ox - 0.55, oy - 4.14, 1.45), (0.36, 0.025, 0.35), mat["glass"], coll)
    cube("agency_door_window_right", (ox + 0.55, oy - 4.14, 1.45), (0.36, 0.025, 0.35), mat["glass"], coll)

    cube("agency_sign_base", (ox, oy - 4.18, 3.05), (1.65, 0.06, 0.28), mat["dark"], coll)
    cube("agency_sign_accident_bar", (ox - 0.45, oy - 4.24, 3.09), (0.7, 0.025, 0.055), mat["yellow"], coll)
    cube("agency_sign_squad_bar", (ox + 0.45, oy - 4.24, 3.0), (0.7, 0.025, 0.055), mat["orange"], coll)

    cube("reception_counter_body", (ox - 1.5, oy + 1.7, 0.55), (1.3, 0.42, 0.55), mat["dark"], coll)
    cube("reception_counter_top", (ox - 1.5, oy + 1.7, 1.12), (1.4, 0.48, 0.08), mat["metal"], coll)
    cube("reception_monitor", (ox - 1.7, oy + 1.38, 1.45), (0.34, 0.06, 0.22), mat["green"], coll)
    cube("reception_keyboard", (ox - 1.35, oy + 1.34, 1.21), (0.32, 0.12, 0.025), mat["dark"], coll)

    for i in range(4):
        x = ox + 2.8
        y = oy + 2.9 - i * 0.55
        cube(f"locker_{i+1}", (x, y, 0.85), (0.38, 0.22, 0.85), mat["metal"], coll)
        cube(f"locker_{i+1}_vent", (x, y - 0.225, 1.2), (0.22, 0.012, 0.025), mat["dark"], coll)

    for i, x in enumerate([-2.7, -0.9, 0.9, 2.7]):
        cube(f"fluorescent_light_{i+1}", (x, oy + 0.2, 2.84), (0.55, 0.045, 0.045), mat["green"], coll)


def add_worker(root: bpy.types.Collection, mat: dict[str, bpy.types.Material]) -> None:
    coll = new_collection("02_worker_character", root)
    ox, oy = -8.0, 0.0

    cube("worker_boot_L", (ox - 0.23, oy, 0.08), (0.18, 0.28, 0.08), mat["dark"], coll)
    cube("worker_boot_R", (ox + 0.23, oy, 0.08), (0.18, 0.28, 0.08), mat["dark"], coll)
    cube("worker_leg_L", (ox - 0.22, oy, 0.55), (0.16, 0.18, 0.44), mat["cloth"], coll)
    cube("worker_leg_R", (ox + 0.22, oy, 0.55), (0.16, 0.18, 0.44), mat["cloth"], coll)
    cube("worker_torso", (ox, oy, 1.18), (0.43, 0.25, 0.55), mat["cloth"], coll)
    cube("worker_chest_harness", (ox, oy - 0.26, 1.22), (0.48, 0.035, 0.12), mat["dark"], coll)
    cube("worker_belt", (ox, oy - 0.01, 0.86), (0.48, 0.27, 0.06), mat["dark"], coll)
    cube("worker_arm_L", (ox - 0.52, oy, 1.1), (0.13, 0.14, 0.52), mat["cloth"], coll)
    cube("worker_arm_R", (ox + 0.52, oy, 1.1), (0.13, 0.14, 0.52), mat["cloth"], coll)
    cube("worker_glove_L", (ox - 0.52, oy - 0.02, 0.52), (0.14, 0.15, 0.12), mat["dark"], coll)
    cube("worker_glove_R", (ox + 0.52, oy - 0.02, 0.52), (0.14, 0.15, 0.12), mat["dark"], coll)
    sphere("worker_helmet", (ox, oy, 1.83), (0.34, 0.31, 0.28), mat["yellow"], coll)
    cube("worker_visor", (ox, oy - 0.28, 1.82), (0.27, 0.035, 0.11), mat["glass"], coll)
    cylinder("worker_air_tank", (ox, oy + 0.32, 1.2), 0.14, 0.72, mat["metal"], coll, vertices=8)
    cube("worker_backpack", (ox, oy + 0.28, 1.08), (0.33, 0.13, 0.38), mat["dark"], coll)


def add_monster(root: bpy.types.Collection, mat: dict[str, bpy.types.Material]) -> None:
    coll = new_collection("03_tall_alarm_monster", root)
    ox, oy = -12.0, 0.0

    cube("monster_feet", (ox, oy - 0.02, 0.08), (0.5, 0.33, 0.08), mat["dark"], coll)
    cube("monster_lower_body", (ox, oy, 0.75), (0.36, 0.24, 0.65), mat["monster"], coll)
    cube("monster_upper_body", (ox, oy, 1.62), (0.5, 0.28, 0.82), mat["monster"], coll)
    cube("monster_spine_shadow", (ox, oy + 0.18, 1.55), (0.08, 0.07, 1.2), mat["dark"], coll)
    sphere("monster_mask_head", (ox, oy - 0.02, 2.55), (0.32, 0.26, 0.42), mat["bone"], coll)
    cube("monster_black_eye_bar", (ox, oy - 0.28, 2.6), (0.27, 0.025, 0.055), mat["dark"], coll)
    cube("monster_mouth_slit", (ox, oy - 0.285, 2.42), (0.22, 0.025, 0.035), mat["dark"], coll)
    cylinder("monster_neck", (ox, oy, 2.12), 0.11, 0.5, mat["monster"], coll, vertices=6)

    cube("monster_arm_L_upper", (ox - 0.6, oy, 1.65), (0.12, 0.12, 0.65), mat["monster"], coll)
    cube("monster_arm_R_upper", (ox + 0.6, oy, 1.65), (0.12, 0.12, 0.65), mat["monster"], coll)
    cube("monster_arm_L_lower", (ox - 0.82, oy - 0.05, 0.95), (0.11, 0.11, 0.72), mat["monster"], coll)
    cube("monster_arm_R_lower", (ox + 0.82, oy - 0.05, 0.95), (0.11, 0.11, 0.72), mat["monster"], coll)

    for side, sx in [("L", -1), ("R", 1)]:
        for i in range(3):
            cone(
                f"monster_claw_{side}_{i+1}",
                (ox + sx * (0.88 + i * 0.045), oy - 0.11, 0.47),
                0.035,
                0.0,
                0.22,
                mat["bone"],
                coll,
                vertices=5,
                rotation=(math.radians(90), 0, 0),
            )


def add_vehicle(root: bpy.types.Collection, mat: dict[str, bpy.types.Material]) -> None:
    coll = new_collection("04_compact_response_vehicle", root)
    ox, oy = 8.0, 0.0

    cube("vehicle_lower_body", (ox, oy, 0.55), (1.65, 0.78, 0.42), mat["orange"], coll)
    cube("vehicle_cabin", (ox - 0.42, oy - 0.02, 1.05), (0.72, 0.7, 0.42), mat["orange"], coll)
    cube("vehicle_rear_box", (ox + 0.66, oy, 1.0), (0.78, 0.72, 0.36), mat["metal"], coll)
    cube("vehicle_front_window", (ox - 0.82, oy - 0.43, 1.12), (0.38, 0.035, 0.18), mat["glass"], coll)
    cube("vehicle_side_window_L", (ox - 0.37, oy - 0.47, 1.12), (0.24, 0.035, 0.18), mat["glass"], coll)
    cube("vehicle_hazard_stripe", (ox + 0.6, oy - 0.81, 0.82), (0.72, 0.035, 0.08), mat["yellow"], coll)
    cube("vehicle_light_bar", (ox - 0.35, oy, 1.53), (0.38, 0.12, 0.05), mat["red"], coll)

    for i, x in enumerate([ox - 1.05, ox + 1.05]):
        cylinder(
            f"vehicle_wheel_frontback_{i+1}_L",
            (x, oy - 0.82, 0.38),
            0.24,
            0.18,
            mat["dark"],
            coll,
            vertices=10,
            rotation=(math.radians(90), 0, 0),
        )
        cylinder(
            f"vehicle_wheel_frontback_{i+1}_R",
            (x, oy + 0.82, 0.38),
            0.24,
            0.18,
            mat["dark"],
            coll,
            vertices=10,
            rotation=(math.radians(90), 0, 0),
        )
        cylinder(
            f"vehicle_hub_{i+1}_L",
            (x, oy - 0.92, 0.38),
            0.11,
            0.04,
            mat["metal"],
            coll,
            vertices=8,
            rotation=(math.radians(90), 0, 0),
        )
        cylinder(
            f"vehicle_hub_{i+1}_R",
            (x, oy + 0.92, 0.38),
            0.11,
            0.04,
            mat["metal"],
            coll,
            vertices=8,
            rotation=(math.radians(90), 0, 0),
        )


def add_map_modules(root: bpy.types.Collection, mat: dict[str, bpy.types.Material]) -> None:
    coll = new_collection("05_map_modules", root)
    oy = 8.0

    cube("corridor_floor_module", (0, oy, -0.04), (2.0, 3.0, 0.04), mat["concrete"], coll)
    cube("corridor_wall_L_module", (-2.0, oy, 1.25), (0.1, 3.0, 1.25), mat["concrete"], coll)
    cube("corridor_wall_R_module", (2.0, oy, 1.25), (0.1, 3.0, 1.25), mat["concrete"], coll)
    cube("corridor_ceiling_module", (0, oy, 2.55), (2.0, 3.0, 0.05), mat["dark"], coll)
    cube("corridor_pipe_top_L", (-1.55, oy, 2.35), (0.06, 2.8, 0.06), mat["metal"], coll)
    cube("corridor_pipe_top_R", (1.55, oy, 2.35), (0.06, 2.8, 0.06), mat["metal"], coll)
    cube("corridor_door_frame", (0, oy + 3.02, 1.25), (0.95, 0.08, 1.25), mat["metal"], coll)
    cube("corridor_door_panel", (0, oy + 3.09, 1.05), (0.72, 0.045, 1.05), mat["dark"], coll)
    cube("corridor_keypad_green", (0.92, oy + 3.14, 1.28), (0.08, 0.025, 0.14), mat["green"], coll)

    street_y = 15.0
    cube("street_asphalt_tile", (0, street_y, -0.05), (3.5, 3.5, 0.05), mat["dark"], coll)
    cube("street_sidewalk_L", (-2.5, street_y, 0.05), (0.8, 3.5, 0.08), mat["concrete"], coll)
    cube("street_sidewalk_R", (2.5, street_y, 0.05), (0.8, 3.5, 0.08), mat["concrete"], coll)
    for i in range(4):
        cube(f"street_center_line_{i+1}", (0, street_y - 2.5 + i * 1.6, 0.02), (0.08, 0.45, 0.012), mat["yellow"], coll)


def add_lighting_and_camera() -> None:
    bpy.ops.object.light_add(type="AREA", location=(0, -6, 7))
    key = bpy.context.object
    key.name = "AS_preview_large_sickly_area_light"
    key.data.energy = 550
    key.data.size = 6

    bpy.ops.object.light_add(type="POINT", location=(-10, -2, 3))
    point = bpy.context.object
    point.name = "AS_preview_red_warning_light"
    point.data.energy = 130
    point.data.color = (1.0, 0.13, 0.08)

    bpy.ops.object.camera_add(location=(7.5, -12.0, 6.2), rotation=(math.radians(60), 0, math.radians(38)))
    bpy.context.scene.camera = bpy.context.object


def export_outputs() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    blend_path = OUTPUT_DIR / BLEND_NAME
    fbx_path = OUTPUT_DIR / FBX_NAME

    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        object_types={"MESH", "EMPTY", "LIGHT", "CAMERA"},
        add_leaf_bones=False,
    )
    print(f"Saved blend: {blend_path}")
    print(f"Exported FBX: {fbx_path}")


def main() -> None:
    reset_scene()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0

    root = new_collection("AccidentSquad_RetroIndustrialKit")
    mats = create_materials()

    add_agency_module(root, mats)
    add_worker(root, mats)
    add_monster(root, mats)
    add_vehicle(root, mats)
    add_map_modules(root, mats)
    add_lighting_and_camera()
    export_outputs()


if __name__ == "__main__":
    main()
