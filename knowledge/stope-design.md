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
- The base is at the back of the lower ore drive and the top is at the floor of the drive above. `Stope Height` is measured from the back (17.5 m on 22 m levels). The lengths are 10–50 m (median 35 m) and the thicknesses 2.1–15 m (median 2.8 m).
- Check that no drive sits inside the planned stope height before building it.

## Method
1. **Find the drive.** "298ID" meant the drive solid whose `INT_ID` was 298, set by an earlier batch interrogation. Read the drive's floor string (one per drive, matched by `DIS Parent ID`).
2. **Export the blocks** around the drive with `scripts/stope-design/export-blocks-around-drive-read-only.cs`. It took ~10 s for 0.4 M sub-blocks out of 55 M.
3. **Design** with `scripts/stope-design/design_stopes.py`:
   - Cut the drive into 1 m slices.
   - In each slice, find the lateral extent of the ≥ cut-off ore in a lower and an upper band, smooth it over 5 m, and keep the width at least the minimum.
   - Keep the base inside the drive width.
   - Choose the stope chainages that maximise metal with each stope above the grade limit, staying inside the main ≥ cut-off run. Without that limit, the optimiser stretches a stope into low-grade tail just to add tonnes.
4. **Build** the solids with `create-stopes-from-geometry.cs`. It makes a `new Polyface()` with `AddVertex(x,y,z)` and `AddTriangle(i,j,k)` (1-based indices), sets `pf.Layer`, and calls `App.ModelSpace.Entities.AddItem(pf,null,null,true,false)`. Check `pf.IsClosed` and `pf.Volume`, and add the layer attribute fields first.
5. **Interrogate** with `scripts/interrogation/batch-interrogate-solids-to-csv.cs`, with density from the model field. Write the INT_ fields onto the stopes. The Deswik numbers matched the Python estimate within 1%.
6. **Report** each stope's length, width, tonnes and grade, the grade after the drive overlap is cut, and why any stope is shorter than the maximum. Leave saving the drawing to the user.

## Reading a Deswik `.gmdlb` block model from run_csharp
- Open it with `new Deswik.BlockModel.ManagedGeoModels.BlockModel(path)`, then call `Load(ModelOpenState.OpenForReadNotIndexed, CancellationToken.None)`. Its `BlockGeoModel` property is a `Deswik.GeoModel.BlockModel`. Calling `Deswik.GeoModel.BlockModel.Open(...)` directly returned null. Dispose the wrapper when finished.
- `Metadata` holds the Origin, the parent size (XSize/YSize/ZSize), NX/NY/NZ and the rotation. Sub-blocked models store one record per sub-block.
- The fields include `_XC,_YC,_ZC,_XSIZE,_YSIZE,_ZSIZE,_IJK` and the model fields. Read them with `GetDoubleValue(field, record)` / `GetValue(field, record)`.
- Records are **sorted by `_IJK = k + NZ*(j + NY*i)`** (k fastest), so a box is read by binary searching each (i,j) column. Never loop over all records through reflection.

## Loading layers
`App.ModelSpace.Entities` only holds layers that are loaded. In a file-per-layer `.dcf`, most layers can be present but empty until they are loaded: call `App.LoadLayer(layerName, true)`, then read `layer.Entities`.
