// Read-only test: on a copy of one stope (not added to the drawing), subtract its overlapping dev solids with CutFirst and TrimFirst; compare volumes.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
const string STOPE = "44B1980";
var ov = System.IO.File.ReadAllLines(@"C:\dswtmp\Stope_Overlaps.tsv").Skip(1).Select(l => l.Split('\t')).Where(c => c[0] == STOPE && c[4] != "FAILED").ToList();
var hs = ov.Select(c => c[2]).ToHashSet(); double expect = ov.Sum(c => double.Parse(c[4], System.Globalization.CultureInfo.InvariantCulture));
Deswik.Graphics.Primaries.Figure s = null; var cutters = new List<Deswik.Graphics.Figures.Polyface>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { if (f.Deleted) continue; if (f.Handle == STOPE) s = f; else if (hs.Contains(f.Handle)) cutters.Add(f.asPolyface); }
double v0 = s.asPolyface.Volume; Out.AppendLine(A(s, "Name") + " volume " + v0.ToString("F1") + " m3, overlap " + expect.ToString("F1") + " m3 with " + cutters.Count + " dev solids -> expected " + (v0 - expect).ToString("F1"));
foreach (var mode in new[] { "CutFirst", "TrimFirst", "RemoveOverlap" })
{
    var mi = typeof(Deswik.Graphics.SolidTools).GetMethods().First(m => m.Name == "BooleanCutSolidWithToleranceMovements" && m.GetParameters().Length == 6);
    var bct = mi.GetParameters()[2].ParameterType; var bc = Enum.Parse(bct, mode);
    var pf = s.asPolyface.CloneOnlyTheMesh();
    int okN = 0, bad = 0; var errs = new List<string>();
    foreach (var c in cutters) { bool ok; var args = new object[] { pf, c, bc, false, 0.001, errs }; try { ok = (bool)mi.Invoke(null, args); pf = (Deswik.Graphics.Figures.Polyface)args[0]; } catch (Exception e) { ok = false; errs.Add((e.InnerException ?? e).Message); } if (ok) okN++; else bad++; }
    pf.ResetVolumeAndAreaCache(); double v1 = pf == null ? -1 : pf.Volume;
    Out.AppendLine("  " + mode + ": ok " + okN + ", failed " + bad + ", volume after " + v1.ToString("F1") + " (removed " + (v0 - v1).ToString("F1") + ")" + (errs.Count > 0 ? "  err: " + errs[0] : ""));
}
return null;
