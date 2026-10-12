# Reference scripts

Working `run_csharp` bodies from real sessions in Deswik CAD 2024.2. Each file is the **body** of
`object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)`, in C# 5.

They were written for one drawing, so check the layer names at the top of each script (`SCHEDULER\TASK SOLIDS`, `DESIGN\FLOOR STRINGS`, `DESIGN\FLOOR STRINGS\SOLIDS`) and the attribute names before running. Claude should read them as examples and adapt them, not run them blind.

| Folder | Script | What it does |
|---|---|---|
| floor-strings | build-lateral-floor-strings.cs | One plain floor line per drive (DIS parent), ends cut where the line leaves its own solid, junction floors fixed. Replaces the layer's strings. |
| floor-strings | build-raise-centre-lines.cs | Straight centre line through each raise's solids; tags LAT_VERT. |
| floor-strings | relevel-ends-inside-face.cs | Re-reads end heights 0.3-0.6 m inside the face. |
| floor-strings | review-floor-strings-read-only.cs | Read-only check for steps, bends, off-floor points, flat raises. |
| legends-attributes | add-layer-attribute-fields.cs | Adds attribute fields to a layer so values show in Properties. |
| legends-attributes | tag-strings-act-type.cs | Copies ACT_TYPE from the drive's solids onto each string. |
| legends-attributes | legend-by-act-type-lw100.cs | Legend by ACT_TYPE with line weight 100, applied to the layer. |
| tunnels-by-rule | measure-profiles-read-only.cs | Measures width/height per ACT_TYPE from existing task solids. |
| tunnels-by-rule | build-sections-and-rules.cs | Creates arch/circle sections and a refreshable tunnels-by-rule solids layer. |
| tunnels-by-rule | test-refresh.cs | Refreshes the rule layer twice and checks the count is stable. |
| interrogation | read-block-model-fields-read-only.cs | Lists a .gmdlb block model's fields. |
| interrogation | batch-interrogate-solids-to-csv.cs | Batch interrogation of each solid (AU mass-weighted, model density) to a CSV. |
| interrogation | write-interrogation-attributes.cs | Writes tonnes, volumes, density and AU from that CSV onto the solids, with layer fields. |
| stope-design | export-blocks-around-drive-read-only.cs | Exports the block model sub-blocks in a box around a drive, plus its floor string, to CSV. |
| stope-design | design_stopes.py | Python (numpy): shapes the walls (5 points up each wall, a section every 5 m) to take in the ≥ cut-off blocks, picks the chainages (max length, pillar, grade limit) and writes the solid geometry. |
| stope-design | create-stopes-from-geometry.cs | Builds closed Polyface stopes from that geometry and tags them like LH_Stope task solids. |
| stope-design | blocks_along_drive.py | Python (numpy): the same wall shaping over the whole drive, with drillability rules (convex sections, ≥55° walls, ≤1 m change per section), cut into 5 m blocks (one closed solid each) for the engineer to pick stopes and pillars. |
| stope-design | create-blocks-from-geometry.cs | Builds the 5 m blocks on STOPE DESIGN\\5M BLOCKS with LH_Stope tags, BLOCK and chainages. |
| stope-design | write-grades-and-grade-legend.cs | Writes interrogated INT_ fields onto the blocks and applies an INT_AU range legend (2–2.6 light blue, 2.6–3 blue, 3–5 red, 5–12 pink, 12+ purple). |

`decline-design/` is a separate Python set (numpy, scipy, lxml) that builds a decline and levels as a .dcf outside Deswik; see its `README.txt`.

See `../knowledge/` for the rules behind them.
| stope-design/whole-mine | study-existing-stopes-read-only.cs, export-*.cs, raster.py, design.py, run_all.py, virtual.py, run_virtual.py, post.py, make_geom.py, create-blocks-mine.cs, interrogate-blocks-mine.cs, write-grades-blocks-mine.cs | 5 m blocks along every drive in the mine (>1.5 g/t only), trial ore drives for unreached ore. See knowledge/stope-design.md. |
| stope-design/whole-mine (v2) | design2.py, run_all2.py, run_virtual2.py, post2.py, make_geom2.py, hull_invalid_blocks.py, create-blocks-mine-v2.cs, interrogate-blocks-mine-v2.cs, write-grades-blocks-mine-v2.cs | v2: all grades (grey below 1.5), blocks below 2.6 split when one piece beats 2.6, floor to floor up to 22 m. |
| stope-design/whole-mine (v3) | design3.py, run_all3.py, run_virtual3.py, post3.py, make_geom3.py, create-blocks-mine-v3.cs, replace-invalid-blocks-v3.cs, interrogate-blocks-mine-v3.cs, write-grades-blocks-mine-v3.cs | v3: always full height, no-grade blocks kept, vertical (strike) splits for >2.6 then >2.0, uses Kennet's legend unchanged. |
