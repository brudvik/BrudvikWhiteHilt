"""Lists config (section, key) pairs bound in the mod source, for translation coverage checks.

Usage: python cfg_keys.py [--check]   (--check: report keys missing from Norwegian.json)
"""
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(r"d:\Development\Sources\BrudvikWhiteHilt\BrudvikWhiteHilt")


def sanitize(text):
    return "".join(c.lower() if c.isalnum() else "_" for c in text)


def family(section):
    return section.split(".", 1)[0]


pairs = []
for f in sorted(ROOT.rglob("*.cs")):
    if "\\obj\\" in str(f) or "\\bin\\" in str(f):
        continue
    text = f.read_text(encoding="utf-8-sig")
    consts = dict(re.findall(r'const string (\w+)\s*=\s*"([^"]+)"', text))
    for m in re.finditer(r'(?:BindAdminOnly|BindLocal|\.Bind)\s*(?:<[^>]+>)?\(\s*([^,\n]+?)\s*,\s*"(\w+)"', text):
        sec_expr, key = m.group(1).strip(), m.group(2)
        line = text.count("\n", 0, m.start()) + 1
        if sec_expr.startswith('"'):
            sec = sec_expr.strip('"')
        else:
            p = re.match(r'(\w+)(?:\s*\+\s*"([^"]*)")?$', sec_expr)
            sec = consts[p.group(1)] + (p.group(2) or "") if p and p.group(1) in consts else "?" + sec_expr
        pairs.append((sec, key, f"{f.relative_to(ROOT)}:{line}"))

if "--check" in sys.argv:
    nb = json.loads((ROOT / "Translations" / "Norwegian.json").read_text(encoding="utf-8-sig"))
    missing = []
    for sec, key, where in pairs:
        if sec.startswith("?"):
            continue
        exact = f"whitehilt_cfg_{sanitize(sec)}_{sanitize(key)}"
        fam = f"whitehilt_cfg_{sanitize(family(sec))}_{sanitize(key)}"
        if exact not in nb and fam not in nb:
            missing.append(f"{exact}  ({where})")
        elif (exact + "_desc") not in nb and (fam + "_desc") not in nb:
            missing.append(f"{exact}_desc  ({where})")
    for fam_name in sorted({family(s) for s, _, _ in pairs if not s.startswith("?")}):
        if f"whitehilt_cfgsec_{sanitize(fam_name)}" not in nb:
            missing.append(f"whitehilt_cfgsec_{sanitize(fam_name)}")
    print("\n".join(missing))
    print(f"{len(missing)} missing")
else:
    for sec, key, where in pairs:
        print(f"{sec}\t{key}\t{where}")
    print(f"{len(pairs)} pairs", file=sys.stderr)
