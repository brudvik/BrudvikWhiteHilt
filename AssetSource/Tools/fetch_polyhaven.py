"""Downloads a Poly Haven model (CC0), cuts it down to a face budget with Blender and writes a glb for the bundle.

Poly Haven models are photoscanned or sculpted and often have 100 000 faces or more. This joins every part, decimates
the result to about --faces faces, keeps only the base colour texture and writes AssetSource/Models/<out>.glb with the
author, license and source in asset.extras (read by glb_info.py).

Usage: python fetch_polyhaven.py <asset id> <out name> [--faces 5000] [--res 1k] [--blender <blender.exe>]
Example: python fetch_polyhaven.py treasure_chest treasurechest --faces 5000
"""
import json
import os
import pathlib
import struct
import subprocess
import sys
import tempfile
import urllib.request

API = "https://api.polyhaven.com"
USER_AGENT = "BrudvikWhiteHilt asset tools (https://github.com/brudvik/BrudvikWhiteHilt)"
MODELS = pathlib.Path(__file__).resolve().parents[1] / "Models"


def fetch(url):
    request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(request, timeout=120) as response:
        return response.read()


def download(asset, res, folder):
    files = json.loads(fetch(f"{API}/files/{asset}"))
    gltf = files["gltf"][res]["gltf"]
    folder.mkdir(parents=True, exist_ok=True)
    main = folder / pathlib.Path(gltf["url"]).name
    main.write_bytes(fetch(gltf["url"]))
    for relative, entry in gltf.get("include", {}).items():
        target = folder / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        if not target.exists():
            target.write_bytes(fetch(entry["url"]))
    return main


def build_in_blender(source, output, faces):
    import bpy

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(source))
    meshes = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    before = len(obj.data.polygons)
    if before > faces:
        modifier = obj.modifiers.new("decimate", "DECIMATE")
        modifier.ratio = faces / before
        modifier.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=modifier.name)

    # Only the base colour goes into the bundle; the other maps only make the file bigger.
    for material in obj.data.materials:
        if material is None or not material.use_nodes:
            continue
        nodes, links = material.node_tree.nodes, material.node_tree.links
        shader = next((node for node in nodes if node.type == "BSDF_PRINCIPLED"), None)
        if shader is None:
            continue
        for socket in shader.inputs:
            if socket.name != "Base Color":
                for link in list(socket.links):
                    links.remove(link)
        for node in list(nodes):
            if node.type in ("NORMAL_MAP", "SEPARATE_COLOR", "SEPARATE_RGB"):
                nodes.remove(node)

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(output), export_format="GLB", use_selection=True, export_yup=True)
    print(f"POLYHAVEN {before} -> {len(obj.data.polygons)} faces")


def add_asset_info(path, asset, info):
    data = path.read_bytes()
    json_length = struct.unpack_from("<I", data, 12)[0]
    gltf = json.loads(data[20:20 + json_length])
    gltf["asset"]["extras"] = {
        "title": info.get("name", asset),
        "author": ", ".join(info.get("authors", {}).keys()) or "?",
        "license": "CC0 (Poly Haven)",
        "source": f"https://polyhaven.com/a/{asset}",
    }
    chunk = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    chunk += b" " * (-len(chunk) % 4)
    rest = data[20 + json_length:]
    header = struct.pack("<4sII", b"glTF", 2, 12 + 8 + len(chunk) + len(rest))
    path.write_bytes(header + struct.pack("<I4s", len(chunk), b"JSON") + chunk + rest)


def option(name, default):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


def main():
    if "bpy" in sys.modules:
        argv = sys.argv[sys.argv.index("--") + 1:]
        build_in_blender(pathlib.Path(argv[0]), pathlib.Path(argv[1]), int(argv[2]))
        return

    asset, name = sys.argv[1], sys.argv[2]
    faces = int(option("--faces", "5000"))
    res = option("--res", "1k")
    blender = option("--blender", None)
    if blender is None:
        found = sorted(pathlib.Path(r"C:\Program Files\Blender Foundation").glob("*/blender.exe"))
        if not found:
            raise SystemExit("Blender not found; pass --blender")
        blender = str(found[-1])

    info = json.loads(fetch(f"{API}/info/{asset}"))
    folder = pathlib.Path(tempfile.gettempdir()) / "wh_polyhaven" / asset
    source = download(asset, res, folder)
    output = MODELS / f"{name}.glb"
    result = subprocess.run([blender, "-b", "--factory-startup", "--python-exit-code", "1", "--python", __file__, "--",
                             str(source), str(output), str(faces)],
                            capture_output=True, text=True, encoding="utf-8", errors="replace")
    for line in result.stdout.splitlines():
        if line.startswith("POLYHAVEN"):
            print(line)
    if result.returncode != 0 or not output.exists():
        print(result.stdout[-3000:], result.stderr[-3000:])
        raise SystemExit("Blender failed")
    add_asset_info(output, asset, info)
    print(f"Wrote {output} ({os.path.getsize(output) // 1024} KB)")


if __name__ == "__main__":
    main()
