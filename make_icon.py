# Generates AutoLockerScreenEx/Assets/icon.ico with Pillow.
# Mirrors the design in Assets/icon.svg (kept as the human-readable source);
# PIL is used directly because no SVG rasterizer is available on this machine.
# Run from repo root: uv run --with pillow auto_locker_screen_ex/make_icon.py
from pathlib import Path

from PIL import Image, ImageDraw

S = 4  # supersample factor
SZ = 256 * S
assets = Path(__file__).parent / "AutoLockerScreenEx" / "Assets"

img = Image.new("RGBA", (SZ, SZ), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# background rounded square with a vertical gradient (#2b6cb0 -> #1a365d)
top, bot = (43, 108, 176), (26, 54, 93)
x0, y0, x1, y1 = 8 * S, 8 * S, 248 * S, 248 * S
for y in range(y0, y1):
    t = (y - y0) / (y1 - y0)
    c = tuple(round(a + (b - a) * t) for a, b in zip(top, bot))
    d.line([(x0, y), (x1, y)], fill=c)
mask = Image.new("L", (SZ, SZ), 0)
ImageDraw.Draw(mask).rounded_rectangle([x0, y0, x1, y1], radius=52 * S, fill=255)
img.putalpha(mask)

# monitor silhouette (translucent white)
overlay = Image.new("RGBA", (SZ, SZ), (0, 0, 0, 0))
od = ImageDraw.Draw(overlay)
w36 = (255, 255, 255, 36)
od.rounded_rectangle([46 * S, 54 * S, 210 * S, 160 * S], radius=12 * S, fill=w36)
od.rectangle([112 * S, 166 * S, 144 * S, 180 * S], fill=w36)
od.rounded_rectangle([88 * S, 182 * S, 168 * S, 190 * S], radius=4 * S, fill=w36)
img = Image.alpha_composite(img, overlay)
d = ImageDraw.Draw(img)

# padlock shackle
white = (255, 255, 255, 255)
d.arc([96 * S, 62 * S, 160 * S, 126 * S], start=180, end=360, fill=white, width=17 * S)
d.line([(96 * S, 94 * S), (96 * S, 122 * S)], fill=white, width=17 * S)
d.line([(160 * S, 94 * S), (160 * S, 122 * S)], fill=white, width=17 * S)

# padlock body
d.rounded_rectangle([74 * S, 118 * S, 182 * S, 204 * S], radius=14 * S, fill=white)

# keyhole as a clock: dark face, light hands
d.ellipse([109 * S, 137 * S, 147 * S, 175 * S], fill=(26, 54, 93, 255))
d.line([(128 * S, 156 * S), (128 * S, 142 * S)], fill=white, width=5 * S)
d.line([(128 * S, 156 * S), (139 * S, 162 * S)], fill=white, width=5 * S)

img = img.resize((256, 256), Image.LANCZOS)
img.save(assets / "icon.png")
img.save(assets / "icon.ico", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
print("wrote", assets / "icon.ico")
