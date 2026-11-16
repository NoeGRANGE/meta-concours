# Renders transparent PNG stickers from a copy of creatures_bureau.blend.
# blender -b src.blend -P stickers.py -- <out_dir>
import bpy, math, os, sys
from mathutils import Vector, Euler

OUT = sys.argv[sys.argv.index("--") + 1]
os.makedirs(OUT, exist_ok=True)
sc = bpy.context.scene
vl = bpy.context.view_layer
for c in ("Etats_Facteur", "Etats_Agents"):
    vl.layer_collection.children[c].exclude = True
vl.layer_collection.children["Famille"].exclude = False
for n in ("Desk", "Wall"):
    bpy.data.objects[n].hide_render = True

def hexcol(h):
    s = [int(h.lstrip("#")[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return (*[c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in s], 1)

# render settings
sc.render.engine = 'CYCLES'
sc.cycles.samples = 256
sc.cycles.use_denoising = True
sc.render.film_transparent = True
sc.render.image_settings.file_format = 'PNG'
sc.render.image_settings.color_mode = 'RGBA'
sc.render.image_settings.color_depth = '8'
sc.render.filter_size = 1.5
# soft warm ambient replaces the bounce the desk and wall used to give
bg = sc.world.node_tree.nodes["Background"]
bg.inputs[0].default_value = hexcol("#B8A898"); bg.inputs[1].default_value = 0.35

# shadow catcher floor (invisible, keeps the contact shadow)
bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, 0))
floor = bpy.context.active_object; floor.name = "ShadowCatcher"
floor.is_shadow_catcher = True
# studio lights only light the creatures: the catcher keeps just the soft ambient contact shadow
for n in ("Key", "Fill", "Rim"):
    bpy.data.objects[n].light_linking.receiver_collection = bpy.data.collections["Famille"]

STRESS_PANIC = "#E2553A"
REST = {"Facteur": "#EFE6D8", "Reunions": "#DCE3E6", "Messages": "#D7E3CF", "Taches": "#E6DCEB", "Agents": "#D9E4EC"}
NAMES = {"Facteur": "mail", "Reunions": "calendar", "Messages": "messages", "Taches": "tasks", "Agents": "agents"}
VARIANT = ["EyeOpen", "EyeShine", "EyeClosed", "EyeWide", "EyePupil", "Brow", "Cheek",
           "MouthSmile", "MouthFlat", "MouthWorried", "MouthOpen", "Sweat",
           "FlameMain", "FlameLeft", "FlameRight", "FlameCore", "PostitPink", "PostitBlue"]
# same as build_creatures.STATES; Sweat added to panic because the brief asks for it
STATES = {
    "calm": dict(scale=1.0, show=["EyeClosed", "Cheek", "MouthSmile"], flames={}),
    "panic": dict(scale=1.4, show=["EyeWide", "EyePupil", "Brow", "MouthOpen", "Sweat", "PostitPink", "PostitBlue"],
                  flames={"FlameMain": 1.3, "FlameLeft": 1.15, "FlameRight": 1.15, "FlameCore": 1.1}),
}
SERIES = {
    "calm": dict(scale=1.0, show=["EyeClosed", "Cheek", "MouthSmile"], flames={}, color="#EFE6D8"),
    "alert": dict(scale=1.10, show=["EyeOpen", "EyeShine", "MouthFlat"], flames={"FlameMain": 0.55}, color="#F2D3AE"),
    "stressed": dict(scale=1.25, show=["EyeOpen", "Brow", "MouthWorried", "Sweat"],
                     flames={"FlameMain": 0.85, "FlameLeft": 0.70, "FlameRight": 0.70, "FlameCore": 0.60}, color="#F1A661"),
    "panic": dict(scale=1.40, show=["EyeWide", "EyePupil", "Brow", "MouthOpen"],
                  flames={"FlameMain": 1.30, "FlameLeft": 1.15, "FlameRight": 1.15, "FlameCore": 1.10}, color="#E2553A"),
}
roots = {n: bpy.data.objects[n] for n in REST}
home = {n: r.location.copy() for n, r in roots.items()}

def apply_state(name, state, visible, table=STATES):
    r, st = roots[name], table[state]
    for o in r.children:
        base = o.name[len(name) + 1:].split("_")[0]
        on = visible
        if base in VARIANT:
            on = visible and (base in st["show"] or base in st["flames"])
            if base in st["flames"]:
                o.scale = [c * st["flames"][base] for c in o["base_scale"]]
        o.hide_render = o.hide_viewport = not on  # hidden-in-viewport parts are not evaluated, framing needs them current
    bm = bpy.data.materials[f"Body_{name}"]
    col = hexcol(st.get("color") or (STRESS_PANIC if state == "panic" else REST[name]))
    p = bm.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = col
    p.inputs["Emission Color"].default_value = col
    p.inputs["Emission Strength"].default_value = 0.12 if (state == "panic" and table is SERIES) else 0.0
    r.scale = (st["scale"],) * 3

cam = sc.camera
cam.data.type = 'ORTHO'

def frame(names, tilt, yaw, res, margin=0.08):
    sc.render.resolution_x, sc.render.resolution_y = res
    cam.rotation_euler = Euler((math.radians(90 - tilt), 0, math.radians(yaw)))
    bpy.context.view_layer.update()
    R = cam.rotation_euler.to_matrix(); Ri = R.inverted()
    dg = bpy.context.evaluated_depsgraph_get()
    pts = []
    for n in names:
        for o in roots[n].children:
            if o.hide_render or o.type != 'MESH':
                continue
            ev = o.evaluated_get(dg); me = ev.to_mesh()
            mw = ev.matrix_world
            pts += [Ri @ (mw @ v.co) for v in me.vertices]
            ev.to_mesh_clear()
    lo = Vector([min(p[i] for p in pts) for i in range(3)])
    hi = Vector([max(p[i] for p in pts) for i in range(3)])
    w, h = hi.x - lo.x, hi.y - lo.y
    aspect = res[0] / res[1]
    # content fills (1 - 2*margin) of the tighter dimension
    cam.data.ortho_scale = max(w, h * aspect) / (1 - 2 * margin)
    cam.location = R @ Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, hi.z + 20))
    cam.data.clip_end = 100

def render(path):
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("[stickers] wrote", path)

if sys.argv[-1] == "states":
    # mail in its 4 states, one shared camera framed on panic so the size steps show
    for m in REST:
        apply_state(m, "calm", False); roots[m].location = home[m]
    roots["Facteur"].location = (0, 0, 0)
    apply_state("Facteur", "panic", True, SERIES)
    frame(["Facteur"], 17, 15, (1200, 1200), margin=0.06)
    for i, st in enumerate(SERIES):
        apply_state("Facteur", st, True, SERIES)
        render(os.path.join(OUT, f"mail_{i}_{st}.png"))
    sys.exit(0)

only = sys.argv[sys.argv.index("--") + 2:] if len(sys.argv) > sys.argv.index("--") + 2 else None
for n in REST:
    for state in ("calm", "panic"):
        tag = f"{NAMES[n]}_{state}"
        if only and tag not in only:
            continue
        for m in REST:
            apply_state(m, state if m == n else "calm", m == n)
            roots[m].location = home[m]
        roots[n].location = (0, 0, 0)
        frame([n], 17, 15, (1024, 1024))
        render(os.path.join(OUT, tag + ".png"))

if not only or "family_calm" in only:
    for m in REST:
        apply_state(m, "calm", True); roots[m].location = home[m]
    frame(list(REST), 17, 0, (2400, 800))
    render(os.path.join(OUT, "family_calm.png"))
