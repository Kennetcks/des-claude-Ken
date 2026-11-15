// Read-only: export the EWY strings, the old EWY strings and all dev strings within 70 m of them, plus schedule dates per string.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var ewy = new[] { "45933C4", "45931DD", "45933B9", "45931DF", "458EDDC", "45931E0", "45931E1", "45931E2" };          // new EWY chain
var oldEwy = new[] { "458EE36", "458EE37", "458EE81", "458EDE3", "458EE16", "458EE18", "458EE19", "458EE1A", "458EE17", "458EE35" }; // replaced / old EWY
var LC = "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11";
var figs = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.IsPolyline && (f.Layer.Name == LC || f.Layer.Name == LC + "\\NEW")) figs.Add(f);
List<double[]> P(Deswik.Graphics.Primaries.Figure f) => Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
var core = figs.Where(f => ewy.Contains(f.Handle)).SelectMany(P).ToList();
double x0 = core.Min(p => p[0]) - 70, x1 = core.Max(p => p[0]) + 70, y0 = core.Min(p => p[1]) - 70, y1 = core.Max(p => p[1]) + 70, z0 = core.Min(p => p[2]) - 25, z1 = core.Max(p => p[2]) + 25;
var sb = new StringBuilder("Handle\tGroup\tName\tACT_TYPE\tMINE\tLEVEL\tLAT_VERT\tCoords\n"); int n = 0;
foreach (var f in figs)
{
    var p = P(f); if (!p.Any(q => q[0] > x0 && q[0] < x1 && q[1] > y0 && q[1] < y1 && q[2] > z0 && q[2] < z1)) continue;
    var grp = ewy.Contains(f.Handle) ? "EWY" : oldEwy.Contains(f.Handle) ? "OLD" : "DEV";
    sb.Append(f.Handle + "\t" + grp + "\t" + A(f, "Name") + "\t" + A(f, "ACT_TYPE") + "\t" + A(f, "MINE") + "\t" + A(f, "LEVEL") + "\t" + A(f, "LAT_VERT") + "\t" + string.Join(";", p.Select(q => q[0].ToString("F2") + "," + q[1].ToString("F2") + "," + q[2].ToString("F2"))) + "\n"); n++;
}
System.IO.File.WriteAllText(@"C:\dswtmp\EWY_Geometry.tsv", sb.ToString(), new UTF8Encoding(false));
// schedule dates per EWY string
var ts = Schedule.Tasks.Where(t => !t.Deleted).ToList(); var sd = new StringBuilder("Handle\tName\tStart\tFinish\tMetres\n");
foreach (var h in ewy) { var g = ts.Where(t => t.GetFieldValueAndCacheIt("DIS Parent ID") == h && t.GetFieldValueAndCacheIt("DIS Activity Type") == "Tunnel").ToList(); if (g.Count == 0) continue; double m = g.Sum(t => { double v; return double.TryParse(t.GetFieldValueAndCacheIt("Sch_metres"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) ? v : 0; }); sd.Append(h + "\t" + g[0].Name + "\t" + g.Min(t => t.Start).ToString("yyyy-MM-dd") + "\t" + g.Max(t => t.Finish).ToString("yyyy-MM-dd") + "\t" + m.ToString("F1") + "\n"); }
System.IO.File.WriteAllText(@"C:\dswtmp\EWY_Dates.tsv", sd.ToString(), new UTF8Encoding(false));
Out.AppendLine("strings exported " + n + "  box X " + x0.ToString("F0") + "-" + x1.ToString("F0") + " Y " + y0.ToString("F0") + "-" + y1.ToString("F0") + " RL " + z0.ToString("F0") + "-" + z1.ToString("F0"));
Out.AppendLine(sd.ToString());
return null;
