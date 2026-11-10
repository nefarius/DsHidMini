"""Generate the original motion-viewer controller mesh (Python 3, no dependencies).

Run from any directory. Coordinates: X = right, Y = USB/shoulders, Z = face.
All surfaces, controls and markings are constructed here; no third-party assets.
"""

from math import cos, sin, pi, sqrt
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "ControlApp/Resources/Models/MotionController.obj"
PARTS = []


def smooth_outline(points, steps=6):
    """Closed Catmull-Rom spline with no repeated endpoint."""
    result = []
    for i, p1 in enumerate(points):
        p0, p2, p3 = points[i - 1], points[(i + 1) % len(points)], points[(i + 2) % len(points)]
        for j in range(steps):
            t = j / steps
            result.append(tuple(0.5 * (2 * b + (-a + c) * t
                + (2 * a - 5 * b + 4 * c - d) * t * t
                + (-a + 3 * b - 3 * c + d) * t * t * t)
                for a, b, c, d in zip(p0, p1, p2, p3)))
    # Counterclockwise outline as viewed from +Z.
    area = sum(x * result[(i + 1) % len(result)][1]
               - result[(i + 1) % len(result)][0] * y for i, (x, y) in enumerate(result))
    return result if area > 0 else result[::-1]


def triangulate(outline):
    """Ear clipping: unlike a centre fan, this also caps concave grips/arrows."""
    remaining = list(range(len(outline)))
    triangles = []

    def cross(a, b, c):
        return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])

    while len(remaining) > 3:
        for i, b in enumerate(remaining):
            a, c = remaining[i - 1], remaining[(i + 1) % len(remaining)]
            if cross(outline[a], outline[b], outline[c]) <= 1e-12:
                continue
            if any(cross(outline[a], outline[b], outline[p]) >= -1e-12
                   and cross(outline[b], outline[c], outline[p]) >= -1e-12
                   and cross(outline[c], outline[a], outline[p]) >= -1e-12
                   for p in remaining if p not in (a, b, c)):
                continue
            triangles.append((a, b, c))
            remaining.pop(i)
            break
        else:
            raise ValueError("Non-simple or degenerate cap outline")
    triangles.append(tuple(remaining))
    return triangles


def loft(name, outline, profile, center=(0, 0, 0), inset=False):
    """Close a shell with rings (XY scale, Z), with shared smooth normals."""
    n = len(outline)
    rings = []
    for scale, z in profile:
        ring = []
        for i, (x, y) in enumerate(outline):
            if inset:
                previous, following = outline[i - 1], outline[(i + 1) % n]
                dx, dy = following[0] - previous[0], following[1] - previous[1]
                length = sqrt(dx * dx + dy * dy)
                px, py = x - dy / length * (1 - scale), y + dx / length * (1 - scale)
            else:
                px, py = x * scale, y * scale
            ring.append((px + center[0], py + center[1], z + center[2]))
        rings.append(ring)
    vertices = [point for ring in rings for point in ring]
    faces = []
    for r in range(len(profile) - 1):
        for i in range(n):
            a, b = r * n + i, r * n + (i + 1) % n
            faces.extend(((a, b, b + n), (a, b + n, a + n)))
    last = (len(profile) - 1) * n

    def cap(ring, offset, reverse):
        # A centred fan keeps round controls rotationally smooth. Concave shell
        # outlines need ear clipping, since their centroid may see across a grip.
        convex = all((ring[i][0] - ring[i - 1][0]) * (ring[(i + 1) % n][1] - ring[i][1])
                     - (ring[i][1] - ring[i - 1][1]) * (ring[(i + 1) % n][0] - ring[i][0]) >= -1e-12
                     for i in range(n))
        if convex:
            middle = len(vertices)
            vertices.append(tuple(sum(point[axis] for point in ring) / n for axis in range(3)))
            triangles = [(middle, offset + i, offset + (i + 1) % n) for i in range(n)]
        else:
            triangles = [(offset + a, offset + b, offset + c) for a, b, c in triangulate(ring)]
        faces.extend((c, b, a) if reverse else (a, b, c) for a, b, c in triangles)

    cap(rings[0], 0, True)
    cap(rings[-1], last, False)
    PARTS.append((name, vertices, faces))


def circle(radius, segments=48):
    return [(radius * cos(2 * pi * i / segments), radius * sin(2 * pi * i / segments))
            for i in range(segments)]


