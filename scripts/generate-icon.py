"""Generates AiUsageBar/Assets/app.ico from code, with no imaging dependency.

The icon is three rising bars, matching the shape TrayIconFactory draws in the
notification area, so the executable and the tray read as the same product. Bar
colors reuse the severity palette (green, amber, red).

Run: python scripts/generate-icon.py
"""

import os
import struct
import zlib

SS = 4  # supersampling factor, for anti-aliasing
SIZES = [16, 24, 32, 48, 64, 128, 256]
OUT = os.path.join("AiUsageBar", "Assets", "app.ico")

BG = (0x26, 0x26, 0x33, 255)      # dark rounded plate
BARS = [
    (0.40, (0x4c, 0xaf, 0x50, 255)),   # green
    (0.62, (0xff, 0xc1, 0x07, 255)),   # amber
    (0.85, (0xf4, 0x43, 0x36, 255)),   # red
]


def blend(dst, i, color):
    """Alpha-composite `color` over the pixel at byte index i."""
    sr, sg, sb, sa = color
    if sa == 255:
        dst[i:i + 4] = bytes((sr, sg, sb, 255))
        return
    a = sa / 255.0
    for k, s in enumerate((sr, sg, sb)):
        dst[i + k] = int(s * a + dst[i + k] * (1 - a))
    dst[i + 3] = max(dst[i + 3], sa)


def rounded_rect(buf, w, x0, y0, x1, y1, r, color):
    """Fill a rounded rectangle. Coordinates are in supersampled pixels."""
    for y in range(int(y0), int(y1)):
        for x in range(int(x0), int(x1)):
            # Distance test only near the corners.
            cx = None
            if x < x0 + r and y < y0 + r:
                cx, cy = x0 + r, y0 + r
            elif x > x1 - r and y < y0 + r:
                cx, cy = x1 - r, y0 + r
            elif x < x0 + r and y > y1 - r:
                cx, cy = x0 + r, y1 - r
            elif x > x1 - r and y > y1 - r:
                cx, cy = x1 - r, y1 - r
            if cx is not None and (x - cx) ** 2 + (y - cy) ** 2 > r * r:
                continue
            blend(buf, (y * w + x) * 4, color)


def downsample(src, w, h, factor):
    """Box-filter the supersampled buffer down to its final size."""
    ow, oh = w // factor, h // factor
    out = bytearray(ow * oh * 4)
    n = factor * factor
    for y in range(oh):
        for x in range(ow):
            r = g = b = a = 0
            for dy in range(factor):
                for dx in range(factor):
                    i = ((y * factor + dy) * w + (x * factor + dx)) * 4
                    sa = src[i + 3]
                    r += src[i] * sa
                    g += src[i + 1] * sa
                    b += src[i + 2] * sa
                    a += sa
            o = (y * ow + x) * 4
            if a:
                out[o] = r // a
                out[o + 1] = g // a
                out[o + 2] = b // a
            out[o + 3] = a // n
    return out, ow, oh


def render(size, plate=True):
    """Draw one icon at `size` pixels. `plate` toggles the dark background."""
    w = h = size * SS
    buf = bytearray(w * h * 4)

    if plate:
        rounded_rect(buf, w, 0, 0, w, h, w * 0.22, BG)

    # Three bars sharing a baseline, insets tuned to stay legible at 16px.
    bar_w = w * 0.16
    gap = w * 0.08
    total = bar_w * 3 + gap * 2
    x = (w - total) / 2
    base = h * 0.80
    radius = bar_w * 0.28

    for frac, color in BARS:
        top = base - (h * 0.60) * frac / 0.85
        rounded_rect(buf, w, x, top, x + bar_w, base, radius, color)
        x += bar_w + gap

    return downsample(buf, w, h, SS)


def png(width, height, rgba):
    def chunk(tag, data):
        return (struct.pack(">I", len(data)) + tag + data
                + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))

    raw = bytearray()
    for y in range(height):
        raw.append(0)  # filter type: none
        raw += rgba[y * width * 4:(y + 1) * width * 4]

    return (b"\x89PNG\r\n\x1a\n"
            + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(bytes(raw), 9))
            + chunk(b"IEND", b""))


def main():
    images = []
    for size in SIZES:
        rgba, w, h = render(size)
        images.append((size, png(w, h, rgba)))
        print(f"  rendered {size}x{size}")

    header = struct.pack("<HHH", 0, 1, len(images))
    offset = 6 + 16 * len(images)
    entries = b""
    blobs = b""
    for size, data in images:
        dim = 0 if size >= 256 else size  # 0 means 256 in the ICO format
        entries += struct.pack("<BBBBHHII", dim, dim, 0, 0, 1, 32, len(data), offset)
        offset += len(data)
        blobs += data

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "wb") as f:
        f.write(header + entries + blobs)
    print(f"wrote {OUT} ({os.path.getsize(OUT)} bytes, {len(images)} sizes)")


if __name__ == "__main__":
    main()
