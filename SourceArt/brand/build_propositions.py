"""Builds propositions.html: logo, colour and video art-direction proposals for Fretlings.

python3 SourceArt/brand/build_propositions.py  (from the repository root)
The page is self-contained except for Google Fonts.
"""
import base64
import pathlib

HERE = pathlib.Path(__file__).parent
ASSETS = HERE / "_assets"


def img(name):
    path = ASSETS / name
    mime = "image/png" if name.endswith(".png") else "image/jpeg"
    return f"data:{mime};base64,{base64.b64encode(path.read_bytes()).decode()}"


MAIL, CAL, MSG, TASKS, AGENTS = (img(f"crop_{n}.png") for n in ("mail", "calendar", "messages", "tasks", "agents"))
F_AGENTS, F_FAMILY, F_PANEL = img("frame_agents.jpg"), img("frame_family.jpg"), img("frame_panel.jpg")

FLAME = """<svg class="flame" viewBox="0 0 20 26"><defs><linearGradient id="fg{id}" x1="0" y1="1" x2="0" y2="0">
<stop offset="0" stop-color="#E2553A"/><stop offset="1" stop-color="#F1A661"/></linearGradient></defs>
<path d="M10 0C13 6 18 10 18 16a8 8 0 0 1-16 0C2 10 7 6 10 0Z" fill="url(#fg{id})"/></svg>"""


def wordmark_b(size, color, uid):
    return (f'<span class="wm-b" style="font-size:{size}px;color:{color}">Fretl'
            f'<span class="i">ı{FLAME.format(id=uid)}</span>ngs</span>')


def wordmark_c(size, color):
    dots = "".join(f'<i style="background:{c}"></i>' for c in ("#5B7FB8", "#7E9AA8", "#8DAA7C", "#A58BB5", "#E0B03A"))
    return f'<span class="wm-c" style="font-size:{size}px;color:{color}">Fretlings<span class="beads">{dots}</span></span>'


def logo_a(bg, color):
    return (f'<div class="logo-a" style="background:{bg}"><img src="{MAIL}"><span style="color:{color}">Fretlings</span></div>')


