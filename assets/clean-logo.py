"""
Cleans up the hand-made logo: snaps the two ink colours to one value each,
turns the near-white background into real transparency, and writes the sizes
the repository uses.

The source carries ~19k distinct colours and a background drifting between 253
and 255, which is what a lossy round trip leaves behind. Every pixel here is a
blend of one ink over white, so the coverage can be recovered per pixel and
re-applied with the exact ink colour.
"""
from PIL import Image

NAVY = (0, 0x21, 0x66)
ORANGE = (0xFA, 0x76, 0x01)


def coverage(value, ink, background=255):
    """How much ink is in this channel, given value = a*ink + (1-a)*background."""
    span = background - ink
    if span == 0:
        return None
    return (background - value) / span


def clean(src):
    # Flatten onto white first. A transparent pixel would otherwise convert to black
    # and be read as full ink coverage, so running this twice would fill the
    # background with colour instead of leaving it alone.
    src = src.convert("RGBA")
    im = Image.new("RGB", src.size, (255, 255, 255))
    im.paste(src, mask=src.getchannel("A"))
    w, h = im.size
    px = im.load()
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    op = out.load()

    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            if r > 250 and g > 250 and b > 250:
                continue  # background noise

            if b > r:
                ink, a = NAVY, coverage(r, NAVY[0])          # red has the widest span for navy
            else:
                ink, a = ORANGE, coverage(b, ORANGE[2])      # blue has the widest span for orange

            a = max(0.0, min(1.0, a))
            if a <= 0.01:
                continue
            op[x, y] = (ink[0], ink[1], ink[2], int(round(a * 255)))

    return out


if __name__ == "__main__":
    # Reads the design master and writes both it and the package icon:
    #   python assets/clean-logo.py
    import os

    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    master = os.path.join(root, "docs", "logo-master.png")
    icon = os.path.join(root, "src", "MqttServices.Core", "logo.png")

    cleaned = clean(Image.open(master))
    cleaned.save(master, optimize=True)
    cleaned.resize((512, 512), Image.LANCZOS).save(icon, optimize=True)
    print("wrote", master)
    print("wrote", icon)
