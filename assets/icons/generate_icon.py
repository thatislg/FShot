"""Generate the F-Shot app icon (1024x1024, transparent).

Concept: a "selection frame + camera aperture" — the core idea of a
screenshot/annotation tool (drag a frame to select a region, then capture it).

  - frame outline : #3D2B1F (dark brown)
  - corner handles: #FB923C (orange)  -> "selection"
  - aperture lens : #7BD5F5 (light blue) -> "capture"

Supersamples at 4x then downscales with LANCZOS for smooth anti-aliased edges.
Usage:  python generate_icon.py
Output: FShot-AppIcon-1024.png (same folder)
"""

from PIL import Image, ImageDraw

OUT = 1024
SS = 4

BLUE = (123, 213, 245, 255)     # #7BD5F5
DARK = (61, 43, 31, 255)        # #3D2B1F
ORANGE = (251, 146, 60, 255)    # #FB923C
WHITE = (255, 255, 255, 255)
FRAME_TINT = (123, 213, 245, 48)  # very light blue fill inside the frame


def draw_icon(size: int) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    k = size / OUT

    def s(v):
        return v * k

    # --- Selection frame (rounded rect) ---
    fx0, fy0, fx1, fy1 = 78.0, 78.0, 946.0, 946.0
    corner = 132.0
    frame_w = 36.0
    d.rounded_rectangle(
        [s(fx0), s(fy0), s(fx1), s(fy1)],
        radius=s(corner),
        fill=FRAME_TINT,
        outline=DARK,
        width=int(s(frame_w)),
    )

    # --- Corner selection handles (orange L-brackets) ---
    arm = 86.0
    handle_w = 34.0

    def bracket(x, y, dx, dy):
        # Draw an L-shaped handle whose corner sits at (x, y).
        d.line(
            [(s(x), s(y)), (s(x), s(y + dy * arm)), (s(x + dx * arm), s(y + dy * arm))],
            fill=ORANGE,
            width=int(s(handle_w)),
            joint="curve",
        )

    bracket(fx0, fy0, 1, 1)       # top-left
    bracket(fx1, fy0, -1, 1)      # top-right
    bracket(fx0, fy1, 1, -1)      # bottom-left
    bracket(fx1, fy1, -1, -1)     # bottom-right

    # --- Camera aperture (centered) ---
    cx = cy = 512.0
    # Outer lens body (light blue) with dark outline.
    d.ellipse(
        [s(cx - 205.0), s(cy - 205.0), s(cx + 205.0), s(cy + 205.0)],
        fill=BLUE,
        outline=DARK,
        width=int(s(frame_w)),
    )
    # Inner lens rim (orange).
    d.ellipse(
        [s(cx - 110.0), s(cy - 110.0), s(cx + 110.0), s(110.0 + cy)],
        fill=ORANGE,
    )
    # Aperture opening (dark) + a white glint for a "lens" feel.
    d.ellipse(
        [s(cx - 46.0), s(cy - 46.0), s(cx + 46.0), s(cy + 46.0)],
        fill=DARK,
    )
    d.ellipse(
        [s(cx - 66.0), s(cy - 82.0), s(cx - 22.0), s(cy - 38.0)],
        fill=WHITE,
    )

    return img


if __name__ == "__main__":
    big = draw_icon(OUT * SS)
    icon = big.resize((OUT, OUT), Image.LANCZOS)
    icon.save("FShot-AppIcon-1024.png")
    print("Wrote FShot-AppIcon-1024.png")
