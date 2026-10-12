# Stope design from a block model

How stopes were designed along an ore drive in a 2026-10 session (the mine, Deswik 2024.2). Confirm the rules with the user each time: they are this engineer's values, not defaults.

## Rules the engineer gave
- Each stope is at most **40 m** long, with a **5 m pillar** between stopes.
- The minimum width is **2.2 m**, and the maximum height is **20 m measured from the drive back**.
- The stope **takes off from the back of the ore drive**. Its base sits a little inside the drive (1 m below the back was used) so the engineer can cut it against the drive later.
- Blocks below **2.5 g/t** are treated as waste when shaping the stope, and each stope's average grade must stay **above 2.6 g/t**. That average includes every block inside the shape, dilution included.
- Save the stopes to the design's stope layer (here `...\DESIGN MASTER\STOPES\STOPE DESIGN`). Tag them like the existing `LH_Stope` task solids, and interrogate them.

## What existing stopes look like (check before designing)
- On `SCHEDULER\TASK SOLIDS` with `ACT_TYPE=LH_Stope` and `STO_DEV=STO`. Fields: Stope Height / Length / Thickness, STOPE TYPE, LEVEL, DRIVE NUMBER, DIRECTION. The names look like `G210 S100 -55`.
- Combined/optimised stopes have a section every 5 m with only top and bottom wall points. Kennet wants more points on the walls, kept smooth.
- The base is at the back of the lower ore drive and the top is at the floor of the drive above. `Stope Height` is measured from the back (17.5 m on 22 m levels). The lengths are 10–50 m (median 35 m) and the thicknesses 2.1–15 m (median 2.8 m).
- Check that no drive sits inside the planned stope height before building it.

## Method
1. **Find the drive.** "298ID" meant the drive solid whose `INT_ID` was 298, set by an earlier batch interrogation. Read the drive's floor string (one per drive, matched by `DIS Parent ID`).
2. **Export the blocks** around the drive with `scripts/stope-design/export-blocks-around-drive-read-only.cs`. It took ~10 s for 0.4 M sub-blocks out of 55 M.
3. **Design** with `scripts/stope-design/design_stopes.py` (v2, the version Kennet asked for after reviewing v1):
   - Sample the model on a 0.5 × 0.25 × 0.5 m grid in drive coordinates.
   - Give each wall **4 points up its height**: the 4 corners plus at most 2 points between them on each wall. Kennet asked for this in v3, after v2 used 5.
   - At each 1 m station and level, place the walls where they maximise Σ tonnes × (grade − cut-off). That takes in whole ≥ cut-off blocks, including high-grade blocks beside the core, and stops at waste. Keep the width at least the minimum and the base inside the drive.
   - Smooth along strike (5 m) and up the wall.
   - Pick the chainages that maximise metal with each stope above the grade limit, staying inside the main ≥ cut-off run. Without that limit, the optimiser stretches a stope into low-grade tail just to add tonnes.
   - Put a section every 5 m (like the existing stopes) and nudge each wall point 0.25 m outward. Accept the move if the added strip carries the cut-off grade **or** at least 15% of its tonnes are in ≥ cut-off blocks, so blocks cut by a wall get taken in, while the stope stays above the grade limit. Pull a wall in only when the strip meets neither test. Keep neighbouring points within 1 m of each other so the walls stay smooth.
   - Report how much of the ≥ cut-off tonnes near the drive ends up inside: 96–99% here. Kennet still saw blocks left behind at 96%.
   - **v1 lesson:** shaping on the mean grade of a 7 m band, with only top and bottom wall points, left high-grade blocks outside. Kennet caught it on a section view.
4. **Build** the solids with `create-stopes-from-geometry.cs`. It makes a `new Polyface()` with `AddVertex(x,y,z)` and `AddTriangle(i,j,k)` (1-based indices), sets `pf.Layer`, and calls `App.ModelSpace.Entities.AddItem(pf,null,null,true,false)`. Check `pf.IsClosed` and `pf.Volume`, and add the layer attribute fields first.
5. **Interrogate** with `scripts/interrogation/batch-interrogate-solids-to-csv.cs`, with density from the model field. Write the INT_ fields onto the stopes. The Deswik numbers matched the Python estimate within 1%.
6. **Report** each stope's length, width, tonnes and grade, the grade after the drive overlap is cut, and why any stope is shorter than the maximum. Leave saving the drawing to the user.

