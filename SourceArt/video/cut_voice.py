"""Cuts the single voice-over recording into one file per line (python3 SourceArt/video/cut_voice.py).

The ranges were found from a local transcription and the silences of the recording: false starts and the first take
of line 07 are left out, line 12 is rejoined around a long pause, and long pauses inside a line are shortened.
Outputs Captures/voice/NN.wav, which build_video.py places in the edit.
"""
import pathlib
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[2]
SOURCE = ROOT.parent / "Audio.m4a"
OUT = ROOT / "Captures" / "voice"

# Line number: one or more (start, end) ranges in seconds in the recording, joined in order.
LINES = {
    1: [(6.5, 9.7)],
    2: [(12.1, 13.6)],
    3: [(17.5, 23.4)],
    4: [(26.6, 38.6)],
    5: [(43.3, 46.6)],
    6: [(51.4, 54.9)],
    7: [(66.4, 68.9)],                 # second take: the first said "mails"
    8: [(72.5, 75.3)],
    9: [(80.2, 83.8)],
    10: [(86.3, 88.8)],
    11: [(91.8, 94.8)],
    12: [(98.0, 100.7), (108.1, 111.2)],  # "Handle it with a pinch, and it sighs" + "with relief", a 7 s pause cut out
    13: [(117.1, 120.1)],
    14: [(125.78, 129.0)],             # after the false start "You can…"
    15: [(134.0, 137.1)],
}

# Pauses longer than this inside a line are shortened to KEEP seconds, so a line fits its shot.
PAUSE, KEEP = 0.45, 0.28


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for number, ranges in LINES.items():
        parts = "".join(f"[0:a]atrim={a}:{b},asetpts=PTS-STARTPTS[p{i}];" for i, (a, b) in enumerate(ranges))
        joined = "".join(f"[p{i}]" for i in range(len(ranges))) + f"concat=n={len(ranges)}:v=0:a=1[j];"
        graph = (parts + joined + f"[j]silenceremove=stop_periods=-1:stop_duration={PAUSE}:stop_threshold=-52dB:"
                 f"stop_silence={KEEP},afade=t=in:d=0.03,areverse,afade=t=in:d=0.08,areverse[a]")
        target = OUT / f"{number:02d}.wav"
        subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(SOURCE), "-filter_complex", graph, "-map", "[a]",
                        "-ar", "48000", str(target)], check=True)
        length = float(subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of",
                                       "csv=p=0", str(target)], capture_output=True, text=True).stdout)
        print(f"{number:02d}  {length:4.1f} s")


if __name__ == "__main__":
    main()
