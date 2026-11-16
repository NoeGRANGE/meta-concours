"""Synthesise every Fretlings sound proposal (original, no third-party audio).

python3 generate_sounds.py  ->  *.wav (48 kHz mono 16-bit) + propositions.html
"""
import base64, html, os, wave
import numpy as np

SR = 48000
OUT = os.path.dirname(os.path.abspath(__file__))
rng = np.random.default_rng(20261116)


# ---------- building blocks ----------
def T(d): return np.arange(int(d * SR)) / SR

def env(d, a, dec):  # soft attack (raised cosine) then exponential decay
    t = T(d); e = np.exp(-np.maximum(t - a, 0) / dec)
    att = t < a; e[att] = 0.5 - 0.5 * np.cos(np.pi * t[att] / a); return e

def sweep(f0, f1, d, curve=4.0):  # exponential-ish glide, returns phase-integrated sine
    t = T(d); f = f1 + (f0 - f1) * np.exp(-curve * t / d)
    return np.sin(2 * np.pi * np.cumsum(f) / SR)

def tone(f, d, partials=((1, 1),)):
    t = T(d); return sum(a * np.sin(2 * np.pi * f * m * t) for m, a in partials)

def svf(x, fc, q=0.7, mode="bp"):  # Chamberlin state-variable filter, fc may vary per sample
    fc = np.broadcast_to(np.asarray(fc, float), x.shape)
    f = 2 * np.sin(np.pi * np.minimum(fc, SR / 6) / SR); k = 1 / q
    lp = bp = 0.0; y = np.empty_like(x)
    for i in range(len(x)):
        hp = x[i] - lp - k * bp; bp += f[i] * hp; lp += f[i] * bp
        y[i] = bp if mode == "bp" else lp if mode == "lp" else hp
    return y

def noise(d): return rng.standard_normal(int(d * SR))

def place(d, *parts):  # mix (start_time, signal, gain) into a buffer of length d
    y = np.zeros(int(d * SR))
    for s, sig, g in parts:
        i = int(s * SR); n = min(len(sig), len(y) - i); y[i:i + n] += g * sig[:n]
    return y

def fade(y, ms=4):
    n = int(ms * SR / 1000); r = np.linspace(0, 1, n)
    y = y.copy(); y[:n] *= r; y[-n:] *= r[::-1]; return y

def finish(y, peak_db, loop=False):
    y = y - y.mean()
    if not loop: y = fade(y)
    return y / np.abs(y).max() * 10 ** (peak_db / 20)


# ---------- elements reused by several sounds ----------
def boop(f0=330, f1=175, d=0.12, dec=0.035):  # muted vinyl pop
    body = sweep(f0, f1, d, 6) * env(d, 0.002, dec)
    thump = svf(noise(d), 900, 1.0, "lp") * env(d, 0.001, 0.008)
    return body + 0.35 * thump

def chime(f, d=0.5, dec=0.12):  # soft bell, few inharmonic partials
    return tone(f, d, ((1, 1), (2.0, .18), (2.76, .06))) * env(d, 0.004, dec)

def knock(f, d=0.1):  # small wooden/plastic mode pair
    return (tone(f, d) * env(d, .001, .02) + .5 * tone(f * 2.6, d) * env(d, .001, .008))


# ---------- the proposals ----------
def landing_a():  # boop + smaller second bounce
    return place(0.42, (0, boop(), 1), (0.21, boop(380, 210, .1, .025), .42))

def landing_b():  # filtered-noise "pop", very matte
    p = lambda d, fc: svf(noise(d), fc, 2.5) * env(d, .001, .018) + .6 * sweep(260, 160, d, 8) * env(d, .001, .03)
    return place(0.36, (0, p(.1, 700), 1), (0.17, p(.08, 850), .35))

def landing_c():  # tiny plastic knock, three settling bounces
    return place(0.5, (0, knock(240), 1), (0.2, knock(255, .08), .45), (0.32, knock(265, .06), .2))

def breath(d, f0, f1, q=3.0):
    t = T(d); fc = f1 + (f0 - f1) * np.exp(-3 * t / d)
    n = svf(noise(d), fc, q) + 0.4 * svf(noise(d), fc * 2.3, q * 1.5)
    return n

def sigh_env(d, a=.18):
    t = T(d); e = np.where(t < a, np.sin(np.pi / 2 * t / a) ** 2, np.exp(-(t - a) / (d * .32)))
    return e