## 5 m blocks along a whole drive (engineer picks stopes and pillars)
Kennet's follow-up request: instead of finished stopes, cut the whole drive into 5 m blocks whose walls follow the lode, grade each block, and colour them by grade so he can choose the stopes and put pillars where grade is lower.
1. `blocks_along_drive.py <folder> 4 2.5 0.15` uses the v3 wall shaping over the full drive with no stope grade limit, then makes every section drillable (below). It writes one closed solid per 5 m (neighbours share their end section; a remainder under 2.5 m joins the last block) to `blocks_5m_geometry.txt`, plus a Python estimate in `blocks.txt`.
2. `create-blocks-from-geometry.cs` builds them on `STOPE DESIGN\5M BLOCKS`, named `G410 S100 B01..`, with LH_Stope tags plus `BLOCK`, `CH_FROM`, `CH_TO`.
3. Interrogate with `batch-interrogate-solids-to-csv.cs` (set LAY to the blocks layer). The CSV carries the figure attributes, so match rows by `BLOCK`.
4. `write-grades-and-grade-legend.cs` writes the INT_ fields, builds a range legend on `INT_AU` and applies it to the layer. Kennet's bands: 0–1 grey, 1–2 green, 2–2.6 light blue (0,176,255), 2.6–3 blue, 3–5 red, 5–12 strong pink (255,0,255, he likes it), 12+ purple. Legend item `From`/`To` are strings; clone the AU legend's last item for an open-ended top band.
5. 298ID result (v2, drillable): 33 blocks, [tonnes removed]. Ch 0–65 is ore (2.2–8.2 g/t), ch 65–115 is below 1.2 g/t, ch 115–120 is 2.2 g/t, then 0.4–1.8 g/t.
- Kennet called the layer "2m blocks" while asking for 5 m blocks; I named it `5M BLOCKS` and said so.

### Drillability: the rods don't bend
Kennet rejected v1 of the blocks: through the low-grade stretch (blocks 13–20) the walls bent, with the top swinging up to 9 m sideways, which a longhole rig can't drill. Cause: where no window is economic, maximising Σ t × (g − cut-off) picks the emptiest window at each level, so the walls chase scraps. Rules now applied to every section (also use them for stopes):
- Holes are straight lines fanned from the rig in the drive, so each section must be **convex**: each wall's outward step per level may stay the same or shrink going up, never grow (no wall bending back in, no S-bends). A convex section with the base inside the drive is reachable from the collar.
- Each wall segment dips at least **55°** (footwall ≥ 50°, 55° preferred, so broken ore runs; `mine-design-standards.md`).
- Each wall point moves at most **1 m between neighbouring sections** (5 m apart).
- Where no window is economic, place the minimum-width window on the most metal (follow the lode trend), not on the least loss.
- Start from straight walls (base to top), then only nudge points where the section stays drillable. On 298ID the walls ended up straight; metal in ch 0–65 fell about 5% against the bent v1.

## Reading a Deswik `.gmdlb` block model from run_csharp
- Open it with `new Deswik.BlockModel.ManagedGeoModels.BlockModel(path)`, then call `Load(ModelOpenState.OpenForReadNotIndexed, CancellationToken.None)`. Its `BlockGeoModel` property is a `Deswik.GeoModel.BlockModel`. Calling `Deswik.GeoModel.BlockModel.Open(...)` directly returned null. Dispose the wrapper when finished.
- `Metadata` holds the Origin, the parent size (XSize/YSize/ZSize), NX/NY/NZ and the rotation. Sub-blocked models store one record per sub-block.
- The fields include `_XC,_YC,_ZC,_XSIZE,_YSIZE,_ZSIZE,_IJK` and the model fields. Read them with `GetDoubleValue(field, record)` / `GetValue(field, record)`.
- Records are **sorted by `_IJK = k + NZ*(j + NY*i)`** (k fastest), so a box is read by binary searching each (i,j) column. Never loop over all records through reflection.

## Loading layers
`App.ModelSpace.Entities` only holds layers that are loaded. In a file-per-layer `.dcf`, most layers can be present but empty until they are loaded: call `App.LoadLayer(layerName, true)`, then read `layer.Entities`.

## Etiquette in the user's drawing
- Don't change layer visibility in bulk. "Display all layers" means Deswik's display of the ticked layers, not ticking every layer. Turning on all 600+ layers froze Deswik for minutes. Record the visible layers before any change and restore exactly those.

