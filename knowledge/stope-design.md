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

## 5 m blocks v4 (horizontal splits): current rules
Kennet (2026-10-12) first asked for vertical splits (v3), then corrected himself: "you need to split the solids horizontally, I made a mistake". v4 is the same as v3 except that the one cut is by height: every 2.5 m, both pieces at least 5 m, one piece above the other. Together the two pieces always fill the full column, from floor to floor or 22 m. The split looks for a piece above 2.6 g/t first, and above 2.0 g/t only when no cut gives that and the block is below 2.0. `SPLIT` is `HEIGHT 2.6` or `HEIGHT 2.0`, and `BASE_ABOVE_FLOOR` gives the upper piece's base. The blocks are on `...\STOPE DESIGN\5M BLOCKS MINE V4`, and V2 and V3 are kept. Scripts: `design4.py`, `run_all4.py`, `run_virtual4.py`, `post4.py`, `make_geom4.py`, `*-v4.cs`.

Result: 4,756 blocks, [figure removed]. Below 2: 2,132 blocks, [figure removed]. 2–2.6: 859 blocks, [figure removed]. Above 2.6: 1,765 blocks, [figure removed]. 1,763 split pieces. 14 bend solids were rebuilt as hulls.

## 5 m blocks v5 (also split ore blocks with a low-grade end): current rules
Kennet (2026-10-12) on v4: "you need to be smart sometimes". A block can be above 2.6 g/t over its full height while its top (or bottom) section is below 2. Split it anyway, "even though the whole block is still above the cut-off", so the high-grade and low-grade sections show as separate blocks. v5 is v4 plus this rule:
- **Ore blocks too.** A block at or above 2.6 g/t gets one horizontal cut when one piece is above 2.6 and the other is below 2.0. The cut is chosen to maximise tonnes × (grade − 2.6) of the high piece, and `SPLIT` is `HEIGHT 2.6 LOW<2`.

The blocks are on `...\STOPE DESIGN\5M BLOCKS MINE V5`, and V2 to V4 are kept. Scripts: `design5.py`, `run_all5.py`, `run_virtual5.py`, `post5.py`, `make_geom5.py`, `*-v5.cs`.

Result: 4,910 blocks, [figure removed]. Below 2: 2,276 blocks, [figure removed]. 2–2.6: 864 blocks, [figure removed]. Above 2.6: 1,770 blocks, [figure removed]. 305 ore blocks were split into high and low: [figure removed] versus [figure removed].

## 5 m blocks v6 (recover ore left beside the walls)
Kennet (2026-10-12) on v5 sent sections where stopes left good ore behind. He said "I could always move the position of the ore drive later" and "you do not need to move the drive, focus on the stopes". The blocks are on `...\STOPE DESIGN\5M BLOCKS MINE V6`, and V2 to V5 are kept. Scripts: `design6.py`, `run_all6.py`, `run_virtual6.py`, `post6.py`, `make_geom6.py`, `*-v6.cs`, plus `capture.py` and `missed.py` to measure recovery.

- **Base free of the drive.** The stope base stays at the drive floor level, but its walls are no longer held inside the drive width. Before, every stope kinked back to the drive at the bottom and left the ore beside it.
- **Wider search and the standard footwall.** Walls range ±17 m, which is the edge of the exported corridor. The dip minimum is 50°, the standard footwall minimum; 55° is only preferred. Walls have 5 points instead of 4.
- **Whole-wall moves.** Besides moving single points, the nudge loop translates a whole wall or tilts it about its base or top. Single-point moves couldn't pass the convex and dip checks, so the walls never reached ore a few metres away. Each drive takes about 3× longer (the mine runs in about 21 min).
- **How to check recovery.** `capture.py` gives the share of ≥ 2.6 g/t unmined model ore inside the blocks; V5 had 71%. `missed.py` sorts the rest by distance. For V5, [figure removed] sat within 4 m of a block, mostly beside the walls, and about [tonnes removed] sat 6–25 m away, out of reach of the drives.
- **Result (Deswik).** 4,770 blocks, [figure removed]. Above 2.6: 1,787 blocks, [figure removed] (V5: [figure removed]). 2–2.6: [tonnes removed]. Below 2: [figure removed].

## 5 m blocks v7 (trial drives for unreached ore, intersection solids)
Kennet (2026-10-12) on v6: "go for them, and create new trial drives for them", meaning the ≥ 2.6 g/t ore 6–25 m from any block. He also asked for intersection solids: "above the MLA, sometimes you need to create a solid too, this is called intersection". The blocks are on `...\STOPE DESIGN\5M BLOCKS MINE V7`. V7 is the V6 blocks unchanged, plus the new trial drive blocks and the intersections.

