"""Renders the Fretlings logo and app icon to PNG with headless Chrome (python3 SourceArt/brand/build_logo.py).

Logo: the mail creature in a soft round badge, then "Fretlings" in Fredoka with a small flame for the dot of the i
(STYLE.md). Fredoka (SIL OFL) is loaded from Google Fonts at render time.
"""
import base64
import pathlib
import subprocess
import tempfile

HERE = pathlib.Path(__file__).parent
OUT = HERE / "logo"
CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
MAIL = base64.b64encode((HERE / "stickers" / "mail_calm.png").read_bytes()).decode()

FLAME = """<svg class="flame" viewBox="0 0 20 26"><defs><linearGradient id="g" x1="0" y1="1" x2="0" y2="0">
<stop offset="0" stop-color="#E2553A"/><stop offset="1" stop-color="#F1A661"/></linearGradient></defs>
<path d="M10 0C13 6 18 10 18 16a8 8 0 0 1-16 0C2 10 7 6 10 0Z" fill="url(#g)"/></svg>"""

HEAD = """<!doctype html><html><head><meta charset="utf-8">
<link href="https://fonts.googleapis.com/css2?family=Fredoka:wght@600&display=block" rel="stylesheet">
<style>html,body{margin:0;background:transparent}
.flame{position:absolute;width:.3em;left:50%;transform:translateX(-50%);top:-.08em}
.i{position:relative;display:inline-block}</style></head><body>"""


def logo(ink, badge):
    return HEAD + f"""<div style="width:1600px;height:480px;display:flex;align-items:center;justify-content:center;gap:44px">
<div style="width:360px;height:360px;border-radius:50%;background:{badge};position:relative;flex:none">
<img src="data:image/png;base64,{MAIL}" style="position:absolute;width:88%;left:6%;top:9%"></div>
<div style="font-family:Fredoka;font-weight:600;font-size:200px;color:{ink};letter-spacing:4px;white-space:nowrap;line-height:1">
Fretl<span class="i">ı{FLAME}</span>ngs</div></div></body></html>"""


def icon():
    # Full-bleed square: the store and the headset round the corners themselves.
    return HEAD + f"""<div style="width:1024px;height:1024px;position:relative;overflow:hidden;
background:radial-gradient(90% 90% at 50% 25%,#FBF6EE,#EADFCF)">
<img src="data:image/png;base64,{MAIL}" style="position:absolute;width:112%;left:-6%;bottom:-13%">
<div style="position:absolute;right:150px;top:110px;width:120px">{FLAME.replace('class="flame"', 'style="width:100%"')}</div>
</div></body></html>"""


def render(html, name, width, height):
    with tempfile.NamedTemporaryFile("w", suffix=".html", delete=False) as page:
        page.write(html)
    subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--default-background-color=00000000",
                    f"--window-size={width},{height}", "--virtual-time-budget=6000",
                    f"--screenshot={OUT / name}", f"file://{page.name}"], check=True, capture_output=True)
    print("wrote", OUT / name)


OUT.mkdir(exist_ok=True)
render(logo("#4A3B33", "#F2D3AE"), "fretlings_logo_light.png", 1600, 480)
render(logo("#FBF6EE", "#F2D3AE"), "fretlings_logo_dark.png", 1600, 480)
render(icon(), "fretlings_icon_1024.png", 1024, 1024)
