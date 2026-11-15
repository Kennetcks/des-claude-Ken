// Snap the cuddy outlines on STRIP STANDARDS to their labels: the opening on the drive wall stays put; the back wall is
// set to the labelled length (centred on its current middle line) at the labelled depth from the opening.
// closed quads: back = v0-v1, opening = v2-v3. open polylines: back = v1-v2, opening ends = v0 and v3.
const string lay = "0\\REFERENCE\\STRIP STANDARDS";
var jobs = new (double cx, double cy, double len, double depth, string name)[] {
    (8060.3, 62855.8, 3.0, 1.5, "Fan Starter x2 strip"), (8045.4, 62882.3, 6.0, 1.5, "Distribution Box Cuddy"),
    (8022.5, 62912.2, 5.5, 2.0, "J/Box & Fan Starter Strip"), (8031.7, 62895.4, 2.0, 1.5, "J/Box Strip") };
var done = new HashSet<int>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || f.Layer.Name != lay || !f.IsPolyline) continue;
    var p = f.asPolyline; if (p.VertexCount != 4) continue;
    var P = Enumerable.Range(0, 4).Select(i => { var q = p.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
    double cx = P.Average(q => q[0]), cy = P.Average(q => q[1]);
    int k = Array.FindIndex(jobs, j => Math.Abs(j.cx - cx) < 0.15 && Math.Abs(j.cy - cy) < 0.15);
    if (k < 0 || !done.Add(k)) continue;
    var job = jobs[k]; bool closed = p.Closed;
    int b0 = closed ? 0 : 1, b1 = closed ? 1 : 2, o0 = closed ? 2 : 0, o1 = 3;
    // opening line and unit normal pointing to the back wall
    double ox = P[o1][0] - P[o0][0], oy = P[o1][1] - P[o0][1], oL = Math.Sqrt(ox * ox + oy * oy); ox /= oL; oy /= oL;
    double nx = -oy, ny = ox; double mx = (P[b0][0] + P[b1][0]) / 2, my = (P[b0][1] + P[b1][1]) / 2;
    double dOld = (mx - P[o0][0]) * nx + (my - P[o0][1]) * ny; if (dOld < 0) { nx = -nx; ny = -ny; dOld = -dOld; }
    double lenOld = Math.Sqrt(Math.Pow(P[b1][0] - P[b0][0], 2) + Math.Pow(P[b1][1] - P[b0][1], 2));
    // new back wall: same middle line, parallel to the opening, labelled length and depth
    double fx = mx - nx * dOld, fy = my - ny * dOld;                       // foot of the middle on the opening line
    double sgn = Math.Sign((P[b1][0] - P[b0][0]) * ox + (P[b1][1] - P[b0][1]) * oy); if (sgn == 0) sgn = 1;
    double bx = fx + nx * job.depth, by = fy + ny * job.depth;
    P[b0][0] = bx - sgn * ox * job.len / 2; P[b0][1] = by - sgn * oy * job.len / 2;
    P[b1][0] = bx + sgn * ox * job.len / 2; P[b1][1] = by + sgn * oy * job.len / 2;
    while (p.VertexCount > 0) p.RemoveVertexAt(0);
    foreach (var q in P) p.AddVertexWithoutAutomaticClose(q[0], q[1], q[2]);
    p.Closed = closed; p.Update();
    double lenNew = Math.Sqrt(Math.Pow(P[b1][0] - P[b0][0], 2) + Math.Pow(P[b1][1] - P[b0][1], 2));
    double dNew = ((P[b0][0] + P[b1][0]) / 2 - P[o0][0]) * nx + ((P[b0][1] + P[b1][1]) / 2 - P[o0][1]) * ny;
    Out.AppendLine($"{job.name,-26} back {lenOld:F3} -> {lenNew:F3}, depth {dOld:F3} -> {dNew:F3}");
}
App.Redraw(true);
Out.AppendLine($"Fixed {done.Count} of {jobs.Length}");
return null;
