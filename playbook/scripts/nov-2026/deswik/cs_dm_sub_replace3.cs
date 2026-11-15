// MA UG Design Master.duf: replace the old G6390 SUB design with the new draft from the Level Design Drafter (exported to
// C:\dswtmp\sub_draft_export.txt). Notes (0\DESIGN MASTER\NOTES, \HAZARDS, \BHID) are kept as they are.
// - centreline 4583727 and wall 4583731: replaced by copies carrying the draft geometry (same layer, style, attributes)
// - decline walls 44BD9FF (end) and 44BDA03 (start): moved to the new 2.5 m fillet ends
// - SUB annotations (CONTROL, GRADE text + leader, GSS, LENGHT, NAME, PROFILE, SOLIDS): turned with the centreline about its
//   start (the new CL is the old one rotated about the same start point and raised a few mm), so they sit as regenerated
// - CABLEBOLTS: 9 CB 02 cable points + label from the draft (templates 3BCE72C point, 4581B39 label), tagged G6390 SUB
// Old vertices / positions to C:\dswtmp\dm_sub_replace_backup3.txt. Dry run unless APPLY. Does not save.
const bool APPLY = false;
var inv = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
if (!App.Filename.EndsWith("MA UG Design Master.duf")) { Out.AppendLine("WRONG WINDOW " + App.Filename); return null; }
var H = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase); foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer != null) H[f.Handle] = f;
var ann = new[] { "4583728", "458372B", "4583729", "458372C", "458372D", "458372E", "458372F", "4583730" };
var oldCab = new[] { "45891ED", "45891EE", "45891EF", "45891F0", "45891F1", "45891F2", "45891F3", "45891F4", "45891F5", "45891F6" }; var hz = new[] { "45891E1", "45891E2", "45891E3", "45891E4", "45891E5", "45891E6" };
var need = new[] { "45891E9", "45891EA", "45891EB", "45891EC", "3BCE72C", "4581B39" }.Concat(ann).ToArray();
foreach (var h in need.Concat(oldCab)) if (!H.ContainsKey(h)) { Out.AppendLine(h + " missing - nothing done"); return null; }
var lines = System.IO.File.ReadAllLines(@"C:\dswtmp\sub_draft_export.txt").Where(l => l.Contains('\t')).ToList();
List<double[]> Pts(string s) => s.Split(';').Select(p => p.Split(',').Select(v => double.Parse(v, inv)).ToArray()).ToList();
string Get(string k) => lines.First(l => l.StartsWith(k + "\t")).Substring(k.Length + 1);
var nCL = Pts(Get("CL")); var nW = Pts(Get("WALL")); var up = Pts(Get("DECUP")); var dn = Pts(Get("DECDN"));
var cab = lines.Where(l => l.StartsWith("CAB\t")).Select(l => Pts(l.Substring(4))[0]).ToList();
var labF = Get("CABLAB").Split('\t'); var labP = Pts(labF[0])[0]; double labRot = double.Parse(labF[1], inv);
List<double[]> P3(string h) => Enumerable.Range(0, H[h].asPolyline.VertexCount).Select(i => { var q = H[h].asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
double D3(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2) + Math.Pow(a[2] - b[2], 2));
var oCL = P3("45891E9"); var oW = P3("45891EA"); var w9 = P3("45891EB"); var wA = P3("45891EC");
double rot = Math.Atan2(nCL[1][1] - nCL[0][1], nCL[1][0] - nCL[0][0]) - Math.Atan2(oCL[1][1] - oCL[0][1], oCL[1][0] - oCL[0][0]), dz = nCL[0][2] - oCL[0][2];
// checks before anything changes
if (D3(oCL[0], nCL[0]) > 0.5 || nCL.Count != 2 || nW.Count != 6) { Out.AppendLine("draft does not match the expected take-off - nothing done"); return null; }
if (D3(w9[^1], oW[0]) > 0.005 || D3(wA[0], oW[^1]) > 0.005) { Out.AppendLine("decline walls do not end on the old SUB wall - nothing done"); return null; }
if (D3(up[^1], nW[0]) > 0.005 || D3(dn[0], nW[^1]) > 0.005) { Out.AppendLine("draft fillet ends do not tie - nothing done"); return null; }
int existingCab = H.Values.Count(f => !oldCab.Contains(f.Handle) && f.Layer.Name == "0\\DESIGN MASTER\\CABLEBOLTS" && Math.Abs(f.BoundingBox.Midpoint.x - nCL[0][0]) < 10 && Math.Abs(f.BoundingBox.Midpoint.y - nCL[0][1]) < 10);
Out.AppendLine($"centreline: start moves {D3(oCL[0], nCL[0]):F3} m, turned {rot * 180 / Math.PI:F3} deg, RL +{dz * 1000:F1} mm; end moves {D3(oCL[1], nCL[1]):F3} m");
Out.AppendLine($"SUB wall: {oW.Count} -> {nW.Count} pts; fillet ends move {D3(oW[0], nW[0]) * 1000:F0} / {D3(oW[^1], nW[^1]) * 1000:F0} mm; 44BD9FF end and 44BDA03 start follow");
Out.AppendLine($"annotations turned with the centreline: {ann.Length} ({string.Join(", ", ann.Select(h => H[h].Layer.Name.Replace("0\\DESIGN MASTER\\CENTRELINES\\", "")))})");
Out.AppendLine($"old SUB cables replaced: {oldCab.Length}; hazards left for the BH warning step: {hz.Count(h => H.ContainsKey(h))}");
Out.AppendLine($"cables: {cab.Count} points + 1 'CB 02' label on 0\\DESIGN MASTER\\CABLEBOLTS (existing cable figures within 10 m: {existingCab})");
var notes = H.Values.Where(f => A(f, "Name") == "G6390 SUB" && f.Layer.Name.StartsWith("0\\DESIGN MASTER\\NOTES")).ToList();
Out.AppendLine($"kept as they are: {notes.Count} G6390 SUB notes ({string.Join(", ", notes.GroupBy(f => f.Layer.Name.Replace("0\\DESIGN MASTER\\", "")).Select(g => g.Key + " " + g.Count()))})");
var other = H.Values.Where(f => A(f, "Name") == "G6390 SUB" && !f.Layer.Name.StartsWith("0\\DESIGN MASTER\\NOTES") && !need.Contains(f.Handle) && !oldCab.Contains(f.Handle) && !hz.Contains(f.Handle)).ToList();
Out.AppendLine("other G6390 SUB figures not covered: " + (other.Count == 0 ? "none" : string.Join(", ", other.Select(f => f.Handle + " [" + f.Layer.Name + "]"))));
if (!APPLY) { Out.AppendLine("dry run - nothing changed"); return null; }
if (existingCab > 0 || other.Count > 0) { Out.AppendLine("unexpected figures at the mouth - nothing done"); return null; }
// backup
var log = new System.Text.StringBuilder();
foreach (var h in new[] { "45891E9", "45891EA", "45891EB", "45891EC" }) { log.AppendLine("== " + h); foreach (var q in P3(h)) log.AppendLine($"{q[0]:R},{q[1]:R},{q[2]:R}"); }
foreach (var h in oldCab) { var q = H[h].IsPoint ? H[h].asPoint.InsertionPoint : H[h].asMText.InsertionPoint; log.AppendLine($"== cable {h} {q.x:R},{q.y:R},{q.z:R}"); }
foreach (var h in ann) { var b = H[h].BoundingBox; log.AppendLine($"== {h} bbox {b.Min.x:R},{b.Min.y:R},{b.Min.z:R} / {b.Max.x:R},{b.Max.y:R},{b.Max.z:R}" + (H[h].IsText ? $" rot {H[h].asText.Rotation:R}" : "")); }
log.AppendLine($"== rotation about {oCL[0][0]:R},{oCL[0][1]:R} by {rot:R} rad, dz {dz:R} (undo: rotate by -rot, dz -dz)");
System.IO.File.WriteAllText(@"C:\dswtmp\dm_sub_replace_backup3.txt", log.ToString());
Deswik.Graphics.Primaries.Figure NewPl(Deswik.Graphics.Primaries.Figure src, List<double[]> p) { var k = (Deswik.Graphics.Primaries.Figure)src.Clone(); k.Deleted = false; var pl = k.asPolyline; bool cl = pl.Closed; while (pl.VertexCount > 0) pl.RemoveVertexAt(0); foreach (var q in p) pl.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]); pl.Closed = cl; App.ModelSpace.Entities.AddItem(k, null, null, true, false); return k; }
var res = new List<string>();
void Rep(string h, List<double[]> p) { var k = NewPl(H[h], p); H[h].Deleted = true; res.Add(h + "->" + k.Handle); }
Rep("45891E9", nCL); Rep("45891EA", nW);
var n9 = w9.Take(w9.Count - 1).ToList(); n9.Add(nW[0]); Rep("45891EB", n9);
var nA = new List<double[]> { nW[^1] }; nA.AddRange(wA.Skip(1)); Rep("45891EC", nA);
var m = new Deswik.Graphics.Geometry.Matrix(); m.RotateZMatrix(rot);
foreach (var h in ann) { var f = H[h]; f.Translate(-oCL[0][0], -oCL[0][1], 0); f.TransformBy(m); f.Translate(nCL[0][0], nCL[0][1], dz); }
foreach (var h in oldCab) H[h].Deleted = true;
var LB = H["3BCE72C"].Layer; var sub = H["45891E9"];
void Attrs(Deswik.Graphics.Primaries.Figure k) { foreach (var a in new[] { "Name", "ACT_TYPE", "GSS", "PROFILE", "LEVEL", "MINE" }) { var v = A(sub, a); if (v != "") k.AttributeValueSet(a, v); } }
var added = new List<string>();
foreach (var p in cab) { var k = (Deswik.Graphics.Primaries.Figure)H["3BCE72C"].Clone(); k.Deleted = false; Attrs(k); k.asPoint.InsertionPoint = new Deswik.Graphics.Geometry.Point(p[0], p[1], p[2]); App.ModelSpace.Entities.AddItem(k, null, null, true, false); added.Add(k.Handle); }
var t = (Deswik.Graphics.Primaries.Figure)H["4581B39"].Clone(); t.Deleted = false; Attrs(t); t.asMText.InsertionPoint = new Deswik.Graphics.Geometry.Point(labP[0], labP[1], labP[2]); t.asMText.Rotation = labRot;
App.ModelSpace.Entities.AddItem(t, null, null, true, false); added.Add(t.Handle);
App.Redraw(true);
Out.AppendLine("applied: " + string.Join(", ", res) + $"; {ann.Length} annotations turned in place; cables added {string.Join(",", added)}; backup C:\\dswtmp\\dm_sub_replace_backup3.txt; NOT saved");
return null;
