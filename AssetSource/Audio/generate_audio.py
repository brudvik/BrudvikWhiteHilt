"""Makes the mod's recorded voices, effect sounds and music with ElevenLabs, as audio.json describes them.

    python AssetSource/Audio/generate_audio.py [--key-file <file with the API key>] [voices] [lines] [effects] [music]

voices   gives every speaker without a voice_id one from Voice Design (the first of its previews; the others are saved
         next to it in the previews folder to listen to) and writes the id back into audio.json.
lines    reads every speaker's lines, in English from the C# source and in Norwegian from Translations/Norwegian.json,
         and writes Voices/voice_<speaker>_<en|no>_<line>.mp3, the names WhiteHiltSound.VoiceClip looks for.
effects  writes Effects/<name>.mp3 from the prompts (<name>_2.mp3 and so on for an effect with variants).
creatures writes Creatures/<prefix><action>.mp3, <prefix><action>_2.mp3 ... for every monster's idle, alert, attack,
         hit and death (and its own attacks), the names MonsterRegistry.AddCreatureSounds looks for.
music    writes Music/<name>.mp3 from the prompts.
With no step named, all five run. Only what is new or changed is made again (cache.json keeps a hash of what each file
was made from), so running it again after changing a line costs only that line. The key is read from the file, or
from ELEVENLABS_API_KEY without one, and never written anywhere. Then build the audio bundle:
AssetSource/build_foraging_bundle.ps1 -AudioOnly.
"""
import argparse
import base64
import hashlib
import json
import os
import pathlib
import re
import sys
import time
import urllib.error
import urllib.request

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parent.parent
SOURCE = REPO / "BrudvikWhiteHilt"
NORWEGIAN = SOURCE / "Translations" / "Norwegian.json"
API = "https://api.elevenlabs.io"

# The biomes in the order of TradingPostGoods.Biomes; a trader speaker is its biome in lower case.
BIOMES = ["meadows", "blackforest", "swamp", "mountain", "plains", "mistlands", "ashlands", "deepnorth"]

LINE_SETS = {
    "trader": [f"whitehilt_trader_{kind}{n}" for kind, count in
               (("greet", 3), ("bye", 2), ("talk", 4), ("start", 2), ("buy", 2), ("sell", 2)) for n in range(1, count + 1)]
              + ["whitehilt_trader_closed"]
              + [f"whitehilt_trader_terror_greet{n}" for n in (1, 2)] + [f"whitehilt_trader_terror_talk{n}" for n in (1, 2, 3)],
    "hunter": [f"whitehilt_trader_hunter_{kind}{n}" for kind, count in
               (("greet", 2), ("bye", 2), ("talk", 4), ("start", 2), ("buy", 2), ("sell", 2)) for n in range(1, count + 1)],
    "skipper": [f"whitehilt_skipper_{kind}{n}" for kind in ("depart", "arrive", "wave") for n in (1, 2)],
    "raider": [f"whitehilt_raider_{kind}{n}" for kind in ("board", "attack", "taunt") for n in (1, 2, 3)],
    "whitehilt": [f"whitehilt_unlock_{tier}" for tier in
                  ("start", "blackforest", "swamp", "mountain", "plains", "mistlands", "ashlands", "deepnorth")]
                 + ["msg_whitehilt_bloodmoon_start", "msg_whitehilt_bloodmoon_end"]
                 + [f"msg_whitehilt_pressure_{n}" for n in (1, 2, 3)],
    "husvette": [f"whitehilt_husvette_{kind}{n}" for kind in ("home", "rest", "cold", "cosy", "storm", "shelter") for n in (1, 2, 3)],
    "munin": [f"whitehilt_munin_{kind}{n}" for kind in ("news", "idle") for n in (1, 2, 3)],
}

# Line names drop these prefixes, as WhiteHiltSound.LineName does.
LINE_PREFIXES = ("msg_whitehilt_", "whitehilt_trader_", "whitehilt_")

