# Backfill, paste and service holes (reference values)

Fill design rules from the engineer's library: Site B paste-fill and backfill guidelines (Site B), Site L paste-fill design guideline (Site L), Site H and Site G D&B standards, plus the AMPS training. Site values differ, so name the source and follow the site's own rules and the engineer's. Stope geometry is in [mine-design-standards.md](mine-design-standards.md); stope tasks such as fill and paste cure are derived tasks (see [deswik-design-practice.md](deswik-design-practice.md)).

## Fill types

| Type | Use | Notes |
|---|---|---|
| Rock fill (development waste) | cheapest, bottom-up longitudinal and Avoca | no cure; rills 35–38° (use 38°); can't be exposed; never use dirt with excess fines |
| CRF / CAF (cemented rock fill) | pillars to mine against or under | 5 % or 8 % cement (geotech memo); 3–4 x the cost of rock fill; keep machines 3 m from a vertical CRF face |
| Paste | tight fill, exposures, top-down | tailings > 65 % solids + binder; needs walls, plugs, cure time and a plant |
| Hydraulic fill | cycloned sands | drains water; barricade failure risk (liquefaction) |

Rock fill tonnes = void volume (stope + drive, from CMS) x 2.1 (SG 2.8 x 0.75 bank-to-loose). Fill to 1.5 m below the grade line. A CRF pillar to fire against needs ≥ 5 m; a CRF sill in a drive: bench 1.5–2 m and fill in 25 m sections retreating to the crosscut.

## Paste strength targets

| Situation | UCS | Source |
|---|---|---|
| Vertically exposed paste / unexposed target | 160 kPa | Site B |
| Mining into or beside paste | 160 kPa | Site B |
| Mining on top of paste | 200 kPa (Site B); 1 MPa for loaders (Site L, ≥ 3.5 % cement, top 1 m waste rock for traction) | |
| Mining under paste | 750 kPa (Site B); plug 2 m + ½ W, or 5 m + ½ W with loose fill above (Site L, W = stope width below) | |
| Re-entry to the containment zone, next pour, no blasting within 80 m | 100 kPa | Site B |
| Design factor of safety on exposure UCS | 1.25 | Site L |

UCS grows with binder content and hydration time (sites fit UCS = f(cement %, solids %, days) from daily 2 / 7 / 28-day samples). Mining sooner than 28 days needs more cement. Binder 4 % minimum for a plug pour against a paste-abutted wall (Site L).

## Paste walls (bulkheads) and pours

- Fibrecrete wall on a frame, ≥ 400 mm thick, 5 m arc radius, ≥ 15 MPa at 48 h before filling (Site L also 21 MPa at 7 days). Depth indicators every 1.5 m and within 0.5 m of the abutments.
- Location: ≥ 4 m upstream of any intersection (Site B), 5 m from the stope brow (Site L), 5–6 m from the final drawpoint and ≥ 3 m from intersections (Site A). Designs valid for 5–7 m wide and high drives (Site L up to 6.0 W x 6.4 H).
- Plug pour first: fill to 3–4 m above the wall, then stop until it reaches 100 kPa. Max head on a wall 15–16 m; continuous filling only if the breather outlet is < 16 m above the lowest wall.
- Each stage ≤ the containment zone volume (bund 3 m high, ≥ 6.6 m base; an up-gradient drive holds less). Exclusion zone stays until paste within 8 m of the wall reaches 100 kPa. Plan ≤ 800 m³ per shift (Site B); vertical rise normally < 10 m per 24 h (Site L).
- Volumes from validated survey solids: union the drive solid and the stope CMS, cut at the paste wall and the previous panel, report volume in 1 m vertical increments. Never use design voids.
- The stope name for the pour comes from the bogging horizon.

## Fill, breather, drain and service holes

| Hole | Rule | Source |
|---|---|---|
| Fill + breather | break into the highest part of the stope; fill hole 0.5 m above the breather; ≥ 1.5 m apart; collars outside the paste wall | Site B, Site H |
| Diameters | 152 mm, fill hole reamed 2 m to 202 mm for the spigot (Site B); both 204 mm, fill collar reamed to 300 mm for 3 m (Site H / Site G); breather 152 mm, 300 mm below the fill hole (Site L) | |
| Downward fill holes | 202 mm if dip > 15°; 152 mm needs ≥ 35° dip with the top 5–10 m reamed to 202 mm | Site L |
| Site G | paste hole 204 mm (collar 300 mm for 3 m), breather 152 mm 1.5 m below, collared 8–9 m back from the brow | |
| Fill level when holes are < 0.5 m apart vertically | fill to 0.5 m below the lowest breakthrough | Site B |
| Blind stopes | can't be tight-filled from upholes unless poly is pushed to the backs (breather pulled back 0.5 m) | Site B |
| Drain holes | 204 mm (152 mm with a reamed collar), designed as downholes from the top drive, ≈ 40 m uphole at Site G | Site H |
| Capital paste holes | standpipes at collar and toe: ream both ends to 300 mm for 3 m | Site H |
| Dip before building the wall | confirm breakthrough, length and which hole is higher; recalc volumes if the RL differs | Site B |

Reticulation: 150 NB steel (Sch 80, 10.35 MPa at Site B; Sch 40, 6.3 MPa at Site L), HDPE only near the discharge (≤ 200 m back, PN16). Friction ≈ 7–11 kN/m per metre in 150 NB steel; max poly length = (rated pressure − paste head − hole friction) / poly friction.

## Backfill hazards to flag in a design

- Undercuts of the upper drive (≥ 1 m from CMS) leave unsupported wedges that rill won't fill; stop the brow / rill and refer to geotech (bolts hold ≈ 1.5 m in good ground).
- Hanging walls flatter than the rill angle leave voids; oversize (> 1/3 stope width) wedges; fines wash out and sinkholes form under a compacted 1 m crust. Bog the crust before shrinking.
- Check the stope face is vertical and not undercut (CMS + memo) before machines go to an open brow. Track fill tonnes against design: a low number means a hang-up.
- Paste below 100 kPa can liquefy under blasting or a rockfall.
