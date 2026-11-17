"""Edits the Fretlings presentation video from the headset recordings (python3 SourceArt/video/build_video.py).

Graphics (cards, captions, stickers) are HTML pages rendered to transparent PNGs by headless Chrome, in the app's
style (STYLE.md); clips are cut and assembled with ffmpeg. Every change is an edit of the EDIT list below, then a
re-run. Recordings are read from Captures/ (not in git); the output goes to Captures/edit/.
"""
import base64
import pathlib
import subprocess
import tempfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
CAPTURES = ROOT / "Captures"
OUT = CAPTURES / "edit"
BRAND = ROOT / "SourceArt" / "brand"
CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
W, H, FPS = 1920, 1080, 30

MUSIC = ROOT / "SourceArt" / "music" / "music_a.wav"           # chosen variant (marimba and pizzicato)
MUSIC_VOLUME = 0.16          # very light: a bed under the app's own sounds and, later, the voice
MUSIC_ENDING = (128.0, 140.0)  # the hook's last statement and the final chord, kept for the video's last seconds

MAIN = CAPTURES / "com.fretlings.app-20261116-232346-0.mp4"     # onboarding, family, calm desk
RELAUNCH = CAPTURES / "com.oculus.vrshell-20261117-001818-0.mp4"  # app relaunched: creatures come back

ACCENT = {"mail": "#5B7FB8", "calendar": "#7E9AA8", "tasks": "#A58BB5", "messages": "#8DAA7C", "agents": "#E0B03A",
          "gesture": "#F1A661"}


def b64(path):
    return base64.b64encode(path.read_bytes()).decode()


FONTS = ('<link href="https://fonts.googleapis.com/css2?family=Fredoka:wght@600&family=Lato:wght@400;700'
         '&display=block" rel="stylesheet">')
BASE_CSS = """html,body{margin:0;width:1920px;height:1080px;background:transparent;overflow:hidden}
.card{position:absolute;left:80px;bottom:80px;background:rgba(251,246,238,.96);border-radius:34px;
 padding:22px 40px 26px 26px;display:flex;gap:22px;align-items:center;box-shadow:0 10px 40px rgba(0,0,0,.25)}
.pill{width:12px;align-self:stretch;border-radius:12px}
.card b{font-family:Fredoka;font-weight:600;font-size:60px;color:#4A3B33;display:block;line-height:1.05}
.card span{font-family:Lato;font-size:34px;color:#8A796D;display:block;margin-top:4px}
.sticker{position:absolute;right:90px;top:80px;width:300px;height:300px;border-radius:50%;background:#F2D3AE;
 border:10px solid #FBF6EE;box-shadow:0 12px 40px rgba(0,0,0,.35);overflow:hidden;transform:rotate(-5deg)}
.sticker img{position:absolute;width:92%;left:4%;top:6%}
.night{position:absolute;inset:0;background:radial-gradient(120% 95% at 50% 100%,#4A403C,#2E2928 70%);
 display:flex;flex-direction:column;align-items:center;justify-content:center;gap:28px}
.tagline{font-family:Lato;font-size:40px;color:#E9DFD2}
.small{font-family:Lato;font-size:28px;color:#B8AA9C;letter-spacing:1px}"""


def render(name, body):
    target = OUT / "gfx" / f"{name}.png"
    html = f"<!doctype html><html><head><meta charset='utf-8'>{FONTS}<style>{BASE_CSS}</style></head><body>{body}</body></html>"
    with tempfile.NamedTemporaryFile("w", suffix=".html", delete=False) as page:
        page.write(html)
    subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--default-background-color=00000000",
                    f"--window-size={W},{H}", "--virtual-time-budget=6000", f"--screenshot={target}",
                    f"file://{page.name}"], check=True, capture_output=True)
    return target


def caption(name, title, line, accent, sticker=None):
    art = ""
    if sticker:
        art = f"<div class='sticker'><img src='data:image/png;base64,{b64(BRAND / 'stickers' / sticker)}'></div>"
    return render(name, f"{art}<div class='card'><div class='pill' style='background:{accent}'></div>"
                        f"<div><b>{title}</b><span>{line}</span></div></div>")


def night_card(name, inner):
    return render(name, f"<div class='night'>{inner}</div>")


def logo(width):
    return f"<img src='data:image/png;base64,{b64(BRAND / 'logo' / 'fretlings_logo_dark.png')}' style='width:{width}px'>"