# The unlock texts are put together in ProgressionManager from the tier's name.
TIER_NAMES = {"start": "Start", "blackforest": "Black Forest", "swamp": "Swamp", "mountain": "Mountain", "plains": "Plains",
              "mistlands": "Mistlands", "ashlands": "Ashlands", "deepnorth": "Deep North"}

# What each of a monster's sounds is, from its description and voice; an action of its own is in its "custom".
ACTIONS = {
    "idle": ("{description} idling, low calm sounds: {voice}, slow breathing", 2.5),
    "alert": ("{description} spotting its prey: a loud aggressive {voice}", 3.0),
    "attack": ("{description} attacking: a short fierce {voice} with a heavy swing", 1.5),
    "attack2": ("{description} attacking again: a short fierce {voice}", 1.8),
    "bite": ("{description} biting: a fast snap and {voice}", 1.2),
    "lash": ("{description} lashing out: a whip crack and {voice}", 1.5),
    "hit": ("{description} getting hurt: a short pained {voice}", 1.0),
    "death": ("{description} dying: a long fading {voice} death cry and a heavy collapse", 3.5),
}

# The variants of a sound differ a little in what they ask for, besides the generator's own chance.
VARIANT_HINTS = ["", ", a different take, sharper and shorter", ", a different take, deeper and rougher"]

VOICE_SETTINGS = {"stability": 0.45, "similarity_boost": 0.8, "style": 0.25, "use_speaker_boost": True}

STRING = r'"((?:[^"\\]|\\.)*)"'


def english_texts():
    """Every English text the mod registers: AddEnglish("key", "text") and Dialogue("kind", "line", ...)."""
    texts = {}
    for path in SOURCE.rglob("*.cs"):
        if "obj" in path.parts or "bin" in path.parts:
            continue
        source = path.read_text(encoding="utf-8")
        for match in re.finditer(r'AddEnglish\(\s*' + STRING + r'\s*,\s*' + STRING + r'\s*\)', source):
            texts[match.group(1)] = unescape(match.group(2))
        for match in re.finditer(r'Dialogue\(\s*' + STRING + r'((?:\s*,\s*' + STRING + r')+)\s*\)', source):
            lines = re.findall(STRING, match.group(2))
            for number, line in enumerate(lines, 1):
                texts[f"whitehilt_trader_{match.group(1)}{number}"] = unescape(line)
    for tier, name in TIER_NAMES.items():
        texts.setdefault(f"whitehilt_unlock_{tier}", f"The White Hilt answers: {name} gear unlocked")
    return texts


def unescape(text):
    return text.replace('\\"', '"').replace("\\\\", "\\")


def monster_names(norwegian):
    """The plague of each biome, in English (TradingPostTerrors.cs) and Norwegian."""
    source = (SOURCE / "TradingPosts" / "TradingPostTerrors.cs").read_text(encoding="utf-8")
    found = re.findall(r'new\("(\w+)", "([^"]+)", "\w+", "\w+"', source)
    if len(found) != len(BIOMES):
        sys.exit(f"Expected {len(BIOMES)} plagues in TradingPostTerrors.cs, found {len(found)}")
    return [{"en": english, "no": norwegian.get(f"enemy_whitehilt_terror{key.lower()}", english)} for key, english in found]


def speaker_lines(speaker, english, norwegian, monsters):
    """(clip name, language, text) for each line a speaker says."""
    result = []
    for key in LINE_SETS[speaker["lines"]]:
        line = next(key[len(prefix):] for prefix in LINE_PREFIXES if key.startswith(prefix))
        for language, table in (("en", english), ("no", norwegian)):
            text = table.get(key)
            if not text:
                sys.exit(f"No {language} text for {key}")
            if "$1" in text:
                text = text.replace("$1", monsters[BIOMES.index(speaker["id"])][language])
            result.append((f"voice_{speaker['id']}_{language}_{line}", language, text))
    return result