def rounded_rect(width, height, radius, steps=8):
    return [(cx + radius * cos(angle + i * pi / (2 * steps)),
             cy + radius * sin(angle + i * pi / (2 * steps)))
            for cx, cy, angle in ((width / 2 - radius, height / 2 - radius, 0),
                                  (-width / 2 + radius, height / 2 - radius, pi / 2),
                                  (-width / 2 + radius, -height / 2 + radius, pi),
                                  (width / 2 - radius, -height / 2 + radius, 3 * pi / 2))
            for i in range(steps + 1)]


def button(name, center, radius, height):
    loft(name, circle(radius), ((0.92, 0), (1, 0.025), (1, height - 0.025),
                              (0.94, height), (0.5, height + 0.008)), center)


def tube(name, points, radius=0.012, closed=False):
    """Thin round strokes for button glyphs; independent of textures/UVs."""
    vertices, faces = [], []
    count = len(points)
    sides = 8
    for i, point in enumerate(points):
        previous = points[(i - 1) % count] if closed or i else points[0]
        following = points[(i + 1) % count] if closed or i < count - 1 else points[-1]
        dx, dy = following[0] - previous[0], following[1] - previous[1]
        length = sqrt(dx * dx + dy * dy)
        nx, ny = -dy / length, dx / length
        for j in range(sides):
            angle = 2 * pi * j / sides
            vertices.append((point[0] + radius * cos(angle) * nx,
                             point[1] + radius * cos(angle) * ny,
                             point[2] + radius * sin(angle)))
    for i in range(count if closed else count - 1):
        for j in range(sides):
            a, b = i * sides + j, i * sides + (j + 1) % sides
            c, d = ((i + 1) % count) * sides + j, ((i + 1) % count) * sides + (j + 1) % sides
            faces.extend(((a, b, d), (a, d, c)))
    if not closed:
        # Outward caps; shared rings keep the small stroke ends smooth.
        for j in range(1, sides - 1):
            faces.extend(((0, j + 1, j), ((count - 1) * sides,
                          (count - 1) * sides + j, (count - 1) * sides + j + 1)))
    PARTS.append((name, vertices, faces))