def sigh_a():  # breathy deflate, formant falls
    d = 1.3; return breath(d, 1500, 450) * sigh_env(d)

def sigh_b():  # toy "pfhuuu": breath + hummed tone sliding down
    d = 1.1; t = T(d)
    f = 240 + 200 * np.exp(-2.5 * t / d) + 4 * np.sin(2 * np.pi * 5 * t)
    hum = np.sin(2 * np.pi * np.cumsum(f) / SR); hum = hum + .25 * np.sin(2 * np.pi * 2 * np.cumsum(f) / SR)
    hum *= np.clip(t / .12, 0, 1) * np.exp(-t / .35)
    return 0.6 * breath(d, 1300, 500) * sigh_env(d) + 0.18 * hum * np.abs(breath(d, 1300, 500)).mean() * 6

def sigh_c():  # sigh_a + tiny sparkle at the end
    s = sigh_a()
    g = np.abs(s).max() * .07
    return place(1.55, (0, s, 1), (0.95, chime(2093, .4, .07), g), (1.03, chime(2637, .4, .07), g * .8),
                 (1.11, chime(3136, .4, .07), g * .6))

def peek_a(): return tone(1250, .07) * env(.07, .006, .015)
def peek_b(): return sweep(620, 940, .09, 3) * env(.09, .01, .022)

def open_a():  # two soft notes up
    return place(0.32, (0, chime(659, .2, .05), 1), (0.07, chime(880, .25, .07), .9))

def close_a():
    return place(0.32, (0, chime(880, .2, .05), .9), (0.07, chime(659, .25, .07), 1))

def whoomp(rise):
    d = .28; t = T(d); u = t / d if rise else 1 - t / d
    fc = 300 + 1400 * u ** 1.5
    e = np.sin(np.pi * np.clip(t / d, 0, 1)) ** 2
    n = svf(noise(d), fc, 1.2, "lp") * e
    s = (sweep(190, 300, d, 2) if rise else sweep(300, 190, d, 2)) * e
    return n / np.abs(n).max() + .5 * s

def open_b(): return whoomp(True)
def close_b(): return whoomp(False)

def tick_a(): return knock(1700, .05) + .3 * svf(noise(.05), 3000, 2) * env(.05, .0005, .003)
def tick_b(): return chime(1568, .35, .07)
def tick_c():  # soft pluck
    return tone(988, .18, ((1, 1), (2, .2))) * env(.18, .002, .035)

def loop(gen, L=3.6, X=0.4):  # render L+X, equal-power crossfade tail onto head
    y = gen(L + X); n = int(X * SR); r = np.linspace(0, np.pi / 2, n)
    out = y[:int(L * SR)].copy(); out[:n] = out[:n] * np.sin(r) + y[-n:] * np.cos(r); return out

def flame_bed(d):  # low soft roar with slow flicker (LFO periods fit the loop length roughly)
    t = T(d); b = svf(noise(d), 380, .8, "lp")
    return b * (1 + .25 * np.sin(2 * np.pi * .83 * t) + .15 * np.sin(2 * np.pi * 1.94 * t + 1))

def crackles(d, rate):
    y = np.zeros(int(d * SR))
    for s in rng.uniform(0, d - .05, rng.poisson(rate * d)):
        i = int(s * SR); k = int(rng.uniform(.002, .012) * SR)
        y[i:i + k] += rng.uniform(.3, 1) * rng.standard_normal(k) * np.exp(-np.arange(k) / (k / 4))
    return svf(y, 2200, 1.0)

def flame_loop_a(): return loop(lambda d: flame_bed(d) / 2 + 0.3 * np.tanh(3 * crackles(d, 7)))  # tanh tames rare loud pops
def flame_loop_b(): return loop(lambda d: flame_bed(d))

def ripple_a():  # airy sweep
    d = .7; t = T(d); fc = 1800 * (3.2 ** (t / d))
    return svf(noise(d), fc, 4) * np.sin(np.pi * t / d) ** 2

def ripple_b():  # quick rising glint arpeggio over faint air
    notes = (1047, 1175, 1319, 1568, 1760, 2093)
    parts = [(i * .045, chime(f, .3, .06), .8 - .08 * i) for i, f in enumerate(notes)]
    air = ripple_a(); air = air / np.abs(air).max() * .4
    return place(.7, (0, air, 1), *parts)

