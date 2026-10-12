# Decline and level design

Rules and working style from a decline and level redesign around new stope groups (the mine, October 2026, four versions until the engineer accepted v4). Use them as defaults for any decline/level concept design. Values marked *(site)* came from that area; measure or ask for your own.

## Design rules

| Topic | Rule |
|---|---|
| Decline gradient | Always 1:7 down. Never change the gradient; use a bigger loop if a level needs more height. |
| Existing decline | Keep the existing decline exactly as drawn down to where the redesign starts *(site: first 300 m)*. Redesign only below that. |
| Joints | The new decline starts exactly on the old string, no gap or kink. Curves flow into each other. |
| Loops | Stacked loops, 22 m radius. Each loop drops about one level (~22 m) so every level comes off the same part of the loop. Follow the site's reference decline style. |
| Standoff | At least 15 m from keep-away solids, and well clear of the stopes. Report the measured value. |
| Levels | At the base of the stopes, following stope spacing. Different groups can sit at different RLs *(site: Group B ~8 m above Group A)*. |
| Accesses | One ACC from the decline per stope group per level. No multiple accesses. |
| Take-offs | Leave the decline perpendicular, at least 10 m straight (absolute minimum 7.5 m), before turning. Ramps too. |
| Access grade | Rise at about 1:50 from the decline to the ore drive. |
| ISP | One ISP on each MLA/ACC. |
| RAR | A RAD runs off each access to the RAR. RAR at least 10 m from any access, ISP or ore drive, and clear of the decline. |
| Ore drives | Always design them: one continuous drive along strike under each stope group. |
| Top stopes | A short ramp up from the decline to reach top stopes is fine. |
| Stopes planned from elsewhere | Still need their own access from the new decline. |
| Decline position | Centre on the orebody, biased toward the lode carrying most production; step loops with depth to follow it. |
| Stopes behind the loop | Route the access around the loop. Never cut through at the same level. |
| Clearance on level | Nothing within 10 m of the decline on the same level, apart from the take-off. |
| Stope groups | Use the group the engineer set on the stopes (e.g. Transformation Details = Group A / Group B) to decide which access each stope belongs to. |

What went wrong in earlier versions (avoid): spiral with ACC/OD/RAR generated per level gave too many branches and fragmented ore drives; one MLA per level instead of one ACC per group; glitches at joints; decline too close to keep-away solids; ramp take-off too tight.

## Checks to report (measured, per rule)
- **Decline:** no gap at the joint, 1:7 everywhere, no sharp bends, ≥ 15 m from keep-away solids.
- **Each level:** one ACC per group, perpendicular ≥ 10 m, rising ~1:50, one ISP.
- **RAR:** ≥ 10 m from every access, ISP and ore drive; clear of the decline.
- **Clearance:** nothing within 10 m of the decline on the level, apart from the take-off.
- Flag accesses running downhill to the ore drive and very long accesses for small tonnes.

## How the engineer wants it delivered
- A Deswik **.dcf** (not a picture or a script). Every string has attributes **LEVEL, ACT_TYPE, LODE/group**.
- Save next to earlier versions with the site's naming (e.g. "SITE - ..."); **never overwrite earlier versions**, bump the version.
- Include a **plan, long section and cross section** image with each version.
- Follow the site's existing designs as the layout reference.
- Report measured results against each rule.
- It's a concept for engineering review, not an approved design. Always ask for a visual check in Deswik.CAD.

## Deswik .dcf file notes (for building files outside Deswik)
- A .dcf is a zip. Each layer is a separate VectorDraw XML part; the root part holds the layer table.
- Polyline vertices are base64: a count, then x, y, z, bulge per point.
- Attributes are the entity's XProperties.
- New strings: copy an existing string in the same file as a template, give it a new handle and GUID, set vertices and attributes.
- Level number = RL − 6000 at that site (Z 6390 = level 390) *(site)*.
- If an existing decline string still carries Deswik decline design settings, editing it with the decline tool may rebuild its full length.
- When the plugin is connected, prefer building strings directly in the open drawing with `run_csharp`; the .dcf route is for when Deswik isn't reachable.

See `scripts/decline-design/` for the Python set that produced the final version (its README lists inputs and the area-specific settings: DYT, DRL, XMIN, top-ramp waypoints, level offsets).
