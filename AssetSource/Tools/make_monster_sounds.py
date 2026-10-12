"""Makes the sounds of the five new monsters (Mist Stag, Nidhogg Spawn, Yggdrasil Warden, Garm Hound, Greenland Shark)
in AssetSource/Sounds from CC0 (public domain) recordings, slowed down, layered and given a low end and a dark room.

Prefixes: miststag, nidhogg, warden, garm, shark; each gets idle, alert, attack, hit and death, and the Mist Stag and the
Nidhogg Spawn an attack2 as well. Output is mono 16-bit 44.1 kHz WAV, 0.4-4 s long, peak about 0.9, like the other
monster sounds.

Sources. All are CC0 1.0 (public domain); the licence was read on each sound's own page on 2026-10-12. Credited in the
README all the same. Freesound previews (hq mp3) are downloaded without a login.

  Freesound, https://freesound.org/people/<user>/sounds/<id>/  (licence: Creative Commons 0)
  653533  "Deer rut - Brame Cerf 02.wav"                 L.Finck           /people/L.Finck/sounds/653533/
  653532  "Deer rut - Brame Cerf .wav"                    L.Finck           /people/L.Finck/sounds/653532/
  475414  "Wood crack_1.wav"                              o_ciz             /people/o_ciz/sounds/475414/
  706977  "Angry snake hissing. OWI.wav"                  JesterWhoo        /people/JesterWhoo/sounds/706977/
  497056  "Low Monster Roar"                              Robson220pl       /people/Robson220pl/sounds/497056/
  825609  "CrocodilianTypeGrowl"                          Ovkovko           /people/Ovkovko/sounds/825609/
  734900  "Monster Guttural Growl, Dry"                   DeqstersLab       /people/DeqstersLab/sounds/734900/
  655707  "Wet Crunching 2"                               Zott820           /people/Zott820/sounds/655707/
  618082  "2022-02-06 wood creak 4.wav"                   sit_walk          /people/sit_walk/sounds/618082/
  618085  "2022-02-06 wood creak 1.wav"                   sit_walk          /people/sit_walk/sounds/618085/
  456814  "Creaking Wood"                                 Breviceps         /people/Breviceps/sounds/456814/
  501302  "Cracking wood"                                 JappeHallunken    /people/JappeHallunken/sounds/501302/
  584891  "big log on dirt throwing.wav"                  KrystianPawlowski /people/KrystianPawlowski/sounds/584891/
  139952  "A Tree Falling Down.wav"                       ecfike            /people/ecfike/sounds/139952/
  109359  "wood breaking.aiff"                            SoundCollectah    /people/SoundCollectah/sounds/109359/
  404920  "Dog Growl - Beast / Creature"                  coldvet           /people/coldvet/sounds/404920/
  829986  "Low dog growl"                                 randbsoundbites   /people/randbsoundbites/sounds/829986/
  342204  "Beast Snarling"                                Christopherderp   /people/Christopherderp/sounds/342204/
  181563  "Fire Crackling 01.wav"                         kingsrow          /people/kingsrow/sounds/181563/
  369589  "howl.wav"                                      taure             /people/taure/sounds/369589/
  336470  "Underwater_rumble_CsG.wav"                     csaszi            /people/csaszi/sounds/336470/
  867644  "Deep Ocean Rumble - Dark Sea Ambience"         bassimat          /people/bassimat/sounds/867644/
  442773  "Big Water Splash"                              qubodup           /people/qubodup/sounds/442773/
  816237  "Crunch"                                        qubodup           /people/qubodup/sounds/816237/
  837799  "Sea Creature Roar"                             Bikkit99          /people/Bikkit99/sounds/837799/

  OpenGameArt (licence: CC0), the same two packs as make_dragon_sounds.py
  pack1/*.ogg  "80 CC0 creature SFX" by rubberduck    https://opengameart.org/content/80-cc0-creature-sfx
  pack2/*.ogg  "80 CC0 creture SFX #2" by rubberduck  https://opengameart.org/content/80-cc0-creture-sfx-2

Each sound is a few layers: a recording, slowed down (which also lowers its pitch, so a growl sounds bigger), placed at
an offset with a gain, and optionally filtered. The mix gets a creature-specific low end, a short dark room (the shark's
is muffled, as if heard under water), fades and a common peak level. Downloads are cached in the temp folder.

Usage: python make_monster_sounds.py   (needs numpy, scipy and soundfile; prints a table and fails if a file is off)
"""
import io
import pathlib
import sys
import tempfile
import time
import urllib.error
import urllib.request
import zipfile

