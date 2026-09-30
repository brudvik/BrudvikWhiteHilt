"""Edits a downloaded glb before conversion: material colours, dropped or kept mesh parts, smaller textures.

Colour-only parts that share a colour end up in one atlas tile in convert_glb.py, so a paint.json cannot tell them
apart; give them their own colours here instead. Dropping a part here is cleaner than a crop box when it touches the rest.
Unused images are blanked, so dropped parts also stop costing space. GitHub rejects files over 100 MB.

Usage:
  python glb_edit.py <in.glb> [--out <out.glb>] [--colour <material>=r,g,b ...] [--drop <node name|index> ...]
                     [--keep <node name|index> ...] [--max-texture 1024]
Without --out the file is changed in place. Run glb_info.py first to see node and material names.
"""
import argparse
import io
import json
import pathlib
import struct

from PIL import Image

JSON_CHUNK = 0x4E4F534A
BIN_CHUNK = 0x004E4942


def read(path):
    data = path.read_bytes()
    offset, gltf, binary = 12, None, b""
    while offset < len(data):
        length, kind = struct.unpack_from("<II", data, offset)
        chunk = data[offset + 8:offset + 8 + length]
        if kind == JSON_CHUNK:
            gltf = json.loads(chunk)
        elif kind == BIN_CHUNK:
            binary = chunk
        offset += 8 + length
    return gltf, binary


def write(path, gltf, binary):
    text = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    text += b" " * (-len(text) % 4)
    binary += b"\0" * (-len(binary) % 4)
    total = 12 + 8 + len(text) + (8 + len(binary) if binary else 0)
    with open(path, "wb") as file:
        file.write(struct.pack("<4sII", b"glTF", 2, total))
        file.write(struct.pack("<II", len(text), JSON_CHUNK) + text)
        if binary:
            file.write(struct.pack("<II", len(binary), BIN_CHUNK) + binary)


def matches(node_index, node, names):
    return str(node_index) in names or node.get("name") in names


def used_images(gltf):
    used = set()
    for node in gltf["nodes"]:
        if "mesh" not in node:
            continue
        for primitive in gltf["meshes"][node["mesh"]]["primitives"]:
            material = gltf["materials"][primitive["material"]] if "material" in primitive else {}
            pbr = material.get("pbrMetallicRoughness", {})
            references = [pbr.get("baseColorTexture"), pbr.get("metallicRoughnessTexture"), material.get("normalTexture"),
                          material.get("occlusionTexture"), material.get("emissiveTexture"),
                          material.get("extensions", {}).get("KHR_materials_pbrSpecularGlossiness", {}).get("diffuseTexture")]
            used |= {gltf["textures"][reference["index"]]["source"] for reference in references if reference}
    return used


def rebuild(gltf, binary, max_texture):
    """Repacks the binary chunk; images are downsized to max_texture and unused ones blanked."""
    image_views = {image["bufferView"]: (index, image) for index, image in enumerate(gltf.get("images", [])) if "bufferView" in image}
    used = used_images(gltf)
    packed = bytearray()
    for view_index, view in enumerate(gltf.get("bufferViews", [])):
        start = view.get("byteOffset", 0)
        chunk = binary[start:start + view["byteLength"]]
        if view_index in image_views:
            image_index, image = image_views[view_index]
            if image_index not in used:
                picture = Image.new("RGB", (1, 1))
            else:
                picture = Image.open(io.BytesIO(chunk))
                if max_texture and max(picture.size) > max_texture:
                    picture.thumbnail((max_texture, max_texture))
                else:
                    picture = None
            if picture is not None:
                buffer = io.BytesIO()
                picture.save(buffer, "PNG", optimize=True)
                chunk = buffer.getvalue()
                image["mimeType"] = "image/png"
        packed += b"\0" * (-len(packed) % 4)
        view["byteOffset"] = len(packed)
        view["byteLength"] = len(chunk)
        packed += chunk
    if gltf.get("buffers"):
        gltf["buffers"][0]["byteLength"] = len(packed)
    return bytes(packed)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("input", type=pathlib.Path)
    parser.add_argument("--out", type=pathlib.Path)
    parser.add_argument("--colour", nargs="+", default=[], metavar="MATERIAL=R,G,B")
    parser.add_argument("--drop", nargs="+", default=[])
    parser.add_argument("--keep", nargs="+", default=[])
    parser.add_argument("--max-texture", type=int, default=0)
    args = parser.parse_args()

    gltf, binary = read(args.input)
    for rule in args.colour:
        name, values = rule.split("=", 1)
        rgb = [float(v) for v in values.split(",")]
        materials = [m for m in gltf.get("materials", []) if m.get("name") == name]
        if not materials:
            raise SystemExit(f"no material named {name}")
        for material in materials:
            material.setdefault("pbrMetallicRoughness", {})["baseColorFactor"] = rgb[:3] + [rgb[3] if len(rgb) > 3 else 1.0]
        print(f"colour {name} -> {rgb}")

    for node_index, node in enumerate(gltf["nodes"]):
        if "mesh" in node and ((args.drop and matches(node_index, node, args.drop)) or (args.keep and not matches(node_index, node, args.keep))):
            del node["mesh"]
            print(f"dropped node {node_index} {node.get('name', '')}")

    binary = rebuild(gltf, binary, args.max_texture)
    out = args.out or args.input
    write(out, gltf, binary)
    print(f"{out} {out.stat().st_size:,} bytes")


if __name__ == "__main__":
    main()