- **Finding the pockets (`pockets2.py`).** Take ≥ 2.6 g/t unmined ore more than 4 m from any V6 block. Cluster it in 10 m cells (≥ 150 t a cell, 26-connected) and keep pockets of ≥ 1,000 t. Each pocket is cut into slices on the nearest ore drive's 22 m level series, so a new drive sits on a real level. A PCA strike line is fitted per slice, giving the trial floor line (T000…). The first run gave 107 lines through [tonnes removed] of ore.
- **Model.** `export-trial-drive-cells-v7-read-only.cs` exports the parent cells within 17.5 m of the new lines. `raster.py n` rasterises corridor, first trial drives and new trial drives together.
- **Blocks (`run_virtual7.py`, `post7.py`).** The v6 design runs on each T line. V6 blocks claim their space first, and a new block is dropped if over 25% of it overlaps a kept block, or if it is within 15 m of the decline. A T drive with no block above 2.0 g/t is dropped as a whole, since it only adds grey blocks.
- **Intersections (`intersections.py`).** Where two drive ends face each other within 0.5–12 m on the same floor (±2 m), the gap between them is the junction, usually where the MLA or an access comes in. That gap gets a solid: the convex hull of the two end blocks' end faces, so it is full height like the blocks. Each end is used once, and a solid is dropped if over 25% of it overlaps another block. Intersection solids are named `<lode><level> INT <drive>/<drive>` and go on `...\INTERSECTIONS`. A drive that starts from the side of another drive (a T-junction) is not filled yet.
- **Recovery.** `PRE=n python3 capture.py kept6.pkl kept7f.pkl` measures it on the bigger raster. The share of ≥ 2.6 ore inside blocks went from 68% to 76%.
- **Result (Deswik).** 5,682 blocks, [figure removed]. Above 2.6: 1,960 blocks, [figure removed] (V6: [figure removed]). The new trial drives add 877 blocks, [figure removed], of which [figure removed] is above 2.6. The ore they reach is mostly diluted into cyan and grey full-height blocks. The 35 intersections add [figure removed], including W148 INT 3000FBF/3000D19 at [figure removed].

## 5 m blocks v8 (no overlaps, close lodes): current rules
Kennet (2026-10-12) on v7 said "the stopes can't intercept them, otherwise I am going to be counting the grade twice". He also said "some time you have lodes close to each other, you need to check if the stope including both lodes grades more than 2.6, otherwise you need to choose one, or optimise getting the higher grade of both". V7 had 171 overlapping block pairs (4,400 m³). 26 of them were neighbours on bends, where the convex-hull rebuild of an inverted solid spread into the next block. The blocks are on `...\5M BLOCKS MINE V8`. Scripts: `design8.py`, `run_all8.py`, `post8.py`, `intersections.py`, `fix_self8.py`, `overlap_check.py`, `make_geom8.py`, `*-v8.cs`.

- **Close lodes (`design8.lode_span`).** At each 1 m station and height band, lodes are runs of ≥ 2.5 g/t cells, with gaps up to 0.5 m bridged. The walls take the span from one lode to another (≤ 20 m wide) with the most ore whose diluted grade is > 2.6. When no span reaches 2.6, they take the old best window, max t × (g − 2.5).
- **Bends.** On the inside of a bend, the walls stop 0.3 m short of where neighbouring section lines cross. No solid comes out inverted, and the convex-hull rebuild is no longer needed.
- **No overlaps between drives (`post8.py`).** Overlap is measured on the solids themselves: 0.25 m points inside both meshes (`overlap_check.py`). A drive that overlaps another drive is designed again with the winning drive's blocks as forbidden space. That space counts as heavy waste, the walls are pushed out of it, and any piece still touching it is dropped.
- **Who wins.** For stacked drives (floors > 5 m apart), the upper drive wins, and the lower block's top is cut flat under the block above with its wall shape kept. For drives side by side, the drive with more tonnes × (grade − 2.6) wins. Repeat until no pair overlaps.
- **Leftovers.** On very sharp kinks of trial lines, blocks two sections apart can still cross. The lower-grade one is dropped (`fix_self8.py`). Intersections may not overlap any block either.
- **Check.** Run `python3 overlap_check.py kept8y.pkl 0.5` before building. V8 leaves two same-drive slivers of about 1 m³ (0.25 m sampling), and no overlaps between drives.
- **Result (Deswik).** 5,643 blocks, [figure removed]. Above 2.6: 1,992 blocks, [figure removed] (V7: [figure removed], which counted the overlaps twice). There are 43 intersections. Capture of ≥ 2.6 model ore is 77% (V7: 76%).

