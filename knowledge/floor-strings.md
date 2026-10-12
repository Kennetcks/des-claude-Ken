# Floor strings from development solids

How to draw design strings (centrelines) from scheduled development task solids. These rules were settled with the engineer after several rounds; follow them unless the user asks for something different.

## What to build
- **Lateral development** (LAT_VERT = LAT): one string per design drive, on the **floor**, midway across the drive.
- **Vertical development** (LAT_VERT = VERT: raises, rises, escapeways): one straight **centre line** through each raise's solids, from bottom to top.
- **Stopes** (STO_DEV = STO): skip them. Don't draw anything.
- Put the strings on their own layer (e.g. `DESIGN\FLOOR STRINGS`). Don't touch other layers.

## The rules the engineer settled on
- **One plain line per drive.** Group solids by the drive they belong to (in Deswik/IS task solids, the `DIS Parent ID` / DIS parent GUID identifies one design drive). Order solids along the drive by shared vertices: adjacent solids share exact vertices (round to ~5 cm to match them).
- **No linking, no extending, no snapping** between drives. Each line ends where that drive's own solids end. Earlier attempts that joined branches to the main drive, extended dead ends, or snapped ends to the nearest string all produced kinks and spikes, and the engineer rejected them.
- **End points stay on the drive's own line.** Stop the line where the drive's direction leaves its own last solid. Don't take the end from a neighbouring solid (that made ends bend 20-80° off the line).
- **Raises:** a straight line through the raise solids' centres. Check every raise ends up steeper than ~45°; a flat or stepped raise means points were taken from the wrong solids.

## Reading the floor height (pitfalls)
- `Polyface.GetMinZ(x, y, BoundingBox)` gives the floor under a point; it returns `double.MaxValue` when the point is outside the solid.
- **Junction solids are boolean-cut** where they overlap the main drive. Inside the overlap their lowest face is the other drive's *roof*, so a naive floor read puts the point several metres too high (up to the drive height). Fix: take the **lowest floor of all solids that touch vertically** at that XY (the true floor of the combined opening).
- **Don't read the floor exactly on a face edge.** At the end face you pick up the face wall. Read the height **0.3-0.6 m inside** the end and use that for the end point.
- Small dips at junctions where an access meets a decline are real (the decline floor is lower). Smooth only steps > ~0.75 m that aren't in the design.

## Attributes on the strings
Copy from the drive's solids onto each string: `ACT_TYPE`, `LAT_VERT`, `DIS Parent ID`. **Add these as attribute fields on the layer first** (see `attributes-and-legends.md`), otherwise the user can't see them in Properties.

## Check before reporting
Run a read-only review over every string and report counts, not just "done":
- jumps > 1 m at grades steeper than ~30% on lateral segments,
- bends > ~45° between segments (many are real design turns, e.g. ODs; list examples by handle),
- points outside every solid, doubled-back segments, raises flatter than 45°,
- lateral drives that ended up with no string.
Re-run the review after every rebuild. Handles change on every redraw, so tell the user.

See `scripts/floor-strings/` for working examples.