class Client:
    def __init__(self, key):
        self.key = key

    def post(self, path, body, query=None):
        url = API + path + (("?" + "&".join(f"{k}={v}" for k, v in query.items())) if query else "")
        data = json.dumps(body).encode("utf-8")
        for attempt in range(5):
            request = urllib.request.Request(url, data=data, method="POST",
                                             headers={"xi-api-key": self.key, "Content-Type": "application/json"})
            try:
                with urllib.request.urlopen(request, timeout=300) as response:
                    return response.read(), response.headers.get("Content-Type", "")
            except urllib.error.HTTPError as error:
                detail = error.read().decode("utf-8", "replace")
                if error.code in (429, 500, 502, 503) and attempt < 4:
                    time.sleep(5 * (attempt + 1))
                    continue
                sys.exit(f"{path}: HTTP {error.code}: {detail[:400]}")
        sys.exit(f"{path}: gave up")

    def json(self, path, body, query=None):
        content, _ = self.post(path, body, query)
        return json.loads(content)


def make_voices(client, spec, english, previews):
    previews.mkdir(parents=True, exist_ok=True)
    for speaker in spec["speakers"]:
        if speaker.get("voice_id"):
            continue
        sample = " ".join(english[key] for key in LINE_SETS[speaker["lines"]][:6]).replace("$1", "the beast")
        while len(sample) < 120:
            sample += " " + sample
        designed = client.json("/v1/text-to-voice/design", {
            "voice_description": speaker["description"], "model_id": "eleven_multilingual_ttv_v2", "text": sample[:900]})
        options = designed["previews"]
        for number, preview in enumerate(options):
            (previews / f"{speaker['id']}_{number}.mp3").write_bytes(base64.b64decode(preview["audio_base_64"]))
        created = client.json("/v1/text-to-voice", {
            "voice_name": speaker["name"], "voice_description": speaker["description"],
            "generated_voice_id": options[speaker.get("preview", 0)]["generated_voice_id"]})
        speaker["voice_id"] = created["voice_id"]
        save_spec(spec)
        print(f"voice {speaker['id']}: {speaker['voice_id']}")


def make_lines(client, spec, english, norwegian, cache):
    out = HERE / "Voices"
    out.mkdir(exist_ok=True)
    monsters = monster_names(norwegian)
    made = 0
    for speaker in spec["speakers"]:
        if not speaker.get("voice_id"):
            sys.exit(f"{speaker['id']} has no voice yet: run the voices step first")
        for name, language, text in speaker_lines(speaker, english, norwegian, monsters):
            stamp = digest(speaker["voice_id"], spec["model"], json.dumps(VOICE_SETTINGS, sort_keys=True), text)
            target = out / f"{name}.mp3"
            if target.exists() and cache.get(name) == stamp:
                continue
            audio, _ = client.post(f"/v1/text-to-speech/{speaker['voice_id']}",
                                   {"text": text, "model_id": spec["model"], "voice_settings": VOICE_SETTINGS},
                                   {"output_format": spec["voice_format"]})
            target.write_bytes(audio)
            cache[name] = stamp
            save_cache(cache)
            made += 1
            print(f"{name}: {text}")
    print(f"lines: {made} made")


def make_effects(client, spec, cache):
    out = HERE / "Effects"
    out.mkdir(exist_ok=True)
    for effect in spec["effects"]:
        count = effect.get("variants", 1)
        for number, name in enumerate(variant_names(effect["name"], count)):
            prompt = effect["prompt"] + (VARIANT_HINTS[number % len(VARIANT_HINTS)] if count > 1 else "")
            if make_sound(client, spec, cache, out, name, prompt, effect["duration"], effect.get("loop", False)):
                print(f"effect {name}")


def variant_names(name, count):
    return [name if number == 0 else f"{name}_{number + 1}" for number in range(count)]


