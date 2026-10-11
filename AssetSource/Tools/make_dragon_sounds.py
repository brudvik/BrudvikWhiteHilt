"""Makes the dragon's five sounds in AssetSource/Sounds from CC0 recordings on OpenGameArt, made deeper and bigger.

Sources (all CC0, public domain; credited in the README all the same):
  monster_roar.wav   "CC0 Deep Monster Roar" by trazzz123   https://opengameart.org/content/cc0-deep-monster-roar
  dragonflap.wav     "Dragon Flap" by VishwaJai              https://opengameart.org/content/dragon-flap-0
  pack1/*.ogg        "80 CC0 creature SFX" by rubberduck     https://opengameart.org/content/80-cc0-creature-sfx
  pack2/*.ogg        "80 CC0 creture SFX #2" by rubberduck   https://opengameart.org/content/80-cc0-creture-sfx-2

Each sound is a few layers: a recording, slowed down (which also lowers its pitch, so a growl sounds bigger), placed at
an offset with a gain. The mix gets more low end, a short dark reverb, fades and a common peak level, and is written as
mono 16-bit 44.1 kHz WAV like the other monster sounds. The downloads are cached in the temp folder.

Usage: python make_dragon_sounds.py   (needs numpy, scipy and soundfile)
"""
import io
import pathlib
import tempfile
import urllib.request
import zipfile

import numpy as np
import soundfile
from scipy import signal

OUT = pathlib.Path(__file__).resolve().parents[1] / "Sounds"
CACHE = pathlib.Path(tempfile.gettempdir()) / "whitehilt_dragon_sources"
RATE = 44100
PEAK = 0.9
USER_AGENT = "BrudvikWhiteHilt-modding (github.com/brudvik/BrudvikWhiteHilt)"
DOWNLOADS = {
    "monster_roar.wav": "https://opengameart.org/sites/default/files/monster_roar.wav",
    "dragonflap.wav": "https://opengameart.org/sites/default/files/dragonflap.wav",
    "pack1": "https://opengameart.org/sites/default/files/80-CC0-creature-SFX_0.zip",
    "pack2": "https://opengameart.org/sites/default/files/80-CC0-creature-sfx-2.zip",
}

# Per sound: layers of (source, speed, offset in seconds, gain, longest part kept in seconds or None). A speed below 1
# plays the recording slower and deeper.
SOUNDS = {
    "dragonalert": [("monster_roar.wav", 0.85, 0.0, 1.0, 4.5)],
    "dragonidle": [("pack1/monster_04.ogg", 0.75, 0.0, 1.0, None), ("dragonflap.wav", 0.9, 0.15, 0.55, None)],
    "dragonattack": [("pack2/roar_04.ogg", 0.6, 0.0, 1.0, None), ("pack2/roar_06.ogg", 0.55, 0.06, 0.7, None)],
    "dragonhit": [("pack2/grunt_07.ogg", 0.7, 0.0, 1.0, None), ("pack2/grunt_09.ogg", 0.6, 0.04, 0.5, None)],
    "dragondeath": [("pack2/roar_05.ogg", 0.55, 0.0, 1.0, None), ("pack1/roar_02.ogg", 0.6, 0.1, 0.5, None),
                    ("pack1/monster_07.ogg", 0.5, 1.1, 0.85, None)],
}


def fetch():
    CACHE.mkdir(parents=True, exist_ok=True)
    for name, url in DOWNLOADS.items():
        target = CACHE / name
        if target.exists():
            continue
        request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
        data = urllib.request.urlopen(request, timeout=60).read()
        if url.endswith(".zip"):
            with zipfile.ZipFile(io.BytesIO(data)) as archive:
                target.mkdir()
                for member in archive.namelist():
                    if member.lower().endswith((".ogg", ".wav")):
                        (target / pathlib.Path(member).name).write_bytes(archive.read(member))
        else:
            target.write_bytes(data)
        print(f"downloaded {name}")


def load(name, speed, keep):
    data, rate = soundfile.read(str(CACHE / name), always_2d=True)
    mono = data.mean(axis=1)
    loud = np.flatnonzero(np.abs(mono) > 0.02 * np.abs(mono).max())
    margin = int(0.05 * rate)
    mono = mono[max(0, loud[0] - margin):loud[-1] + margin]
    if keep is not None:
        mono = mono[:int(keep * rate)]
    # Resampled to RATE / speed and then played at RATE: slower and lower by the same factor.
    up, down = int(round(RATE / speed)), rate
    divisor = np.gcd(up, down)
    return signal.resample_poly(mono, up // divisor, down // divisor)


def master(mix):
    low = signal.sosfilt(signal.butter(2, 250, "lowpass", fs=RATE, output="sos"), mix)
    mix = mix + 0.6 * low
    rng = np.random.default_rng(5)
    tail = int(0.7 * RATE)
    impulse = rng.normal(size=tail) * np.exp(-np.linspace(0, 6, tail))
    impulse = signal.sosfilt(signal.butter(2, 1800, "lowpass", fs=RATE, output="sos"), impulse)
    wet = signal.fftconvolve(mix, impulse)
    wet = np.pad(wet, (0, len(mix) + tail - len(wet)))
    mix = np.concatenate([mix, np.zeros(tail)]) + 0.18 * wet / np.abs(wet).max() * np.abs(mix).max()
    fade_in, fade_out = int(0.01 * RATE), int(0.25 * RATE)
    mix[:fade_in] *= np.linspace(0, 1, fade_in)
    mix[-fade_out:] *= np.linspace(1, 0, fade_out)
    return mix / np.abs(mix).max() * PEAK


def main():
    fetch()
    for name, layers in SOUNDS.items():
        parts = [(load(source, speed, keep), int(offset * RATE), gain) for source, speed, offset, gain, keep in layers]
        mix = np.zeros(max(len(audio) + start for audio, start, _ in parts))
        for audio, start, gain in parts:
            mix[start:start + len(audio)] += gain * audio / np.abs(audio).max()
        mix = master(mix)
        path = OUT / f"{name}.wav"
        soundfile.write(str(path), mix, RATE, subtype="PCM_16")
        print(f"{path.name}: {len(mix) / RATE:.2f} s")


if __name__ == "__main__":
    main()
