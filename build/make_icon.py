#!/usr/bin/env python3
"""Thunderstore icon: 256x256 PNG. A mint diamond and three stacked set bars on dark wood.
Without PIL, which the build box lacks.
Run from the repo root: python3 build/make_icon.py"""
import struct
import zlib
import math

S = 256
SS = 3                       # supersample
W = S * SS

px = bytearray(W * W * 4)

def blend(x, y, r, g, b, a):
    if x < 0 or y < 0 or x >= W or y >= W:
        return
    i = (y * W + x) * 4
    ia = 1.0 - a
    px[i] = int(r * a + px[i] * ia)
    px[i + 1] = int(g * a + px[i + 1] * ia)
    px[i + 2] = int(b * a + px[i + 2] * ia)
    px[i + 3] = int(min(255, 255 * a + px[i + 3] * ia))

def rounded_rect(x0, y0, x1, y1, rad, col, alpha=1.0):
    for y in range(int(y0), int(y1)):
        for x in range(int(x0), int(x1)):
            dx = max(x0 + rad - x, 0, x - (x1 - 1 - rad))
            dy = max(y0 + rad - y, 0, y - (y1 - 1 - rad))
            if dx * dx + dy * dy <= rad * rad:
                blend(x, y, *col, alpha)

# Colors
PLATE = (0x1c, 0x16, 0x11)
BORDER = (0x6b, 0x52, 0x33)
ORANGE = (0xf2, 0xa6, 0x4a)
GREY = (0xb3, 0xa5, 0x88)
MINT = (0x7e, 0xe0, 0xc3)

# Dark plate with rounded border
rounded_rect(0, 0, W, W, 28 * SS, BORDER)
rounded_rect(8 * SS, 8 * SS, W - 8 * SS, W - 8 * SS, 22 * SS, PLATE)

# Three rounded bars at x 40-150, y 70/116/162, height 30
bar_rad = 8 * SS
for i, y in enumerate((70, 116, 162)):
    col = ORANGE if i == 0 else GREY
    rounded_rect(40 * SS, y * SS, 150 * SS, (y + 30) * SS, bar_rad, col)

# Mint diamond at (192, 128) with radius 44
cx, cy, r = 192 * SS, 128 * SS, 44 * SS
for y in range(int(cy - r), int(cy + r) + 1):
    for x in range(int(cx - r), int(cx + r) + 1):
        dx = x - cx
        dy = y - cy
        if abs(dx) + abs(dy) <= r:
            blend(x, y, *MINT, 1.0)

# Downsample
out = bytearray()
for y in range(S):
    row = bytearray([0])
    for x in range(S):
        r = g = b = a = 0
        for sy in range(SS):
            for sx in range(SS):
                i = ((y * SS + sy) * W + (x * SS + sx)) * 4
                r += px[i]; g += px[i + 1]; b += px[i + 2]; a += px[i + 3]
        n = SS * SS
        row += bytes((r // n, g // n, b // n, a // n))
    out += row

def chunk(tag, data):
    c = tag + data
    return struct.pack(">I", len(data)) + c + struct.pack(">I", zlib.crc32(c) & 0xffffffff)

png = b"\x89PNG\r\n\x1a\n"
png += chunk(b"IHDR", struct.pack(">IIBBBBB", S, S, 8, 6, 0, 0, 0))
png += chunk(b"IDAT", zlib.compress(bytes(out), 9))
png += chunk(b"IEND", b"")
open("thunderstore/icon.png", "wb").write(png)
print("wrote thunderstore/icon.png", S, "x", S, len(png), "bytes")