def graphics():
    (OUT / "gfx").mkdir(parents=True, exist_ok=True)
    family = f"<img src='data:image/png;base64,{b64(BRAND / 'stickers' / 'family_calm.png')}' style='width:1300px'>"
    return {
        "title": night_card("title", logo(1100) +
                            "<div class='tagline'>Tiny creatures that panic so you don't have to.</div>"),
        "end": night_card("end", family + logo(900) +
                          "<div class='small'>META QUEST 3 · 3S &nbsp;·&nbsp; META VR GLASSES &nbsp;·&nbsp; DEMO MODE INCLUDED</div>"),
        "relief": caption("relief", "Handle it…", "…and it sighs with relief", ACCENT["gesture"]),
        "platforms": caption("platforms", "Plugs into your favorite platforms", "the mail, calendar, task and chat apps you already use",
                             ACCENT["gesture"]),
        "problem": caption("problem", "Notifications interrupt.", "Fretlings just worry. Quietly.", ACCENT["gesture"]),
        "setup": caption("setup", "Hands only", "No controllers. No room scan.", ACCENT["gesture"]),
        "mail": caption("mail", "Mail", "heats up when important emails pile up", ACCENT["mail"], "mail_calm.png"),
        "calendar": caption("calendar", "Calendar", "gets nervous as your next meeting comes closer", ACCENT["calendar"],
                            "calendar_panic.png"),
        "tasks": caption("tasks", "Tasks", "collects sticky notes when things go overdue", ACCENT["tasks"],
                         "tasks_panic.png"),
        "messages": caption("messages", "Messages", "starts chattering when someone is waiting", ACCENT["messages"],
                            "messages_panic.png"),
        "agents": caption("agents", "Agents", "watches over your AI coding sessions", ACCENT["agents"], "agents_panic.png"),
        "memory": caption("memory", "Next morning", "right where you left them", ACCENT["gesture"]),
    }


# One entry per shot: (source, start s, end s, overlay or None). A "card" shot is a still graphic of the given length.
def edit(g):
    return [
        ("clip", MAIN, 42.0, 49.5, None),          # hook: Agents panics, look, bubble, panel
        ("card", g["title"], 2.6),
        ("clip", MAIN, 84.0, 91.0, g["problem"]),   # calm desk while working
        ("clip", MAIN, 3.5, 18.5, g["setup"]),      # hand on desk, Mail drops in, look, pinch, the family arrives
        ("states",),                                # the four stress states, on the night background
        ("clip", MAIN, 26.0, 30.5, g["mail"]),
        ("clip", MAIN, 67.5, 73.5, g["calendar"]),
        ("clip", MAIN, 35.0, 40.0, g["tasks"]),
        ("clip", MAIN, 54.0, 57.5, g["messages"]),
        ("clip", MAIN, 49.5, 54.0, g["agents"]),
        ("clip", MAIN, 57.5, 63.5, g["relief"]),    # the user clears the messages: calm again, with a sigh
        ("clip", MAIN, 91.0, 96.0, g["platforms"]),  # the whole family calm on the desk
        ("clip", MAIN, 116.8, 118.6, None),        # the desk before quitting (stops before the head turns away)
        # Relaunched: the empty desk, then the creatures appear at 20.45 s, slowed down so the moment reads clearly.
        ("clip", RELAUNCH, 19.3, 20.6, g["memory"], 0.55, True, False),
        ("clip", RELAUNCH, 22.45, 27.5, g["memory"], 1.0, False, True),   # same caption, kept on screen
        ("card", g["end"], 4.5),
    ]


STATES = [("calm", "Calm"), ("alert", "Alert"), ("stressed", "Stressed"), ("panic", "Panic")]
STATES_LENGTH = 6.5
STATES_STEP = 0.75   # seconds between two creatures appearing


def states_layers():
    """The night background with its line, then one transparent layer per state (creature and its name), so each
    can drop in on its own."""
    folder = BRAND / "stickers" / "states"
    layers = [night_card("states_bg", "<div style='position:absolute;top:110px;width:100%;text-align:center;"
                         "font-family:Fredoka;font-weight:600;font-size:64px;color:#FBF6EE'>"
                         "The more it needs you, the more it shows.</div>")]
    for i, (key, label) in enumerate(STATES):
        art = folder / f"mail_{i}_{key}.png"
        left = 120 + i * 430
        layers.append(render(f"state_{i}", f"<img src='data:image/png;base64,{b64(art)}' style='position:absolute;"
                             f"left:{left}px;top:250px;width:400px'><div style='position:absolute;left:{left}px;"
                             f"top:680px;width:400px;text-align:center;font-family:Fredoka;font-weight:600;"
                             f"font-size:48px;color:#FBF6EE'>{label}</div>"))
    return layers