import numpy as np
import soundfile
from scipy import signal

OUT = pathlib.Path(__file__).resolve().parents[1] / "Sounds"
CACHE = pathlib.Path(tempfile.gettempdir()) / "whitehilt_monster_sources"
DRAGON_CACHE = pathlib.Path(tempfile.gettempdir()) / "whitehilt_dragon_sources"
RATE = 44100
PEAK = 0.9
MAX_SECONDS = 3.9
USER_AGENT = "BrudvikWhiteHilt-modding (github.com/brudvik/BrudvikWhiteHilt)"

# Source name -> (Freesound sound id, Freesound user id; the user id is in the waveform URL of the sound's page).
FREESOUND = {
    "stag_a": (653533, 14042309), "stag_b": (653532, 14042309), "crack": (475414, 7009965),
    "snake": (706977, 14781925), "lowroar": (497056, 4444010), "croc": (825609, 17675403),
    "gutt": (734900, 189858), "wet": (655707, 2558531), "creak4": (618082, 11204664),
    "creak1": (618085, 11204664), "creakb": (456814, 9159316), "crackwood": (501302, 10865671),
    "log": (584891, 13194852), "tree": (139952, 2337290), "woodbreak": (109359, 1868848),
    "dogbeast": (404920, 5965692), "doglow": (829986, 16968183), "snarl": (342204, 3908740),
    "fire": (181563, 1857065), "howl": (369589, 2940947), "rumble": (336470, 1147184),
    "ocean": (867644, 15636277), "splash": (442773, 71257), "crunch": (816237, 71257),
    "sea": (837799, 16586370),
}
PACKS = {
    "pack1": "https://opengameart.org/sites/default/files/80-CC0-creature-SFX_0.zip",
    "pack2": "https://opengameart.org/sites/default/files/80-CC0-creature-sfx-2.zip",
}


def L(source, speed=1.0, at=0.0, gain=1.0, keep=None, start=0.0, low=None, high=None):
    """One layer. speed < 1 plays slower and deeper; start is where the part begins in the recording (seconds of the
    original, after leading silence is cut) and keep how long it is in the result (so it takes keep * speed of the
    original); at is where it lands in the mix; low/high are low-pass / high-pass Hz."""
    return dict(source=source, speed=speed, at=at, gain=gain, keep=keep, start=start, low=low, high=high)


# Per creature: bass = how much of the < 250 Hz part is added again, drive = soft clipping for a hoarse edge,
# muffle = low-pass over the whole mix (the shark is heard under water), room = (seconds, damping Hz, wet level).
VOICES = {
    "miststag": dict(bass=0.5, drive=0.0, muffle=None, room=(1.3, 2600, 0.24)),
    "nidhogg": dict(bass=0.9, drive=0.0, muffle=None, room=(0.9, 1300, 0.20)),
    "warden": dict(bass=0.7, drive=0.0, muffle=None, room=(0.5, 2200, 0.12)),
    "garm": dict(bass=0.45, drive=1.1, muffle=None, room=(0.5, 3200, 0.12)),
    "shark": dict(bass=0.8, drive=0.0, muffle=1300, room=(1.1, 900, 0.30)),
}