page = f"""<!doctype html><html lang="fr"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Fretlings — identité</title>
<link href="https://fonts.googleapis.com/css2?family=Fredoka:wght@500;600;700&family=Nunito:wght@700;800;900&family=Lato:wght@400;700&display=swap" rel="stylesheet">
<style>
:root{{--card:#FBF6EE;--ink:#4A3B33;--muted:#9A897C;--peach:#F2D3AE;--orange:#F1A661;--red:#E2553A;--night:#2E2928;--night2:#3A3436}}
*{{box-sizing:border-box}} body{{margin:0;background:#F4EDE2;color:var(--ink);font-family:Lato,system-ui,sans-serif;line-height:1.5}}
main{{max-width:1120px;margin:0 auto;padding:32px 16px 80px}}
h1{{font-family:Fredoka;font-weight:700;font-size:44px;margin:0}} h2{{font-family:Fredoka;font-weight:600;font-size:28px;margin:56px 0 8px}}
h3{{font-family:Fredoka;font-weight:600;font-size:20px;margin:0 0 6px}} p.lead{{font-size:18px;color:var(--muted);margin:4px 0 0}}
.grid{{display:grid;gap:16px}} .g2{{grid-template-columns:repeat(auto-fit,minmax(320px,1fr))}} .g3{{grid-template-columns:repeat(auto-fit,minmax(260px,1fr))}}
.card{{background:var(--card);border-radius:24px;padding:20px;box-shadow:0 2px 0 #E6DCCD}}
.reco{{outline:3px solid var(--orange)}} .tag{{display:inline-block;background:var(--orange);color:#fff;font-weight:700;font-size:13px;border-radius:99px;padding:2px 10px;margin-left:8px;vertical-align:middle}}
.pair{{display:grid;grid-template-columns:1fr;gap:10px;margin:12px 0}}
.tile{{border-radius:18px;height:130px;display:flex;align-items:center;justify-content:center;overflow:hidden;white-space:nowrap}}
.light{{background:#FBF6EE}} .dark{{background:var(--night)}}
.note{{font-size:15px;color:#6E5F55}} .note b{{color:var(--ink)}}
.logo-a{{display:flex;align-items:center;gap:10px;padding:6px 14px;border-radius:16px}}
.logo-a img{{width:64px;height:56px;object-fit:cover;object-position:50% 30%;border-radius:50%;clip-path:circle(46%)}}
.logo-a span{{font-family:Fredoka;font-weight:600;font-size:40px;letter-spacing:.5px}}
.wm-b{{font-family:Fredoka;font-weight:600;letter-spacing:.5px;position:relative}}
.wm-b .i{{position:relative;display:inline-block}} .wm-b .flame{{position:absolute;width:.32em;left:50%;transform:translateX(-50%);top:-.12em}}
.wm-c{{font-family:Nunito;font-weight:900;position:relative;display:inline-block;padding-bottom:.35em}}
.wm-c .beads{{position:absolute;left:0;right:0;bottom:0;display:flex;justify-content:center;gap:.18em}}
.wm-c .beads i{{width:.2em;height:.2em;border-radius:50%;display:block}}
.icon{{width:110px;height:110px;border-radius:30px;background:linear-gradient(160deg,#F7EFE3,#EADFCF);position:relative;overflow:hidden;box-shadow:inset 0 -6px 0 rgba(0,0,0,.05)}}
.icon img{{position:absolute;width:118%;left:-9%;bottom:-16%}} .icon .flame{{position:absolute;width:22px;right:20px;top:14px}}
.sw{{display:flex;gap:8px;margin:10px 0}} .sw div{{flex:1;height:64px;border-radius:14px;display:flex;align-items:flex-end;padding:6px;font-size:11px;font-weight:700}}
.frame{{position:relative;aspect-ratio:16/9;border-radius:18px;overflow:hidden;background:#000}}
.frame>img.bg{{width:100%;height:100%;object-fit:cover;display:block}}
.cap{{position:absolute;left:4%;bottom:7%;background:rgba(251,246,238,.95);border-radius:14px;padding:6px 12px 7px 9px;display:flex;gap:8px;align-items:center;max-width:58%}}
.cap .pill{{width:5px;align-self:stretch;border-radius:5px}} .cap b{{font-family:Fredoka;font-weight:600;font-size:15px;display:block;line-height:1.1}} .cap span{{font-size:11px;color:var(--muted);display:block;line-height:1.3}}
.title-card{{position:absolute;inset:0;background:radial-gradient(120% 90% at 50% 100%,#4A403C,var(--night) 70%);display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px}}
.title-card .row{{display:flex;gap:6px;align-items:flex-end}} .title-card .row img{{width:48px;height:42px;object-fit:cover;object-position:50% 40%;border-radius:12px}}
.title-card .tagline{{color:#E9DFD2;font-size:15px}}
.sticker{{position:absolute;right:5%;top:8%;width:18%;border-radius:50%;overflow:hidden;border:5px solid var(--card);box-shadow:0 6px 20px rgba(0,0,0,.35);transform:rotate(-6deg)}}
.sticker img{{width:100%;display:block}}
ul{{margin:8px 0 0;padding-left:20px}} li{{margin:4px 0}}
table{{width:100%;border-collapse:collapse;font-size:15px}} td,th{{text-align:left;padding:8px;border-bottom:1px solid #E6DCCD;vertical-align:top}}
@media (max-width:600px){{h1{{font-size:34px}} .pair{{grid-template-columns:1fr}}}}
</style></head><body><main>
<h1>Fretlings — identité visuelle</h1>
<p class="lead">Propositions de logo, de couleurs et de direction artistique pour la vidéo. Rien n'est encore produit : choisis, je décline ensuite (logo final, icône, couverture, cartons et légendes de la vidéo).</p>
<p class="note"><b>Fil conducteur</b> : un jouet de designer posé sur un vrai bureau. Doux, mat, chaleureux, un peu drôle ; l'urgence en orange-rouge et nulle part ailleurs. On reprend exactement l'univers de l'app (cartes crème arrondies, encre cacao, pastels des créatures), pour que la vidéo, le logo et l'app ne fassent qu'un.</p>

<h2>1. Logo</h2>
<div class="grid g2">
 <div class="card reco"><h3>A. Le Facteur + le nom <span class="tag">ma reco</span></h3>
  <div class="pair"><div class="tile light">{logo_a("transparent", "#4A3B33")}</div><div class="tile dark">{logo_a("transparent", "#FBF6EE")}</div></div>
  <p class="note">La mascotte réelle (rendu 3D) dans une pastille, puis le nom en <b>Fredoka</b>, arrondie comme les créatures. Immédiatement reconnaissable, chaleureux, et c'est <b>notre propre personnage</b>, ce qui sert « Boldest Original Concept ». Version finale avec un rendu détouré et une petite flamme.</p></div>
 <div class="card"><h3>B. Le « i » qui s'inquiète</h3>
  <div class="pair"><div class="tile light">{wordmark_b(46, "#4A3B33", "b1")}</div><div class="tile dark">{wordmark_b(46, "#FBF6EE", "b2")}</div></div>
  <p class="note">Logo typographique : le point du « i » devient une petite flamme. Très lisible en petit, élégant, raconte l'idée (ça chauffe). Moins « personnage ».</p></div>
 <div class="card"><h3>C. La famille en perles</h3>
  <div class="pair"><div class="tile light">{wordmark_c(46, "#4A3B33")}</div><div class="tile dark">{wordmark_c(46, "#FBF6EE")}</div></div>
  <p class="note">Le nom en <b>Nunito</b> très gras, soutenu par cinq perles aux couleurs des cinq créatures (comme les billes d'Agents). Joue sur la famille ; un peu plus abstrait.</p></div>
 <div class="card"><h3>D. L'icône de l'app (avec A, B ou C)</h3>
  <div class="pair"><div class="tile light"><div class="icon"><img src="{MAIL}">{FLAME.format(id="d1")}</div></div><div class="tile dark"><div class="icon"><img src="{MAIL}">{FLAME.format(id="d2")}</div></div></div>
  <p class="note">Le Facteur qui dépasse du bas d'un carré crème, avec sa petite flamme : lisible à 64 px dans la bibliothèque du casque. Sert d'icône et d'avatar Devpost, quel que soit le logo choisi.</p></div>
</div>

<h2>2. Couleurs</h2>
<div class="grid g3">
 <div class="card reco"><h3>1. Crème &amp; cacao <span class="tag">ma reco</span></h3>
  <div class="sw"><div style="background:#FBF6EE;color:#4A3B33">#FBF6EE</div><div style="background:#4A3B33;color:#FBF6EE">#4A3B33</div><div style="background:#F2D3AE;color:#4A3B33">#F2D3AE</div><div style="background:#F1A661;color:#fff">#F1A661</div></div>
  <p class="note">La palette de l'app. Légendes et cartes de la vidéo identiques aux bulles du casque : continuité parfaite entre l'app et la vidéo.</p></div>
 <div class="card reco"><h3>2. Nuit chaude, pour les cartons <span class="tag">avec 1</span></h3>
  <div class="sw"><div style="background:#2E2928;color:#FBF6EE">#2E2928</div><div style="background:#3A3436;color:#FBF6EE">#3A3436</div><div style="background:#FBF6EE;color:#4A3B33">#FBF6EE</div><div style="background:#E2553A;color:#fff">#E2553A</div></div>
  <p class="note">Fond anthracite chaud (celui des rendus Blender) pour l'ouverture et la fin : les créatures crème ressortent, et ça tranche avec les plans en passthrough.</p></div>
 <div class="card"><h3>3. Pastels de la famille, en accents</h3>
  <div class="sw"><div style="background:#5B7FB8;color:#fff">Mail</div><div style="background:#7E9AA8;color:#fff">Cal.</div><div style="background:#8DAA7C;color:#fff">Msg</div><div style="background:#A58BB5;color:#fff">Tasks</div><div style="background:#E0B03A;color:#fff">Agents</div></div>
  <p class="note">Une couleur par créature, déjà utilisée par la pastille du panneau : on la reprend sur la légende quand on présente chaque créature.</p></div>
</div>
<p class="note"><b>Typo</b> : <b>Fredoka</b> pour les titres et le logo (ronde, joyeuse, licence OFL : utilisable commercialement), <b>Lato</b> pour les textes (déjà dans l'app).</p>

<h2>3. Direction artistique de la vidéo</h2>
<div class="grid g2">
 <div class="card"><h3>Ouverture et fin : carton « nuit chaude »</h3>
  <div class="frame"><div class="title-card">
   <div class="row"><img src="{MAIL}"><img src="{CAL}"><img src="{MSG}"><img src="{TASKS}"><img src="{AGENTS}"></div>
   <div style="font-family:Fredoka;font-weight:600;font-size:44px;color:#FBF6EE">Fretlings</div>
   <div class="tagline">Tiny creatures that panic so you don't have to.</div></div></div>
  <p class="note">Les créatures tombent une à une et rebondissent (comme dans l'app), puis le nom apparaît avec le même petit rebond que les bulles. 2–3 s maximum.</p></div>
 <div class="card"><h3>Légendes sur les plans du casque</h3>
  <div class="frame"><img class="bg" src="{F_AGENTS}"><div class="cap"><div class="pill" style="background:#E0B03A"></div><div><b>Agents</b><span>worries when your coding sessions need you</span></div></div></div>
  <p class="note">La carte crème arrondie de l'app, avec la pastille de couleur de la créature. Toujours en bas à gauche, jamais sur la créature. Entre et sort en 0,25 s avec un léger rebond.</p></div>
 <div class="card"><h3>Présentation d'une créature : vignette</h3>
  <div class="frame"><img class="bg" src="{F_FAMILY}"><div class="sticker"><img src="{CAL}"></div><div class="cap"><div class="pill" style="background:#7E9AA8"></div><div><b>Calendar</b><span>gets nervous as your next meeting comes closer</span></div></div></div>
  <p class="note">Le rendu 3D de la créature surgit en vignette (comme un autocollant) quand on la présente : il relie la vidéo au logo et rend chaque personnage mémorable.</p></div>
 <div class="card"><h3>Les gestes, en surimpression</h3>
  <div class="frame"><img class="bg" src="{F_PANEL}"><div class="cap"><div class="pill" style="background:#F1A661"></div><div><b>Look, then pinch</b><span>no controllers, ever</span></div></div></div>
  <p class="note">Quand on montre un geste, une courte consigne (3–4 mots) dans la même carte, avec la couleur d'accent orange.</p></div>
</div>
<div class="card" style="margin-top:16px"><h3>Règles de montage</h3>
<table>
<tr><th>Rythme</th><td>Un plan toutes les 2 à 4 s ; jamais plus de 6 s sans changement. Coupes sur le temps de la musique.</td></tr>
<tr><th>Mouvement</th><td>Rebonds doux et courts (0,25 s), comme dans l'app. Pas de transitions tape-à-l'œil : coupe franche ou fondu court (0,2 s).</td></tr>
<tr><th>Texte</th><td>Phrases de 3 à 8 mots, en anglais. Une seule information à l'écran à la fois. Sous-titres de la voix off en bas au centre, en Lato blanc sur bandeau cacao translucide.</td></tr>
<tr><th>Musique</th><td>Douce et rebondissante (pizzicati, marimba, piano léger), 95–110 bpm, sous la voix (−18 dB) ; les sons de l'app restent audibles par-dessus.</td></tr>
<tr><th>À éviter</th><td>Menus du Quest, logos et marques, mouvements de tête rapides, cartes plein écran sur les créatures.</td></tr>
</table></div>
</main></body></html>"""

(HERE / "propositions.html").write_text(page, encoding="utf-8")
print("wrote", HERE / "propositions.html")
