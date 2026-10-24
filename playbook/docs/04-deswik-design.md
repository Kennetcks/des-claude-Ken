# 04 – Deswik CAD design work

## Jobs done

| Job | How |
|---|---|
| Decline loop radius options (R16–R20, R22–R26) | New polylines on the active layer; start and end points fixed; curve moved north only; one constant gradient (`de3_*_options.cs`) |
| Flat section in a loop (15 m flat at ch 30–45) | `test2_flat_option.cs` |
| Fan chamber on the RAD centreline | Split the string at existing vertices so the flat section is its own string with its own attributes; later shortened to 8.5 m centred in the flat (`drafter_fan_chamber_*.cs`). **No vertex moved, so no grade changed.** |
| Centreline attributes for the scheduler | Copy the full attribute set from a reference string, then set ACT_TYPE / Description / PROFILE / SLOT_FLG (`final_layer_*.cs`, `test2_attributes.cs`) |
| Read strings, grades and attributes | `drafter_read_centrelines.cs` – lists every vertex with segment length, dz, grade and chainage |
| Plan-view screenshots | Set the view (azimuth, view size) and export a bitmap; clip boxes gave blank images in the drafter file |

## API notes (Deswik 2026.1)

- Polyline vertices: `figure.asPolyline` → `VertexList` (type `Deswik.Graphics.Geometry.Verticies`, **not** an `IList`). Use its methods: `RemoveAfter(i)`, `RemoveBefore(i)`, `InsertAt(i, Point)`, `RemoveAll()`.
- `figure.Clone()` returns a `Figure` (keeps colour, line type, layer and attributes); trim its `asPolyline.VertexList`, then `App.ModelSpace.Entities.AddItem(...)`.
- Attributes: `f.AttributeValueGet(name, "")` / `f.AttributeValueSet(name, value)`; add the field to the layer before setting. Setting properties on items from the layer's `Attributes` enumerator does **not** persist – copy definitions with `Attributes.Add(MemberwiseClone(src), …)`.
- Handles: compare with `$"{f.GetType().GetProperty("Handle")?.GetValue(f)}"` – `Convert.ToString(handle)` sometimes differs and lookups miss.
- `App.Layers.FindName` can miss a layer that `App.ActiveLayer` returns – use the active layer object.
- Task solids are about 3.5 m long but 5–6 m wide – chain them by solid centres in handle order, not by the long axis.
- The `LEVEL` attribute may be RL minus a fixed offset, and can differ between mines in the same file – check before comparing levels with RL.

## Fan chamber example

Key dimensions read from the fan chamber GA drawing: length 8,500 mm, width 10,500 mm min, height 6,500 mm, 300 mm concrete base. The chamber string got `ACT_TYPE = FAN`, `PROFILE = 10.5mW x 6.8mH SA` (6.5 m + 0.3 m base). Open items left for the engineer: a ground-support standard for a 10.5 m span (geotech), and checking `FAN` is a valid scheduler activity type.
