"""Makes the textures and sounds of the portal travel effects, all from scratch (no outside sources).

Writes to BrudvikWhiteHilt/Assets/PortalFx:
  RuneRing.png  512 x 512, white Elder Futhark ring with a soft glow on transparent; tinted in the game.
  Spark.png     64 x 64, soft white dot for the particles.
  Depart.wav    mono 16-bit 44.1 kHz: a rising shimmer and a whoosh that peaks at DEPART_PEAK seconds
                (PortalFxSettings FlashAt default), then a bright strike and a short tail.
  Arrive.wav    mono 16-bit 44.1 kHz: a soft thump, a falling shimmer and a bell-like chime.
And a preview of the ring on dark ground to %TEMP%\\wh_portalfx\\preview.png.

Usage: python AssetSource\\Tools\\make_portal_fx.py
"""
import math
import os
import pathlib
import tempfile
import wave

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = pathlib.Path(__file__).resolve().parent
OUT_DIR = HERE.parent.parent / "BrudvikWhiteHilt" / "Assets" / "PortalFx"
PREVIEW_DIR = pathlib.Path(tempfile.gettempdir()) / "wh_portalfx"

RING_SIZE = 512
SUPER = 4
RING_OUTER = 0.47     # share of the full size
RING_INNER = 0.35
RUNE_RADIUS = 0.41
RUNE_HEIGHT = 0.07
LINE = 0.009          # stroke width, share of the full size

RATE = 44100
DEPART_SECONDS = 2.3
DEPART_PEAK = 1.7
ARRIVE_SECONDS = 1.8

# Elder Futhark as strokes in a unit cell (x right, y up, 0..1); each rune is a list of polylines.
RUNES = [
    [[(0.3, 0), (0.3, 1)], [(0.3, 0.6), (0.75, 0.85)], [(0.3, 0.35), (0.75, 0.6)]],
    [[(0.3, 0), (0.3, 1), (0.7, 0.7), (0.7, 0)]],
    [[(0.35, 0), (0.35, 1)], [(0.35, 0.75), (0.7, 0.5), (0.35, 0.25)]],
    [[(0.3, 0), (0.3, 1)], [(0.3, 1), (0.7, 0.75)], [(0.3, 0.7), (0.7, 0.45)]],
    [[(0.3, 0), (0.3, 1), (0.7, 0.75), (0.3, 0.5), (0.7, 0)]],
    [[(0.65, 1), (0.35, 0.5), (0.65, 0)]],
    [[(0.25, 0), (0.75, 1)], [(0.75, 0), (0.25, 1)]],
    [[(0.3, 0), (0.3, 1), (0.7, 0.75), (0.3, 0.5)]],
    [[(0.3, 0), (0.3, 1)], [(0.7, 0), (0.7, 1)], [(0.3, 0.65), (0.7, 0.35)]],
    [[(0.5, 0), (0.5, 1)], [(0.3, 0.65), (0.7, 0.35)]],
    [[(0.5, 0), (0.5, 1)]],
    [[(0.45, 0.95), (0.25, 0.7), (0.45, 0.45)], [(0.55, 0.55), (0.75, 0.3), (0.55, 0.05)]],
    [[(0.5, 0), (0.5, 1)], [(0.5, 1), (0.72, 0.8)], [(0.5, 0), (0.28, 0.2)]],
    [[(0.3, 0), (0.3, 1)], [(0.3, 0.95), (0.65, 0.7), (0.75, 0.95)], [(0.3, 0.05), (0.65, 0.3), (0.75, 0.05)]],
    [[(0.5, 0), (0.5, 1)], [(0.5, 0.55), (0.2, 0.9)], [(0.5, 0.55), (0.8, 0.9)]],
    [[(0.65, 1), (0.35, 0.65), (0.65, 0.35), (0.35, 0)]],
    [[(0.5, 0), (0.5, 1)], [(0.5, 1), (0.2, 0.7)], [(0.5, 1), (0.8, 0.7)]],
    [[(0.3, 0), (0.3, 1), (0.7, 0.75), (0.3, 0.5), (0.7, 0.25), (0.3, 0)]],
    [[(0.25, 0), (0.25, 1), (0.5, 0.7), (0.75, 1), (0.75, 0)]],
    [[(0.25, 0), (0.25, 1), (0.75, 0.5)], [(0.75, 0), (0.75, 1), (0.25, 0.5)]],
    [[(0.35, 0), (0.35, 1), (0.7, 0.7)]],
    [[(0.5, 1), (0.75, 0.5), (0.5, 0), (0.25, 0.5), (0.5, 1)]],
    [[(0.2, 0), (0.2, 1), (0.8, 0), (0.8, 1), (0.2, 0)]],
    [[(0.2, 0), (0.75, 0.55), (0.5, 0.8), (0.25, 0.55), (0.8, 0)]],
]


