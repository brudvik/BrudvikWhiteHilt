"""Makes the logo copies the mod uses from AssetSource/Logo/brudvik-white-hilt-logo.png.

Writes BrudvikWhiteHilt/Assets/Logo/WhiteHiltLogo.png (1024 px, embedded in the DLL) and
BrudvikWhiteHilt/Package/icon.png (256 px, the Thunderstore icon).
"""
from pathlib import Path

from PIL import Image

root = Path(__file__).resolve().parent.parent
source = Image.open(root / "AssetSource" / "Logo" / "brudvik-white-hilt-logo.png").convert("RGBA")

embedded = root / "BrudvikWhiteHilt" / "Assets" / "Logo" / "WhiteHiltLogo.png"
embedded.parent.mkdir(parents=True, exist_ok=True)
source.resize((1024, 1024), Image.LANCZOS).save(embedded, optimize=True)
source.resize((256, 256), Image.LANCZOS).save(root / "BrudvikWhiteHilt" / "Package" / "icon.png", optimize=True)
print("wrote", embedded, "and Package/icon.png")
