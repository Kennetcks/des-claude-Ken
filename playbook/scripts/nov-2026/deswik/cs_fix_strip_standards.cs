// Snap the STRIP STANDARDS outlines to their labelled sizes (plan dimensions), keeping position, bearing and right angles.
// Fillet triangles: legs from the right-angle corner set to the labelled length. Laser channels: long sides to 1.0 m,
// centred. Pump strip: front edge to 1.0 m, centred. Fan starter: drop the duplicate point, long edge to 6.5 m, centred.
const string lay = "0\\REFERENCE\\STRIP STANDARDS";
var jobs = new (double x, double y, string kind, double len, string name)[] {
    (8079.97, 62863.67, "tri", 1.5, "Small fillet"), (8077.23, 62867.29, "tri", 1.5, "Small fillet"),
    (8079.35, 62851.89, "tri", 2.5, "Loader/Truck access 2.5"), (8080.38, 62844.54, "tri", 2.5, "Loader/Truck access 2.5"),
    (8047.28, 62860.89, "rect", 1.0, "Laser Channel"), (8052.02, 62864.57, "rect", 1.0, "Laser Channel"),
    (8060.55, 62844.85, "edge0", 1.0, "Pump Strip"), (8060.15, 62857.49, "long", 6.5, "Fan Starter x2") };
double D(double[] a, double[] b) => Math.Sqrt((b[0] - a[0]) * (b[0] - a[0]) + (b[1] - a[1]) * (b[1] - a[1]));
// move vertices i and j apart/together along their edge (plan) so the edge is len long, centred
void SetEdge(List<double[]> P, int i, int j, double len)
{
    double cx = (P[i][0] + P[j][0]) / 2, cy = (P[i][1] + P[j][1]) / 2, L = D(P[i], P[j]), ux = (P[j][0] - P[i][0]) / L, uy = (P[j][1] - P[i][1]) / L;
    P[i][0] = cx - ux * len / 2; P[i][1] = cy - uy * len / 2; P[j][0] = cx + ux * len / 2; P[j][1] = cy + uy * len / 2;
}
var found = new HashSet<int>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != lay || !f.IsPolyline) continue;
    var p = f.asPolyline; var v0 = p.GetVertex(0);
    int k = Array.FindIndex(jobs, j => Math.Abs(j.x - v0.x) < 0.01 && Math.Abs(j.y - v0.y) < 0.01);
    if (k < 0 || !found.Add(k)) continue;
    var job = jobs[k];
    var P = Enumerable.Range(0, p.VertexCount).Select(i => { var q = p.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
    for (int i = P.Count - 1; i > 0; i--) if (D(P[i], P[i - 1]) < 0.001) P.RemoveAt(i);        // duplicate points
    string before = string.Join(" ", Enumerable.Range(0, P.Count).Select(i => D(P[i], P[(i + 1) % P.Count]).ToString("F3")));
    if (job.kind == "tri")
    {
        int hyp = Enumerable.Range(0, 3).OrderByDescending(i => D(P[i], P[(i + 1) % 3])).First(); int c = (hyp + 2) % 3;  // right-angle corner
        foreach (int m in new[] { hyp, (hyp + 1) % 3 }) { double L = D(P[c], P[m]); P[m][0] = P[c][0] + (P[m][0] - P[c][0]) / L * job.len; P[m][1] = P[c][1] + (P[m][1] - P[c][1]) / L * job.len; }
    }
    else if (job.kind == "rect")
    {
        int e = D(P[0], P[1]) > D(P[1], P[2]) ? 0 : 1;      // long sides: (e,e+1) and (e+2,e+3)
        SetEdge(P, e, e + 1, job.len); SetEdge(P, (e + 3) % 4, e + 2, job.len);
    }
    else if (job.kind == "edge0") SetEdge(P, 0, 1, job.len);
    else if (job.kind == "long") { int e = Enumerable.Range(0, P.Count).OrderByDescending(i => D(P[i], P[(i + 1) % P.Count])).First(); SetEdge(P, e, (e + 1) % P.Count, job.len); }
    string after = string.Join(" ", Enumerable.Range(0, P.Count).Select(i => D(P[i], P[(i + 1) % P.Count]).ToString("F3")));
    bool closed = p.Closed;
    while (p.VertexCount > 0) p.RemoveVertexAt(0);
    foreach (var q in P) p.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
    p.Closed = closed; p.Update();
    Out.AppendLine($"{job.name,-26} edges {before}  ->  {after}");
}
App.Redraw(true);
Out.AppendLine($"Fixed {found.Count} of {jobs.Length} outlines");
return null;
