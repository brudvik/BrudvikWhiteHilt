"""Makes the Dragon Horn's call in AssetSource/Sounds/dragonhorn.wav from a CC0 recording of a real Viking horn.

Source (CC0, public domain; credited in the README all the same, as its maker asks):
  viking_horn.mp3   "Viking Horn in D.wav" by roemergruft   https://freesound.org/people/roemergruft/sounds/704440/
                    (Freesound's high-quality preview, which needs no login)

The steady part of the note is played by four horns at once: the recording itself, a second one a few cents flat and a
moment later (two players are never exactly together), one an octave below for weight, and one a fifth above that joins
after half a second, so the call opens like a fanfare. The mix gets a slow swell, a soft release, and a long, dark hall
reverb that keeps ringing after the note ends. Written as mono 16-bit 44.1 kHz WAV like the other sounds.

Usage: python make_dragon_horn_sound.py   (needs numpy, scipy and soundfile)
"""
import pathlib
import tempfile
import urllib.request

import numpy as np
import soundfile
from scipy import signal

OUT = pathlib.Path(__file__).resolve().parents[1] / "Sounds" / "dragonhorn.wav"
CACHE = pathlib.Path(tempfile.gettempdir()) / "whitehilt_horn_sources"
SOURCE = CACHE / "viking_horn.mp3"
URL = "https://cdn.freesound.org/previews/704/704440_3121495-hq.mp3"
USER_AGENT = "BrudvikWhiteHilt-modding (github.com/brudvik/BrudvikWhiteHilt)"
RATE = 44100
PEAK = 0.9

# Seconds of the recording used: the horn sounds from 0.6 s to 7.6 s; this takes its attack and steady middle.
NOTE_START = 0.55
NOTE_LENGTH = 3.6
SWELL = 0.35
RELEASE = 0.6

# Layers of (speed, offset in seconds, gain). A speed below 1 plays slower and lower; 2 ** (cents / 1200) detunes.
LAYERS = [
    (1.0, 0.0, 1.0),
    (2 ** (-9 / 1200), 0.035, 0.55),
    (0.5, 0.0, 0.45),
    (1.5, 0.5, 0.25),
]

REVERB_SECONDS = 2.8
REVERB_WET = 0.38


def fetch():
    CACHE.mkdir(parents=True, exist_ok=True)
    if not SOURCE.exists():
        request = urllib.request.Request(URL, headers={"User-Agent": USER_AGENT})
        SOURCE.write_bytes(urllib.request.urlopen(request, timeout=60).read())
        print(f"downloaded {SOURCE.name}")


def note():
    data, rate = soundfile.read(str(SOURCE), always_2d=True)
    mono = data.mean(axis=1)
    if rate != RATE:
        divisor = np.gcd(RATE, rate)
        mono = signal.resample_poly(mono, RATE // divisor, rate // divisor)
    return mono[int(NOTE_START * RATE):int((NOTE_START + NOTE_LENGTH) * RATE)]


def layer(audio, speed):
    # Resampled to 1 / speed of its length and played at RATE: slower and lower by the same factor. A slowed layer
    # only takes as much of the note as fits the original length, so every layer ends together.
    part = audio[:int(len(audio) * min(1.0, speed))]
    up, down = int(round(1000 / speed)), 1000
    divisor = np.gcd(up, down)
    return signal.resample_poly(part, up // divisor, down // divisor)


def envelope(audio):
    swell, release = int(SWELL * RATE), int(RELEASE * RATE)
    shaped = audio.copy()
    shaped[:swell] *= np.sin(np.linspace(0, np.pi / 2, swell)) ** 2
    shaped[-release:] *= np.cos(np.linspace(0, np.pi / 2, release)) ** 2
    return shaped


def hall(mix):
    rng = np.random.default_rng(11)
    tail = int(REVERB_SECONDS * RATE)
    impulse = rng.normal(size=tail) * np.exp(-np.linspace(0, 7, tail))
    impulse = signal.sosfilt(signal.butter(2, 2500, "lowpass", fs=RATE, output="sos"), impulse)
    impulse = np.concatenate([np.zeros(int(0.045 * RATE)), impulse])
    wet = signal.fftconvolve(mix, impulse)
    dry = np.pad(mix, (0, len(wet) - len(mix)))
    return dry + REVERB_WET * wet / np.abs(wet).max() * np.abs(mix).max()


def main():
    fetch()
    source = note()
    parts = [(envelope(layer(source, speed)), int(offset * RATE), gain) for speed, offset, gain in LAYERS]
    mix = np.zeros(max(len(audio) + start for audio, start, _ in parts))
    for audio, start, gain in parts:
        mix[start:start + len(audio)] += gain * audio / np.abs(audio).max()
    # A little more body below 200 Hz, as a big horn heard up close.
    mix = mix + 0.35 * signal.sosfilt(signal.butter(2, 200, "lowpass", fs=RATE, output="sos"), mix)
    mix = hall(mix)
    fade = int(0.3 * RATE)
    mix[-fade:] *= np.linspace(1, 0, fade)
    mix = mix / np.abs(mix).max() * PEAK
    soundfile.write(str(OUT), mix, RATE, subtype="PCM_16")
    print(f"{OUT.name}: {len(mix) / RATE:.2f} s")


if __name__ == "__main__":
    main()
