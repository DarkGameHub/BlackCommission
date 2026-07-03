"""Render BC-reskinned previews of Quaternius monster candidates.
For each FBX: import, override every material with the recolored BC atlas,
pose to Idle if available, staged render. Also prints the action (clip) list.
Headless: blender --background --python preview_candidates.py
"""
import bpy, math, os
from mathutils import Vector, Euler

QM = "C:/Users/yanfe/AppData/Local/Temp/claude/D--BlackCommission/785721a1-067c-4b6b-aa91-93c9d12e356b/scratchpad/qm"
OUT = "D:/BlackCommission/tools/rigging/preview"

CANDIDATES = [
    ("Big_Orc",          f"{QM}/fbx/Big_Orc.fbx",          f"{QM}/fbx/Atlas_Big_BC.png"),
    ("Big_Yeti",         f"{QM}/fbx/Big_Yeti.fbx",         f"{QM}/fbx/Atlas_Big_BC.png"),
    ("Big_Demon",        f"{QM}/fbx/Big_Demon.fbx",        f"{QM}/fbx/Atlas_Big_BC.png"),
    ("Big_Alien",        f"{QM}/fbx/Big_Alien.fbx",        f"{QM}/fbx/Atlas_Big_BC.png"),
    ("Flying_GhostSkull", f"{QM}/fbx/Flying_GhostSkull.fbx", f"{QM}/fbx/Atlas_Flying_BC.png"),
]


def wipe():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.lights,
                 bpy.data.cameras, bpy.data.armatures, bpy.data.actions,
                 bpy.data.images):
        for b in list(coll):
            try:
                coll.remove(b)
            except Exception:
                pass


def bc_material(atlas_path):
    m = bpy.data.materials.new("BC_Skin")
    m.use_nodes = True
    bsdf = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    tex = m.node_tree.nodes.new('ShaderNodeTexImage')
    tex.image = bpy.data.images.load(atlas_path)
    tex.interpolation = 'Closest'
    m.node_tree.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
    bsdf.inputs['Roughness'].default_value = 0.85
    return m


def stage_and_render(name):
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    if not meshes:
        print(f"[QM] {name}: NO MESHES")
        return
    dg = bpy.context.evaluated_depsgraph_get()
    lo = Vector((1e9,) * 3)
    hi = Vector((-1e9,) * 3)
    for o in meshes:
        oe = o.evaluated_get(dg)
        for c in oe.bound_box:
            w = oe.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, w))
            hi = Vector(map(max, hi, w))
    ctr = (lo + hi) / 2
    dim = max(hi - lo)

    bpy.ops.mesh.primitive_plane_add(size=dim * 12, location=(ctr.x, ctr.y, lo.z))
    gm = bpy.data.materials.new("Ground")
    gm.use_nodes = True
    gb = next(n for n in gm.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    gb.inputs['Base Color'].default_value = (0.09, 0.10, 0.10, 1)
    gb.inputs['Roughness'].default_value = 0.95
    bpy.context.active_object.data.materials.append(gm)

    world = bpy.data.worlds[0]
    world.use_nodes = True
    world.node_tree.nodes.get("Background").inputs[0].default_value = (0.055, 0.065, 0.075, 1.0)

    bpy.ops.object.light_add(type='AREA', location=(ctr.x + dim * 1.2, ctr.y - dim * 1.6, lo.z + dim * 1.9))
    k = bpy.context.active_object
    k.data.energy = 320 * dim * dim
    k.data.size = dim * 2
    k.rotation_euler = Euler((math.radians(40), 0, math.radians(30)), 'XYZ')
    bpy.ops.object.light_add(type='SPOT', location=(ctr.x - dim * 1.6, ctr.y + dim * 1.2, lo.z + dim * 1.1))
    r = bpy.context.active_object
    r.data.energy = 600 * dim * dim
    r.data.color = (1.0, 0.5, 0.28)
    r.data.spot_size = math.radians(75)
    r.rotation_euler = Euler((math.radians(65), 0, math.radians(-125)), 'XYZ')
    bpy.ops.object.light_add(type='POINT', location=(ctr.x, ctr.y - dim * 0.7, ctr.z))
    f = bpy.context.active_object
    f.data.energy = 25 * dim * dim
    f.data.color = (1.0, 0.35, 0.25)

    bpy.ops.object.empty_add(location=(ctr.x, ctr.y, ctr.z * 1.05))
    tgt = bpy.context.active_object
    bpy.ops.object.camera_add(location=(ctr.x + dim * 1.05, ctr.y - dim * 1.55, lo.z + dim * 0.5))
    cam = bpy.context.active_object
    cam.data.lens = 42
    tc = cam.constraints.new(type='TRACK_TO')
    tc.target = tgt
    tc.track_axis = 'TRACK_NEGATIVE_Z'
    tc.up_axis = 'UP_Y'
    bpy.context.scene.camera = cam

    scene = bpy.context.scene
    for eng in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE', 'BLENDER_WORKBENCH'):
        try:
            scene.render.engine = eng
            break
        except TypeError:
            continue
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.filepath = f"{OUT}/qm_{name}.png"
    bpy.ops.render.render(write_still=True)
    print(f"[QM] wrote qm_{name}.png")


for name, fbx, atlas in CANDIDATES:
    wipe()
    bpy.ops.import_scene.fbx(filepath=fbx)
    acts = sorted(a.name for a in bpy.data.actions)
    print(f"[QM] {name} actions ({len(acts)}): {', '.join(acts)}")

    mat = bc_material(atlas)
    for o in bpy.context.scene.objects:
        if o.type == 'MESH':
            if o.data.materials:
                for i in range(len(o.data.materials)):
                    o.data.materials[i] = mat
            else:
                o.data.materials.append(mat)

    arm = next((o for o in bpy.context.scene.objects if o.type == 'ARMATURE'), None)
    idle = next((a for a in bpy.data.actions if 'idle' in a.name.lower()), None)
    if arm and idle:
        if not arm.animation_data:
            arm.animation_data_create()
        arm.animation_data.action = idle
        mid = int(sum(idle.frame_range) / 2)
        bpy.context.scene.frame_set(mid)
        print(f"[QM] {name} posed: {idle.name} @f{mid}")

    stage_and_render(name)

print("[QM] ALL DONE")