## 5 m blocks on Kennet's own strings (ST layer, 2026-10-13)
Kennet put level strings on `DESIGN\CREATING PLILLAR/STOPES\ST` and said: "use this lines as reference to create the stopes (5m one)", not the whole mine, "after that, you are going to create conceptual extra strings", and "capture anything in a 50m radius strings". Scripts are in `scripts/stope-design/st-strings/`.

- **Reading his strings.** Run `export-st-strings-read-only.cs` to write `st_strings.csv`. His layout is one ramp plus, on each level, a 1:50 access off the ramp and flat ore drives. So: steeper than 1:12 is the ramp (skipped), between 1:200 and 1:12 is an access (blocks only above 1.5 g/t), and flat is an ore drive (blocks kept at any grade).
- **Model.** `export-st-cells-read-only.cs` exports the parent cells within 70 m of the strings (50 m search plus the 17.5 m wall reach). Build the raster with `raster.py s st_blocks.f32`.
- **Blocks (`st_stage1.py`).** v8 rules (`design8.py`), then `st_common.resolve` so no two strings' blocks overlap, then kink drops.
- **Extra strings (`st_stage2.py`).** Take ≥ 2.6 g/t unmined ore within 50 m (3D) of any ST string and more than 4 m from the ST blocks. Pockets are built as in v7 (10 m cells ≥ 150 t, pockets ≥ 1,000 t). Lines go on the nearest ST ore drive's 22 m level series, named X000…. Blocks are designed along them, and the ST blocks always win any overlap. Extra strings with no block above 2.0 are dropped.
- **Building.** Run `st_geom.py` to write `st_blocks_geometry.txt` and `st_extra_strings.csv`, then `create-st-blocks.cs`, `interrogate-st-blocks.cs` and `write-grades-st-blocks.cs`. Blocks go on `DESIGN\CREATING PLILLAR/STOPES\5M BLOCKS\{ST ORE DRIVES, ST ACCESSES, EXTRA STRINGS, INTERSECTIONS}` and the lines on `...\EXTRA STRINGS`. DRIVE NUMBER is the handle of his string.
- **First run (Deswik).** 1,871 blocks, [figure removed]. ST ore drives: 913 blocks, [figure removed], of which [figure removed] is above 2.6. Extra strings: 63 lines, 883 blocks, [figure removed], of which [figure removed] is above 2.6. Capture of the ≥ 2.6 ore within 50 m ([figure removed]) went from 56% with the ST blocks alone to 73% with the extra strings.
- **Redo: no low-grade blocks (Kennet, 2026-10-13).** "if the 5m block is below 0.5g/t and you can split it into section, just do not create it". A block below 0.5 g/t is kept only when it splits by height into a piece above 2.0 g/t and a lower piece (both pieces are kept). Any other block below 0.5 g/t is not created, on every layer. `st_common.clean` applies it to the python estimate (`MIN_G`), and intersections use `MIN_G=0.5 python3 intersections.py`. After interrogating, run `drop-low-st-blocks.cs`, because a few blocks estimated just above 0.5 come out below it in Deswik (5 blocks in the redo). Same layers as the first run: delete the old figures first.
- **Redo result (Deswik).** 1,743 blocks, [figure removed]; above 2.6, [figure removed] (the same as the first run). ST ore drives: 881 blocks, [figure removed] ([figure removed] above 2.6). Accesses: 52 blocks, [figure removed]. Intersections: 23, [figure removed]. Extra strings: 63 lines, 787 blocks, [figure removed] ([figure removed] above 2.6). That is 128 fewer blocks and [tonnes removed] less low-grade rock than the first run. The 22 blocks still below 0.5 are all the low half of a split.
- **Cut at 1.5 g/t (Kennet, 2026-10-13).** "to make it better, delete, using the same logic, what is below 1.5g/t". Every unsplit block with INT_AU below 1.5 was deleted, on all layers (`drop-low-st-blocks.cs`, `MIN = 1.5`). Split blocks are kept whole, including a low piece below 1.5. 213 blocks were removed ([tonnes removed]): 20 ST ore drive, 1 access, 2 intersections and 190 extra string. All 63 extra strings still have blocks. **Current result:** 1,530 blocks, [figure removed]; above 2.6, [figure removed] (unchanged). ST ore drives: 861 blocks, [figure removed]. Accesses: 51 blocks, [figure removed]. Intersections: 21, [figure removed]. Extra strings: 597 blocks, [figure removed].
- **One STOPES layer, stopes up to the string above (Kennet, 2026-10-13).** "make the intersections, st access go under the same layer... called stopes, do not need to differentiate". The ST ore drive, access and intersection blocks now all go on `...\5M BLOCKS\STOPES`. Extra-string blocks stay on `...\5M BLOCKS\EXTRA STRINGS`, and SOURCE still records the type. Then: "when the stopes are one above another... make sure the bottom one extend until the floor of the top one (the string for example), if one day I want to double lift, I could just combine them". He then said "just extend the height to the same RL of the string", with no reshaping to the upper outline. In `design8.py`, set `CEIL_R=15 HUP=30`. A stope with a string above it within 15 m in plan, and at most 30 m up, goes floor to floor to that string's RL. Otherwise it stays at 22 m. Most gaps are 22 to 23 m, and 370 to 397 is 27 m. The whole-mine defaults (7 m, 22 m) are unchanged. The 1.5 g/t cut now runs in the design too (`st_common.MIN_G = 1.5`, intersections `MIN_G=1.5`), and again after interrogation. **Result (Deswik):** 1,522 blocks, [figure removed]; above 2.6, [figure removed]. STOPES: 926 blocks, [figure removed]. Extra strings: 596 blocks, [figure removed].
- **Blind stopes 26 m (Kennet, 2026-10-13).** "when the stope is blind, i told you to go to higher level of 22m, however, I was not considering that we have a development drive. as rule, lets make them 26m when blind. but it is going to end up around 22 after we take the development drive out". In `design8.py`, set `HBLIND=26`, with the whole-mine default still 22 m. A stope with a string above it (within 15 m in plan, up to 30 m) still goes floor to floor to that string's RL. Run with `CEIL_R=15 HUP=30 HBLIND=26`. Kennet moved the extra string lines to `...\5M BLOCKS\EXTRA STRINGS\EXTRA STRINGS`, and `create-st-blocks.cs` now writes them there. **Result (Deswik):** 1,449 blocks, [figure removed]; above 2.6, [figure removed]. STOPES: 952 blocks, [figure removed]. Extra strings: 59 lines, 497 blocks, [figure removed]. The taller ST stopes take more ore, so fewer extra-string blocks are needed.

