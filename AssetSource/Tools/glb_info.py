"""Shows what is in a glb: author and license, each mesh part with its material and its bounds as the model stands
(node transforms applied, glTF axes: y up), materials, textures, skins, animations and totals. For a rigged model the
part bounds are the bind pose; use preview_animations.py to see it posed.

Usage:
  python glb_info.py <file.glb> [...]
  python glb_info.py <file.glb> --profile <axis 0|1|2> [--slices N]   extent of the other two axes per slice
  python glb_info.py <file.glb> --sides <out.png> [--right x,y,z] [--up x,y,z]
      draws the model from two opposite sides with flat material colours, to tell front from back
"""
import argparse
import io
import pathlib
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
import convert_glb  # noqa: E402

PALETTE = [(200, 80, 60), (70, 130, 200), (90, 170, 90), (220, 190, 70), (160, 90, 180), (230, 140, 60), (120, 120, 120),
           (60, 180, 180)]


def parts(gltf, binary):
    """Yields (node index, node name, material name, material index, points, triangles) per primitive, in model space."""
    for node_index, node in enumerate(gltf["nodes"]):
        if "mesh" not in node:
            continue
        matrix = convert_glb.world_matrix(gltf, node_index)
        for primitive in gltf["meshes"][node["mesh"]]["primitives"]:
            points = [convert_glb.transform(matrix, p, 1.0) for p in convert_glb.read_accessor(gltf, binary, primitive["attributes"]["POSITION"])]
            if "indices" in primitive:
                indices = [i[0] for i in convert_glb.read_accessor(gltf, binary, primitive["indices"])]
            else:
                indices = list(range(len(points)))
            material_index = primitive.get("material")
            material = gltf["materials"][material_index].get("name", "") if material_index is not None else ""
            yield node_index, node.get("name", ""), material, material_index, points, indices


def summary(path):
    gltf, binary = convert_glb.read_glb(path)
    extras = gltf.get("asset", {}).get("extras", {})
    print(f"===== {path.name}")
    print(f"  title   {extras.get('title', '?')}")
    print(f"  author  {extras.get('author', '?')}")
    print(f"  license {extras.get('license', '?')}")
    print(f"  source  {extras.get('source', '?')}")
    low, high, triangles = [float("inf")] * 3, [float("-inf")] * 3, 0
    for node_index, name, material, _, points, indices in parts(gltf, binary):
        part_low = [min(p[a] for p in points) for a in range(3)]
        part_high = [max(p[a] for p in points) for a in range(3)]
        low = [min(a, b) for a, b in zip(low, part_low)]
        high = [max(a, b) for a, b in zip(high, part_high)]
        triangles += len(indices) // 3
        print(f"  node {node_index:3} {name[:34]:34} mat {material[:18]:18} tris {len(indices) // 3:6}  "
              f"min {[round(v, 3) for v in part_low]} max {[round(v, 3) for v in part_high]}")
    size = [round(h - l, 3) for l, h in zip(low, high)]
    print(f"  total {triangles} triangles, size {size} (x, y up, z), height {size[1]}")
    for index, material in enumerate(gltf.get("materials", [])):
        pbr = material.get("pbrMetallicRoughness", {})
        texture = pbr.get("baseColorTexture")
        source = gltf["textures"][texture["index"]]["source"] if texture else None
        print(f"  material {index} {material.get('name', '')[:24]:24} texture {source} colour {pbr.get('baseColorFactor')}"
              f" {'emission ' if material.get('emissiveTexture') else ''}{list(material.get('extensions', {}).keys()) or ''}")
    for index, image in enumerate(gltf.get("images", [])):
        size = gltf["bufferViews"][image["bufferView"]]["byteLength"] if "bufferView" in image else 0
        try:
            _, data = convert_glb.image_bytes(gltf, binary, index)
            width, height = Image.open(io.BytesIO(data)).size
        except Exception:
            width = height = "?"
        print(f"  image {index} {image.get('mimeType')} {width}x{height} {size:,} bytes")
    for index, skin in enumerate(gltf.get("skins", [])):
        print(f"  skin {index} {len(skin['joints'])} joints")
    for animation in gltf.get("animations", []):
        length = max((gltf["accessors"][sampler["input"]].get("max", [0])[0] for sampler in animation["samplers"]), default=0)
        print(f"  animation {animation.get('name', '')!r}: {len(animation['channels'])} channels, {length:.2f} s")


