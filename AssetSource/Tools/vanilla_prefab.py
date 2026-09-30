"""Shows how a vanilla Valheim prefab is built, read straight from the game's bundles with UnityPy.

Prints the hierarchy with active flags, local position, rotation (Euler) and scale, mesh names and bounds, materials
and components (MonoBehaviours with their script class, or their first fields when the script lives in another file).
With --materials, also each material's shader, keywords, textures and colours. With --values, the field values of every
MonoBehaviour (numbers, flags, names of referenced prefabs; an item's shared data and attack), and each Animator's
controller with its states, parameters and clips. With --hash, Valheim's GetStableHashCode of names (ZDO keys, prefab
hashes in logs).

Usage:
  python vanilla_prefab.py <prefab> [...] [--bundle <file name>] [--materials] [--values] [--all-bundles]
  python vanilla_prefab.py --hash <name> [...]
Item prefabs live in SoftRef bundle c4210710, which is searched first; --all-bundles also searches the rest (slow,
a minute or two). Found bundles are cached in BrudvikWhiteHiltUnity/Preview/prefab_bundles.json.
"""
import argparse
import json
import math
import os
import pathlib

import UnityPy

GAME = pathlib.Path(r"C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\StreamingAssets")
ITEM_BUNDLE = GAME / "SoftRef" / "Bundles" / "c4210710"
CACHE = pathlib.Path(__file__).resolve().parents[2] / "BrudvikWhiteHiltUnity" / "Preview"
SKIP_FIELDS = {"m_GameObject", "m_Enabled", "m_Script", "m_Name", "m_EditorHideFlags", "m_EditorClassIdentifier"}

environments = {}
cab_index = None


def stable_hash(text):
    """Valheim's string.GetStableHashCode."""
    def wrap(value):
        value &= 0xFFFFFFFF
        return value - (1 << 32) if value >= 1 << 31 else value

    first = second = 5381
    for i in range(0, len(text), 2):
        first = wrap(((first << 5) + first) ^ ord(text[i]))
        if i == len(text) - 1:
            break
        second = wrap(((second << 5) + second) ^ ord(text[i + 1]))
    return wrap(first + second * 1566083941)


def load(path):
    key = str(path)
    if key not in environments:
        environments[key] = UnityPy.load(key)
    return environments[key]


def cabs():
    """Maps every serialized file (CAB-...) to the bundle holding it, for references across bundles."""
    global cab_index
    if cab_index is None:
        cache = CACHE / "cabmap.json"
        if cache.exists():
            cab_index = json.loads(cache.read_text())
        else:
            cab_index = {}
            for path in GAME.rglob("*"):
                if path.is_file():
                    try:
                        for name in UnityPy.load(str(path)).files:
                            cab_index[name.lower()] = str(path)
                    except Exception:
                        continue
            CACHE.mkdir(parents=True, exist_ok=True)
            cache.write_text(json.dumps(cab_index))
    return cab_index


def follow(owner, pointer):
    """Reads what a PPtr points to, also in another bundle."""
    file_id, path_id = pointer["m_FileID"], pointer["m_PathID"]
    if path_id == 0:
        return None
    if file_id == 0:
        return owner.objects[path_id]
    cab = owner.externals[file_id - 1].path.lower().split("/")[-1]
    bundle = cabs().get(cab)
    if bundle is None:
        return None
    serialized = next(v for k, v in load(bundle).files.items() if k.lower() == cab)
    return serialized.objects[path_id]


def euler(q):
    x, y, z, w = q.x, q.y, q.z, q.w
    ex = math.degrees(math.asin(max(-1.0, min(1.0, 2 * (w * x - y * z)))))
    ey = math.degrees(math.atan2(2 * (w * y + x * z), 1 - 2 * (x * x + y * y)))
    ez = math.degrees(math.atan2(2 * (w * z + x * y), 1 - 2 * (x * x + z * z)))
    return [round(ex, 1), round(ey, 1), round(ez, 1)]


def vector(v):
    return [round(v.x, 3), round(v.y, 3), round(v.z, 3)]