# (file name without .wav): layers. The voice is the part before the last word.
SOUNDS = {
    # Mist Stag: a wavering rut roar slowed down, breath, antler clatter for the charge, a thud for the stomp.
    "miststagidle": [L("stag_a", 0.8, 0.0, 1.0, keep=2.6, start=2.3),
                     L("pack1/breath.ogg", 0.7, 0.2, 0.45, high=300)],
    "miststagalert": [L("stag_b", 0.7, 0.0, 1.0, keep=3.3, start=0.8),
                      L("stag_a", 0.5, 0.15, 0.6, keep=3.0, start=2.3),
                      L("pack1/breath.ogg", 0.6, 0.0, 0.3, high=300)],
    "miststagattack": [L("stag_b", 0.9, 0.0, 1.0, keep=1.3, start=36.5),
                       L("crack", 1.2, 0.04, 0.8, keep=0.6, start=7.6, low=5000),
                       L("crack", 1.05, 0.2, 0.6, keep=0.3, start=14.8, low=5000),
                       L("stag_a", 0.6, 0.0, 0.5, keep=1.5, start=13.3)],
    "miststagattack2": [L("pack2/stomp_01.ogg", 0.7, 0.0, 1.0), L("log", 0.8, 0.02, 0.7, keep=1.2, start=3.4),
                        L("stag_a", 0.65, 0.1, 0.55, keep=1.2, start=29.7)],
    "miststaghit": [L("pack2/grunt_06.ogg", 0.8, 0.0, 1.0), L("stag_b", 1.0, 0.0, 0.6, keep=0.8, start=11.2)],
    "miststagdeath": [L("stag_b", 0.6, 0.0, 1.0, keep=3.0, start=36.5), L("pack2/die_01.ogg", 0.7, 0.4, 0.5),
                      L("pack1/breath.ogg", 0.5, 2.0, 0.4, high=300)],
    # Nidhogg Spawn: reptile hiss and croc growl, a huge low roar, wet snaps, a crushing blow.
    "nidhoggidle": [L("snake", 0.7, 0.0, 0.8, keep=3.0, start=2.7), L("croc", 0.6, 0.3, 0.55, keep=2.0, start=0.6)],
    "nidhoggalert": [L("lowroar", 0.75, 0.0, 1.0, keep=3.4), L("gutt", 0.6, 0.15, 0.7, keep=2.5, start=0.15),
                     L("snake", 0.6, 0.0, 0.4, keep=2.5, start=2.7)],
    "nidhoggattack": [L("wet", 0.8, 0.0, 0.9, keep=0.7, start=0.05), L("snake", 1.0, 0.0, 0.8, keep=0.8, start=2.7),
                      L("croc", 0.8, 0.1, 0.6, keep=1.0, start=0.6), L("pack2/slime_03.ogg", 0.9, 0.05, 0.5)],
    "nidhoggattack2": [L("pack2/stomp_01.ogg", 0.55, 0.0, 1.0), L("lowroar", 0.6, 0.05, 0.8, keep=1.6),
                       L("log", 0.6, 0.0, 0.6, keep=1.2, start=3.4)],
    "nidhogghit": [L("pack2/monster_09.ogg", 0.8, 0.0, 1.0), L("croc", 1.0, 0.0, 0.6, keep=0.8, start=0.6)],
    "nidhoggdeath": [L("lowroar", 0.5, 0.0, 1.0, keep=3.2), L("pack2/die_03.ogg", 0.6, 0.6, 0.6),
                     L("snake", 0.5, 1.5, 0.45, keep=2.0, start=2.7)],
    # Yggdrasil Warden: groaning wood, cracking branches, a heavy wooden thud.
    "wardenidle": [L("creak4", 0.7, 0.0, 1.0, keep=3.0), L("creakb", 0.6, 0.5, 0.7, keep=2.5),
                   L("lowroar", 0.45, 0.3, 0.3, keep=2.5, low=400)],
    "wardenalert": [L("creakb", 0.55, 0.0, 1.0, keep=3.0), L("tree", 0.8, 0.3, 0.8, keep=2.4),
                    L("lowroar", 0.5, 0.0, 0.6, keep=3.0)],
    "wardenattack": [L("log", 0.6, 0.0, 1.0, keep=1.6, start=3.4), L("crackwood", 0.8, 0.0, 0.7, keep=1.0),
                     L("pack2/stomp_01.ogg", 0.5, 0.0, 0.8)],
    "wardenhit": [L("crackwood", 1.0, 0.0, 1.0, keep=1.0), L("woodbreak", 1.0, 0.0, 0.7, keep=0.8)],
    "wardendeath": [L("tree", 0.8, 0.0, 1.0, keep=3.4), L("woodbreak", 0.7, 0.4, 0.7, keep=2.0),
                    L("creak1", 0.5, 0.2, 0.6, keep=3.0)],
    # Garm Hound: snarling dog with a hoarse, burning edge, a howl, fire crackle, a yelp.
    "garmidle": [L("doglow", 0.8, 0.0, 1.0), L("doglow", 0.7, 1.1, 0.8),
                 L("fire", 1.0, 0.0, 0.35, keep=2.6, start=28.5, high=500)],
    "garmalert": [L("howl", 0.85, 0.0, 1.0, keep=3.4), L("dogbeast", 0.8, 0.0, 0.5, keep=2.5, start=26.1),
                  L("fire", 1.0, 0.5, 0.3, keep=2.5, start=28.5, high=500)],
    "garmattack": [L("snarl", 0.9, 0.0, 1.0, keep=1.2), L("dogbeast", 0.8, 0.0, 0.8, keep=1.2, start=33.0),
                   L("pack1/barking_01.ogg", 0.7, 0.0, 0.5),
                   L("fire", 1.0, 0.1, 0.4, keep=1.0, start=28.5, high=500)],
    "garmhit": [L("pack1/hurt_02.ogg", 0.85, 0.0, 1.0), L("pack1/barking_02.ogg", 0.8, 0.0, 0.5)],
    "garmdeath": [L("pack1/hurt_04.ogg", 0.7, 0.0, 1.0), L("howl", 1.1, 0.3, 0.6, keep=2.0, start=5.6),
                  L("dogbeast", 0.6, 0.7, 0.5, keep=1.8, start=19.8)],
    # Greenland Shark: muffled deep rumble, splash, a crunching bite; the whole mix sounds under water.
    "sharkidle": [L("rumble", 0.8, 0.0, 0.6, keep=3.0, start=18.2), L("sea", 0.5, 0.0, 0.8, keep=3.0, start=2.6)],
    "sharkalert": [L("sea", 0.55, 0.0, 1.0, keep=3.4, start=2.6), L("ocean", 0.8, 0.0, 0.35, keep=3.4, start=68.5)],
    "sharkattack": [L("crunch", 0.8, 0.1, 1.0, keep=1.0), L("splash", 1.0, 0.0, 0.8, keep=1.6),
                    L("wet", 0.7, 0.05, 0.6, keep=1.0, start=0.05)],
    "sharkhit": [L("splash", 1.0, 0.0, 0.9, keep=1.0), L("sea", 0.8, 0.0, 0.6, keep=1.0, start=2.6)],
    "sharkdeath": [L("sea", 0.45, 0.0, 1.0, keep=3.4, start=2.6), L("ocean", 0.8, 0.0, 0.35, keep=3.4, start=197.1),
                   L("splash", 0.9, 0.3, 0.6, keep=1.5)],
}


