"""
Créatures de bureau — construction procédurale dans Blender (5.x).

Lancement :
  - dans Blender : onglet Scripting > ouvrir ce fichier > Run Script
  - en ligne de commande : blender -b -P build_creatures.py -- <dossier_sortie>

Chaque créature est modélisée à ~1 unité de haut puis réduite à ~7 cm
par l'empty racine (échelle 0.07) pour un usage réel sur un bureau.
Les parties de visage alternatives (yeux, bouches, sourcils, sueur, flammes,
post-it supplémentaires) sont des objets séparés : le script Unity
les active / désactive et les met à l'échelle selon le niveau de stress.
"""
import bpy, bmesh, math, os, sys
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else os.path.join(os.path.dirname(bpy.data.filepath or "."), "out")
os.makedirs(OUT, exist_ok=True)
REAL_SCALE = 0.07

# ---------------------------------------------------------------- scene reset
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

def hexcol(h, a=1.0):
    h = h.lstrip("#")
    srgb = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    lin = [c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb]
    return (*lin, a)

_mats = {}
def mat(name, color, rough=0.55, emit=0.0):
    if name in _mats:
        return _mats[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value = hexcol(color)
    p.inputs["Roughness"].default_value = rough
    if "Specular IOR Level" in p.inputs:
        p.inputs["Specular IOR Level"].default_value = 0.3
    if emit:
        p.inputs["Emission Color"].default_value = hexcol(color)
        p.inputs["Emission Strength"].default_value = emit
    m.diffuse_color = hexcol(color)
    _mats[name] = m
    return m

def set_color(m, color):
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = hexcol(color)
    m.diffuse_color = hexcol(color)

def link(obj, col):
    col.objects.link(obj)
    return obj

def mesh_obj(name, bm, col, material, smooth=True):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    if smooth:
        for p in me.polygons:
            p.use_smooth = True
    me.materials.append(material)
    return link(bpy.data.objects.new(name, me), col)

def sphere_bm(r=1.0, u=32, v=16):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r)
    return bm

def cube_bm(sx, sy, sz):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for vtx in bm.verts:
        vtx.co.x *= sx; vtx.co.y *= sy; vtx.co.z *= sz
    return bm

def bevel_bm(bm, amount):
    bmesh.ops.bevel(bm, geom=list(bm.edges), offset=amount, segments=3, affect='EDGES', profile=0.5)
    return bm

def teardrop_bm(r=1.0):
    bm = sphere_bm(r, 24, 16)
    for vtx in bm.verts:
        if vtx.co.z > 0:
            t = vtx.co.z / r
            k = (1 - t) ** 1.3
            vtx.co.x *= k; vtx.co.y *= k
            vtx.co.z *= 2.6
            vtx.co.x += 0.18 * math.sin(t * 3.0) * t
    return bm

def tube(name, pts, radius, col, material):
    cu = bpy.data.curves.new(name + "_crv", 'CURVE')
    cu.dimensions = '3D'
    cu.bevel_depth = radius
    cu.bevel_resolution = 4
    cu.use_fill_caps = True
    sp = cu.splines.new('POLY')
    sp.points.add(len(pts) - 1)
    for i, p in enumerate(pts):
        sp.points[i].co = (*p, 1.0)
    tmp = bpy.data.objects.new(name + "_tmp", cu)
    col.objects.link(tmp)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(tmp.evaluated_get(dg))
    me.name = name
    bpy.data.objects.remove(tmp)
    bpy.data.curves.remove(cu)
    for p in me.polygons:
        p.use_smooth = True
    me.materials.append(material)
    return link(bpy.data.objects.new(name, me), col)

def orient(obj, loc, normal, spin=0.0):
    q = normal.to_track_quat('-Y', 'Z')
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = q @ Matrix.Rotation(spin, 4, 'Y').to_quaternion()
    obj.location = loc

# ---------------------------------------------------------------- materials
M = {
    "ink": mat("Ink", "#2A2622", 0.4),
    "white": mat("EyeWhite", "#FFF6EC", 0.35),
    "cheek": mat("Cheek", "#E9A891", 0.7),
    "sweat": mat("Sweat", "#9FD3F0", 0.15),
    "cap": mat("CapNavy", "#33507F", 0.6),
    "visor": mat("CapVisor", "#253D63", 0.5),
    "paper": mat("Paper", "#F4EEE2", 0.8),
    "grey": mat("LineGrey", "#B9B2A6", 0.8),
    "red": mat("CalendarRed", "#D9534A", 0.6),
    "flame_o": mat("FlameOrange", "#FF7A1A", 0.5, emit=0.9),
    "flame_r": mat("FlameRed", "#F0452A", 0.5, emit=0.9),
    "flame_y": mat("FlameYellow", "#FFC24E", 0.5, emit=1.2),
    "bubble": mat("Bubble", "#F7F4EE", 0.5),
    "bubble_on": mat("BubbleLit", "#FFC24E", 0.5, emit=1.5),
    "postit_y": mat("PostitYellow", "#F5D76E", 0.85),
    "postit_p": mat("PostitPink", "#F2A7C3", 0.85),
    "postit_b": mat("PostitBlue", "#9FD3F0", 0.85),
    "postit_line": mat("PostitLine", "#B99A2E", 0.85),
}