def family_a():  # three creature boops, each a bit higher, + a sparkle
    return place(.7, (0, boop(300, 160), 1), (.13, boop(360, 200), .85), (.25, boop(430, 240), .75),
                 (.38, chime(2093, .3, .06), .12))

def family_b():  # rising C-E-G marimba notes
    m = lambda f: tone(f, .3, ((1, 1), (3.9, .15))) * env(.3, .002, .06)
    return place(.62, (0, m(523), 1), (.1, m(659), .9), (.2, m(784), .85))

def agent_a(): return chime(660, .9, .22)  # single warm ding
def agent_b():  # calm falling two-tone
    return place(.9, (0, chime(784, .5, .14), .85), (.16, chime(659, .7, .2), 1))
def agent_c():  # "lamp on": tiny switch click + warm hum swell
    d = .7; t = T(d)
    hum = tone(330, d, ((1, 1), (2, .3), (3, .08))) * np.sin(np.pi * np.clip(t / d, 0, 1)) ** 1.5
    return place(d, (0, knock(2100, .03), .5), (.02, hum, .8))


# ---------- catalogue (French texts for the page) ----------
C = [
 ("landing", "Atterrissage / rebond", "Une créature retombe sur le bureau après avoir été lâchée, ou à son premier placement.",
  [("a", landing_a, -9, "« Boop » mat de vinyle, puis un second rebond plus petit et plus aigu."),
   ("b", landing_b, -10, "« Pop » de souffle filtré, très mat, presque un toucher de feutre."),
   ("c", landing_c, -10, "Petit choc de jouet en plastique, trois rebonds qui se calment.")],
  "a", "30 %", "Le plus « vinyle » et le plus doux ; ses deux rebonds collent à l'animation (deux rebonds environ). Caler le second rebond sur l'impact réel si possible."),
 ("sigh", "Soupir de soulagement", "La créature repasse au calme après une panique : c'est la récompense.",
  [("a", sigh_a, -9, "Souffle qui se dégonfle, le timbre descend doucement."),
   ("b", sigh_b, -9, "« Pfiouu » de jouet : souffle plus une petite voix fredonnée qui glisse vers le grave."),
   ("c", sigh_c, -9, "Le souffle de A, suivi d'une minuscule étincelle de trois notes.")],
  "c", "45 %", "C'est le moment signature : il mérite d'être entendu. L'étincelle dit « c'est réglé » sans rien d'humain. Si elle paraît trop sucrée, A seul."),
 ("peek", "Bulle d'aperçu", "L'utilisateur regarde une créature 0,4 s : la bulle apparaît.",
  [("a", peek_a, -20, "Blip sinus très court et très bas."),
   ("b", peek_b, -20, "Petite bulle qui monte, un peu plus ronde.")],
  "silence", "0 % (ou B à 10 %)", "Le regard balaie souvent les créatures : un son à chaque fois deviendrait vite un tic. Je laisserais cette bulle muette ; à défaut, B à peine audible."),
 ("panel_open", "Ouverture du panneau", "Pincement en regardant une créature : le panneau de détail s'ouvre.",
  [("a", open_a, -13, "Deux notes douces qui montent (mi, la)."),
   ("b", open_b, -13, "« Whoomp » feutré : souffle qui s'ouvre, avec un léger glissé vers l'aigu.")],
  "b", "25 %", "Plus spatial et moins « interface de téléphone » que les deux notes ; il se fond dans le passthrough."),
 ("panel_close", "Fermeture du panneau", "Le panneau se ferme (regard ailleurs 3 s ou fermeture).",
  [("a", close_a, -15, "Miroir de l'ouverture A : deux notes qui descendent."),
   ("b", close_b, -15, "Miroir de l'ouverture B : le souffle se referme.")],
  "b", "15 %", "Garder la paire assortie à l'ouverture, plus basse. Comme la fermeture est souvent automatique, on peut aussi la laisser muette."),
 ("tick", "Élément lu / fait", "Pincer une ligne du panneau marque l'élément lu ou fait (jusqu'à 5 fois de suite).",
  [("a", tick_a, -14, "Petit « tic » sec de bois clair."),
   ("b", tick_b, -14, "Minuscule clochette, une seule note."),
   ("c", tick_c, -14, "Pincement de corde doux, rond.")],
  "c", "30 %", "Assez satisfaisant sans lasser à la cinquième fois. Varier la hauteur de ±1 demi-ton à chaque coup (pitch aléatoire dans l'AudioSource) évite l'effet mitraillette."),
 ("flame_loop", "Crépitement des flammes (boucle)", "En boucle tant que le Facteur est en panique et que ses flammes sont visibles.",
  [("a", flame_loop_a, -20, "Fond de feu sourd avec quelques crépitements épars."),
   ("b", flame_loop_b, -22, "Fond de souffle seul, qui ondule, sans crépitement.")],
  "b", "8 à 10 %, en son spatialisé, fondu d'entrée 1 s", "Un crépitement en boucle pendant qu'on travaille finit par agacer. B ne s'entend que si on approche la tête : il donne de la présence aux flammes sans réclamer d'attention. Option honnête : pas de son du tout, l'image suffit."),
 ("ripple", "Onde de calibration", "La main posée à plat est détectée : l'onde lumineuse parcourt le bureau.",
  [("a", ripple_a, -16, "Souffle aérien qui balaie vers l'aigu."),
   ("b", ripple_b, -16, "Arpège rapide de petites étincelles sur le même souffle.")],
  "b", "35 %", "Ce moment n'arrive qu'à la calibration : il peut être un peu plus présent et confirmer « bureau détecté ». L'arpège suit bien la lumière qui s'étend."),
 ("family", "Arrivée de la famille", "Fin de l'onboarding : les trois autres créatures tombent sur le bureau.",
  [("a", family_a, -9, "Trois « boop » d'atterrissage de plus en plus aigus, et une étincelle."),
   ("b", family_b, -10, "Trois notes de marimba qui montent (do, mi, sol).")],
  "a", "35 %", "Réutilise le langage sonore de l'atterrissage : on comprend que ce sont des créatures qui arrivent. Idéalement, jouer un atterrissage par créature au moment réel de chaque impact, ce qui revient au même."),
 ("agent", "Session d'agent en attente", "La lampe frontale de la créature des agents s'allume : une session attend l'utilisateur.",
  [("a", agent_a, -14, "Un seul « ding » chaud, qui s'éteint lentement."),
   ("b", agent_b, -14, "Deux notes qui descendent (sol, mi) : calme, « quand tu veux »."),
   ("c", agent_c, -16, "Petit déclic d'interrupteur puis un bourdonnement chaud qui gonfle : la lampe s'allume.")],
  "c", "25 %", "Ne ressemble pas à une notification de téléphone ; il décrit ce qu'on voit (la lampe). Les deux notes de B sont plus claires mais plus proches d'une alerte : à garder si C passe inaperçu."),
]