def download(url):
    for attempt in range(5):
        request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
        try:
            return urllib.request.urlopen(request, timeout=60).read()
        except urllib.error.HTTPError as error:
            if error.code not in (429, 503):
                raise
            time.sleep(10 * (attempt + 1))
    raise RuntimeError(f"could not download {url}")


def fetch():
    CACHE.mkdir(parents=True, exist_ok=True)
    for name, (sound_id, user_id) in FREESOUND.items():
        target = CACHE / f"{name}.mp3"
        if target.exists():
            continue
        url = f"https://cdn.freesound.org/previews/{sound_id // 1000}/{sound_id}_{user_id}-hq.mp3"
        target.write_bytes(download(url))
        print(f"downloaded {name} ({url})")
        time.sleep(2)
    for name, url in PACKS.items():
        target = CACHE / name
        if target.exists():
            continue
        if (DRAGON_CACHE / name).exists():
            # The dragon script already downloaded the pack; reuse it.
            target.mkdir()
            for file in (DRAGON_CACHE / name).iterdir():
                (target / file.name).write_bytes(file.read_bytes())
            continue
        with zipfile.ZipFile(io.BytesIO(download(url))) as archive:
            target.mkdir()
            for member in archive.namelist():
                if member.lower().endswith((".ogg", ".wav")):
                    (target / pathlib.Path(member).name).write_bytes(archive.read(member))
        print(f"downloaded {name}")


def path_of(source):
    return CACHE / (source if "/" in source else f"{source}.mp3")


def read_trimmed(source):
    data, rate = soundfile.read(str(path_of(source)), always_2d=True)
    mono = data.mean(axis=1)
    loud = np.flatnonzero(np.abs(mono) > 0.02 * np.abs(mono).max())
    margin = int(0.03 * rate)
    return mono[max(0, loud[0] - margin):loud[-1] + margin], rate