STRESS_COLORS = {"calm": "#EFE6D8", "alert": "#F2D3AE", "stressed": "#F1A661", "panic": "#E2553A"}

# ---------------------------------------------------------------- creature
def build_creature(name, kind, body_color, col):
    root = link(bpy.data.objects.new(name, None), col)
    root.empty_display_type = 'PLAIN_AXES'
    parts = {}

    def add(key, obj):
        obj.parent = root
        parts[key] = obj
        return obj

    # --- body : galet / dumpling
    body_mat = mat(f"Body_{name}", body_color, 0.6)
    bm = sphere_bm(0.5, 48, 32)
    for vtx in bm.verts:
        x, y, z = vtx.co
        t = (z + 0.5)
        s = 1.10 - 0.30 * t ** 2
        if z < -0.28:
            z = -0.28 + (z + 0.28) * 0.35
        vtx.co = Vector((x * s, y * s * 0.92, z))
    minz = min(v.co.z for v in bm.verts)
    for vtx in bm.verts:
        vtx.co.z -= minz
    body = add("Body", mesh_obj(f"{name}_Body", bm, col, body_mat))
    sub = body.modifiers.new("Subdivision", 'SUBSURF'); sub.levels = 1; sub.render_levels = 2
    H = max(v.co.z for v in body.data.vertices)

    dg = bpy.context.evaluated_depsgraph_get()
    tree = BVHTree.FromObject(body, dg)

    def surf(x, z, out=0.0):
        loc, n, _, _ = tree.ray_cast(Vector((x, -3.0, z)), Vector((0, 1, 0)))
        return loc + n * out, n

    # --- yeux
    ez, ex = 0.53 * H, 0.135
    for side, sx in (("L", -ex), ("R", ex)):
        loc, n = surf(sx, ez, 0.01)
        o = add(f"EyeOpen_{side}", mesh_obj(f"{name}_EyeOpen_{side}", sphere_bm(1, 24, 12), col, M["ink"]))
        o.scale = (0.045, 0.02, 0.05); orient(o, loc, n)
        loc2, _ = surf(sx + 0.015, ez + 0.02, 0.025)
        hl = add(f"EyeShine_{side}", mesh_obj(f"{name}_EyeShine_{side}", sphere_bm(1, 12, 8), col, M["white"]))
        hl.scale = (0.013, 0.006, 0.013); orient(hl, loc2, n)
        # yeux fermés (arc heureux)
        pts = [tuple(surf(sx + 0.05 * t, ez + 0.022 * (1 - t * t), 0.008)[0]) for t in [i / 4 - 1 for i in range(9)]]
        add(f"EyeClosed_{side}", tube(f"{name}_EyeClosed_{side}", pts, 0.011, col, M["ink"]))
        # yeux écarquillés (panique)
        loc3, n3 = surf(sx, ez, 0.012)
        w = add(f"EyeWide_{side}", mesh_obj(f"{name}_EyeWide_{side}", sphere_bm(1, 24, 12), col, M["white"]))
        w.scale = (0.075, 0.025, 0.075); orient(w, loc3, n3)
        loc4, _ = surf(sx, ez, 0.03)
        pu = add(f"EyePupil_{side}", mesh_obj(f"{name}_EyePupil_{side}", sphere_bm(1, 16, 8), col, M["ink"]))
        pu.scale = (0.032, 0.012, 0.032); orient(pu, loc4, n3)
        # sourcils (stressé / panique)
        bz = ez + 0.11
        outer, inner = (sx * 1.45, sx * 0.55) if True else None
        pts = [tuple(surf(outer, bz + 0.015, 0.008)[0]), tuple(surf(inner, bz - 0.02, 0.008)[0])]
        add(f"Brow_{side}", tube(f"{name}_Brow_{side}", pts, 0.012, col, M["ink"]))
        # joues
        loc5, n5 = surf(sx * 1.75, ez - 0.09, 0.004)
        c = add(f"Cheek_{side}", mesh_obj(f"{name}_Cheek_{side}", sphere_bm(1, 16, 8), col, M["cheek"]))
        c.scale = (0.05, 0.01, 0.035); orient(c, loc5, n5)

    # --- bouches
    mz = 0.38 * H
    ts = [i / 4 - 1 for i in range(9)]
    def mouth(key, f, w=0.07):
        pts = [tuple(surf(w * t, mz + f(t), 0.008)[0]) for t in ts]
        return add(key, tube(f"{name}_{key}", pts, 0.011, col, M["ink"]))
    mouth("MouthSmile", lambda t: -0.028 * (1 - t * t))
    mouth("MouthFlat", lambda t: 0.0, 0.055)
    mouth("MouthWorried", lambda t: 0.022 * (1 - t * t), 0.06)
    loc, n = surf(0, mz - 0.01, 0.01)
    mo = add("MouthOpen", mesh_obj(f"{name}_MouthOpen", sphere_bm(1, 24, 12), col, M["ink"]))
    mo.scale = (0.06, 0.02, 0.075); orient(mo, loc, n)

    # --- goutte de sueur
    loc, n = surf(0.36, 0.66 * H, 0.03)
    sw = add("Sweat", mesh_obj(f"{name}_Sweat", teardrop_bm(1), col, M["sweat"]))
    sw.scale = (0.035, 0.025, 0.035); sw.location = loc

    # --- accessoires
    if kind == "facteur":
        # flammes (derrière la casquette, mises à l'échelle par le stress)
        for key, pos, s, m in (("FlameMain", (0, 0.05, H + 0.14), 0.12, "flame_r"),
                               ("FlameLeft", (-0.18, 0.06, H + 0.03), 0.09, "flame_o"),
                               ("FlameRight", (0.18, 0.06, H + 0.03), 0.09, "flame_o"),
                               ("FlameCore", (0, -0.03, H + 0.11), 0.07, "flame_y")):
            f = add(key, mesh_obj(f"{name}_{key}", teardrop_bm(1), col, M[m]))
            f.scale = (s, s, s); f.location = pos
        # casquette de facteur
        bm = sphere_bm(1, 40, 20)
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -0.01], context='VERTS')
        crown = add("CapCrown", mesh_obj(f"{name}_CapCrown", bm, col, M["cap"]))
        crown.scale = (0.31, 0.29, 0.17); crown.location = (0, 0.01, H - 0.09)
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=40, radius1=1, radius2=1, depth=1)
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.y > 0.05], context='VERTS')
        visor = add("CapVisor", mesh_obj(f"{name}_CapVisor", bm, col, M["visor"]))
        visor.scale = (0.25, 0.16, 0.02); visor.location = (0, -0.2, H - 0.095)
        visor.rotation_euler = (math.radians(12), 0, 0)
        band = add("CapBand", mesh_obj(f"{name}_CapBand", sphere_bm(1, 40, 6), col, M["visor"]))
        band.scale = (0.315, 0.295, 0.025); band.location = (0, 0.01, H - 0.085)
        # badge enveloppe
        env = add("CapBadge", mesh_obj(f"{name}_CapBadge", bevel_bm(cube_bm(0.11, 0.015, 0.075), 0.004), col, M["paper"], smooth=False))
        env.location = (0, -0.255, H - 0.005); env.rotation_euler = (math.radians(-28), 0, 0)
        vpts = [(-0.05, -0.27, H + 0.025), (0, -0.282, H + 0.0), (0.05, -0.27, H + 0.025)]
        add("CapBadgeV", tube(f"{name}_CapBadgeV", vpts, 0.005, col, M["cap"]))

    elif kind == "reunions":
        page = add("CalendarPage", mesh_obj(f"{name}_CalendarPage", bevel_bm(cube_bm(0.32, 0.035, 0.27), 0.012), col, M["paper"]))
        page.location = (0, 0.02, H + 0.1)
        head = add("CalendarHeader", mesh_obj(f"{name}_CalendarHeader", bevel_bm(cube_bm(0.322, 0.04, 0.075), 0.012), col, M["red"]))
        head.location = (0, 0.02, H + 0.2)
        for i, (w, z) in enumerate(((0.2, H + 0.11), (0.13, H + 0.06))):
            ln = add(f"CalendarLine{i}", mesh_obj(f"{name}_CalendarLine{i}", cube_bm(w, 0.006, 0.018), col, M["grey"], smooth=False))
            ln.location = (-0.03 + (w - 0.2) / 2, -0.0, z)
        for side, x in (("L", -0.09), ("R", 0.09)):
            ring = add(f"CalendarRing_{side}", mesh_obj(f"{name}_CalendarRing_{side}", sphere_bm(1, 12, 8), col, M["grey"]))
            ring.scale = (0.018, 0.03, 0.03); ring.location = (x, 0.0, H + 0.245)

    elif kind == "messages":
        bub = add("Bubble", mesh_obj(f"{name}_Bubble", sphere_bm(1, 40, 20), col, M["bubble"]))
        bub.scale = (0.26, 0.07, 0.15); bub.location = (0, 0.02, H + 0.33)
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=16, radius1=1, radius2=0, depth=1)
        tail = add("BubbleTail", mesh_obj(f"{name}_BubbleTail", bm, col, M["bubble"]))
        tail.scale = (0.05, 0.03, 0.12); tail.location = (-0.05, 0.02, H + 0.2)
        tail.rotation_euler = (0, math.radians(160), 0)
        for i, x in enumerate((-0.09, 0, 0.09)):
            d = add(f"BubbleDot{i}", mesh_obj(f"{name}_BubbleDot{i}", sphere_bm(1, 12, 8), col, M["ink"]))
            d.scale = (0.03, 0.015, 0.03); d.location = (x, -0.05, H + 0.33)

    elif kind == "taches":
        loc, n = surf(0.0, 0.86 * H, 0.012)
        for key, m, spin, dx, dz, hidden in (("PostitPink", "postit_p", 0.25, 0.04, 0.05, True),
                                             ("PostitBlue", "postit_b", -0.22, -0.04, 0.03, True),
                                             ("PostitMain", "postit_y", -0.1, 0, 0, False)):
            p = add(key, mesh_obj(f"{name}_{key}", bevel_bm(cube_bm(0.24, 0.012, 0.22), 0.004), col, M[m], smooth=False))
            off = 0.0 if key == "PostitMain" else -0.006
            l2, n2 = surf(dx, 0.86 * H + dz, 0.012 + (0.008 if key == "PostitMain" else 0))
            orient(p, l2, n2, spin)
        for i, (w, dz) in enumerate(((0.15, 0.03), (0.1, -0.01))):
            l3, n3 = surf(-0.02, 0.86 * H + dz, 0.021)
            ln = add(f"PostitLine{i}", mesh_obj(f"{name}_PostitLine{i}", cube_bm(w, 0.003, 0.014), col, M["postit_line"], smooth=False))
            orient(ln, l3, n3, -0.1)

    return root, parts, body_mat, H


