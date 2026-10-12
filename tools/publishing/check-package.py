"""Checks the Thunderstore package that publish.ps1 built: entry names, versions, the three mod files, the SHA256."""
import hashlib
import json
import pathlib
import re
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
PATH = ROOT / "BrudvikWhiteHilt" / "bin" / "Release" / "BrudvikWhiteHilt.dll.zip"

plugin = (ROOT / "BrudvikWhiteHilt" / "BrudvikWhiteHilt.cs").read_text(encoding="utf-8-sig")
version = re.search(r'PluginVersion = "([0-9.]+)"', plugin).group(1)

z = zipfile.ZipFile(PATH)
names = z.namelist()
print(len(names), "entries")
for info in z.infolist():
    print(f"  {info.filename}  {info.file_size}")
    if "\\" in info.filename:
        print("BACKSLASH", info.filename)

manifest = json.loads(z.read("manifest.json").decode("utf-8-sig"))
print("manifest", manifest["version_number"], "OK" if manifest["version_number"] == version else "MISMATCH")
changelog = z.read("CHANGELOG.md").decode("utf-8-sig")
print("changelog head", [line for line in changelog.splitlines() if line.startswith("## ")][0])
readme = z.read("README.md").decode("utf-8-sig")
print("readme", re.search(r"New in [0-9.]+", readme).group(0))
for required in ("plugins/BrudvikWhiteHilt.dll", "plugins/whitehilt_foraging", "plugins/whitehilt_decor"):
    print(required, required in names)

data = z.read("plugins/BrudvikWhiteHilt.dll")
print("dll", len(data), version.encode() in data or version in data.decode("utf-16-le", "ignore"))
print("sha256", hashlib.sha256(PATH.read_bytes()).hexdigest())
print("zip bytes", PATH.stat().st_size)
