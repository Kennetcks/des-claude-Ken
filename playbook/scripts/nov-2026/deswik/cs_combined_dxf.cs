// Read-only: write the COMBINED 12.11 strings to an R12 ASCII DXF (3D polylines, one layer per ACT_TYPE) for Ventsim.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString().Trim();
const string MAIN = "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11";
var OUT = @"<SHARE>\03 Planning\03 Scheduling\03 3MS\06 Dec\Documentation\SITE - COMBINED 12.11 centrelines for Ventsim.dxf";
if (System.IO.File.Exists(OUT)) OUT = OUT.Replace(".dxf", " " + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".dxf");
var ic = System.Globalization.CultureInfo.InvariantCulture;
string N(double v) => v.ToString("0.###", ic);
string Lay(string s) { s = (s == "" ? "OTHER" : s).ToUpperInvariant(); return new string(s.Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_').ToArray()); }
var figs = new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null && f.Layer.Name == MAIN && f.IsPolyline && f.asPolyline.VertexCount >= 2) figs.Add(f);
var layers = figs.Select(f => Lay(A(f, "ACT_TYPE"))).Distinct().OrderBy(x => x).ToList();
var sb = new System.Text.StringBuilder();
void G(int code, string v) { sb.Append(code).Append("\r\n").Append(v).Append("\r\n"); }
G(0, "SECTION"); G(2, "HEADER"); G(9, "$ACADVER"); G(1, "AC1009"); G(9, "$INSUNITS"); G(70, "6"); G(0, "ENDSEC");
G(0, "SECTION"); G(2, "TABLES"); G(0, "TABLE"); G(2, "LAYER"); G(70, layers.Count.ToString());
int col = 1; foreach (var l in layers) { G(0, "LAYER"); G(2, l); G(70, "0"); G(62, (col++ % 255 + 1).ToString()); G(6, "CONTINUOUS"); }
G(0, "ENDTAB"); G(0, "ENDSEC");
G(0, "SECTION"); G(2, "ENTITIES");
int nv = 0;
foreach (var f in figs)
{
    var l = Lay(A(f, "ACT_TYPE")); var pl = f.asPolyline;
    G(0, "POLYLINE"); G(8, l); G(66, "1"); G(10, "0"); G(20, "0"); G(30, "0"); G(70, pl.Closed ? "9" : "8");
    for (int i = 0; i < pl.VertexCount; i++) { var q = pl.GetVertex(i); G(0, "VERTEX"); G(8, l); G(10, N(q.x)); G(20, N(q.y)); G(30, N(q.z)); G(70, "32"); nv++; }
    G(0, "SEQEND"); G(8, l);
}
G(0, "ENDSEC"); G(0, "EOF");
System.IO.File.WriteAllText(OUT, sb.ToString(), new System.Text.ASCIIEncoding());
// a small key so the DXF lines can be traced back to the tasks
var key = new System.Text.StringBuilder("Handle\tName\tMINE\tLEVEL\tACT_TYPE\tPROFILE\tVertices\tLength_m\n");
foreach (var f in figs) { var pl = f.asPolyline; double len = 0; for (int i = 1; i < pl.VertexCount; i++) { var a = pl.GetVertex(i - 1); var b = pl.GetVertex(i); len += Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z)); } key.Append(f.Handle + "\t" + A(f, "Name") + "\t" + A(f, "MINE") + "\t" + A(f, "LEVEL") + "\t" + A(f, "ACT_TYPE") + "\t" + A(f, "PROFILE") + "\t" + pl.VertexCount + "\t" + len.ToString("F1", ic) + "\n"); }
System.IO.File.WriteAllText(OUT.Replace(".dxf", " - key.tsv"), key.ToString());
Out.AppendLine("wrote " + OUT); Out.AppendLine("strings " + figs.Count + ", vertices " + nv + ", layers: " + string.Join(", ", layers));
return null;