## 5 m blocks for the whole mine
Kennet's follow-up (2026-10-12): run the 5 m blocks for every ore drive, judge where else stopes belong, and **only create blocks above 1.5 g/t**. A block may be shorter than full height when that pays, **never taller**. He combines the blocks into stopes himself. Scripts: `scripts/stope-design/whole-mine/`.

**Learn from the existing stopes first** (`study-existing-stopes-read-only.cs`). At the mine, 214 of the 218 `LH_Stope` task solids sit on an OD. `Stope Height` is 17.5 m from the back on 22 m levels, so the top is the floor of the drive above. That gave the cap: **≤ 17.5 m above the back, and never into a drive overhead**. LEVEL = floor RL − 6000. The lodes are W, G and H, taken from the stope names.

**Pipeline** (whole mine ~10 min):
1. `export-floor-strings-and-stopes-read-only.cs`: floor strings plus the existing stope meshes.
2. `export-corridor-cells-read-only.cs`: one pass over all 55 M model records.
   - Read with `Delegate.CreateDelegate` on `GetDoubleValue(string,long)` and `GetLongValue`, about 0.5 µs per read, 36 s in total.
   - It writes the sub-blocks in the parent cells within 12 m of every lateral non-decline string, from the floor −2 to +28 m. That is 6.4 M cells, as float32 binary.
   - It also writes every ≥ 2.5 g/t cell outside those parent cells.
3. Move the files to the container: the device work folder → `device_stage_files`.
4. `raster.py`: puts the sub-blocks on a 0.5 × 1.25 × 0.625 m raster inside the parent cells, so lookups are vectorised.
5. `run_all.py` / `design.py`: one job per drive, with 4 processes. Drives skipped: DEC, SMP, EWD, SUB, PST, DSP.
   - Sample a grid in drive coordinates, with U ±14 m.
   - Optimise the windows per station and level.
   - Fit the walls with a **non-increasing (isotonic) step fit** per wall, so the section stays convex, with steps ≤ the 55° limit and ≤ 1 m change between 5 m sections.
   - Nudge each wall point with a local evaluation over the two neighbouring blocks only.
   - Lower the top in 2.5 m bands while the top band is below 2.5 g/t, to a minimum height of 7.5 m.
   - Keep a block only if its diluted grade is > 1.5.
6. `virtual.py` + `run_virtual.py`: ore that no drive reaches (about [tonnes removed] here, mostly lode above or below the designed levels).
   - Cluster the ore into pockets.
   - On the nearest OD's 22 m level series, draw a trial OD line through the ore centroid every 5 m along strike.
   - Export the model around those lines (`export-trial-drive-cells-read-only.cs`), then design blocks along them.
7. `post.py`: priority order is OD blocks, then other drives (ACC/MLA/RAD/ISP), then trial drives.
   - Drop a block when it overlaps already-kept blocks by more than 25% (1 m voxels), or sits within 15 m of the decline.
   - `IN_PLAN_PCT` = the share of the block inside the existing stopes (point-in-mesh voxels).
8. `make_geom.py` → `create-blocks-mine.cs`, which puts the blocks on `5M BLOCKS MINE\{ORE DRIVES, OTHER DRIVES, NO DRIVE}` with the trial lines.
9. `interrogate-blocks-mine.cs` → `write-grades-blocks-mine.cs` writes the INT_ fields and applies the '5m blocks AU' legend. Then remove the blocks that interrogate at ≤ 1.5 g/t.

**Checks to report.** Ore capture is the share of the ≥ 2.5 g/t ore inside the current stopes that falls inside the blocks.
- With straight walls (base to top) and U ±8 m, only **66%** was captured, because wide lodes (11–15 m stopes) were missed.
- After the isotonic convex fit and U ±14 m, **80%**.
- Deswik's interrogation matched the Python estimate within 0.3%.

Result 2026-10-12: 2,535 blocks, [figure removed]. OD [figure removed], other drives [figure removed], trial drives [figure removed].

## 5 m blocks v2 (all grades, split at 2.6, floor to floor)
Kennet's redo (2026-10-12), saved on new layers `...\STOPE DESIGN\5M BLOCKS MINE V2\{ORE DRIVES, OTHER DRIVES, NO DRIVE, NO DRIVE\TRIAL ORE DRIVES}`; v1 is left untouched. Scripts: `design2.py`, `run_all2.py`, `run_virtual2.py`, `post2.py`, `make_geom2.py`, `*-v2.cs` in `scripts/stope-design/whole-mine/`.