def describe_material(pointer_owner, pointer, depth):
    target = follow(pointer_owner, pointer)
    if target is None:
        return
    material = target.read_typetree()
    properties = material["m_SavedProperties"]
    textures = [name for name, value in properties["m_TexEnvs"] if value["m_Texture"]["m_PathID"] != 0]
    colours = {name: tuple(round(value[c], 2) for c in "rgba") for name, value in properties["m_Colors"]}
    shader = "?"
    try:
        shader_object = follow(target.assets_file, material["m_Shader"])
        shader = shader_object.read_typetree().get("m_ParsedForm", {}).get("m_Name", "?") if shader_object else "?"
    except Exception:
        pass
    indent = "  " * depth + "    "
    print(f"{indent}material {material['m_Name']} shader={shader}")
    print(f"{indent}  keywords={material.get('m_ValidKeywords') or material.get('m_ShaderKeywords')}")
    print(f"{indent}  textures={textures}")
    print(f"{indent}  colours={colours}")


def walk(transform, depth, materials, values=False):
    game_object = transform.m_GameObject.read()
    notes = []
    renderers = []
    for component in game_object.m_Components:
        pointer = component.component if hasattr(component, "component") else component
        owner = pointer.assetsfile if hasattr(pointer, "assetsfile") else pointer.assets_file
        kind = pointer.type.name
        try:
            if kind == "MeshFilter":
                mesh = pointer.read().m_Mesh.read()
                box = mesh.m_LocalAABB
                notes.append(f"Mesh[{mesh.m_Name} c={vector(box.m_Center)} e={vector(box.m_Extent)}]")
            elif kind in ("MeshRenderer", "SkinnedMeshRenderer"):
                tree = pointer.read_typetree()
                names = []
                for material_pointer in tree["m_Materials"]:
                    target = follow(owner, material_pointer)
                    names.append(target.read().m_Name if target else "?")
                if kind == "SkinnedMeshRenderer":
                    notes.append(f"SMR[bones={len(tree['m_Bones'])}]")
                notes.append(f"{'MR' if kind == 'MeshRenderer' else 'SMR'}{names}")
                renderers.append((owner, tree["m_Materials"]))
            elif kind == "MonoBehaviour":
                tree = pointer.read_typetree()
                script = None
                try:
                    script_object = follow(owner, tree["m_Script"])
                    script = script_object.read().m_ClassName if script_object else None
                except Exception:
                    pass
                fields = [k for k in tree if k not in SKIP_FIELDS][:5]
                notes.append(script or "MB{" + ",".join(fields) + "}")
            elif kind == "BoxCollider":
                box = pointer.read()
                notes.append(f"Box[c={vector(box.m_Center)} s={vector(box.m_Size)}]")
            elif kind != "Transform":
                notes.append(kind)
        except Exception:
            notes.append(kind + "!")
    print("  " * depth + f"{game_object.m_Name} act={int(game_object.m_IsActive)} layer={game_object.m_Layer} "
          f"p={vector(transform.m_LocalPosition)} r={euler(transform.m_LocalRotation)} s={vector(transform.m_LocalScale)} {' '.join(notes)}")
    if materials:
        for owner, pointers in renderers:
            for pointer in pointers:
                describe_material(owner, pointer, depth)
    if values:
        describe_values(game_object, depth)
    for child in transform.m_Children:
        walk(child.read(), depth + 1, materials, values)


def short(owner, value, depth=0):
    """A field value made readable: references become names, zero damage and long lists are left out."""
    if isinstance(value, dict) and "m_PathID" in value:
        if value["m_PathID"] == 0:
            return None
        try:
            target = follow(owner, value)
            return target.read().m_Name if target else "?"
        except Exception:
            return "?"
    if isinstance(value, dict):
        if depth > 1:
            return "{...}"
        shown = {key: short(owner, item, depth + 1) for key, item in value.items() if item not in (0, 0.0, "", None, [])}
        return shown or None
    if isinstance(value, list):
        if len(value) > 12 or depth > 1:
            return f"[{len(value)} items]"
        return [short(owner, item, depth + 1) for item in value]
    if isinstance(value, float):
        return round(value, 3)
    return value