def write_wav(path, y):
    pcm = np.round(np.clip(y, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
    return pcm


def main():
    rows, data = [], {}
    for key, *_ , variants, _rec, _vol, _why in C:
        for v, gen, db, _ in variants:
            is_loop = "loop" in key
            y = finish(gen(), db, is_loop)
            name = f"{key}_{v}.wav"; pcm = write_wav(os.path.join(OUT, name), y)
            peak = 20 * np.log10(np.abs(pcm).max() / 32767)
            # self-check: level, clipping, DC, loop seam
            assert abs(peak - db) < 0.1 and np.abs(pcm).max() < 32767, name
            assert abs(pcm.mean()) / 32767 < 1e-3, name
            if is_loop:
                step = np.abs(np.diff(pcm.astype(float)))
                assert abs(float(pcm[-1]) - pcm[0]) < 4 * step.mean() + 1, name
            else:
                assert abs(int(pcm[0])) < 50 and abs(int(pcm[-1])) < 50, name
            rows.append((name, len(pcm) / SR, peak, np.sqrt(np.mean((pcm / 32767.) ** 2))))
            with open(os.path.join(OUT, name), "rb") as f: data[name] = base64.b64encode(f.read()).decode()
    print(f"{'file':22} {'dur s':>6} {'peak dBFS':>10} {'rms dBFS':>9}")
    for n, d, p, r in rows: print(f"{n:22} {d:6.2f} {p:10.1f} {20*np.log10(r):9.1f}")
    write_html(data)


def write_html(data):
    e = html.escape; secs = []
    for i, (key, title, when, variants, rec, vol, why) in enumerate(C, 1):
        items = []
        for v, _g, _db, desc in variants:
            n = f"{key}_{v}.wav"; star = " rec" if v == rec else ""
            loopbox = f'<label class="lp"><input type="checkbox" checked onchange="this.closest(\'.var\').querySelector(\'audio\').loop=this.checked"> en boucle</label>' if "loop" in key else ""
            items.append(f'<div class="var{star}"><button onclick="play(this)" aria-label="Écouter {e(title)} {v.upper()}">▶ {v.upper()}</button>'
                         f'<audio preload="auto" {"loop" if "loop" in key else ""} src="data:audio/wav;base64,{data[n]}"></audio>'
                         f'<p>{e(desc)}<span class="fn">{n}</span></p>{loopbox}</div>')
        pick = "Silence" if rec == "silence" else f"Variante {rec.upper()}"
        secs.append(f'<section><h2><span class="num">{i}</span>{e(title)}</h2><p class="when">{e(when)}</p>{"".join(items)}'
                    f'<div class="recbox"><b>Recommandation : {pick}</b> · volume {e(vol)}<br>{e(why)}</div></section>')
    page = f"""<!doctype html><html lang="fr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Sons Fretlings</title><style>
:root{{--bg:#FBF6EE;--ink:#4A3B33;--soft:#9A897C;--peach:#F2D3AE;--orange:#F1A661;--vinyl:#EFE6D8}}
*{{box-sizing:border-box}}body{{margin:0;background:var(--bg);color:var(--ink);font:16px/1.5 -apple-system,BlinkMacSystemFont,"Segoe UI",Roboto,sans-serif}}
main{{max-width:760px;margin:0 auto;padding:32px 16px 64px}}h1{{font-size:28px;margin:0 0 8px}}
.intro{{background:var(--vinyl);border-radius:16px;padding:16px 20px;margin:16px 0 32px}}.intro p{{margin:.4em 0}}
section{{background:#fff8;border:1px solid var(--vinyl);border-radius:16px;padding:16px 20px;margin:0 0 20px}}
h2{{font-size:20px;margin:0 0 4px;display:flex;align-items:center;gap:10px}}.num{{background:var(--peach);border-radius:50%;width:28px;height:28px;display:inline-grid;place-items:center;font-size:14px}}
.when{{color:var(--soft);margin:0 0 12px}}.var{{display:flex;gap:12px;align-items:flex-start;padding:8px;border-radius:12px;flex-wrap:wrap}}
.var.rec{{background:var(--peach)}}.var p{{margin:4px 0;flex:1;min-width:200px}}.fn{{display:block;color:var(--soft);font-size:12px}}
button{{font:inherit;font-weight:600;border:0;border-radius:999px;padding:6px 14px;background:var(--orange);color:var(--ink);cursor:pointer;min-width:64px}}
button.on{{background:var(--ink);color:var(--bg)}}button:focus-visible{{outline:3px solid var(--ink);outline-offset:2px}}
.lp{{font-size:14px;color:var(--soft);padding-top:6px}}.recbox{{margin-top:10px;border-left:4px solid var(--orange);padding:6px 12px}}
</style></head><body><main><h1>Sons des Fretlings : propositions</h1>
<div class="intro"><p><b>Origine.</b> Tous ces sons sont synthétisés par nous, en Python, sans aucun échantillon ni son tiers (<code>generate_sounds.py</code> les régénère à l'identique). Ils sont donc originaux et utilisables commercialement sans question de licence.</p>
<p><b>Recommandation générale.</b> Garder peu de sons, et seulement aux moments que l'utilisateur provoque ou qui récompensent : soupir, atterrissage, élément lu, ouverture du panneau, calibration, arrivée de la famille, lampe de l'agent. Laisser <b>muette la bulle d'aperçu</b> (le regard la déclenche trop souvent), et garder les <b>flammes</b> soit muettes, soit à peine audibles en son spatialisé. Le soupir est le seul son qu'on veut vraiment entendre. Les volumes proposés sont relatifs au volume général de l'app ; à valider au casque, dans une pièce calme.</p>
<p>La variante surlignée est celle que je recommande.</p></div>
{"".join(secs)}</main><script>
function play(b){{const a=b.parentNode.querySelector('audio');document.querySelectorAll('audio').forEach(x=>{{if(x!==a){{x.pause();x.previousElementSibling.classList.remove('on')}}}});
if(!a.paused){{a.pause();b.classList.remove('on');return}}a.currentTime=0;a.play();b.classList.add('on');a.onended=()=>b.classList.remove('on')}}
</script></body></html>"""
    with open(os.path.join(OUT, "propositions.html"), "w", encoding="utf-8") as f: f.write(page)


if __name__ == "__main__":
    main()
