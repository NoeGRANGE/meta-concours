"""Builds the Devpost image gallery, 3:2 JPGs of 1500x1000 (python3 SourceArt/brand/build_gallery.py).

Frames come from the headset recordings in Captures/ (not in git); the states and before/after images are composed
here. The captions to paste on Devpost are in DEVPOST.md.
"""
import pathlib
import shutil
import subprocess
import tempfile

from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[2]
BRAND = ROOT / "SourceArt" / "brand"
OUT = BRAND / "devpost" / "gallery"
MAIN = ROOT / "Captures" / "com.fretlings.app-20261116-232346-0.mp4"
RELAUNCH = ROOT / "Captures" / "com.oculus.vrshell-20261117-001818-0.mp4"
W, H = 1500, 1000
FONT = BRAND.parent.parent / "Assets" / "_Project" / "Art" / "Fonts" / "Lato-Regular.ttf"

# (file name, source video, time in seconds, left edge of the 1620x1080 crop in the 1920x1080 frame)
FRAMES = [
    ("02_hand_on_desk", MAIN, 4.9, 300),
    ("03_meet_mail", MAIN, 10.3, 215),
    ("04_detail_panel", MAIN, 13.4, 250),
    ("05_family", MAIN, 17.0, 150),
    ("07_agents_panel", MAIN, 49.5, 200),
    ("08_messages_waiting", MAIN, 57.0, 150),
    ("09_relief", MAIN, 60.8, 150),
    ("10_calendar_panic", MAIN, 71.0, 100),
]


def frame(source, at):
    with tempfile.NamedTemporaryFile(suffix=".png", delete=False) as shot:
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-ss", f"{at}", "-i", str(source), "-frames:v", "1", shot.name],
                       check=True)
        return Image.open(shot.name).convert("RGB")


def crop(image, left):
    return image.crop((left, 0, left + 1620, 1080)).resize((W, H), Image.LANCZOS)


def label(image, text, x, y, size=40):
    draw = ImageDraw.Draw(image, "RGBA")
    font = ImageFont.truetype(str(FONT), size)
    box = draw.textbbox((0, 0), text, font=font)
    pad = 18
    draw.rounded_rectangle((x, y, x + box[2] + 2 * pad, y + box[3] + 2 * pad), radius=24, fill=(251, 246, 238, 245))
    draw.text((x + pad, y + pad - box[1] // 2), text, font=font, fill=(74, 59, 51))


def states_image():
    image = Image.new("RGB", (W, H), (46, 41, 40))
    names = ["Calm", "Alert", "Stressed", "Panic"]
    font = ImageFont.truetype(str(FONT), 46)
    draw = ImageDraw.Draw(image)
    title = "The more it needs you, the more it shows."
    box = draw.textbbox((0, 0), title, font=ImageFont.truetype(str(FONT), 52))
    draw.text(((W - box[2]) // 2, 90), title, font=ImageFont.truetype(str(FONT), 52), fill=(251, 246, 238))
    for i, name in enumerate(names):
        sticker = Image.open(BRAND / "stickers" / "states" / f"mail_{i}_{name.lower()}.png").resize((360, 360))
        x = 30 + i * 365
        image.paste(sticker, (x, 260), sticker)
        box = draw.textbbox((0, 0), name, font=font)
        draw.text((x + (360 - box[2]) // 2, 650), name, font=font, fill=(233, 223, 210))
    return image


def before_after_image():
    image = Image.new("RGB", (W, H), (46, 41, 40))
    # The same lower band of the desk in both, where the creatures sit.
    before = frame(RELAUNCH, 20.2).crop((200, 560, 1720, 1060)).resize((1500, 494))
    after = frame(RELAUNCH, 24.5).crop((200, 560, 1720, 1060)).resize((1500, 494))
    image.paste(before, (0, 0))
    image.paste(after, (0, 506))
    label(image, "App relaunched…", 28, 28)
    label(image, "…they're right where you left them", 28, 534)
    return image


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    shutil.copy(BRAND / "devpost" / "thumbnail_a_desk.jpg", OUT / "01_desk_hero.jpg")
    for name, source, at, left in FRAMES:
        crop(frame(source, at), left).save(OUT / f"{name}.jpg", quality=90)
    states_image().save(OUT / "06_states.jpg", quality=90)
    before_after_image().save(OUT / "11_remembers_desk.jpg", quality=90)
    shutil.copy(BRAND / "devpost" / "thumbnail_b_brand.jpg", OUT / "12_brand.jpg")
    for path in sorted(OUT.glob("*.jpg")):
        print(path.name, Image.open(path).size, f"{path.stat().st_size // 1024} KB")


if __name__ == "__main__":
    main()