VARIANT_PARTS = ["EyeOpen", "EyeShine", "EyeClosed", "EyeWide", "EyePupil", "Brow", "Cheek",
                 "MouthSmile", "MouthFlat", "MouthWorried", "MouthOpen", "Sweat",
                 "FlameMain", "FlameLeft", "FlameRight", "FlameCore", "PostitPink", "PostitBlue"]

STATES = {
    "calm":     dict(scale=1.00, show=["EyeClosed", "Cheek", "MouthSmile"], flames={}),
    "alert":    dict(scale=1.10, show=["EyeOpen", "EyeShine", "MouthFlat"], flames={"FlameMain": 0.55}),
    "stressed": dict(scale=1.25, show=["EyeOpen", "Brow", "MouthWorried", "Sweat"],
                     flames={"FlameMain": 0.85, "FlameLeft": 0.7, "FlameRight": 0.7, "FlameCore": 0.6}),
    "panic":    dict(scale=1.40, show=["EyeWide", "EyePupil", "Brow", "MouthOpen"],
                     flames={"FlameMain": 1.3, "FlameLeft": 1.15, "FlameRight": 1.15, "FlameCore": 1.1}),
}

def apply_state(root, parts, body_mat, state):
    st = STATES[state]
    for key, obj in parts.items():
        base = key.split("_")[0]
        if base in VARIANT_PARTS:
            on = base in st["show"] or (base in st["flames"] and st["flames"][base] > 0)
            if base.startswith("Postit"):
                on = state in ("stressed", "panic") and (base == "PostitBlue" or state == "panic")
            obj.hide_render = obj.hide_viewport = not on
            if base in st["flames"]:
                k = st["flames"][base]
                obj.scale = tuple(c * k for c in obj["base_scale"])
    set_color(body_mat, STRESS_COLORS[state])
    root.scale = (st["scale"],) * 3


