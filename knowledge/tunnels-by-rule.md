# Solids from strings: tunnels by rule (refreshable layer)

When the user wants solids built from design strings "by rules", so that **refreshing the layer** rebuilds them, use Deswik's native **Section Along Polyline Rules** (tunnels by rule). Don't write a C# "refresh" script for this; the layer refresh replaces it.

## How it works
- The output layer becomes `LayerType.TunnelSolids`. Its rules are stored in the layer's XProperties (`_dw_SectionAlongPolylineRules_*`).
- Input: one or more string layers. A rule attribute (e.g. `ACT_TYPE`) picks a section for each string.
- Sections live in `App.Sections`. Section y = 0 is the string, so an **arch with its floor at y = 0 sits on a floor string**, and a **circle centred on 0,0 is centred on a raise centre line**.

## API (verified 2024.2)
```csharp
// Arch section (custom vertices, floor at y=0)
var s = new Deswik.Graphics.Tunnels.Section(name);
s.Add(-W/2, 0); s.Add(W/2, 0); s.Add(W/2, H - rise);  /* arc points ... */  s.Add(-W/2, H - rise);
App.Sections.Add(s);
// Circle section
var c = new Deswik.Graphics.Tunnels.CircleTunnelInformation(); c.Radius = D/2; c.CircleVerticesCount = 24;
var cs = new Deswik.Graphics.Tunnels.Section(c.GetMyType(), c); cs.Name = name; App.Sections.Add(cs);
App.Sections.ValuesSaveToDocumentTempLayer(App);   // keep sections with the drawing

var r = new Deswik.Graphics.Tunnels.SectionAlongPolylineRules(App);
r.InputLayers.Clear(); r.InputLayers.Add(stringLayer);
r.Filter = "<No Filtering>"; r.CreateSolids = true; r.OutputSolidsLayer = solidsLayer;
r.CreateWalls = false; r.OutputWallsLayer = ""; r.AttributeName = "ACT_TYPE"; r.UseFixedAzimuth = false;
r.Clear();
r.Add(new Deswik.Graphics.Tunnels.SectionAlongPolylineRule("DEC", "FS 5.5W X 5.5H ARCH", -1));
bool ok = r.Execute(App);          // r.Errors lists problems
// SectionAlongPolylineRules.Refresh(App, layerName) is what Deswik's layer Refresh calls.
```

## Gotchas
- **The input string layer must be visible** when refreshing; Deswik selects strings by visibility.
- Refresh recreates the output layer (`Layers.Add` with delete-first). Old solids are soft-deleted, so **count only figures with `!f.Deleted`**.
- Refresh **wipes the output layer's legend**. Solids copy the string's pen colour, so colour the strings by the rule attribute and the solids follow.
- One section per attribute value: every raise of a type gets the same diameter, even if the source solids varied.
- Tell the user: right-click the layer > Refresh after editing strings, then save the drawing so new sections are kept.

## Sizing sections from existing solids
If there's no profile table in the drawing, measure width/height per ACT_TYPE from the existing task solids (median width across and height above the floor), and check the result: total volume of the new solids vs the task solids, per ACT_TYPE. ~95-102% is a good match; report outliers.

Example sizes used on one underground mine (measure your own drawing, don't assume these):

| ACT_TYPE | Section |
|---|---|
| DEC, MLA, ISP, DSP | 5.5 W x 5.5 H arch, 0.93 m rise |
| ACC, RAD, DDD, EWD, SUB, PST | 5.0 W x 5.0 H arch |
| OD, SMP | 4.5 W x 4.5 H arch |
| RAR | 5.2 m circle |
| RBH, RHB | 4.5 m circle |
| EWR | 1.1 m circle |
| SRH | 0.5 m circle |

Name new sections with a prefix (e.g. `FS ...`) so they don't clash with the site's own sections.

See `scripts/tunnels-by-rule/`.
