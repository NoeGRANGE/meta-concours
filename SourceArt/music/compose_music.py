#!/usr/bin/env python3
"""Fretlings: original background music for the 2:20 product video.

Everything is synthesized here (numpy + stdlib wave, ffmpeg only for loudness
measurement and MP3 export): no samples, no soundfonts, no third-party audio.

    python3 compose_music.py

writes music_{a,b,c}.wav (48 kHz 16-bit stereo, 140 s), matching 192 kbps MP3s
and ecoute.html next to this script. Fixed seed: the output is reproducible.
"""
import base64
import itertools
import re
import subprocess
import wave
from pathlib import Path

import numpy as np

SR = 48000
BPM = 105
BEAT = 60 / BPM
BAR = 4 * BEAT
LENGTH = 140.0
N = int(SR * LENGTH)
OUT = Path(__file__).resolve().parent
FFMPEG = "/opt/homebrew/bin/ffmpeg"
SEED = 20261117
TARGET_LUFS = -16.0
CEILING = 10 ** (-1.0 / 20)

# ---------------------------------------------------------------- structure
# D major, one chord per bar. At 105 BPM a bar is 2.286 s, so the sections
# land on bars near the requested marks (bar 56 is exactly 2:08).
SECTIONS = [("hook", 0), ("bed", 4), ("lift", 20), ("breakdown", 37),
            ("return", 50), ("ending", 56), ("final", 60)]
PROG = (["I", "V", "vi", "IV"]
        + ["I", "vi", "IV", "V"] * 4
        + ["I", "V", "vi", "IV"] * 4 + ["V"]
        + ["vi9", "IV9", "ii9", "V"] * 3 + ["Vsus"]
        + ["I", "V", "vi", "IV", "ii", "V"]
        + ["I", "V", "vi", "IV"]
        + ["I"])
assert len(PROG) == 61
CHORDS = {  # pitch classes (root first), bass root midi
    "I": ([2, 6, 9], 38), "V": ([9, 1, 4], 45), "vi": ([11, 2, 6], 47),
    "IV": ([7, 11, 2], 43), "ii": ([4, 7, 11], 40),
    "vi9": ([11, 2, 6, 1], 47), "IV9": ([7, 11, 2, 9], 43),
    "ii9": ([4, 7, 11, 6], 40), "Vsus": ([9, 2, 4], 45),
}
# The 2-bar hook motif: call over I-V, answer over vi-IV. (beat, beats, midi)
CALL = [(0, .5, 78), (.5, .5, 81), (1, 1, 83), (2, .5, 81), (2.5, .5, 78), (3, 1, 76),
        (4, .5, 76), (4.5, .5, 73), (5, 1, 69), (6, .5, 73), (6.5, 1.5, 76)]
ANSWER = [(0, .5, 78), (.5, .5, 81), (1, 1, 83), (2, .5, 86), (2.5, .5, 83), (3, 1, 81),
          (4, .5, 79), (4.5, .5, 78), (5, 1, 76), (6, 2, 74)]
END_ANSWER = ANSWER[:6] + [(4, .5, 79), (4.5, .5, 78), (5, .5, 76), (5.5, 1.5, 81),
                           (7, 1, 76), (8, 2.1, 74)]  # lands on D over the final I
HEAD = [(2, .5, 78), (2.5, .5, 81), (3, 1, 83)]
TAIL = [(2, .5, 76), (2.5, .5, 73), (3, 1, 69)]
CURIOUS1 = [(2, .5, 78), (2.5, .5, 79), (3, 1, 83)]
CURIOUS2 = [(2, .5, 76), (2.5, .5, 77), (3, 1, 81)]  # chromatic F natural: the "hmm?"
COUNTER = {"I": [(0, 2, 81), (2, 2, 78)], "V": [(0, 2, 76), (2, 2, 73)],
           "vi": [(0, 2, 74), (2, 2, 78)], "IV": [(0, 3, 79), (3, 1, 81)]}


def hz(m):
    return 440.0 * 2 ** ((m - 69) / 12)


def tsec(bar, beat=0.0):
    return (bar * 4 + beat) * BEAT


def section(bar):
    return [name for name, start in SECTIONS if bar >= start][-1]