# ---------------------------------------------------------------- lights, camera, ground
def setup_render():
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 64
    scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = 1800, 760
    scene.render.film_transparent = False
    world = bpy.data.worlds.new("World"); scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = hexcol("#2B2C30"); bg.inputs[1].default_value = 0.6
    col = scene.collection
    ground = bpy.data.meshes.new("Ground")
    bm = cube_bm(30, 30, 0.02); bm.to_mesh(ground); bm.free()
    g = bpy.data.objects.new("Desk", ground); g.location = (0, 0, -0.01)
    ground.materials.append(mat("Desk", "#6B5442", 0.75)); col.objects.link(g)
    wall = bpy.data.meshes.new("Wall")
    bm = cube_bm(30, 0.02, 12); bm.to_mesh(wall); bm.free()
    w = bpy.data.objects.new("Wall", wall); w.location = (0, 6, 6)
    wall.materials.append(mat("Wall", "#26272B", 0.9)); col.objects.link(w)
    for nm, loc, energy, size, rot in (("Key", (-4, -5, 6), 1500, 4, (math.radians(50), 0, math.radians(-35))),
                                       ("Fill", (5, -4, 3), 500, 4, (math.radians(65), 0, math.radians(50))),
                                       ("Rim", (0, 4, 5), 900, 3, (math.radians(-40), 0, 0))):
        ld = bpy.data.lights.new(nm, 'AREA'); ld.energy = energy; ld.size = size
        lo = bpy.data.objects.new(nm, ld); lo.location = loc; lo.rotation_euler = rot
        col.objects.link(lo)
    cam = bpy.data.cameras.new("Cam"); cam.type = 'ORTHO'; cam.ortho_scale = 7.2
    co = bpy.data.objects.new("Camera", cam); co.location = (0, -12, 2.6)
    co.rotation_euler = (math.radians(82), 0, 0)
    col.objects.link(co); scene.camera = co