def profile(path, axis, slices):
    gltf, binary = convert_glb.read_glb(path)
    points = [(p, material) for _, _, material, _, part_points, _ in parts(gltf, binary) for p in part_points]
    low, high = min(p[axis] for p, _ in points), max(p[axis] for p, _ in points)
    others = [a for a in range(3) if a != axis]
    for index in range(slices):
        a, b = low + (high - low) * index / slices, low + (high - low) * (index + 1) / slices
        inside = [(p, m) for p, m in points if a <= p[axis] <= b]
        if not inside:
            print(f"{a:9.3f}..{b:9.3f}  -")
            continue
        extents = "  ".join(f"{'xyz'[o]} {min(p[o] for p, _ in inside):8.3f}..{max(p[o] for p, _ in inside):8.3f}" for o in others)
        materials = ",".join(sorted({m[:10] for _, m in inside}))
        print(f"{a:9.3f}..{b:9.3f}  n={len(inside):5}  {extents}  {materials}")


def sides(path, out, right, up):
    gltf, binary = convert_glb.read_glb(path)
    view = [right[1] * up[2] - right[2] * up[1], right[2] * up[0] - right[0] * up[2], right[0] * up[1] - right[1] * up[0]]
    triangles = []
    for _, _, material, material_index, points, indices in parts(gltf, binary):
        colour = PALETTE[(material_index or 0) % len(PALETTE)]
        triangles += [([points[v] for v in indices[t:t + 3]], colour) for t in range(0, len(indices) - 2, 3)]
    dot = lambda p, axis: sum(p[k] * axis[k] for k in range(3))
    all_points = [p for tri, _ in triangles for p in tri]
    centre_u = sum(dot(p, right) for p in all_points) / len(all_points)
    centre_v = sum(dot(p, up) for p in all_points) / len(all_points)
    span = max(max(abs(dot(p, right) - centre_u), abs(dot(p, up) - centre_v)) for p in all_points) * 2.2
    size = 420
    sheet = Image.new("RGB", (size * 2, size), "white")
    for side, sign in enumerate((1, -1)):
        image = Image.new("RGB", (size, size), "white")
        draw = ImageDraw.Draw(image)
        screen_right = right if sign == 1 else [-x for x in right]
        for tri, colour in sorted(triangles, key=lambda t: sign * sum(dot(p, view) for p in t[0])):
            draw.polygon([((dot(p, screen_right) - centre_u * sign) / span * size + size / 2, size / 2 - (dot(p, up) - centre_v) / span * size)
                          for p in tri], fill=colour, outline=tuple(max(0, c - 40) for c in colour))
        draw.text((4, 4), "from +view" if sign == 1 else "from -view", fill="black")
        sheet.paste(image, (side * size, 0))
    legend = ImageDraw.Draw(sheet)
    for index, material in enumerate(gltf.get("materials", [])[:len(PALETTE)]):
        legend.rectangle([4, size - 16 * (index + 1) - 4, 14, size - 16 * index - 8], fill=PALETTE[index])
        legend.text((18, size - 16 * (index + 1) - 5), material.get("name", str(index))[:30], fill="black")
    sheet.save(out)
    print(f"{out}  (view direction {[round(v, 3) for v in view]})")


def vector(text):
    return [float(v) for v in text.split(",")]


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("files", nargs="+", type=pathlib.Path)
    parser.add_argument("--profile", type=int, choices=(0, 1, 2))
    parser.add_argument("--slices", type=int, default=40)
    parser.add_argument("--sides")
    parser.add_argument("--right", type=vector, default=[1.0, 0.0, 0.0])
    parser.add_argument("--up", type=vector, default=[0.0, 1.0, 0.0])
    args = parser.parse_args()
    for path in args.files:
        if args.profile is not None:
            profile(path, args.profile, args.slices)
        elif args.sides:
            sides(path, args.sides, args.right, args.up)
        else:
            summary(path)


if __name__ == "__main__":
    main()