# ---------------------------------------------------------------- dsp helpers
def filt(x, lo=None, hi=None, order=2):
    """Zero-phase Butterworth-shaped band filter applied in the frequency domain."""
    n = len(x)
    m = 1 << int(np.ceil(np.log2(n + min(n, SR // 2))))
    f = np.fft.rfftfreq(m, 1 / SR)
    f[0] = 1e-3
    g = np.ones_like(f)
    if lo:
        g /= np.sqrt(1 + (lo / f) ** (2 * order))
    if hi:
        g /= np.sqrt(1 + (f / hi) ** (2 * order))
    return np.fft.irfft(np.fft.rfft(x, m) * g, m)[:n]


def movavg(x, w):
    w = max(1, int(w))
    c = np.cumsum(np.concatenate([[0.0], x]))
    out = (c[w:] - c[:-w]) / w
    pad = len(x) - len(out)
    return np.concatenate([np.full(pad // 2, out[0]), out, np.full(pad - pad // 2, out[-1])])


def fade(x, a=0.002, r=0.010):
    na, nr = max(2, int(a * SR)), max(2, int(r * SR))
    x[:na] *= 0.5 - 0.5 * np.cos(np.linspace(0, np.pi, na))
    x[-nr:] *= 0.5 + 0.5 * np.cos(np.linspace(0, np.pi, nr))
    return x


def release(x, dur, tau):
    """Exponential damping after `dur`, truncated at about -52 dB."""
    end = min(len(x), int((dur + 6 * tau) * SR))
    x = x[:end].copy()
    k = int(max(dur, 0) * SR)
    if k < end:
        x[k:] *= np.exp(-np.arange(end - k) / (tau * SR))
    return x


def tvec(seconds):
    return np.arange(int(seconds * SR)) / SR


# ---------------------------------------------------------------- instruments
# Each tonal synth returns (mono signal, release tau or None for free ringing).
def marimba(m, dur, rng):
    f = hz(m)
    tau = float(np.clip(0.55 * (440 / f) ** 0.5, 0.18, 0.9))
    t = tvec(5 * tau)
    x = np.sin(2 * np.pi * f * t) * np.exp(-t / tau)
    for ratio, amp, td in ((3.93, .22, .22), (9.2, .05, .08)):  # bar-tuned overtones
        if f * ratio < 9000:
            x += amp * np.sin(2 * np.pi * f * ratio * t) * np.exp(-t / (tau * td))
    k = int(0.012 * SR)
    x[:k] += 0.06 * filt(rng.standard_normal(k), 300, 2500) * np.exp(-np.arange(k) / (0.002 * SR))
    return x, None


def ocarina(m, dur, rng):
    f = hz(m)
    t = tvec(dur + 1.0)
    vib = 1 + 0.004 * np.sin(2 * np.pi * 5.2 * t) * np.clip((t - 0.25) / 0.3, 0, 1)
    ph = 2 * np.pi * f * np.cumsum(vib) / SR
    x = np.sin(ph) + 0.12 * np.sin(2 * ph) + 0.04 * np.sin(3 * ph)
    return x * np.clip(t / 0.07, 0, 1), 0.12


def pizz(m, dur, rng):
    f = hz(m)
    t = tvec(1.2)
    x = np.zeros_like(t)
    for n in range(1, 7):
        if n * f < 4000:
            x += n ** -1.3 * np.sin(2 * np.pi * n * f * t) * np.exp(-t / (0.5 / n ** 0.8))
    return x * np.clip(t / 0.003, 0, 1), 0.06


def roundbass(m, dur, rng):
    f = hz(m)
    t = tvec(dur + 0.6)
    x = np.sin(2 * np.pi * f * t) + 0.25 * np.sin(4 * np.pi * f * t) + 0.08 * np.sin(6 * np.pi * f * t)
    x *= np.exp(-t / 1.5) * np.clip(t / 0.008, 0, 1)
    return np.tanh(1.3 * x) / np.tanh(1.3), 0.07


_KS = {}


def uke(m, dur, rng):
    """Karplus-Strong string, cached per pitch, retuned by resampling."""
    if m not in _KS:
        f = hz(m)
        p = int(round(SR / f))
        total = int(2.0 * SR)
        burst = filt(np.random.default_rng(m).uniform(-1, 1, p), hi=3500)  # soft nylon pick
        y = np.zeros(total + p + 1)
        y[1:p + 1] = burst
        d = np.exp(-1 / (1.1 * f))  # same ~1.1 s decay for every string
        for s in range(p + 1, len(y), p):
            e = min(s + p, len(y))
            y[s:e] = d * 0.5 * (y[s - p:e - p] + y[s - p - 1:e - p - 1])
        y = y[1:total + 1]
        ratio = f / (SR / (p + 0.5))  # the averaging filter adds half a sample of delay
        y = np.interp(np.arange(0, len(y) - 1, ratio), np.arange(len(y)), y)
        y = filt(y, lo=90)
        _KS[m] = y / np.max(np.abs(y))
    return _KS[m].copy(), 0.05


def _partials(f, t, parts, attack):
    x = np.zeros_like(t)
    for ratio, amp, tau in parts:
        if f * ratio < 11000:
            x += amp * np.sin(2 * np.pi * f * ratio * t) * np.exp(-t / tau)
    return x * np.clip(t / attack, 0, 1)


def bell(m, dur, rng):
    t = tvec(4.5)
    return _partials(hz(m), t, ((1, 1, .9), (2.76, .25, .25), (5.4, .08, .08)), 0.002), None


def musicbox(m, dur, rng):
    t = tvec(5.0)
    return _partials(hz(m), t, ((1, 1, 1.0), (2.0, .2, .35), (5.93, .1, .1)), 0.001), None


def felt(m, dur, rng):
    f = hz(m)
    tau0 = float(np.clip(2.0 * (262 / f) ** 0.5, 0.4, 3.0))
    t = tvec(min(dur + 1.2, 5.0))
    x = np.zeros_like(t)
    for n in range(1, 9):
        fn = n * f * np.sqrt(1 + 0.0004 * n * n)  # slight string stiffness
        if fn > 7000:
            break
        tn = tau0 / (1 + 0.7 * (n - 1))
        env = 0.6 * np.exp(-t / (tn * 0.25)) + 0.4 * np.exp(-t / tn)
        x += n ** -1.5 * np.exp(-0.35 * (n - 1)) * env * (
            np.sin(2 * np.pi * fn * t) + 0.6 * np.sin(2 * np.pi * fn * 1.0006 * t))
    k = int(0.02 * SR)
    x[:k] += 0.03 * filt(rng.standard_normal(k), hi=300) * np.exp(-np.arange(k) / (0.005 * SR))
    return x * np.clip(t / 0.005, 0, 1), 0.18


def pad(m, dur, rng):
    f = hz(m)
    t = tvec(dur + 2.5)
    x = np.zeros_like(t)
    for det in (-0.0035, 0, 0.0035):
        for n in range(1, 6):
            x += n ** -2 * np.sin(2 * np.pi * f * (1 + det) * n * t + rng.uniform(0, 2 * np.pi))
    return x * np.clip(t / 0.6, 0, 1) ** 2, 0.4


# Percussion: fresh noise on every hit, so repeats never sound copy-pasted.
def shaker(m, dur, rng):
    t = tvec(0.09)
    env = np.minimum(t / 0.012, 1) * np.exp(-np.maximum(t - 0.012, 0) / 0.025)
    return filt(rng.standard_normal(len(t)), 3500, 8500) * env


def rim(m, dur, rng):
    t = tvec(0.06)
    return (0.6 * np.sin(2 * np.pi * 820 * t) * np.exp(-t / 0.012)
            + 0.3 * np.sin(2 * np.pi * 1650 * t) * np.exp(-t / 0.008)
            + 0.4 * filt(rng.standard_normal(len(t)), 1500, 5000) * np.exp(-t / 0.003))


def kick(m, dur, rng):
    t = tvec(0.35)
    ph = 2 * np.pi * np.cumsum(58 + 70 * np.exp(-t / 0.03)) / SR
    return np.sin(ph) * np.exp(-t / 0.16)


def clap(m, dur, rng):
    t = tvec(0.2)
    env = sum(np.where(t >= o, np.exp(-(t - o) / 0.004), 0) for o in (0, .009, .018))
    env += 0.6 * np.where(t >= .025, np.exp(-(t - .025) / 0.05), 0)
    return filt(rng.standard_normal(len(t)), 900, 4500) * env


def snap(m, dur, rng):
    t = tvec(0.06)
    return (filt(rng.standard_normal(len(t)), 1800, 4500) * np.exp(-t / 0.007)
            + 0.4 * np.sin(2 * np.pi * 2100 * t) * np.exp(-t / 0.01))


def tick(m, dur, rng):
    t = tvec(0.05)
    return (0.7 * filt(rng.standard_normal(len(t)), 900, 3500) * np.exp(-t / 0.006)
            + 0.3 * np.sin(2 * np.pi * 1300 * t) * np.exp(-t / 0.01))


def swell(m, dur, rng):
    t = tvec(dur)
    return filt(rng.standard_normal(len(t)), 600, 5000) * (t / dur) ** 2


TONAL = {f.__name__: f for f in (marimba, ocarina, pizz, roundbass, uke, bell, musicbox, felt, pad)}
PERC = {f.__name__: f for f in (shaker, rim, kick, clap, snap, tick, swell)}
PERC_ROLE = {"shaker": "shaker", "rim": "back", "clap": "back", "snap": "back",
             "tick": "back", "kick": "kick", "swell": "swell"}


# ---------------------------------------------------------------- harmony
def voicing(pcs, prev, lo, hi):
    """4-note voicing in [lo, hi] closest to the previous one (smooth voice leading)."""
    centre = (lo + hi) / 2
    best = None
    # triads: double the root, else the fifth, else the third
    doubles = [list(pcs) + [d] for d in (pcs[0], pcs[2], pcs[1])] if len(pcs) == 3 else [list(pcs)]
    combos = ((k, combo) for k, ps in enumerate(doubles) for combo in
              itertools.product(*[[m for m in range(lo, hi + 1) if m % 12 == pc] for pc in ps]))
    for k, combo in combos:
        c = sorted(combo)
        if len(set(c)) < 4 or c[-1] - c[0] > 16:
            continue
        cost = 0.15 * abs(np.mean(c) - centre) + 2 * k
        cost += sum(abs(a - b) for a, b in zip(c, prev)) if prev else 0
        if best is None or cost < best[0]:
            best = (cost, c)
    return best[1]


def voicings(lo, hi):
    out, prev = [], None
    for ch in PROG:
        prev = voicing(CHORDS[ch][0], prev, lo, hi)
        out.append(prev)
    return out


# ---------------------------------------------------------------- comping styles
def comp_arp(add, bar, sec, vc, inst):
    if sec == "final":
        for i, m in enumerate(vc):
            add(tsec(bar) + i * 0.025, inst, m, 1.2, .55, "comp")
    elif sec == "breakdown":
        for b, i in enumerate([0, 2, 1, 3]):
            add(tsec(bar, b), inst, vc[i], BEAT, .26, "comp")
    elif sec == "bed":  # light syncopated bounce, room for the voice
        for b, i, v in ((0, 0, .42), (1.5, 2, .3), (2, 1, .36), (3, 3, .3)):
            add(tsec(bar, b), inst, vc[i], BEAT / 2, v, "comp")
    else:
        for k, i in enumerate([0, 2, 1, 3, 2, 1, 3, 2]):
            add(tsec(bar, k / 2), inst, vc[i], BEAT / 2, .5 if k % 2 == 0 else .36, "comp")


def comp_strum(add, bar, sec, vc, inst):
    def strum(beat, down, vel, beats):
        notes = vc if down else vc[::-1][:3]
        for i, m in enumerate(notes):
            add(tsec(bar, beat) + i * (.011 if down else .008), inst, m, beats * BEAT,
                vel * (1 - .06 * i), "comp")
    if sec == "final":
        for i, m in enumerate(vc):
            add(tsec(bar) + i * 0.03, inst, m, 1.2, .6, "comp")
        return
    if sec == "breakdown":  # finger-picked, let ring
        for k, i in enumerate([0, 2, 1, 3, 0, 2, 1, 3]):
            add(tsec(bar, k / 2), inst, vc[i], BEAT, .3 if k % 2 else .36, "comp")
        return
    pat = ([(0, 1, .34), (1.5, 0, .18), (2, 1, .3), (3.5, 0, .18)] if sec == "bed" else
           [(0, 1, .6), (1, 1, .45), (1.5, 0, .35), (2.5, 0, .35), (3, 1, .5), (3.5, 0, .35)])
    for j, (b, down, v) in enumerate(pat):
        nxt = pat[j + 1][0] if j + 1 < len(pat) else 4
        strum(b, down, v, nxt - b)


def comp_felt(add, bar, sec, vc, inst):
    def chord(beat, notes, beats, vel, roll=.015):
        for i, m in enumerate(notes):
            add(tsec(bar, beat) + i * roll, inst, m, beats * BEAT, vel, "comp")
    if sec == "final":
        chord(0, vc, 1.2 / BEAT, .5, .04)
    elif sec == "breakdown":
        for b, i in enumerate([0, 2, 1, 3]):
            add(tsec(bar, b), inst, vc[i], BEAT, .28, "comp")
    elif sec == "bed":
        chord(0, vc, 2, .32)
        chord(2, [vc[1], vc[3]], 1.5, .24)
        add(tsec(bar, 3.5), inst, vc[2], BEAT / 2, .25, "comp")
    else:
        chord(0, vc, 1.5, .45)
        for b, notes in ((1.5, [vc[2]]), (2, vc[:2]), (2.5, [vc[3]]), (3, [vc[2]]), (3.5, [vc[3]])):
            chord(b, notes, .5, .3)


# ---------------------------------------------------------------- variants
BASS_PLUCKY = {
    "hook": [(0, 1.5, 0, .9), (1.5, .5, 0, .6), (2, 1, 7, .8), (3, 1, 12, .6)],
    "bed": [(0, 2, 0, .85), (2, 1.5, 7, .7), (3.5, .5, 0, .5)],
    "lift": [(0, 1, 0, .9), (1, .5, 0, .5), (1.5, 1, 7, .7), (2.5, .5, 0, .6), (3, .5, 12, .6), (3.5, .5, 7, .6)],
    "breakdown": [(b, .4, 0, .5) for b in range(4)],
}
BASS_PLUCKY["return"] = BASS_PLUCKY["lift"]
BASS_PLUCKY["ending"] = BASS_PLUCKY["hook"]
BASS_LONG = {
    "hook": [(0, 2.5, 0, .85), (2.5, 1.5, 7, .6)],
    "bed": [(0, 4, 0, .8)],
    "lift": [(0, 2.5, 0, .85), (2.5, 1.5, 7, .6)],
    "breakdown": [(0, 2, 0, .6), (2, 2, 0, .5)],
}
BASS_LONG["return"] = BASS_LONG["ending"] = BASS_LONG["lift"]

VARIANTS = {
    "a": dict(
        title="Marimba & pizzicato", lead="marimba", lead2=None, hint="marimba", comp="marimba",
        comp_fn=comp_arp, comp_range=(55, 74), bass="pizz", bass_pat=BASS_PLUCKY, counter="ocarina",
        pad_secs={"breakdown"}, back={s: "rim" for s in ("hook", "bed", "lift", "return", "ending")},
        shaker_secs={"hook", "bed", "lift", "return", "ending"}, kick=True, pulse="shaker",
        mix=dict(lead=(.5, .1, .22), hint=(.3, .25, .35), comp=(.3, -.15, .22), bass=(.6, 0, .04),
                 counter=(.2, .35, .35), pad=(.08, 0, .4), shaker=(.1, .45, .12), back=(.18, -.3, .2),
                 kick=(.4, 0, .02), swell=(.05, 0, .5))),
    "b": dict(
        title="Ukulele & claps", lead="uke", lead2="bell", hint="bell", comp="uke",
        comp_fn=comp_strum, comp_range=(60, 76), bass="roundbass", bass_pat=BASS_PLUCKY, counter="bell",
        pad_secs={"breakdown"},
        back={"hook": "clap", "bed": "snap", "lift": "clap", "return": "clap", "ending": "clap"},
        shaker_secs={"hook", "lift", "return", "ending"}, kick=True, pulse="shaker",
        mix=dict(lead=(.45, .1, .22), lead2=(.12, .2, .3), hint=(.25, .25, .35), comp=(.3, -.2, .18),
                 bass=(.5, 0, .04), counter=(.18, .35, .35), pad=(.1, 0, .4), shaker=(.1, .45, .12),
                 back=(.2, -.3, .22), kick=(.35, 0, .02), swell=(.05, 0, .5))),
    "c": dict(
        title="Music box & felt piano", lead="musicbox", lead2=None, hint="musicbox", comp="felt",
        comp_fn=comp_felt, comp_range=(52, 72), bass="roundbass", bass_pat=BASS_LONG, counter="musicbox",
        pad_secs={"bed", "lift", "breakdown", "return", "ending", "final"},
        back={s: "tick" for s in ("hook", "bed", "lift", "return", "ending")},
        shaker_secs={"lift", "return"}, kick=False, pulse="tick",
        mix=dict(lead=(.4, .15, .35), hint=(.25, .3, .4), comp=(.4, -.1, .3), bass=(.4, 0, .05),
                 counter=(.18, .35, .4), pad=(.16, 0, .45), shaker=(.06, .4, .15), back=(.1, -.3, .2),
                 swell=(.05, 0, .5))),
}


def perc_hits(v, sec, bar):
    """(beat, instrument, velocity) for one bar."""
    if sec == "final":
        return []
    if sec == "breakdown":  # drums out, a soft eighth-note pulse keeps time
        return [(k / 2, v["pulse"], .22 if k % 2 else .14) for k in range(8)]
    hits = []
    if sec in v["shaker_secs"]:
        lvl = {"hook": .5, "bed": .38, "lift": .55, "return": .5, "ending": .5}[sec]
        hits += [(k / 2, "shaker", lvl * (1.0 if k % 2 else .65)) for k in range(8)]
    bk = v["back"][sec]
    bv = .4 if sec == "bed" else .6
    hits += [(1, bk, bv), (3, bk, bv)]
    if v["kick"] and sec != "bed":
        hits += [(0, "kick", .7), (2, "kick", .6)]
        if sec == "lift" and bar % 2:
            hits.append((2.5, "kick", .35))
    if bar == 36:  # little fill into the breakdown
        hits += [(2.5, bk, .3), (3.5, bk, .45)]
    if bar == 59:  # let the last bar breathe before the final chord
        hits = [h for h in hits if h[0] < 2]
    return hits


def build(key, v):
    rng = np.random.default_rng(SEED + ord(key))
    ev = []

    def add(t, inst, m, dur, vel, role, tight=False):
        if role not in ("pad", "swell"):  # humanize timing and velocity
            t += float(np.clip(rng.normal(0, .003 if tight else .005), -.012, .012))
            vel *= rng.uniform(.9, 1.05)
        ev.append((max(t, 0.0), inst, m, dur, vel, role))

    vcs = voicings(*v["comp_range"])
    held = {}  # pad notes tied across bars

    def flush_pad(keep):
        for m in list(held):
            if m not in keep:
                start = held.pop(m)
                add(start, "pad", m, cur - start + .1, .5, "pad")

    for bar in range(61):
        sec = section(bar)
        _, root = CHORDS[PROG[bar]]
        vc = vcs[bar]
        cur = tsec(bar)
        if sec == "final":
            add(cur, v["bass"], root, 1.2, .8, "bass")
        else:
            for b, d, off, vel in v["bass_pat"][sec]:
                add(tsec(bar, b), v["bass"], root + off, d * BEAT, vel, "bass")
        v["comp_fn"](add, bar, sec, vc, v["comp"])
        if sec in v["pad_secs"]:
            flush_pad(vc)
            for m in vc:
                held.setdefault(m, cur)
        else:
            flush_pad(())
        for b, inst, vel in perc_hits(v, sec, bar):
            add(tsec(bar, b), inst, 0, .1, vel, PERC_ROLE[inst])
    cur = tsec(60) + 1.2
    flush_pad(())

    def phrase(bar0, notes, inst, role, vel, shift=0):
        for b, d, m in notes:
            add(tsec(bar0, b), inst, m + shift, d * BEAT, vel, role, tight=True)

    def lead(bar0, notes, vel, shift=0):
        phrase(bar0, notes, v["lead"], "lead", vel, shift)
        if v["lead2"]:
            phrase(bar0, notes, v["lead2"], "lead2", vel, shift + 12)

    lead(0, CALL, .8)  # hook
    lead(2, ANSWER, .8)
    for bar0, frag in ((7, TAIL), (13, HEAD), (19, TAIL), (26, HEAD), (34, HEAD)):
        phrase(bar0, frag, v["hint"], "hint", .45, 12)  # sparse hints under the voice
    for bar in range(20, 36):
        phrase(bar, COUNTER[PROG[bar]], v["counter"], "counter", .4)
    for bar in range(37, 49, 2):
        phrase(bar, CURIOUS1 if PROG[bar] == "vi9" else CURIOUS2, v["hint"], "hint", .4, 12)
    add(tsec(48), "swell", 0, 2 * BAR, .5, "swell")
    phrase(50, CALL, v["hint"], "hint", .35, 12)  # warm reprise, up and soft
    phrase(52, ANSWER, v["hint"], "hint", .35, 12)
    lead(55, [(3, .5, 74), (3.5, .5, 76)], .55)  # pickup into the ending
    lead(56, CALL, .8)
    lead(58, END_ANSWER, .8)
    return ev


# ---------------------------------------------------------------- rendering
def make_ir(rng, rt60=1.6):
    t = tvec(2.2)
    pre = int(0.018 * SR)
    chans = []
    for _ in range(2):
        body = (filt(rng.standard_normal(len(t)), hi=2500) * np.exp(-6.91 * t / rt60)
                + 0.5 * filt(rng.standard_normal(len(t)), 2500, 9000) * np.exp(-6.91 * t / (rt60 * .45)))
        body[:pre] = 0
        body[pre:pre + 240] *= np.linspace(0, 1, 240)
        chans.append(body / np.sqrt(np.sum(body ** 2)))
    return chans


def render(events, mix, seed):
    rng = np.random.default_rng(seed)
    total = N + 3 * SR
    dry = np.zeros((2, total))
    send = np.zeros(total)
    cache = {}
    for t, inst, m, dur, vel, role in sorted(events):
        if inst in PERC:
            x = fade(PERC[inst](m, dur, rng), .001, .005)
        else:
            key = (inst, m, round(dur, 2))
            if key not in cache:
                x, rel = TONAL[inst](m, dur, rng)
                if rel:
                    x = release(x, dur, rel)
                cache[key] = x
            x = cache[key]
            last = LENGTH - 0.25 - t  # anything still ringing at the end decays in time
            if len(x) / SR > last:
                x = release(x, last - 1.5, 0.25)
            x = fade(x.copy())
        g, pan, s = mix[role]
        seg = x * vel * g
        i = int(round(t * SR))
        n = min(len(seg), total - i)
        a = (pan + 1) * np.pi / 4
        dry[0, i:i + n] += seg[:n] * np.cos(a)
        dry[1, i:i + n] += seg[:n] * np.sin(a)
        send[i:i + n] += seg[:n] * s
    ir = make_ir(rng)
    m = 1 << int(np.ceil(np.log2(total + len(ir[0]))))
    S = np.fft.rfft(send, m)
    wet = np.stack([np.fft.irfft(S * np.fft.rfft(h, m), m)[:total] for h in ir])
    x = (dry + 0.8 * wet)[:, :N]
    return np.stack([filt(c, lo=30, hi=11000) for c in x])  # gentle bus EQ


def compress(x, ratio=1.5):
    p = movavg((x.mean(0)) ** 2, 0.03 * SR)
    lvl = 10 * np.log10(p + 1e-12)
    thr = np.percentile(lvl[lvl > -70], 95) - 3
    gr = movavg(-np.maximum(0, lvl - thr) * (1 - 1 / ratio), 0.15 * SR)
    return x * 10 ** (gr / 20)


def limit(x, ceiling=CEILING):
    need = np.minimum(1, ceiling / np.maximum(np.abs(x).max(0), 1e-9))
    w = int(0.004 * SR)
    nb = -(-len(need) // w)
    blocks = np.pad(need, (0, nb * w - len(need)), constant_values=1).reshape(nb, w).min(1)
    padded = np.pad(blocks, 1, constant_values=1)
    blocks = np.minimum(np.minimum(padded[:-2], padded[1:-1]), padded[2:])
    g = np.repeat(blocks, w)[:len(need)]
    g = movavg(g, w)  # each sample's gain only averages windows that already cover it
    return np.clip(x * g, -ceiling, ceiling)


def write_wav(path, x):
    pcm = np.round(np.clip(x, -1, 1).T * 32767).astype("<i2")
    with wave.open(str(path), "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


def ebur128(path):
    r = subprocess.run([FFMPEG, "-hide_banner", "-nostats", "-i", str(path), "-af",
                        "ebur128=peak=true", "-f", "null", "-"], capture_output=True, text=True)
    s = r.stderr[r.stderr.rfind("Summary:"):]
    lufs = float(re.search(r"I:\s+(-?[\d.]+) LUFS", s).group(1))
    lra = float(re.search(r"LRA:\s+(-?[\d.]+) LU", s).group(1))
    tp = float(re.search(r"Peak:\s+(-?[\d.]+) dBFS", s).group(1))
    return lufs, lra, tp


def master(x, wav):
    x = compress(x)
    x = x / np.abs(x).max() * 0.5
    write_wav(wav, x)
    lufs = ebur128(wav)[0]
    for _ in range(3):  # gain to target, limit, re-measure (limiting costs a little loudness)
        x = limit(x * 10 ** ((TARGET_LUFS - lufs) / 20))
        write_wav(wav, x)
        lufs = ebur128(wav)[0]
        if abs(lufs - TARGET_LUFS) < 0.3:
            break
    k = int(0.4 * SR)  # safety only: the tail is already ~-50 dB here
    x[:, -k:] *= np.linspace(1, 0, k) ** 2
    x[:, :96] *= np.linspace(0, 1, 96)
    write_wav(wav, x)
    return x


# ---------------------------------------------------------------- checks
def db(v):
    return 20 * np.log10(max(v, 1e-12))


def check(x, wav):
    lufs, lra, tp = ebur128(wav)
    mono = x.mean(0)
    # a click is broadband: compare >12 kHz peaks at section boundaries with the rest of the track
    hf = np.abs(filt(mono, lo=12000, order=4))
    mask = np.zeros(len(hf), bool)
    for _, b in SECTIONS[1:]:
        mask[int((tsec(b) - .05) * SR):int((tsec(b) + .05) * SR)] = True
    near, elsewhere = hf[mask].max(), hf[~mask].max()
    spec = np.abs(np.fft.rfft(mono)) ** 2
    f = np.fft.rfftfreq(len(mono), 1 / SR)
    rms = {name: db(np.sqrt(np.mean(mono[int(tsec(s) * SR):int(tsec(e) * SR)] ** 2)))
           for (name, s), (_, e) in zip(SECTIONS[:-1], SECTIONS[1:])}
    res = dict(
        lufs=lufs, lra=lra, true_peak=tp, sample_peak=db(np.abs(x).max()),
        clipped=int(np.sum(np.abs(x) >= 0.999)), dc=float(np.abs(x.mean(1)).max()),
        boundary_hf_ratio=near / elsewhere,
        tail_last_100ms_db=db(np.sqrt(np.mean(mono[-SR // 10:] ** 2))),
        tail_139s_db=db(np.sqrt(np.mean(mono[int(139 * SR):int(139.5 * SR)] ** 2))),
        below_60hz_pct=100 * spec[f < 60].sum() / spec.sum(),
        above_12khz_pct=100 * spec[f > 12000].sum() / spec.sum(),
        section_rms_db=rms, seconds=x.shape[1] / SR)
    assert res["seconds"] == LENGTH and res["clipped"] == 0 and res["dc"] < 1e-3
    assert res["sample_peak"] <= -0.9 and res["tail_last_100ms_db"] < -50
    return res


# ---------------------------------------------------------------- page
DESCRIPTIONS = {
    "a": ("Marimba et pizzicato",
          "Lames de bois mates, basse pincée, shaker et rimshot discrets. Rebondissant et chaleureux, "
          "c'est le son le plus proche du « pop » mat du vinyle des créatures. Les notes de marimba "
          "s'éteignent vite : elles laissent de la place à la voix."),
    "b": ("Ukulélé et claquements",
          "Cordes pincées (Karplus-Strong) grattées en rythme, basse ronde, claquements de doigts sous "
          "la voix puis claquements de mains dans les passages plus vifs. Le plus enjoué et le plus "
          "« pub d'app » des trois, un peu plus chargé dans les médiums."),
    "c": ("Boîte à musique et piano feutré",
          "Piano feutré, nappe chaude, mélodie en boîte à musique, presque pas de percussions. Le plus "
          "tendre et le plus calme ; il soutient moins bien le passage « meet the family »."),
}


def mmss(s):
    return f"{int(s // 60)}:{s % 60:04.1f}"


def page(results):
    marks = [("Accroche", "hook", "motif de deux mesures, mélodie"),
             ("Tapis", "bed", "accords et rythme léger sous la voix, motif à peine évoqué"),
             ("Élan", "lift", "contre-chant et percussions en plus, « meet the family »"),
             ("Pause", "breakdown", "sans batterie, pulsation douce, un peu curieuse (« real coding session »)"),
             ("Retour", "return", "reprise chaleureuse"),
             ("Fin", "ending", "le motif une dernière fois, accord final sur la tonique (2:17,1) qui s'éteint seul")]
    starts = dict(SECTIONS)
    rows = "".join(
        f'<li><button data-t="{tsec(starts[k]):.2f}">{mmss(tsec(starts[k]))}</button>'
        f"<b>{label}</b> <span>{txt}</span></li>" for label, k, txt in marks)
    cards = ""
    for key, (name, desc) in DESCRIPTIONS.items():
        r = results[key]
        b64 = base64.b64encode((OUT / f"music_{key}.mp3").read_bytes()).decode()
        badge = '<span class="badge">Recommandée</span>' if key == "a" else ""
        cards += f"""
<section class="card{' reco' if key == 'a' else ''}">
  <h2>{key.upper()} · {name} {badge}</h2>
  <p>{desc}</p>
  <audio controls preload="metadata" src="data:audio/mpeg;base64,{b64}"></audio>
  <p class="meta">{r['lufs']:.1f} LUFS intégrés · crête vraie {r['true_peak']:.1f} dBTP · plage {r['lra']:.1f} LU · fichier <code>music_{key}.wav</code></p>
</section>"""
    return f"""<!doctype html>
<html lang="fr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Musique Fretlings</title>
<style>
:root {{ --cream:#FBF6EE; --vinyl:#EFE6D8; --cocoa:#4A3B33; --soft:#9A897C; --peach:#F2D3AE; --orange:#F1A661; }}
* {{ box-sizing:border-box; }}
body {{ margin:0; background:var(--cream); color:var(--cocoa); font:16px/1.55 system-ui,-apple-system,"Segoe UI",sans-serif; }}
main {{ max-width:760px; margin:0 auto; padding:40px 16px 64px; }}
h1 {{ font-size:1.8rem; margin:0 0 4px; }}
h2 {{ font-size:1.15rem; margin:0 0 6px; }}
.lead {{ color:var(--soft); margin:0 0 28px; }}
.card {{ background:var(--vinyl); border-radius:18px; padding:18px 20px; margin:0 0 16px; }}
.card.reco {{ box-shadow:0 0 0 2px var(--orange); }}
.badge {{ background:var(--orange); color:var(--cocoa); border-radius:999px; padding:1px 10px; font-size:.8rem; margin-left:6px; vertical-align:2px; }}
audio {{ width:100%; margin:6px 0 2px; }}
.meta {{ color:var(--soft); font-size:.85rem; margin:4px 0 0; }}
ol {{ list-style:none; padding:0; margin:0 0 28px; }}
li {{ display:flex; gap:12px; align-items:baseline; padding:6px 0; border-bottom:1px solid var(--peach); }}
li span {{ color:var(--soft); }}
button {{ font:inherit; font-variant-numeric:tabular-nums; border:0; border-radius:8px; background:var(--peach); color:var(--cocoa); padding:2px 8px; cursor:pointer; min-width:4.2em; }}
button:hover, button:focus-visible {{ background:var(--orange); outline:none; }}
.reco-text {{ border-left:4px solid var(--orange); padding:4px 0 4px 14px; margin:28px 0 0; }}
</style></head><body><main>
<h1>Musique de la vidéo</h1>
<p class="lead">Trois variantes de 2:20, composées et synthétisées entièrement par le script <code>compose_music.py</code> : aucun échantillon, aucune banque de sons, donc aucune question de licence. Ré majeur, 105 BPM.</p>
<h2>Découpage</h2>
<p class="meta">Un clic sur une heure place les trois lecteurs à cet endroit. Les sections tombent sur les mesures, à moins d'une seconde des repères demandés.</p>
<ol>{rows}</ol>
{cards}
<p class="reco-text"><b>Recommandation : A, marimba et pizzicato.</b> C'est le timbre qui ressemble le plus aux créatures : rond, mat, boisé, un peu drôle sans être enfantin. Ses notes brèves laissent des trous où la voix passe, même avant la baisse de 20 dB, et il s'accorde aux bruitages doux de l'app. B est une bonne seconde option si la vidéo doit paraître plus enjouée ; C convient mieux à une version plus posée.</p>
</main>
<script>
document.querySelectorAll("button[data-t]").forEach(b => b.addEventListener("click", () => {{
  document.querySelectorAll("audio").forEach(a => {{ a.currentTime = +b.dataset.t; }});
}}));
document.querySelectorAll("audio").forEach(a => a.addEventListener("play", () => {{
  document.querySelectorAll("audio").forEach(o => {{ if (o !== a) o.pause(); }});
}}));
</script>
</body></html>
"""


def main():
    results = {}
    for key, v in VARIANTS.items():
        wav = OUT / f"music_{key}.wav"
        x = master(render(build(key, v), v["mix"], SEED + ord(key)), wav)
        results[key] = check(x, wav)
        subprocess.run([FFMPEG, "-hide_banner", "-loglevel", "error", "-y", "-i", str(wav),
                        "-codec:a", "libmp3lame", "-b:a", "192k", str(OUT / f"music_{key}.mp3")], check=True)
        r = results[key]
        print(f"[{key}] {v['title']}: {r['lufs']:.1f} LUFS, LRA {r['lra']:.1f}, true peak {r['true_peak']:.1f} dBTP, "
              f"sample peak {r['sample_peak']:.2f} dBFS, DC {r['dc']:.1e}, boundary HF ratio "
              f"{r['boundary_hf_ratio']:.2f}, tail 139 s {r['tail_139s_db']:.0f} dB / last 100 ms "
              f"{r['tail_last_100ms_db']:.0f} dB, <60 Hz {r['below_60hz_pct']:.1f} %, >12 kHz "
              f"{r['above_12khz_pct']:.3f} %")
        print("     section RMS dB:", {k: round(val, 1) for k, val in r["section_rms_db"].items()})
    (OUT / "ecoute.html").write_text(page(results), encoding="utf-8")
    print("wrote", OUT / "ecoute.html")


if __name__ == "__main__":
    main()