setup_render()

# --- collection 1 : les 4 états du facteur (pour le rendu)
col_states = bpy.data.collections.new("Etats_Facteur"); scene.collection.children.link(col_states)
xs = (-2.55, -0.85, 0.85, 2.6)
for state, x in zip(("calm", "alert", "stressed", "panic"), xs):
    root, parts, bmat, H = build_creature(f"Facteur_{state}", "facteur", STRESS_COLORS[state], col_states)
    for o in parts.values():
        o["base_scale"] = list(o.scale)
    apply_state(root, parts, bmat, state)
    root.location = (x, 0, 0)

scene.render.filepath = os.path.join(OUT, "rendu_etats_facteur.png")
bpy.ops.render.render(write_still=True)

# --- collection 2 : la famille (modèles à exporter, état calme par défaut)
col_states.hide_render = True
bpy.context.view_layer.layer_collection.children["Etats_Facteur"].exclude = True
col_family = bpy.data.collections.new("Famille"); scene.collection.children.link(col_family)
family = {}
for (nm, kind, colr), x in zip((("Facteur", "facteur", "#EFE6D8"), ("Reunions", "reunions", "#DCE3E6"),
                                ("Messages", "messages", "#D7E3CF"), ("Taches", "taches", "#E6DCEB")), xs):
    root, parts, bmat, H = build_creature(nm, kind, colr, col_family)
    for o in parts.values():
        o["base_scale"] = list(o.scale)
    family[nm] = (root, parts, bmat)
    st = STATES["calm"]
    for key, o in parts.items():
        base = key.split("_")[0]
        if base in VARIANT_PARTS:
            o.hide_render = o.hide_viewport = base not in st["show"]
    root.location = (x, 0, 0)

scene.render.filepath = os.path.join(OUT, "rendu_famille.png")
bpy.ops.render.render(write_still=True)

# --- export GLB : une créature par fichier, toutes les variantes incluses, taille réelle (~7 cm)
for nm, (root, parts, bmat) in family.items():
    saved = root.location.copy()
    root.location = (0, 0, 0); root.scale = (REAL_SCALE,) * 3
    for o in parts.values():
        o.hide_viewport = o.hide_render = False
        if "base_scale" in o:
            o.scale = o["base_scale"]
    bpy.ops.object.select_all(action='DESELECT')
    for o in [root] + list(parts.values()):
        o.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.gltf(filepath=os.path.join(OUT, f"{nm}.glb"), use_selection=True,
                              export_apply=True, export_format='GLB')
    root.scale = (1, 1, 1); root.location = saved
    st = STATES["calm"]
    for key, o in parts.items():
        base = key.split("_")[0]
        if base in VARIANT_PARTS:
            o.hide_render = o.hide_viewport = base not in st["show"]

bpy.context.view_layer.layer_collection.children["Etats_Facteur"].exclude = False
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "creatures_bureau.blend"))
print("OK ->", OUT)