def make_sound(client, spec, cache, out, name, prompt, duration, loop=False):
    stamp = digest(prompt, str(duration), str(loop))
    target = out / f"{name}.mp3"
    if target.exists() and cache.get(name) == stamp:
        return False
    body = {"text": prompt, "duration_seconds": duration, "prompt_influence": 0.5}
    if loop:
        body["loop"] = True
    audio, _ = client.post("/v1/sound-generation", body, {"output_format": spec["effect_format"]})
    target.write_bytes(audio)
    cache[name] = stamp
    save_cache(cache)
    return True


def make_creatures(client, spec, cache):
    out = HERE / "Creatures"
    out.mkdir(exist_ok=True)
    count = spec.get("creature_variants", 1)
    made = 0
    for creature in spec["creatures"]:
        for action in creature["actions"]:
            template, duration = ACTIONS[action]
            base = creature.get("custom", {}).get(action) or template.format(**creature)
            for number, name in enumerate(variant_names(creature["prefix"] + action, count)):
                if make_sound(client, spec, cache, out, name, base + VARIANT_HINTS[number % len(VARIANT_HINTS)], duration):
                    made += 1
                    print(f"creature {name}")
    print(f"creatures: {made} made")


def make_music(client, spec, cache):
    out = HERE / "Music"
    out.mkdir(exist_ok=True)
    for piece in spec["music"]:
        stamp = digest(piece["prompt"], str(piece["length_ms"]))
        target = out / f"{piece['name']}.mp3"
        if target.exists() and cache.get(piece["name"]) == stamp:
            continue
        audio, _ = client.post("/v1/music", {"prompt": piece["prompt"], "music_length_ms": piece["length_ms"]},
                               {"output_format": spec["effect_format"]})
        target.write_bytes(audio)
        cache[piece["name"]] = stamp
        save_cache(cache)
        print(f"music {piece['name']}")


def digest(*parts):
    return hashlib.sha1("|".join(parts).encode("utf-8")).hexdigest()


def load_spec():
    return json.loads((HERE / "audio.json").read_text(encoding="utf-8"))


def save_spec(spec):
    (HERE / "audio.json").write_text(json.dumps(spec, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def load_cache():
    path = HERE / "cache.json"
    return json.loads(path.read_text(encoding="utf-8")) if path.exists() else {}


def save_cache(cache):
    (HERE / "cache.json").write_text(json.dumps(cache, indent=1, sort_keys=True) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("steps", nargs="*", choices=["voices", "lines", "effects", "creatures", "music"], default=[])
    parser.add_argument("--key-file", help="text file holding the ElevenLabs API key (default: ELEVENLABS_API_KEY)")
    parser.add_argument("--previews", default=str(pathlib.Path.home() / "whitehilt_voice_previews"),
                        help="folder for the Voice Design previews")
    parser.add_argument("--list", action="store_true", help="only print the lines and their length, make nothing")
    args = parser.parse_args()
    steps = args.steps or ["voices", "lines", "effects", "creatures", "music"]

    spec = load_spec()
    english = english_texts()
    norwegian = json.loads(NORWEGIAN.read_text(encoding="utf-8"))
    if args.list:
        monsters = monster_names(norwegian)
        total = 0
        for speaker in spec["speakers"]:
            for name, _, text in speaker_lines(speaker, english, norwegian, monsters):
                total += len(text)
                print(f"{name}: {text}")
        print(f"{total} characters")
        return

    key = (pathlib.Path(args.key_file).read_text(encoding="utf-8") if args.key_file else os.environ.get("ELEVENLABS_API_KEY", "")).strip()
    if not key:
        sys.exit("No API key: pass --key-file or set ELEVENLABS_API_KEY")
    client = Client(key)
    cache = load_cache()
    if "voices" in steps:
        make_voices(client, spec, english, pathlib.Path(args.previews))
    if "lines" in steps:
        make_lines(client, spec, english, norwegian, cache)
    if "effects" in steps:
        make_effects(client, spec, cache)
    if "creatures" in steps:
        make_creatures(client, spec, cache)
    if "music" in steps:
        make_music(client, spec, cache)


if __name__ == "__main__":
    main()
