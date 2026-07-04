"""Civic Idol (市政圣像) probe + hero preview.
Imports the Quaternius Big_Demon FBX, prints the object/action inventory (to learn
the trident node name for the Unity-side prefab strip), applies the statue-mode BC
atlas, and renders two stills: frozen mid-stride (Run midframe) and dormant (Idle
midframe). Headless: blender --background --python preview_civic_idol.py
"""
import bpy, math
from mathutils import Vector, Euler

FBX = "D:/BlackCommission/tools/rigging/input/qm/Big_Demon.fbx"
ATLAS = "D:/BlackCommission/Assets/_Project/Art/Monsters/CivicIdol/CivicIdol_Atlas.png"
OUT = "D:/BlackCommission/tools/rigging/preview"

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

bpy.ops.import_scene.fbx(filepath=FBX)

print("[IDOL] ---- object inventory ----")
for o in bpy.context.scene.objects:
    parent = o.parent.name if o.parent else "-"
    print(f"[IDOL] obj: {o.name:32s} type={o.type:8s} parent={parent}")
print("[IDOL] ---- actions ----")
for a in sorted(bpy.data.actions, key=lambda a: a.name):
    print(f"[IDOL] act: {a.name:40s} frames={a.frame_range[0]:.0f}-{a.frame_range[1]:.0f}")

# Strip handheld props for the statue read (report what matched).
doomed = [o for o in bpy.context.scene.objects
          if o.type == 'MESH' and any(k in o.name.lower() for k in ("trident", "weapon", "fork"))]
for o in doomed:
    print(f"[IDOL] DELETING prop mesh: {o.name}")
    bpy.data.objects.remove(o, do_unlink=True)
if not doomed:
    print("[IDOL] no separate prop mesh found (trident may be fused or bone-parented)")

mat = bpy.data.materials.new("BC_Statue")
mat.use_nodes = True
bsdf = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
tex = mat.node_tree.nodes.new('ShaderNodeTexImage')
tex.image = bpy.data.images.load(ATLAS)
tex.interpolation = 'Closest'
mat.node_tree.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
bsdf.inputs['Roughness'].default_value = 0.9

for o in bpy.context.scene.objects:
    if o.type == 'MESH':
        if o.data.materials:
            for i in range(len(o.data.materials)):
                o.data.materials[i] = mat
        else:
            o.data.materials.append(mat)

arm = next((o for o in bpy.context.scene.objects if o.type == 'ARMATURE'), None)


def pose(substr):
    act = next((a for a in bpy.data.actions if substr.lower() in a.name.lower()), None)
    if not (arm and act):
        print(f"[IDOL] pose '{substr}' NOT FOUND")
        return
    if not arm.animation_data:
        arm.animation_data_create()
    arm.animation_data.action = act
    mid = int(sum(act.frame_range) / 2)
    bpy.context.scene.frame_set(mid)
    print(f"[IDOL] posed {act.name} @f{mid}")


def stage():
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
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
    print(f"[IDOL] bounds dim={dim:.2f} ctr=({ctr.x:.2f},{ctr.y:.2f},{ctr.z:.2f})")

    bpy.ops.mesh.primitive_plane_add(size=dim * 12, location=(ctr.x, ctr.y, lo.z))
    gm = bpy.data.materials.new("Ground")
    gm.use_nodes = True
    gb = next(n for n in gm.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    gb.inputs['Base Color'].default_value = (0.09, 0.10, 0.10, 1)
    gb.inputs['Roughness'].default_value = 0.95
    bpy.context.active_object.data.materials.append(gm)

    world = bpy.data.worlds[0]
    world.use_nodes = True
    world.node_tree.nodes.get("Background").inputs[0].default_value = (0.05, 0.06, 0.07, 1.0)

    # Key: cold civic fluorescent from high left.
    bpy.ops.object.light_add(type='AREA', location=(ctr.x + dim * 1.2, ctr.y - dim * 1.6, lo.z + dim * 1.9))
    k = bpy.context.active_object
    k.data.energy = 300 * dim * dim
    k.data.size = dim * 2
    k.data.color = (0.85, 0.95, 1.0)
    k.rotation_euler = Euler((math.radians(40), 0, math.radians(30)), 'XYZ')
    # Rim: sodium amber from behind-right (corridor lamp language).
    bpy.ops.object.light_add(type='SPOT', location=(ctr.x - dim * 1.6, ctr.y + dim * 1.2, lo.z + dim * 1.1))
    r = bpy.context.active_object
    r.data.energy = 550 * dim * dim
    r.data.color = (1.0, 0.55, 0.25)
    r.data.spot_size = math.radians(75)
    r.rotation_euler = Euler((math.radians(65), 0, math.radians(-125)), 'XYZ')

    bpy.ops.object.empty_add(location=(ctr.x, ctr.y, ctr.z * 1.05))
    tgt = bpy.context.active_object
    bpy.ops.object.camera_add(location=(ctr.x + dim * 1.05, ctr.y - dim * 1.55, lo.z + dim * 0.55))
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


def shoot(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print(f"[IDOL] wrote {path}")


stage()
pose("Run")
shoot(f"{OUT}/civic_idol_stride.png")
pose("Idle")
shoot(f"{OUT}/civic_idol_dormant.png")
print("[IDOL] DONE")
