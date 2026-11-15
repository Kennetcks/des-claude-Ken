// Read-only: (1) each vertex of 458905E/5F/60 against the W390 RAD W solid floor (CP chain, projected in plan) and the pit
// surface 4589044 RL above it; (2) the pit surface profile along the W412 RAD W solids' bearing (az 117.53) from the W390
// RAD start, and the W412 RAD W solid corners.
var inv = System.Globalization.CultureInfo.InvariantCulture;
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var H = new Dictionary<string, Deswik.Graphics.Primaries.Figure>(StringComparer.OrdinalIgnoreCase); foreach (Deswik.Graphics.Primaries.Figure x in App.ModelSpace.Entities) if (!x.Deleted && x.Layer != null) H[x.Handle] = x;
var pit = H["4589044"]; var pf = pit.asPolyface; var bb = pit.BoundingBox;
double Pz(double x, double y) { try { var z = pf.GetMaxZ(x, y, bb); return z; } catch (Exception e) { Out.AppendLine("GetMaxZ err " + e.Message); return double.NaN; } }
List<double[]> CP(string nm) => H.Values.Where(f => f.IsPolyface && f.Layer.Name == "0\\3MS\\TASK SOLIDS" && A(f, "Name") == nm).Select(f => new[] { double.Parse(A(f, "_CPX"), inv), double.Parse(A(f, "_CPY"), inv), double.Parse(A(f, "_CPZ"), inv), double.Parse(A(f, "_Azimuth"), inv), double.Parse(A(f, "_Gradient"), inv) }).OrderByDescending(c => c[2]).ToList();
var rad = CP("W390 RAD W");
(double z, double d, double s) Sol(double x, double y)
{
    double bd = 1e9, bz = 0, bs = 0, c0 = 0;
    for (int i = 1; i < rad.Count; i++)
    {
        double vx = rad[i][0] - rad[i - 1][0], vy = rad[i][1] - rad[i - 1][1], L = Math.Sqrt(vx * vx + vy * vy), t = Math.Max(0, Math.Min(1, ((x - rad[i - 1][0]) * vx + (y - rad[i - 1][1]) * vy) / (L * L)));
        double dd = Math.Sqrt(Math.Pow(rad[i - 1][0] + t * vx - x, 2) + Math.Pow(rad[i - 1][1] + t * vy - y, 2));
        if (dd < bd) { bd = dd; bz = rad[i - 1][2] + t * (rad[i][2] - rad[i - 1][2]); bs = c0 + t * L; }
        c0 += L;
    }
    return (bz, bd, bs);
}
foreach (var h in new[] { "458905E", "458905F", "4589060" })
{
    var f = H[h]; var dzs = new List<double>(); Out.AppendLine($"=== {h} {A(f, "ACT_TYPE")} n={f.asPolyline.VertexCount}");
    for (int i = 0; i < f.asPolyline.VertexCount; i++) { var q = f.asPolyline.GetVertex(i); var s = Sol(q.x, q.y); dzs.Add(q.z - s.z); Out.AppendLine($"  v{i} ({q.x:F3},{q.y:F3},{q.z:F3}) solid ch {s.s:F2} floor {s.z:F3} off {s.d:F2} dz {q.z - s.z:F3} | pit {Pz(q.x, q.y):F3}"); }
    Out.AppendLine($"  dz range {dzs.Min():F3} to {dzs.Max():F3}, mean {dzs.Average():F3}");
}
var w = CP("W412 RAD W"); foreach (var c in w) Out.AppendLine($"W412 RAD W CP ({c[0]:F3},{c[1]:F3},{c[2]:F3}) az {c[3]:F2} gr {c[4]:F3} pit {Pz(c[0], c[1]):F3}");
foreach (var f in H.Values.Where(f => f.IsPolyface && A(f, "Name") == "W412 RAD W")) { var b = f.BoundingBox; Out.AppendLine($"  solid {f.Handle} bbox ({b.Min.x:F2},{b.Min.y:F2},{b.Min.z:F3})-({b.Max.x:F2},{b.Max.y:F2},{b.Max.z:F3}) attrs LEVEL={A(f, "LEVEL")} MINE={A(f, "MINE")} ACT={A(f, "ACT_TYPE")} PROFILE={A(f, "PROFILE")} Rate={A(f, "Rate")}"); }
double az = w[0][3] * Math.PI / 180; var s0 = rad[0];
Out.AppendLine($"pit profile from W390 RAD first CP ({s0[0]:F3},{s0[1]:F3},{s0[2]:F3}) along az {w[0][3]:F2}:");
for (double d = -6; d <= 30; d += 1) { double x = s0[0] + d * Math.Sin(az), y = s0[1] + d * Math.Cos(az); Out.AppendLine($"  d {d,5:F1} ({x:F2},{y:F2}) pit {Pz(x, y):F3}"); }
Out.AppendLine("pit profile back along the W390 RAD (az 130.92 from first CP, i.e. outward):");
double a2 = (rad[0][3] + 180) * Math.PI / 180; for (double d = -4; d <= 20; d += 1) { double x = s0[0] + d * Math.Sin(a2), y = s0[1] + d * Math.Cos(a2); Out.AppendLine($"  d {d,5:F1} pit {Pz(x, y):F3}"); }
return null;
