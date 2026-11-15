// Cut stope solids by the scheduled development solids they overlap (CutFirst), replacing the mesh in place (handle + attributes kept).
// TARGET "task": LH_Stope task solids (limit to ONLY if set). TARGET "source": solids on 02 STOPES\COMBINED USED (+ sublayers).
// A cut is kept only if every boolean succeeds and the volume drop is within 2% (or 0.5 m3) of the measured overlap; else the solid is left as is and listed.
// Saves each changed solid's original mesh (vertices + faces) to C:\dswtmp\StopeCut_Backup\<handle>.txt first.
const bool APPLY = false; const string TARGET = "task"; const string ONLY = "44B1980";
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var mi = typeof(Deswik.Graphics.SolidTools).GetMethods().First(m => m.Name == "BooleanCutSolidWithToleranceMovements" && m.GetParameters().Length == 6);
var cutFirst = Enum.Parse(mi.GetParameters()[2].ParameterType, "CutFirst");
(double[], double[]) Box(Deswik.Graphics.Primaries.Figure f) { var vl = f.asPolyface.VertexList as System.Collections.IEnumerable; double x0 = 1e18, y0 = 1e18, z0 = 1e18, x1 = -1e18, y1 = -1e18, z1 = -1e18; foreach (var q in vl) { var ty = q.GetType(); double x = Convert.ToDouble(ty.GetProperty("x").GetValue(q)), y = Convert.ToDouble(ty.GetProperty("y").GetValue(q)), z = Convert.ToDouble(ty.GetProperty("z").GetValue(q)); x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); z0 = Math.Min(z0, z); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); z1 = Math.Max(z1, z); } return (new[] { x0, y0, z0 }, new[] { x1, y1, z1 }); }
bool Ov(double[] alo, double[] ahi, double[] blo, double[] bhi) => alo[0] < bhi[0] && blo[0] < ahi[0] && alo[1] < bhi[1] && blo[1] < ahi[1] && alo[2] < bhi[2] && blo[2] < ahi[2];
var targets = new List<(Deswik.Graphics.Primaries.Figure f, double[] lo, double[] hi)>(); var devs = new List<(Deswik.Graphics.Primaries.Figure f, double[] lo, double[] hi)>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyface) continue;
    bool isTarget = TARGET == "task" ? (f.Layer.Name == "SCHEDULER\\TASK SOLIDS" && A(f, "DIS Activity Type") == "Solid" && A(f, "ACT_TYPE") == "LH_Stope" && (ONLY == "" || f.Handle == ONLY))
                                     : f.Layer.Name.StartsWith("0\\06 SITE\\03 ENGINEERING\\02 STOPES\\COMBINED USED");
    if (isTarget) { var b = Box(f); targets.Add((f, b.Item1, b.Item2)); }
    else if (f.Layer.Name == "SCHEDULER\\TASK SOLIDS" && A(f, "DIS Activity Type") == "Tunnel" && A(f, "SCHEDULED") == "1") { var b = Box(f); devs.Add((f, b.Item1, b.Item2)); }
}
if (APPLY) System.IO.Directory.CreateDirectory(@"C:\dswtmp\StopeCut_Backup");
int cut = 0, none = 0, rejected = 0; double removed = 0; var rej = new List<string>(); var sb = new StringBuilder("Handle\tName\tVolBefore\tOverlap\tVolAfter\tResult\n");
foreach (var t in targets)
{
    var cs = new List<Deswik.Graphics.Figures.Polyface>(); double overlap = 0;
    foreach (var d in devs) { if (!Ov(t.lo, t.hi, d.lo, d.hi)) continue; double v; bool ok; try { ok = t.f.asPolyface.BooleanIntersectionVolume(d.f.asPolyface, out v, false); } catch { ok = false; v = 0; } if (ok && v >= 0.05) { cs.Add(d.f.asPolyface); overlap += v; } }
    if (cs.Count == 0) { none++; continue; }
    double v0 = t.f.asPolyface.Volume; var pf = t.f.asPolyface.CloneOnlyTheMesh(); bool all = true; var errs = new List<string>();
    foreach (var c in cs) { var args = new object[] { pf, c, cutFirst, false, 0.001, errs }; try { if (!(bool)mi.Invoke(null, args)) all = false; pf = (Deswik.Graphics.Figures.Polyface)args[0]; } catch { all = false; } if (pf == null) { all = false; break; } }
    double v1 = pf == null ? -1 : pf.Volume; double drop = v0 - v1; bool good = all && v1 > 0 && Math.Abs(drop - overlap) <= Math.Max(0.5, 0.02 * overlap);
    sb.Append(t.f.Handle + "\t" + A(t.f, "Name") + "\t" + v0.ToString("F1") + "\t" + overlap.ToString("F1") + "\t" + v1.ToString("F1") + "\t" + (good ? "CUT" : "REJECTED") + "\n");
    if (!good) { rejected++; rej.Add(A(t.f, "Name") + " [" + t.f.Handle + "] before " + v0.ToString("F0") + " overlap " + overlap.ToString("F1") + " after " + v1.ToString("F1") + (all ? "" : " (a boolean failed)")); continue; }
    cut++; removed += drop;
    if (APPLY)
    {
        var bk = new StringBuilder(); foreach (var q in (System.Collections.IEnumerable)t.f.asPolyface.VertexList) { var ty = q.GetType(); bk.Append("V " + ty.GetProperty("x").GetValue(q) + " " + ty.GetProperty("y").GetValue(q) + " " + ty.GetProperty("z").GetValue(q) + "\n"); }
        bk.Append("F " + string.Join(" ", ((System.Collections.IEnumerable)t.f.asPolyface.FaceList).Cast<object>()) + "\n");
        System.IO.File.WriteAllText(@"C:\dswtmp\StopeCut_Backup\" + t.f.Handle + ".txt", bk.ToString());
        t.f.asPolyface.VertexList = pf.VertexList; t.f.asPolyface.FaceList = pf.FaceList; t.f.asPolyface.ResetVolumeAndAreaCache(); t.f.Update();
    }
}
System.IO.File.WriteAllText(@"C:\dswtmp\StopeCut_" + TARGET + (APPLY ? "_Applied" : "_DryRun") + ".tsv", sb.ToString(), new UTF8Encoding(true));
Out.AppendLine((APPLY ? "APPLIED" : "DRY RUN") + " target " + TARGET + (ONLY != "" ? " (only " + ONLY + ")" : "") + ": solids " + targets.Count + "  cut " + cut + "  no overlap " + none + "  rejected " + rejected + "  volume removed " + removed.ToString("F0") + " m3");
foreach (var r in rej.Take(15)) Out.AppendLine("  REJ " + r);
if (APPLY && ONLY != "") foreach (var t in targets) Out.AppendLine("  check " + A(t.f, "Name") + " volume now " + t.f.asPolyface.Volume.ToString("F1") + "  DIS Parent ID " + A(t.f, "DIS Parent ID") + "  handle " + t.f.Handle);
return null;
