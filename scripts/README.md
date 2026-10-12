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

See `../knowledge/` for the rules behind them.
