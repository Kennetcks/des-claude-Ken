// Ventilation Plan Master: give 0\PRIMARY STATIONS\LABELS the station layer's attribute fields, trying the API calls in turn.
var src = App.Layers.First(l => l.Name == "0\\PRIMARY STATIONS"); var lay = App.Layers.First(l => l.Name == "0\\PRIMARY STATIONS\\LABELS");
int N() => lay.Attributes.Count;
try { src.Attributes.CopyToLayer(lay, true, false); } catch (Exception e) { Out.AppendLine("CopyToLayer: " + e.Message); }
Out.AppendLine("after CopyToLayer(append): " + N());
if (N() == 0) { try { src.Attributes.CopyToLayer(lay); } catch (Exception e) { Out.AppendLine("CopyToLayer(1): " + e.Message); } Out.AppendLine("after CopyToLayer(lay): " + N()); }
if (N() == 0) { foreach (Deswik.Common.Core.Objects.Attribute a in src.Attributes) lay.Attributes.Add(a.Name, a.Type, a.DefaultValue, a.Group); Out.AppendLine("after Add each: " + N()); }
Out.AppendLine(string.Join(", ", lay.Attributes.Cast<Deswik.Common.Core.Objects.Attribute>().Select(a => a.Name)));
return null;
