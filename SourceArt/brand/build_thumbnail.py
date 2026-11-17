"""Renders the Devpost thumbnails, 3:2 (python3 SourceArt/brand/build_thumbnail.py).

A: a real headset frame (the agents creature panicking, the user's hand reaching in) with the logo on a cream card.
B: the brand version, the family stickers on the night background.
"""
import base64
import pathlib
import subprocess
import tempfile

HERE = pathlib.Path(__file__).parent
OUT = HERE / "devpost"
CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
W, H = 1500, 1000


def b64(path):
    return base64.b64encode(path.read_bytes()).decode()


HEAD = ("<!doctype html><html><head><meta charset='utf-8'><link href='https://fonts.googleapis.com/css2?"
        "family=Fredoka:wght@600&family=Lato:wght@400;700&display=block' rel='stylesheet'><style>"
        f"html,body{{margin:0;width:{W}px;height:{H}px;overflow:hidden}}</style></head><body>")

FRAME = b64(HERE / "_assets" / "thumb_src_43.0.jpg")
LOGO_LIGHT = b64(HERE / "logo" / "fretlings_logo_light.png")
LOGO_DARK = b64(HERE / "logo" / "fretlings_logo_dark.png")
FAMILY = b64(HERE / "stickers" / "family_calm.png")
PANIC = b64(HERE / "stickers" / "mail_panic.png")

A = HEAD + f"""<div style="position:relative;width:{W}px;height:{H}px;background:#000">
<img src="data:image/jpeg;base64,{FRAME}" style="position:absolute;height:{H}px;left:-20px;top:0">
<div style="position:absolute;right:46px;top:46px;background:rgba(251,246,238,.96);border-radius:36px;
 padding:26px 34px 28px;box-shadow:0 12px 40px rgba(0,0,0,.3);width:560px">
<img src="data:image/png;base64,{LOGO_LIGHT}" style="width:560px;display:block;margin:-30px 0 -26px -20px">
<div style="font-family:Lato;font-weight:700;font-size:30px;line-height:1.3;color:#4A3B33;margin-top:6px">
Tiny desk creatures that panic about your inbox, meetings and tasks… so you don't have to.</div></div></div></body></html>"""

B = HEAD + f"""<div style="position:relative;width:{W}px;height:{H}px;
background:radial-gradient(120% 95% at 50% 100%,#4A403C,#2E2928 70%);display:flex;flex-direction:column;
align-items:center;justify-content:center;gap:10px">
<img src="data:image/png;base64,{LOGO_DARK}" style="width:1050px;margin:-40px 0 -20px">
<div style="font-family:Lato;font-size:36px;color:#E9DFD2;margin-bottom:10px">Tiny creatures that panic so you don't have to.</div>
<div style="position:relative;width:1300px;height:440px">
<img src="data:image/png;base64,{FAMILY}" style="position:absolute;width:1300px;left:0;top:0">
<img src="data:image/png;base64,{PANIC}" style="position:absolute;width:300px;left:-30px;top:120px;display:none">
</div></div></body></html>"""


def render(html, name):
    with tempfile.NamedTemporaryFile("w", suffix=".html", delete=False) as page:
        page.write(html)
    png = OUT / f"{name}.png"
    subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", f"--window-size={W},{H}",
                    "--virtual-time-budget=6000", f"--screenshot={png}", f"file://{page.name}"],
                   check=True, capture_output=True)
    jpg = OUT / f"{name}.jpg"
    subprocess.run(["sips", "-s", "format", "jpeg", "-s", "formatOptions", "90", str(png), "--out", str(jpg)],
                   check=True, capture_output=True)
    png.unlink()
    print("wrote", jpg)


OUT.mkdir(exist_ok=True)
render(A, "thumbnail_a_desk")
render(B, "thumbnail_b_brand")