def describe_values(game_object, depth):
    indent = "  " * depth + "    "
    for component in game_object.m_Components:
        pointer = component.component if hasattr(component, "component") else component
        owner = pointer.assetsfile if hasattr(pointer, "assetsfile") else pointer.assets_file
        try:
            if pointer.type.name == "MonoBehaviour":
                tree = pointer.read_typetree()
                if "m_itemData" in tree:
                    shared = tree["m_itemData"]["m_shared"]
                    print(f"{indent}item {short(owner, {k: v for k, v in shared.items() if k != 'm_attack' and k != 'm_secondaryAttack'})}")
                    print(f"{indent}  attack {short(owner, shared.get('m_attack', {}))}")
                    continue
                fields = {key: short(owner, value) for key, value in tree.items() if key not in SKIP_FIELDS and not key.endswith("Effects")}
                print(f"{indent}values {{{', '.join(f'{k}: {v}' for k, v in fields.items() if v not in (None, 0, 0.0, '', []))}}}")
            elif pointer.type.name == "Animator":
                tree = pointer.read_typetree()
                controller = follow(owner, tree["m_Controller"])
                if controller is None:
                    continue
                data = controller.read_typetree()
                names = sorted({name for _, name in data.get("m_TOS", []) if name and "->" not in name and "." not in name})
                clips = [clip.read().m_Name for clip in (follow(controller.assets_file, c) for c in data.get("m_AnimationClips", [])) if clip]
                print(f"{indent}animator {data.get('m_Name')}: states and parameters {names}")
                print(f"{indent}  clips {clips}")
        except Exception as error:
            print(f"{indent}{pointer.type.name}: {error}")


def find_root(path, name):
    """The root Transform of a GameObject called name in a bundle, preferring an item or piece prefab."""
    best = None
    for obj in load(path).objects:
        if obj.type.name != "GameObject":
            continue
        try:
            if (obj.peek_name() if hasattr(obj, "peek_name") else obj.read().m_Name) != name:
                continue
            game_object = obj.read()
        except Exception:
            continue
        pointers = [(c.component if hasattr(c, "component") else c) for c in game_object.m_Components]
        transform = next((p.read() for p in pointers if p.type.name == "Transform"), None)
        if transform is None or transform.m_Father.path_id != 0:
            continue
        # Several objects can share a name (a model and the prefab); the prefab carries a ZNetView or a Rigidbody.
        score = sum(1 for p in pointers if p.type.name in ("Rigidbody", "MonoBehaviour"))
        if best is None or score > best[0]:
            best = (score, transform)
    return best[1] if best else None


def bundles(all_bundles):
    cache = CACHE / "prefab_bundles.json"
    known = json.loads(cache.read_text()) if cache.exists() else {}
    order = [ITEM_BUNDLE]
    if all_bundles:
        order += sorted((p for p in (GAME / "SoftRef" / "Bundles").iterdir() if p.is_file() and p != ITEM_BUNDLE), key=os.path.getsize)
        order += [p for p in GAME.glob("*") if p.is_file()]
    return known, cache, order


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("names", nargs="*")
    parser.add_argument("--bundle")
    parser.add_argument("--materials", action="store_true")
    parser.add_argument("--values", action="store_true")
    parser.add_argument("--all-bundles", action="store_true")
    parser.add_argument("--hash", action="store_true")
    args = parser.parse_args()
    if args.hash:
        for name in args.names:
            print(f"{stable_hash(name):12}  {name}")
        return

    known, cache, order = bundles(args.all_bundles)
    for name in args.names:
        candidates = [GAME / "SoftRef" / "Bundles" / args.bundle] if args.bundle else ([pathlib.Path(known[name])] if name in known else order)
        for path in candidates:
            try:
                root = find_root(path, name)
            except Exception:
                continue
            if root is not None:
                print(f"===== {name} in {path.name}")
                walk(root, 0, args.materials, args.values)
                known[name] = str(path)
                break
        else:
            print(f"===== {name} not found{'' if args.all_bundles else ' (try --all-bundles)'}")
    CACHE.mkdir(parents=True, exist_ok=True)
    cache.write_text(json.dumps(known, indent=1))


if __name__ == "__main__":
    main()
