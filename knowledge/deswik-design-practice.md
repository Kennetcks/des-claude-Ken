# Deswik design practice at underground sites

How mine design is organised in Deswik.CAD / Deswik.IS at sites in the engineer's document library (Sites A to E). Use it to read an unfamiliar site drawing quickly and to set up new designs the way planners expect. Engineering values are in [mine-design-standards.md](mine-design-standards.md). Always follow the conventions already used in the open drawing over these.

## Layers and versions

- **Centrelines are the design.** The centreline polyline carries geometry and every attribute; walls, solids and annotation are generated from it. Keep one up-to-date centreline per drive in the master layer.
- Typical tree (Site D): `DESIGN | DEV | LAT` (lateral centrelines, with child annotation layers NAME, DIMENSION, SUPPORT, GRADE), `DESIGN | DEV | VERT` (rises and service holes), `DESIGN | DEV | WALLS` (+ `ANNOTATION`), `DESIGN | STOPE | X SEC`, `SOLID`, `SLICE`. Grouping layers stay empty. Designs under review go in a separate `NEW DESIGN` tree that mirrors `DESIGN`.
- Version control (Site E): per level, `Proposed\<design no>`, `Approved\<design no>` (never edited, permanent record) and a `Live Master Design` layer holding only current approved strings with superseded parts removed. Survey takes strings only from the live master.
- Superseded designs (Site A): set `Active = 0` and move to `DESIGN|ARCHIVE`; mark the PDF "SUPERSEDED".
- Before importing a string or solid into a master layer, strip its attributes; imported entities add their attribute fields to the layer and the field list grows into clutter. Never delete a field that is in use on the layer (it wipes that value from every entity).
- Copy approved designs into the master with *Paste at original coordinates*, then refresh annotation layers ("Refresh Layer Objects in Children").

## Attributes on centrelines

Common set (names vary by site): `ZONE` (orebody / mining area), `LEVEL`, `ACT` / `ACT_TYPE` / Function (3-letter activity), `LABEL` (unique suffix), `FINCAT` / Cost Cat (CAP / OP), `MINING` (HORZ, VERT, SVC, CABLE, STRIP), `DESIGN` (design number or stope note version), `DIMENSION` / `PROFILE` (must match a defined tunnel section), `STATUS`, `SUPPORT` (ground support standard), `SUPPORT INT` (intersection cable standard, first segment of the turnout only), `NORTHING` / `EASTING` / `GRID`, `Priority`.

- **STATUS** values: CONCEPT (LOM), REVIEW, MINEABLE (area note), APPROVED, MINED. Don't set a stope MINED until every activity on it is finished.
- **Activity types are QA.** Deswik.IS *Create Tunnels* only builds walls and solids for centrelines whose ACT + DIMENSION match a defined activity type, so a missing wall means a bad attribute.
- **Names** are built by formula (*Modify > Attributes > Using Formula*): Site D `ZONE LEVEL ACT LABEL`; Site A `ZONE LEVEL [LENS] NORTHING FUNCTION DIRECTION ID` (remove the double spaces the formula leaves). Decline and its stub drives use a ramp-style name without LEVEL (one decline, not "1945 Decline", "1915 Decline").
- Levels named by the access take-off RL (rounded) or a nominal number; stubs off the decline by RL rounded to 5 m; production crosscuts by the first three digits of the northing; stopes by the centroid on a site grid (Site D 25 x 25 m grid labels).
- FINCAT simplification (Site D): ore drives and crosscuts operating, all other development capital, stopes operating.
- Typical function codes: ACC, DEC/INC, FWD, ODR/OD, XC, SPL/SP, SMP, SVC, FAW, RAW, FAR, RAR, ESC/EWD, PMP, DDC/DDP, SLD (slot drive), BMIN (backfill mining), BDO (backfill dig-out), RHB (rehab), CBB/CBC (cable brow/crown), RBA (raisebore slot), PRD (production drilling), STP (stope), BF/PF/RF (fill).

## Geometry conventions

