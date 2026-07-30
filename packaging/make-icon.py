#!/usr/bin/env python3
"""Génère l'icône de Cocktails (shaker bleu sur fond sombre arrondi) en 1024×1024.

Sortie : le chemin passé en argument (défaut: icon_1024.png). Le script de packaging
en dérive ensuite le .icns via sips + iconutil.
"""
import sys
from PIL import Image, ImageDraw

S = 1024


def vgrad(top, bottom, size):
    """Dégradé vertical (top→bottom) en image size×size."""
    col = Image.new("RGBA", (1, size))
    for y in range(size):
        t = y / (size - 1)
        col.putpixel((0, y), tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,))
    return col.resize((size, size))


def main(out):
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))

    # Fond : carré arrondi (style icône macOS) avec dégradé bleu nuit.
    bg_mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(bg_mask).rounded_rectangle([36, 36, S - 36, S - 36], radius=228, fill=255)
    img.paste(vgrad((0x1B, 0x24, 0x42), (0x0A, 0x0D, 0x18), S), (0, 0), bg_mask)

    # Halo bleu diffus derrière le shaker.
    glow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse([300, 280, 724, 840], fill=(0x5B, 0x8C, 0xFF, 70))
    glow = glow.filter(__import__("PIL.ImageFilter", fromlist=["GaussianBlur"]).GaussianBlur(80))
    img = Image.alpha_composite(img, Image.composite(glow, Image.new("RGBA", (S, S), (0, 0, 0, 0)), bg_mask))

    # Silhouette du shaker (cobbler : corps + bande + dôme + bouton).
    sh = Image.new("L", (S, S), 0)
    sd = ImageDraw.Draw(sh)
    sd.rounded_rectangle([382, 435, 642, 836], radius=61, fill=255)   # corps
    sd.rounded_rectangle([383, 397, 641, 454], radius=26, fill=255)   # bande du couvercle
    sd.rounded_rectangle([415, 301, 609, 409], radius=73, fill=255)   # dôme
    sd.ellipse([491, 256, 533, 298], fill=255)                        # bouton

    img.paste(vgrad((0x9C, 0xB6, 0xFF), (0x3C, 0x5C, 0xA6), S), (0, 0), sh)

    # Contour + reflet.
    od = ImageDraw.Draw(img)
    dark = (0x0C, 0x14, 0x2A, 255)
    od.rounded_rectangle([382, 435, 642, 836], radius=61, outline=dark, width=11)
    od.rounded_rectangle([383, 397, 641, 454], radius=26, outline=dark, width=11)
    od.rounded_rectangle([415, 301, 609, 409], radius=73, outline=dark, width=11)
    od.ellipse([491, 256, 533, 298], outline=dark, width=11)
    od.rounded_rectangle([425, 492, 462, 775], radius=19, fill=(255, 255, 255, 72))

    img.save(out)
    print("écrit :", out)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "icon_1024.png")
