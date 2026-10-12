# Attributes, legends and line weights (Deswik 2024.2 API)

## Attributes are invisible until the layer has the field
`figure.AttributeValueSet(name, value)` stores the value, but the user **won't see it in Deswik Properties** unless the layer defines that attribute field. Always add the fields to the layer first, then set (or re-set) the values:

```csharp
var at = layer.Attributes;
// The AttributeType enum isn't easy to reference directly; get it from the Add overload.
var addM = at.GetType().GetMethods().First(m => m.Name == "Add" && m.GetParameters().Length == 3
                                             && m.GetParameters()[0].ParameterType == typeof(string));
object strT = Enum.Parse(addM.GetParameters()[1].ParameterType, "String");
addM.Invoke(at, new object[] { "ACT_TYPE", strT, "" });
// then: foreach figure on the layer: f.AttributeValueSet("ACT_TYPE", value);
```

## Legends
- `App.Legends.Find(name)`, `App.Legends.Add(name, filters, globalConstants)` (copy `Filters`/`GlobalConstants` from an existing legend), `App.Legends.ApplyToLayer(layerName, legendName)`, `App.Legends.Save()`.
- Items are `Deswik.Common.Core.Legends.LegendItem`. Easiest: clone an existing item with its copy constructor `new LegendItem(template)`, then set `Field`, `Value`, `Description`, `PenColor` (System.Drawing.Color), `LineWeight`, `Transparency`.
- `legend.DefaultLineWeight = 100;`
- When the user says "just choose the colours", pick distinct colours per value and list them in the reply.

## Line weight and colour on figures
- `f.LineWeight = Deswik.Graphics.Primaries.Figure.ConstLineWeight.LW_100;`
- `f.PenColor = new Deswik.Graphics.Objects.Color(System.Drawing.Color.FromArgb(r, g, b));`
- Set both on the legend and on the figures. If lines still look thin, line weight display is probably off in the user's view.

See `scripts/legends-attributes/`.
