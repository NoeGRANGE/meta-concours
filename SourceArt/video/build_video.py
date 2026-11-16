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
        "todo_agents": night_card("todo_agents", "<div class='tagline'>[ Real coding session — to be filmed ]</div>"),
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
        ("clip", MAIN, 42.0, 52.0, None),          # hook: Agents panics, look, bubble, panel
        ("card", g["title"], 2.6),
        ("clip", MAIN, 84.0, 91.0, g["problem"]),   # calm desk while working
        ("clip", MAIN, 3.5, 18.5, g["setup"]),      # hand on desk, Mail drops in, look, pinch, the family arrives
        ("clip", MAIN, 26.0, 30.5, g["mail"]),
        ("clip", MAIN, 67.5, 73.5, g["calendar"]),
        ("clip", MAIN, 35.0, 40.0, g["tasks"]),
        ("clip", MAIN, 55.0, 59.5, g["messages"]),
        ("clip", MAIN, 49.0, 54.5, g["agents"]),
        ("card", g["todo_agents"], 3.0),
        ("clip", MAIN, 117.0, 119.5, None),        # the desk before quitting
        ("clip", RELAUNCH, 18.8, 20.1, g["memory"]),  # relaunched: the creatures come back
        ("clip", RELAUNCH, 22.3, 27.5, g["memory"]),
        ("card", g["end"], 4.5),
    ]


def encode_clip(index, source, start, end, overlay):
    target = OUT / "parts" / f"{index:02d}.mp4"
    duration = end - start
    video = f"[0:v]fps={FPS},scale={W}:{H}:force_original_aspect_ratio=increase,crop={W}:{H},setsar=1[v]"
    inputs = ["-ss", f"{start}", "-t", f"{duration}", "-i", str(source)]
    if overlay:
        inputs += ["-loop", "1", "-t", f"{duration}", "-i", str(overlay)]
        video += (f";[1:v]format=rgba,fade=in:st=0.35:d=0.25:alpha=1,"
                  f"fade=out:st={max(0.6, duration - 0.45)}:d=0.25:alpha=1[o];[v][o]overlay=0:0[v]")
    subprocess.run(["ffmpeg", "-v", "error", "-y", *inputs, "-filter_complex", video, "-map", "[v]", "-map", "0:a",
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
        else:
            parts.append(encode_card(index, shot[1], shot[2]))
        print("part", index, parts[-1].name)

    listing = OUT / "parts.txt"
    listing.write_text("".join(f"file '{p}'\n" for p in parts))
    final = OUT / "fretlings_draft.mp4"
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-f", "concat", "-safe", "0", "-i", str(listing),
                    "-c:v", "libx264", "-crf", "18", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "192k",
                    "-movflags", "+faststart", str(final)], check=True)
    print("wrote", final)


if __name__ == "__main__":
    main()