## Stope groups A-F and the "Stope groups" legend (2026-10-13)

Kennet asked to group the ST 5 m stopes into 6 groups and see them with a legend (not a filter).
- Grouping: k-means on each drive's centre (x, y, 1.5 x lowest RL), best of 40 seeds; every stope of a drive stays in one group, intersection solids count as their own drive. Groups named by area: A East lode (6176-6269, [tonnes removed]), B South upper (6259-6413, [tonnes removed]), C North upper (6292-6426, [tonnes removed]), D East lower (6064-6176, [tonnes removed]), E West deep (6056-6171, [tonnes removed]), F Main middle (6172-6324, [tonnes removed]). Drive-to-group table: `scripts/stope-design/st-strings/stope_groups.csv`.
- `group-st-stopes.cs` writes GROUP (A-F) and Transformation Details ("Group A".."Group F") on 5M BLOCKS\STOPES and \EXTRA STRINGS, creates the "Stope groups" legend (Field GROUP, Value items, one colour each) and applies it to those two layers. "5m blocks AU v2" is untouched; switch back from the layer legend list.
- Legend API: `new Deswik.Common.Core.Legends.Legend(name, other.Filters, other.GlobalConstants)`, items cloned with `new LegendItem(existingItem)` then Field/Value/Description/PenColor set, `App.Legends.Add(legend)`, `App.Legends.ApplyToLayer(layer, name)`. Layer's current legend is the layer property `Legend`.

## Stope shapes from the ST 5 m blocks (2026-10-13)

Kennet: combine the 5 m blocks into stopes, max 40-50 m, one 5 m pillar after each; pieces above 2.4 g/t but every stope averages above 2.5; cut a low-grade block and add its good part to the neighbour for a smoother stope; ore from the floor up = just cut flat there; higher grade at the top of an end = chamfer the end wall, max 37 deg (he confirmed off vertical); ends can also stay vertical - the chamfer is only to pick up the higher grade; always operational.