- Lateral polylines point in the direction of mining. Vertical polylines run **bottom to top** whatever the drilling direction; the bottom point sits at the backs of the lower drive, the top at the floor of the upper drive.
- Break lateral strings at the level split RLs and give each piece that LEVEL (use judgement for a short tail across a split). Don't break vertical development or service holes; their LEVEL is the bottom (paste holes take their stope's name).
- Insert points 2 m from each end of a vertical string so dependencies can be built to the drives.
- Walls: open at the start of a drive (fillets), closed at the end. Generate with *Deswik.IS > Create Tunnels > Create polylines and solids*, then break and delete start / intermediate end walls and add fillets.
- Fillets: trim or extend the turning drive's walls to the main wall in plan (don't snap, keep the wall gradient), insert points at the intersections and 2–3 m along each wall, and move the end points onto the main wall points. Spin in 3D to check.
- Stripping (wall, back, floor) is drawn as its own polyline on the WALLS layer with `MINING = STRIP`, red, plus a mining instruction text ("Mine" for single pass, "Strip" for a second pass). Back strips are drawn 0.25 m inside the drive outline so they show on plots.
- Vertical outlines: insert a block (circle or rectangle rotated to the design azimuth) at the top, bottom and each level crossed; give each the LEVEL of the drive it sits in.
- Stope cross sections snap to a regular grid (tessellated guideline surface) so adjacent stopes match; register a section to the next with *Modify > Register > To working plane or elevation* to compare. Stopes for concept / area notes come from cross sections; approved stopes from solids (*Create from solids*).
- Store manual stope-note values (drill metres, fill volumes, rate overrides) as attributes on the source solid, not only in the schedule, so they survive re-creating the task. Use −1 to suppress a derived task (Site D).

## Solids, tasks and scheduling

- *Draw > Tunnels > By Rule* with the PROFILE attribute builds solids for every centreline at once (Build List, then pick the section for each value). Square LHRs: *Draw > Solids > Link Polylines* between top and bottom outlines. Round raises: *Draw > Tunnels > By Section*. See [tunnels-by-rule.md](tunnels-by-rule.md).
- Derived tasks hang off the parent by filter: fibrecrete, development cables, extra ground support, boxhole pad, shrink, fill, paste cure, brow cables. They're created once per parent string, so put extra support for one cut on a ~3.5 m sub-string.
- Edit task attributes in CAD and batch-update to the schedule, or update both; otherwise the next re-create brings old values back.
- Adding a new activity type means adding it to every CAD and scheduler filter and report line, or its metres and tonnes vanish from the outputs.
- Northing on stopes drives naming and grouping; Site E rounds it to the nearest 5 by formula before creating tasks.

## Plans, controls and hazards

- Minimum plan content (Site A): centrelines, names, dimensions, ground support, gradients, walls, wall / back stripping, fan stripping, vent controls, raisebore specs, HOLD / STOP / BREAKTHROUGH annotations, concept and previous designs (walls only), void and service-hole hazard zones, survey as-built floor strings, structures and lithology sliced on the design plane, diamond holes with 10 m hazard zones (13 m under GEO CONTROL), date, A3 1:500 (1:250 for paste development), north arrow, grid ticks.
- Controls: **STOP** = significant risk, removed only by re-issuing the design; **HOLD** = stop until a condition is met (numbered in a hold register); **BREAKTHROUGH** = new memo before breaking into a void; **GEO CONTROL** = geologist may steer ±1 m per cut for up to 3 cuts.
- Wall colours by STATUS (Site D): CONCEPT colour 4 dashed, APPROVED 29, MINED 253, REVIEW 2 heavy; stripping and instructions red (colour 0) so they always show. Apply with *Modify > Colour and Line Type from Legend*.
- Surpac export (Site E): set a single "Surpac String ID" attribute (centreline 110, walls 111, cables 112, stripping 8, text 1000) and export with text as points. Other sites use 201 outline / 202 floor centreline / 208–209 rise outline / centre, 211 stripping, 212 stop points (Site B).
- Before any design goes out, check it against the latest survey strings and DTMs, the void model, planned stopes, service holes, ungrouted and planned drillholes, and HV cables within 20 m.