def encode_states(index):
    target = OUT / "parts" / f"{index:02d}.mp4"
    layers = states_layers()
    inputs, graph = [], f"[0:v]scale={W}:{H},format=rgba[s0];"
    for layer in layers:
        inputs += ["-loop", "1", "-t", f"{STATES_LENGTH}", "-i", str(layer)]
    for i in range(1, len(layers)):
        at = 0.5 + (i - 1) * STATES_STEP
        graph += (f"[{i}:v]format=rgba,fade=in:st={at}:d=0.2:alpha=1[l{i}];"
                  f"[s{i - 1}][l{i}]overlay=0:0[s{i}];")
    last = len(layers) - 1
    graph += (f"[s{last}]fade=in:st=0:d=0.3,fade=out:st={STATES_LENGTH - 0.3}:d=0.3,format=yuv420p,fps={FPS}[v];")
    # Each creature lands with the app's own landing sound.
    landing = ROOT / "Assets" / "_Project" / "Art" / "Audio" / "landing.wav"
    sound_inputs = []
    for i in range(len(STATES)):
        sound_inputs += ["-i", str(landing)]
        delay = int((0.5 + i * STATES_STEP) * 1000)
        graph += f"[{len(layers) + i}:a]adelay={delay}|{delay},volume=0.6[d{i}];"
    graph += "".join(f"[d{i}]" for i in range(len(STATES)))
    graph += f"amix=inputs={len(STATES)}:normalize=0,apad,atrim=0:{STATES_LENGTH},aformat=sample_rates=48000:channel_layouts=stereo[a]"
    subprocess.run(["ffmpeg", "-v", "error", "-y", *inputs, *sound_inputs, "-filter_complex", graph,
                    "-map", "[v]", "-map", "[a]", "-t", f"{STATES_LENGTH}", "-c:v", "libx264", "-crf", "18",
                    "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "192k", str(target)], check=True)
    return target


def encode_clip(index, source, start, end, overlay, speed=1.0, fade_in=True, fade_out=True):
    target = OUT / "parts" / f"{index:02d}.mp4"
    duration = (end - start) / speed
    video = (f"[0:v]setpts=PTS/{speed},fps={FPS},scale={W}:{H}:force_original_aspect_ratio=increase,"
             f"crop={W}:{H},setsar=1[v]")
    inputs = ["-ss", f"{start}", "-t", f"{end - start}", "-i", str(source)]
    if overlay:
        inputs += ["-loop", "1", "-t", f"{duration}", "-i", str(overlay)]
        fades = []
        if fade_in:
            fades.append("fade=in:st=0.35:d=0.25:alpha=1")
        if fade_out:
            fades.append(f"fade=out:st={max(0.6, duration - 0.45)}:d=0.25:alpha=1")
        video += f";[1:v]{','.join(['format=rgba', *fades])}[o];[v][o]overlay=0:0[v]"
    video += f";[0:a]atempo={speed}[a]" if speed >= 0.5 else ";[0:a]anull[a]"
    subprocess.run(["ffmpeg", "-v", "error", "-y", *inputs, "-filter_complex", video, "-map", "[v]", "-map", "[a]",
                    "-c:v", "libx264", "-crf", "18", "-preset", "medium", "-pix_fmt", "yuv420p",
                    "-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-ac", "2", str(target)], check=True)
    return target


def encode_card(index, image, duration):
    target = OUT / "parts" / f"{index:02d}.mp4"
    video = (f"[0:v]scale={W}:{H},format=rgba,fade=in:st=0:d=0.3,fade=out:st={duration - 0.3}:d=0.3,"
             f"format=yuv420p,fps={FPS}[v]")
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-loop", "1", "-t", f"{duration}", "-i", str(image),
                    "-f", "lavfi", "-t", f"{duration}", "-i", "anullsrc=r=48000:cl=stereo",
                    "-filter_complex", video, "-map", "[v]", "-map", "1:a", "-c:v", "libx264", "-crf", "18",
                    "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "192k", str(target)], check=True)
    return target


def main():
    (OUT / "parts").mkdir(parents=True, exist_ok=True)
    g = graphics()
    parts = []
    for index, shot in enumerate(edit(g)):
        if shot[0] == "clip":
            parts.append(encode_clip(index, *shot[1:]))
        elif shot[0] == "states":
            parts.append(encode_states(index))
        else:
            parts.append(encode_card(index, shot[1], shot[2]))
        print("part", index, parts[-1].name)

    listing = OUT / "parts.txt"
    listing.write_text("".join(f"file '{p}'\n" for p in parts))
    assembled = OUT / "assembled.mp4"
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "concat", "-safe", "0", "-i", str(listing),
                    "-c:v", "libx264", "-crf", "18", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "192k",
                    str(assembled)], check=True)
    final = OUT / "fretlings_draft.mp4"
    add_music(assembled, final)
    print("wrote", final)


def add_music(video, target):
    """Lays the music very low under the clips' own sound. Its opening runs as long as needed, then crossfades into
    its real ending, so the final chord lands with the end card."""
    length = float(subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0",
                                   str(video)], check=True, capture_output=True, text=True).stdout)
    start, end = MUSIC_ENDING
    opening = length - (end - start) + 1.0
    graph = (f"[1:a]asplit[a1][a2];[a1]atrim=0:{opening},asetpts=PTS-STARTPTS[m1];"
             f"[a2]atrim={start}:{end},asetpts=PTS-STARTPTS[m2];[m1][m2]acrossfade=d=1[m];"
             f"[m]volume={MUSIC_VOLUME}[mv];[0:a][mv]amix=inputs=2:duration=first:normalize=0[a]")
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(video), "-i", str(MUSIC), "-filter_complex", graph,
                    "-map", "0:v", "-map", "[a]", "-c:v", "copy", "-c:a", "aac", "-b:a", "192k",
                    "-movflags", "+faststart", str(target)], check=True)


if __name__ == "__main__":
    main()
