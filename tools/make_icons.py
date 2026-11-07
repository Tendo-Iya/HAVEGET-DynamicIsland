import os
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "assets")
os.makedirs(OUT, exist_ok=True)

S = 1024
SS = 2
W = S * SS


def vertical_gradient(size, top, bottom):
    img = Image.new("RGBA", (1, size))
    for y in range(size):
        t = y / max(1, size - 1)
        img.putpixel((0, y), tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(4)))
    return img.resize((size, size), Image.Resampling.BILINEAR)


base = vertical_gradient(W, (12, 22, 34, 255), (4, 8, 14, 255))
mask = Image.new("L", (W, W), 0)
md = ImageDraw.Draw(mask)
pad = 30 * SS
md.rounded_rectangle([pad, pad, W - pad, W - pad], radius=218 * SS, fill=255)
base.putalpha(mask)
img = base

# Soft brand glow in the upper half.
glow = Image.new("RGBA", (W, W), (0, 0, 0, 0))
gd = ImageDraw.Draw(glow)
cx, cy = W * 0.55, W * 0.36
for i in range(18, 0, -1):
    rad = int(W * (0.20 + i * 0.016))
    alpha = int(2 + i * 1.8)
    gd.ellipse([cx - rad, cy - rad, cx + rad, cy + rad], fill=(59, 158, 255, alpha))
glow = glow.filter(ImageFilter.GaussianBlur(52 * SS))
img = Image.alpha_composite(img, glow)

# Hairline outer border.
border = Image.new("RGBA", (W, W), (0, 0, 0, 0))
bd = ImageDraw.Draw(border)
bd.rounded_rectangle([pad, pad, W - pad, W - pad], radius=218 * SS, outline=(210, 235, 255, 36), width=2 * SS)
img = Image.alpha_composite(img, border)

# Azure brand tile.
mark_size = 500 * SS
mark_box = ((W - mark_size) // 2, (W - mark_size) // 2 - 4 * SS,
            (W + mark_size) // 2, (W + mark_size) // 2 - 4 * SS)
mark_mask = Image.new("L", (W, W), 0)
mmd = ImageDraw.Draw(mark_mask)
mmd.rounded_rectangle(mark_box, radius=150 * SS, fill=255)

mark = vertical_gradient(W, (120, 195, 255, 255), (20, 100, 190, 255))
mark.putalpha(mark_mask)
# A soft azure halo behind the tile.
halo = Image.new("RGBA", (W, W), (0, 0, 0, 0))
hd = ImageDraw.Draw(halo)
hd.rounded_rectangle([mark_box[0] - 26 * SS, mark_box[1] - 20 * SS,
                      mark_box[2] + 26 * SS, mark_box[3] + 34 * SS],
                     radius=174 * SS, fill=(44, 150, 255, 55))
halo = halo.filter(ImageFilter.GaussianBlur(42 * SS))
img = Image.alpha_composite(img, halo)
img = Image.alpha_composite(img, mark)

draw = ImageDraw.Draw(img)
# Top highlight on the tile.
highlight = Image.new("RGBA", (W, W), (0, 0, 0, 0))
hld = ImageDraw.Draw(highlight)
hld.rounded_rectangle([mark_box[0] + 3 * SS, mark_box[1] + 3 * SS,
                       mark_box[2] - 3 * SS, mark_box[3] - 3 * SS],
                      radius=147 * SS, outline=(255, 255, 255, 62), width=2 * SS)
img = Image.alpha_composite(img, highlight)
draw = ImageDraw.Draw(img)

# Stylized H, rounded and centered.
stroke = 54 * SS
left = W // 2 - 132 * SS
right = W // 2 + 132 * SS
top = W // 2 - 132 * SS + 4 * SS
bottom = W // 2 + 132 * SS + 4 * SS
draw.line([(left, top), (left, bottom)], fill=(249, 255, 252, 255), width=stroke)
draw.line([(right, top), (right, bottom)], fill=(249, 255, 252, 255), width=stroke)
draw.line([(left, (top + bottom) // 2), (right, (top + bottom) // 2)], fill=(249, 255, 252, 255), width=stroke)
# Round the stroke ends explicitly.
r = stroke // 2
for x, y in [(left, top), (left, bottom), (right, top), (right, bottom)]:
    draw.ellipse([x - r, y - r, x + r, y + r], fill=(249, 255, 252, 255))

# Small live dot, sitting on the tile corner.
dot_cx, dot_cy = W // 2 + 236 * SS, W // 2 - 236 * SS
dot_r = 33 * SS
draw.ellipse([dot_cx - dot_r - 7 * SS, dot_cy - dot_r - 7 * SS,
              dot_cx + dot_r + 7 * SS, dot_cy + dot_r + 7 * SS],
             fill=(6, 12, 21, 255), outline=(255, 255, 255, 50), width=2 * SS)
draw.ellipse([dot_cx - dot_r, dot_cy - dot_r, dot_cx + dot_r, dot_cy + dot_r],
             fill=(66, 166, 255, 255))
inner_r = 10 * SS
draw.ellipse([dot_cx - inner_r, dot_cy - inner_r, dot_cx + inner_r, dot_cy + inner_r],
             fill=(239, 255, 249, 235))

img = img.resize((S, S), Image.Resampling.LANCZOS)
img.save(os.path.join(OUT, "app.png"))
img.save(os.path.join(OUT, "app.ico"),
         sizes=[(16, 16), (20, 20), (24, 24), (32, 32), (40, 40), (48, 48),
                (64, 64), (128, 128), (256, 256)])

# Installer icon: same artwork plus a fine setup ring.
setup = img.copy()
sd = ImageDraw.Draw(setup)
sd.rounded_rectangle([38, 38, S - 38, S - 38], radius=204, outline=(77, 185, 255, 92), width=7)
setup.save(os.path.join(OUT, "setup.ico"),
           sizes=[(16, 16), (20, 20), (24, 24), (32, 32), (40, 40), (48, 48),
                  (64, 64), (128, 128), (256, 256)])

print("icons written to", OUT)