def build():
    outline = smooth_outline([
        (-2.01, 0.38), (-1.97, 0.70), (-1.70, 0.91), (-1.25, 0.92),
        (-0.86, 0.80), (0, 0.79), (0.86, 0.80), (1.25, 0.92),
        (1.70, 0.91), (1.97, 0.70), (2.01, 0.38), (2.08, -0.12),
        (2.12, -0.79), (2.02, -1.39), (1.83, -1.62), (1.58, -1.57),
        (1.35, -1.19), (1.11, -0.75), (0.88, -0.65), (0.55, -0.78),
        (0, -0.73), (-0.55, -0.78), (-0.88, -0.65), (-1.11, -0.75),
        (-1.35, -1.19), (-1.58, -1.57), (-1.83, -1.62), (-2.02, -1.39),
        (-2.12, -0.79), (-2.08, -0.12)])
    loft("ShellLower", outline, ((0.83, -0.31), (0.92, -0.29), (0.98, -0.20),
                                (1, -0.09), (1, -0.035)), inset=True)
    loft("Seam", outline, ((0.995, -0.035), (0.995, -0.013)), inset=True)
    loft("ShellUpper", outline, ((1, -0.013), (1.003, 0.06), (0.995, 0.14),
                                (0.975, 0.21), (0.94, 0.255), (0.88, 0.28),
                                (0.84, 0.285)), inset=True)

    for x in (-0.67, 0.67):
        center = (x, -0.61, 0.22)
        loft("StickSocket", circle(0.39), ((1, 0), (1, 0.08), (0.93, 0.15),
                                         (0.74, 0.20), (0.55, 0.20)), center)
        button("StickStem", (x, -0.61, 0.39), 0.14, 0.19)
        loft("StickRubber", circle(0.285), ((0.75, 0), (0.94, 0.035), (1, 0.10),
                                         (0.98, 0.15), (0.85, 0.19),
                                         (0.55, 0.21), (0.20, 0.22)), (x, -0.61, 0.53))
        # Subtle raised rubber edge, visible when the controller is tilted.
        tube("StickRim", [(x + 0.263 * cos(t), -0.61 + 0.263 * sin(t), 0.70)
                          for t in (i * 2 * pi / 64 for i in range(64))], 0.012, True)

    for x in (-1.35, 1.35):
        loft("ControlWell", circle(0.51, 64), ((1, 0), (1, 0.02), (0.96, 0.035)), (x, 0.17, 0.29))

    # Four individual bevelled directional buttons pointing into the centre.
    arrow = [(-0.12, -0.10), (0, -0.19), (0.12, -0.10), (0.12, 0.14), (-0.12, 0.14)]
    for angle in (0, pi / 2, pi, 3 * pi / 2):
        shape = [(x * cos(angle) - (y + 0.26) * sin(angle),
                  x * sin(angle) + (y + 0.26) * cos(angle)) for x, y in arrow]
        loft("DPad", shape, ((0.92, 0), (1, 0.025), (1, 0.09), (0.88, 0.12)), (-1.35, 0.17, 0.325))

    for dx, dy, glyph in ((0, 0.30, "Triangle"), (0.30, 0, "Circle"),
                          (0, -0.30, "Cross"), (-0.30, 0, "Square")):
        x, y, z = 1.35 + dx, 0.17 + dy, 0.455
        button("FaceButton", (x, y, 0.325), 0.175, 0.11)
        if glyph == "Triangle":
            points = [(x, y + 0.105, z), (x - 0.095, y - 0.075, z), (x + 0.095, y - 0.075, z)]
        elif glyph == "Circle":
            points = [(x + 0.095 * cos(i * 2 * pi / 40), y + 0.095 * sin(i * 2 * pi / 40), z)
                      for i in range(40)]
        elif glyph == "Square":
            points = [(x - 0.08, y - 0.08, z), (x + 0.08, y - 0.08, z),
                      (x + 0.08, y + 0.08, z), (x - 0.08, y + 0.08, z)]
        else:
            tube(glyph, [(x - 0.07, y - 0.07, z), (x + 0.07, y + 0.07, z)])
            tube(glyph, [(x - 0.07, y + 0.07, z), (x + 0.07, y - 0.07, z)])
            continue
        tube(glyph, points, closed=True)

    loft("Select", rounded_rect(0.23, 0.10, 0.025), ((1, 0), (1, 0.04), (0.85, 0.055)), (-0.32, 0.16, 0.30))
    loft("Start", [(-0.09, -0.07), (0.10, 0), (-0.09, 0.07)],
         ((1, 0), (1, 0.04), (0.85, 0.055)), (0.32, 0.16, 0.30))
    button("HomeBezel", (0, -0.23, 0.30), 0.165, 0.04)
    button("Home", (0, -0.23, 0.34), 0.125, 0.04)

    for x in (-1.33, 1.33):
        shape = rounded_rect(0.72, 0.38, 0.12)
        loft("Shoulder", shape, ((0.88, 0), (1, 0.04), (1, 0.19), (0.86, 0.23)), (x, 0.84, 0.04))
        loft("Trigger", rounded_rect(0.68, 0.40, 0.13),
             ((0.75, 0), (1, 0.05), (1, 0.20), (0.88, 0.24)), (x, 0.91, -0.30))

    # USB recess on the nose: rotate a small rounded rectangle from XY into XZ.
    loft("UsbRim", rounded_rect(0.34, 0.15, 0.035), ((1, 0), (1, 0.018)), (0, 0, 0))
    name, vertices, faces = PARTS.pop()
    PARTS.append((name, [(x, 0.795 + z, y + 0.02) for x, y, z in vertices],
                  [(a, c, b) for a, b, c in faces]))
    for i in range(4):
        loft("LedOn" if i == 0 else "LedOff", rounded_rect(0.07, 0.035, 0.01),
             ((1, 0), (1, 0.008)), ((i - 1.5) * 0.15, 0.58, 0.294))


def write_obj():
    lines = ["# Original DsHidMini motion controller. SPDX-License-Identifier: BSD-3-Clause",
             "# Generated by build/generate_motion_controller.py; X right, +Y nose, +Z face."]
    offset = 1
    for name, vertices, faces in PARTS:
        normals = [[0.0, 0.0, 0.0] for _ in vertices]
        for a, b, c in faces:
            u = [vertices[b][i] - vertices[a][i] for i in range(3)]
            v = [vertices[c][i] - vertices[a][i] for i in range(3)]
            cross = (u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0])
            assert sum(value * value for value in cross) > 1e-20, (name, a, b, c)
            for index in (a, b, c):
                for axis in range(3):
                    normals[index][axis] += cross[axis]
        lines.append("g " + name)
        for vertex in vertices:
            lines.append("v " + " ".join(f"{value:.6f}" for value in vertex))
        for normal in normals:
            length = sqrt(sum(value * value for value in normal))
            assert length > 1e-12, name
            lines.append("vn " + " ".join(f"{value / length:.6f}" for value in normal))
        for face in faces:
            lines.append("f " + " ".join(f"{offset + i}//{offset + i}" for i in face))
        offset += len(vertices)
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text("\n".join(lines) + "\n", encoding="ascii")
    print(f"{OUTPUT}: {offset - 1:,} vertices; {sum(len(f) for _, _, f in PARTS):,} triangles")


if __name__ == "__main__":
    build()
    write_obj()
