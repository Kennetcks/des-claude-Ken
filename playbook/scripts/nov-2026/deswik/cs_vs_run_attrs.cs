// Ventilation Plan Master: run the "Vent Station Labels" macro, then read back each label figure's text and attributes.
var mac = App.Macros.FindName("Vent Station Labels"); IEnumerable<string> msgs = null; mac.Run(out msgs);
if (msgs != null) foreach (var m in msgs) Out.AppendLine("macro: " + m);
Deswik.Graphics.Primaries.Figure any = null;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != "0\\PRIMARY STATIONS\\LABELS") continue; any = f;
    var names = f.Layer.Attributes.Cast<Deswik.Common.Core.Objects.Attribute>().Select(a => a.Name).ToList();
    Out.AppendLine($"{f.Handle} {(f.IsText ? "text '" + f.asText.TextString + "'" : f.IsInsert ? "symbol" : "other")}: " + string.Join(" | ", names.Select(n => $"{n}={f.AttributeValueGet(n, "")}")));
}
if (any == null) Out.AppendLine("no labels found");
return null;