Method (`scripts/stope-design/stope-shapes`):
1. `export-blocks-read-only.cs` -> st_blocks_export.tsv (blocks, split pieces a = lower / b = upper, geometry).
2. `stopes.py`: per drive, long section on a 0.5 m grid (piece grade uniform). Each candidate stope (contiguous columns, 10-50 m, no chainage gaps) is shaped by coordinate descent: top = full height over one contiguous run of columns, a single flat cut height (7.5 m steps of 2.5) on each side of it; end chamfer offset 0..tan(37)*end height in 2.5 m steps (only when the end top is >= 12.5 m); objective t*(g-2.4), redone on 2.5 if the average falls below 2.5; >= 1,500 t. DP picks non-overlapping stopes with >= 5 m between top ends; > 40 m scores 5% less so 50 m is used only when it pays. Intersection solids >= 2.5 g/t become their own stopes.
3. `geom.py`: clip each piece's mesh by the flat top and the chamfer planes (plane through the end column's floor/top points, horizontal across the drive), stretch pieces 5 cm so neighbours overlap, union with manifold3d, collapse short edges (`collapse.py`). `write_geom.py` writes stope_shapes_geometry.txt (faces ONE-based for Polyface.AddTriangle).
4. `create-stope-shapes.cs` on 5M BLOCKS\STOPE SHAPES; Deswik rejects union slivers (Polyface.Volume = -1, interrogation silently returns Success=false with no CSV) - `Polyface.RepairAlternate(true,true,true)` fixes them all.
5. `interrogate-stope-shapes.cs`, then `write-grades-stope-shapes.cs` (INT_*, AU v2 legend applied to the layer, stope_shapes_grades.csv). `allpdf.py` draws every drive's long section.

First result: 115 stopes, [figure removed] (INT), none below 2.5, 16 with chamfers, median 35 m. Python estimate vs Deswik INT_AU within +-0.03 g/t typically.
Gotcha: GROUP comes from the blocks as they are in the drawing; Kennet re-grouped some (group H appeared).

### Stope shapes v2 (Kennet's review, 2026-10-13) - current, layer 5M BLOCKS\STOPE SHAPES V2 (v1 kept)
- A chamfered end goes straight up 5 m from the floor first, then leans max 40 deg off vertical (he settled on 40 after first saying 37 and 45). Grid mask: foot at end+d for z<5, line from (end+d, 5) to (end, top) above. Solids: pieces split at floor+5, lower part cut by the vertical plane at the foot, upper part by the inclined plane.
- Every stope keeps >= 3 m along the floor at full stope height (between the chamfer feet, on columns at the stope's max top) - rejects stopes that are almost all chamfer.
- Pillars: "pillar means money lost" - put them in the lowest-grade sections when possible. The DP maximises metal above 2.4 in stopes, which is the same as leaving the least metal in pillars.
- Result: 116 stopes [figure removed] INT, none < 2.5, 16 chamfered (max 36.8 deg), median 33 m. Between stopes (pillars + low-grade gaps): 59 gaps, [figure removed]. 89 of 116 solids needed RepairAlternate.

### Stope Final (2026-10-13) - current, layer DESIGN\CREATING PLILLAR/STOPES\STOPE FINAL
Kennet compared v2 with his own stopes on SCHEDULER\TASK SOLIDS\ST (that layer holds every LH_Stope solid twice; dedupe by volume + centroid) and asked why v2 had no stopes in some areas his have. Main cause: about [figure removed] sat more than 50 m from any ST string, so no block was ever made there. He asked to add extra strings there and rerun the whole chain (5 m blocks, grades, stopes) on a new layer "Stope Final".
- Target boxes: bounds of his ST pieces with no ST string nearby, +-5 m (8 boxes). `st_stage2.py` takes them via `TARGET_BOXES=<json>`; ore inside a box counts as if it were within 50 m. The block-model raster must cover the boxes too (export the box cells and build raster `s2` = ST cells + box cells; `MODEL=s2`). Without that the boxes are empty (first try found [tonnes removed]).
- Blocks on STOPE FINAL\5M BLOCKS\{STOPES, EXTRA STRINGS}, lines on STOPE FINAL\EXTRA STRINGS: same create / interrogate / write-grades / drop-low scripts with the layer and file names changed.
- GROUP: blocks that keep their name take Kennet's group from the old blocks; new ones take the nearest old block's group.
- Stopes: v2 rules unchanged. Result 131 stopes, [figure removed] INT ([ounces removed]) vs v2 116 / [tonnes removed] ([ounces removed]) and his ST 126 / [figure removed] ([ounces removed]). 17 stopes ([tonnes removed]) sit in the target areas. 103 of 131 solids needed RepairAlternate.