def make_ring():
    size = RING_SIZE * SUPER
    half = size / 2
    width = max(1, round(LINE * size))
    image = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(image)
    for radius, scale in ((RING_OUTER, 1.0), (RING_OUTER - 0.012, 0.5), (RING_INNER, 1.0), (RING_INNER + 0.012, 0.5)):
        r = radius * size
        draw.ellipse([half - r, half - r, half + r, half + r], outline=255, width=max(1, round(width * scale)))

    cell = RUNE_HEIGHT * size
    for index, rune in enumerate(RUNES):
        angle = 2 * math.pi * index / len(RUNES)
        # The rune's up points away from the centre; its right follows the ring clockwise.
        up = (math.sin(angle), -math.cos(angle))
        right = (math.cos(angle), math.sin(angle))
        centre = (half + up[0] * RUNE_RADIUS * size, half + up[1] * RUNE_RADIUS * size)
        for line in rune:
            points = []
            for x, y in line:
                u = (x - 0.5) * cell * 0.75
                v = (y - 0.5) * cell
                points.append((centre[0] + right[0] * u + up[0] * v, centre[1] + right[1] * u + up[1] * v))
            draw.line(points, fill=255, width=width, joint="curve")

    sharp = image.resize((RING_SIZE, RING_SIZE), Image.LANCZOS)
    glow = sharp.filter(ImageFilter.GaussianBlur(6))
    alpha = np.maximum(np.asarray(sharp, dtype=np.float32), np.asarray(glow, dtype=np.float32) * 1.6)
    alpha = np.clip(alpha, 0, 255).astype(np.uint8)
    rgba = np.zeros((RING_SIZE, RING_SIZE, 4), dtype=np.uint8)
    rgba[..., :3] = 255
    rgba[..., 3] = alpha
    return Image.fromarray(rgba, "RGBA")


def make_spark():
    size = 64
    y, x = np.mgrid[0:size, 0:size]
    d = np.hypot(x - (size - 1) / 2, y - (size - 1) / 2) / (size / 2)
    alpha = np.clip(np.exp(-(d * 2.6) ** 2) + np.exp(-(d * 7.0) ** 2) * 0.6, 0, 1)
    alpha[d >= 1] = 0
    rgba = np.zeros((size, size, 4), dtype=np.uint8)
    rgba[..., :3] = 255
    rgba[..., 3] = (alpha * 255).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")


def one_pole(signal, cutoff):
    """Low-pass filter with a cutoff in Hz per sample."""
    out = np.empty_like(signal)
    state = 0.0
    k = 1.0 - np.exp(-2.0 * np.pi * np.asarray(cutoff, dtype=np.float64) / RATE)
    k = np.broadcast_to(k, signal.shape)
    for i in range(len(signal)):
        state += k[i] * (signal[i] - state)
        out[i] = state
    return out


def reverb(signal, mix=0.3):
    out = signal.copy()
    for delay_ms, gain in ((29.7, 0.72), (37.1, 0.7), (41.1, 0.68), (43.7, 0.66)):
        delay = int(RATE * delay_ms / 1000)
        comb = np.zeros(len(signal) + delay)
        for i in range(len(signal)):
            comb[i + delay] = signal[i] + comb[i] * gain
        out += comb[delay:delay + len(signal)] * mix / 4
    return out


def bell(t, start, base, decay, partials=((1.0, 1.0), (2.76, 0.5), (5.4, 0.25), (8.9, 0.12))):
    out = np.zeros_like(t)
    local = t - start
    on = local >= 0
    for ratio, gain in partials:
        out[on] += gain * np.sin(2 * np.pi * base * ratio * local[on]) * np.exp(-local[on] * decay * ratio ** 0.5)
    return out