def load(layer):
    mono, rate = read_trimmed(layer["source"])
    begin = int(layer["start"] * rate)
    end = len(mono) if layer["keep"] is None else begin + int(layer["keep"] * layer["speed"] * rate)
    mono = mono[begin:end]
    # Resampled to RATE / speed and then played at RATE: slower and lower by the same factor.
    up, down = int(round(RATE / layer["speed"])), rate
    divisor = np.gcd(up, down)
    audio = signal.resample_poly(mono, up // divisor, down // divisor)
    if layer["high"]:
        audio = signal.sosfilt(signal.butter(2, layer["high"], "highpass", fs=RATE, output="sos"), audio)
    if layer["low"]:
        audio = signal.sosfilt(signal.butter(2, layer["low"], "lowpass", fs=RATE, output="sos"), audio)
    # Short fades so a cut part does not click.
    fade = min(int(0.03 * RATE), len(audio) // 4)
    audio[:fade] *= np.linspace(0, 1, fade)
    audio[-fade:] *= np.linspace(1, 0, fade)
    return audio / np.abs(audio).max()


def master(mix, voice):
    low = signal.sosfilt(signal.butter(2, 250, "lowpass", fs=RATE, output="sos"), mix)
    mix = mix + voice["bass"] * low
    if voice["drive"]:
        mix = np.tanh(voice["drive"] * mix / np.abs(mix).max())
    if voice["muffle"]:
        mix = signal.sosfilt(signal.butter(3, voice["muffle"], "lowpass", fs=RATE, output="sos"), mix)
    seconds, damping, wet_level = voice["room"]
    rng = np.random.default_rng(5)
    tail = int(seconds * RATE)
    impulse = rng.normal(size=tail) * np.exp(-np.linspace(0, 6, tail))
    impulse = signal.sosfilt(signal.butter(2, damping, "lowpass", fs=RATE, output="sos"), impulse)
    wet = signal.fftconvolve(mix, impulse)
    wet = np.pad(wet, (0, len(mix) + tail - len(wet)))
    mix = np.concatenate([mix, np.zeros(tail)]) + wet_level * wet / np.abs(wet).max() * np.abs(mix).max()
    mix = mix[:int(MAX_SECONDS * RATE)]
    fade_in, fade_out = int(0.01 * RATE), int(0.3 * RATE)
    mix[:fade_in] *= np.linspace(0, 1, fade_in)
    mix[-fade_out:] *= np.linspace(1, 0, fade_out)
    return mix / np.abs(mix).max() * PEAK


def make(name, layers):
    voice = VOICES[next(prefix for prefix in VOICES if name.startswith(prefix))]
    parts = [(load(layer), int(layer["at"] * RATE), layer["gain"]) for layer in layers]
    mix = np.zeros(max(len(audio) + start for audio, start, _ in parts))
    for audio, start, gain in parts:
        mix[start:start + len(audio)] += gain * audio
    mix = master(mix, voice)
    soundfile.write(str(OUT / f"{name}.wav"), mix, RATE, subtype="PCM_16")


def check():
    """Reads every file back from disk and prints a table; returns False when one is off."""
    print(f"{'file':<22}{'seconds':>8}{'peak':>7}{'rms dB':>8}  format")
    good = True
    for name in SOUNDS:
        path = OUT / f"{name}.wav"
        info = soundfile.info(str(path))
        data, rate = soundfile.read(str(path))
        seconds = len(data) / rate
        peak = float(np.abs(data).max())
        rms = 20 * np.log10(max(float(np.sqrt(np.mean(data ** 2))), 1e-9))
        ok = (info.channels == 1 and info.subtype == "PCM_16" and rate == RATE and 0.4 <= seconds <= 4.0
              and 0.85 <= peak <= 0.95 and rms > -40)
        good = good and ok
        print(f"{name:<22}{seconds:>8.2f}{peak:>7.2f}{rms:>8.1f}  mono {info.subtype} {rate} {'ok' if ok else 'FAIL'}")
    return good


def main():
    fetch()
    if "--info" in sys.argv:
        for source in sorted({layer["source"] for layers in SOUNDS.values() for layer in layers}):
            mono, rate = read_trimmed(source)
            print(f"{source:<22}{len(mono) / rate:>7.2f} s at {rate} Hz")
        return
    for name, layers in SOUNDS.items():
        make(name, layers)
    if not check():
        sys.exit(1)


if __name__ == "__main__":
    main()