- **Height.** Base on the drive floor, maximum 22 m. When a drive passes overhead within 22.5 m of the floor (B/T, not blind), the top is that drive's floor: floor to floor, never lowered (`BREAKTHROUGH=Y`). Blind blocks are capped at 22 m and lowered in 2.5 m steps while the top band is below 2.5 g/t (minimum 7.5 m).
- **No grade filter.** Every block with grade is kept on ore drives and trial drives; blocks below 1.5 g/t show grey and semi-transparent. On other drive types (ACC, MLA, ISP, RAD) only blocks above 1.5 g/t are kept, so no blocks follow access drives through waste.
- **Split.** A block below 2.6 g/t is tested for one cut: along strike every 0.5 m (pieces ≥ 1.5 m) or by height every 2.5 m (pieces ≥ 5 m). It is split only when one piece is above 2.6 g/t, at the cut with the most tonnes × (grade − 2.6). Pieces are named `B07a`/`B07b`, with `SPLIT=STRIKE|HEIGHT` and `BASE_ABOVE_FLOOR` for the upper piece of a height split.
- **Overlap order.** Ore blocks (> 1.5 g/t) claim space before grey ones, then OD, other drives, trial drives, as in v1. The decline standoff is still 15 m.
- **Legend `5m blocks AU v2`** on `INT_AU`, from the drawing's `AU` legend bins (Kennet's image): below 1.5 grey (LegendItem `Transparency` 90; 255 is opaque), 1.5–2 cyan, 2–3 green, 3–4 yellow, 4–6 orange, 6–10 red, 10+ magenta.
- **Invalid solids.** 6 of 4,034 blocks came out with `Polyface.Volume == -1` (end sections crossing on a tight bend) and were skipped by interrogation. Check for that after creating blocks; `hull_invalid_blocks.py` rebuilds them as convex hulls, then interrogate just those.
- **Result.** 4,034 blocks, [figure removed] (Deswik). Grey < 1.5: 1,272 blocks, [figure removed]. 1.5–2.6: 1,081 blocks, [figure removed]. Above 2.6: 1,681 blocks, [figure removed]. 703 split pieces; 351 of them are above 2.6 ([tonnes removed]). 2,043 blocks are floor to floor (B/T).
- **Interrogation is slow.** About 4,000 solids take more than the 60 s tool wait; the run keeps going in Deswik. Poll the CSV size until it stops growing before writing grades.

## 5 m blocks v3 (Kennet's review of v2)
Kennet (2026-10-12) on v2: "it looks much better" but asked for these changes. They are built on `...\STOPE DESIGN\5M BLOCKS MINE V3`, and v2 is kept. Scripts: `design3.py`, `run_all3.py`, `run_virtual3.py`, `post3.py`, `make_geom3.py`, `*-v3.cs`, `replace-invalid-blocks-v3.cs`.

- **Always full height.** Blocks are never lowered. They go floor to floor where a drive is above within 22 m, otherwise 22 m. His example: a low-grade block that v2 had cut to 9.5 m should "extend all the way".
- **No gaps under a block.** Ore drive and trial drive blocks are kept even with no grade. His example: a block with ore on top had "nothing at the bottom".
- **Split only vertically.** Use one cut along strike, so both pieces are full height, never one above the other. Try for a piece above 2.6 g/t first. If no cut gives one, try for a piece above 2.0 g/t (only when the block is below 2.0). `SPLIT` holds `STRIKE 2.6` or `STRIKE 2.0`.
- **Overlap bug fixed.** The 1 m voxel overlap check counted voxels shared on the boundary between neighbouring blocks of the same drive, so small pieces were dropped. Overlap is now only checked against other drives' blocks.
- **Legend.** Kennet edited `5m blocks AU v2` himself (grey below 2, cyan 2–2.6, green 2.6–3, then yellow, orange, red, magenta). Read it and colour by the first item that matches; never rebuild or `Clear()` it.
- **Invalid solids are found at creation.** `create-blocks-mine-v3.cs` lists solids with Volume < 0 (15 of 4,202). Run `hull_invalid_blocks.py` and `replace-invalid-blocks-v3.cs` on them before interrogating. Interrogation and grade writing only touch blocks without `INT_DATE`, so both can be re-run.
- **Result.** 4,202 blocks, [figure removed]. Below 2: 2,031 blocks, [figure removed]. 2–2.6: 698 blocks, [figure removed]. Above 2.6: 1,473 blocks, [figure removed]. 660 split pieces.