def normalise(signal, peak_db=-3.0):
    peak = np.max(np.abs(signal))
    return signal / peak * 10 ** (peak_db / 20) if peak > 0 else signal


def fade(signal, seconds):
    n = int(RATE * seconds)
    signal[-n:] *= np.linspace(1, 0, n)
    return signal


def make_depart():
    rng = np.random.default_rng(7)
    t = np.arange(int(RATE * DEPART_SECONDS)) / RATE
    rise = np.clip(t / DEPART_PEAK, 0, 1)
    after = t > DEPART_PEAK

    # Shimmer: a few detuned partials gliding up an octave and a half, trembling faster as they rise.
    freq = 220 * 2 ** (rise * 1.6)
    phase = 2 * np.pi * np.cumsum(freq) / RATE
    tremolo = 0.6 + 0.4 * np.sin(2 * np.pi * np.cumsum(4 + rise * 14) / RATE)
    shimmer = sum(np.sin(phase * ratio + i) * gain for i, (ratio, gain) in enumerate(((1, 1), (1.5, 0.5), (2.01, 0.45), (3.02, 0.2))))
    shimmer *= tremolo * rise ** 1.5 * np.where(after, np.exp(-(t - DEPART_PEAK) * 9), 1)

    # Whoosh: noise opening up in brightness and loudness towards the peak, then cut.
    noise = rng.standard_normal(len(t))
    whoosh = one_pole(noise, 300 + 5000 * rise ** 2.5) * rise ** 3 * np.where(after, np.exp(-(t - DEPART_PEAK) * 14), 1)

    rumble = np.sin(2 * np.pi * 50 * t) * 0.5 * rise * np.where(after, np.exp(-(t - DEPART_PEAK) * 6), 1)
    strike = bell(t, DEPART_PEAK, 1320, 3.5) * 0.9
    signal = shimmer * 0.35 + whoosh * 1.8 + rumble * 0.4 + strike * 0.5
    return fade(normalise(reverb(signal, 0.35)), 0.15)


def make_arrive():
    rng = np.random.default_rng(11)
    t = np.arange(int(RATE * ARRIVE_SECONDS)) / RATE
    thump = np.sin(2 * np.pi * (45 + 40 * np.exp(-t * 20)) * t) * np.exp(-t * 7)
    dust = one_pole(rng.standard_normal(len(t)), 900) * np.exp(-t * 9)
    fall = 1320 * 2 ** (-np.clip(t / 0.6, 0, 1) * 1.6)
    shimmer = np.sin(2 * np.pi * np.cumsum(fall) / RATE) * np.exp(-t * 3.5)
    chime = bell(t, 0.05, 660, 2.2) + bell(t, 0.05, 990, 2.6) * 0.5
    signal = thump * 0.9 + dust * 1.2 + shimmer * 0.25 + chime * 0.45
    return fade(normalise(reverb(signal, 0.35)), 0.2)


def write_wav(path, signal):
    data = (np.clip(signal, -1, 1) * 32767).astype("<i2")
    with wave.open(str(path), "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(data.tobytes())


def preview(ring, spark):
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    ground = Image.new("RGBA", (RING_SIZE + 160, RING_SIZE), (40, 36, 30, 255))
    tint = Image.new("RGBA", ring.size, (140, 203, 255, 255))
    tinted = Image.composite(tint, Image.new("RGBA", ring.size, (0, 0, 0, 0)), ring.getchannel("A"))
    ground.alpha_composite(tinted, (0, 0))
    ground.alpha_composite(spark.resize((128, 128)), (RING_SIZE + 16, 16))
    ground.save(PREVIEW_DIR / "preview.png")


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    ring = make_ring()
    spark = make_spark()
    ring.save(OUT_DIR / "RuneRing.png")
    spark.save(OUT_DIR / "Spark.png")
    write_wav(OUT_DIR / "Depart.wav", make_depart())
    write_wav(OUT_DIR / "Arrive.wav", make_arrive())
    preview(ring, spark)
    for name in ("RuneRing.png", "Spark.png", "Depart.wav", "Arrive.wav"):
        print(f"{name}: {os.path.getsize(OUT_DIR / name)} bytes")
    print(f"preview: {PREVIEW_DIR / 'preview.png'}")


if __name__ == "__main__":
    main()
