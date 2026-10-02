"""Crops every PNG in a folder to its visible part and scales it to the documentation size (as render_showcase.py does).

Usage: python crop_pictures.py <source folder> <target folder>
"""
import pathlib
import sys

from render_showcase import crop


def main(source, target):
    target.mkdir(parents=True, exist_ok=True)
    for picture in sorted(source.glob("*.png")):
        crop(picture, target / picture.name)


if __name__ == "__main__":
    main(pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2]))
