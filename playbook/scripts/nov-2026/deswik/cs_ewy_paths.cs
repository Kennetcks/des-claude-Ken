// Read-only: walking routes along the dev centrelines from each W ACC / G MLA far end to the EWY (shortest path).
// Writes C:\dswtmp\EWY_paths.tsv (PathId, Side, Label, seq, x, y, z) and the handles of the strings each route uses.
string A(Deswik.Graphics.Primaries.Figure f, string k) => (f.AttributeValueGet(k, "") ?? "").ToString();
var ci = System.Globalization.CultureInfo.InvariantCulture;
var LC = "0\\06 SITE\\03 ENGINEERING\\01 DEV STRINGS\\COMBINED 12.11";
var ewy = new HashSet<string> { "45933C4", "45931DD", "45933B9", "45931DF", "458EDDC", "45931E0", "45931E1", "45931E2" };
var sources = new[] { ("458EF8F", "W", "from W6368 ACC"), ("458EF8E", "W", "from W6348 ACC"), ("458EF8D", "W", "from W6328 ACC"), ("458EF8C", "W", "from W6306 ACC"),
                      ("458EDB4", "G", "from G6370 MLA"), ("458EDB3", "G", "from G6350 MLA"), ("458EDB2", "G", "from G6320 MLA"), ("458EDB1", "G", "from G6300 MLA") };
var strs = new Dictionary<string, List<double[]>>(); var nameOf = new Dictionary<string, string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities)
{
    if (f.Deleted || f.Layer == null || !f.IsPolyline || !(f.Layer.Name == LC || f.Layer.Name == LC + "\\NEW")) continue;
    var p = Enumerable.Range(0, f.asPolyline.VertexCount).Select(i => { var q = f.asPolyline.GetVertex(i); return new[] { q.x, q.y, q.z }; }).ToList();
    if (!p.Any(q => Math.Abs(q[0] - 8100) < 200 && Math.Abs(q[1] - 63100) < 200 && q[2] > 6280 && q[2] < 6420)) continue;
    // densify to ~2 m so junctions land near a node
    var d = new List<double[]> { p[0] }; for (int i = 1; i < p.Count; i++) { double L = Math.Sqrt(Math.Pow(p[i][0] - p[i - 1][0], 2) + Math.Pow(p[i][1] - p[i - 1][1], 2) + Math.Pow(p[i][2] - p[i - 1][2], 2)); int n = Math.Max(1, (int)Math.Ceiling(L / 2)); for (int k = 1; k <= n; k++) { double t = (double)k / n; d.Add(new[] { p[i - 1][0] + t * (p[i][0] - p[i - 1][0]), p[i - 1][1] + t * (p[i][1] - p[i - 1][1]), p[i - 1][2] + t * (p[i][2] - p[i - 1][2]) }); } }
    strs[f.Handle] = d; nameOf[f.Handle] = A(f, "Name");
}
// nodes
var nodes = new List<(string h, int i, double[] p)>(); var idx = new Dictionary<string, int>();
foreach (var kv in strs) for (int i = 0; i < kv.Value.Count; i++) { idx[kv.Key + "#" + i] = nodes.Count; nodes.Add((kv.Key, i, kv.Value[i])); }
double D(double[] a, double[] b) => Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2) + Math.Pow(a[2] - b[2], 2));
var adj = new List<(int to, double w)>[nodes.Count]; for (int i = 0; i < nodes.Count; i++) adj[i] = new List<(int, double)>();
void E(int a, int b, double w) { adj[a].Add((b, w)); adj[b].Add((a, w)); }
foreach (var kv in strs) for (int i = 1; i < kv.Value.Count; i++) E(idx[kv.Key + "#" + (i - 1)], idx[kv.Key + "#" + i], D(kv.Value[i - 1], kv.Value[i]));
// junctions: every string end links to the nearest node of any other string within 3.5 m
var grid = nodes.Select((n, k) => (n, k)).GroupBy(x => ((int)Math.Floor(x.n.p[0] / 5), (int)Math.Floor(x.n.p[1] / 5), (int)Math.Floor(x.n.p[2] / 5))).ToDictionary(g => g.Key, g => g.Select(x => x.k).ToList());
foreach (var kv in strs) foreach (var ei in new[] { 0, kv.Value.Count - 1 })
{
    var a = idx[kv.Key + "#" + ei]; var pa = nodes[a].p; int best = -1; double bd = 3.5;
    var cx = (int)Math.Floor(pa[0] / 5); var cy = (int)Math.Floor(pa[1] / 5); var cz = (int)Math.Floor(pa[2] / 5);
    for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++) for (int dz = -1; dz <= 1; dz++) if (grid.TryGetValue((cx + dx, cy + dy, cz + dz), out var l)) foreach (var k in l) { if (nodes[k].h == kv.Key) continue; var dd = D(pa, nodes[k].p); if (dd < bd) { bd = dd; best = k; } }
    if (best >= 0) E(a, best, bd + 0.01);
}
var sb = new StringBuilder("PathId\tSide\tLabel\tSeq\tX\tY\tZ\tHandle\n"); var used = new HashSet<string>();
int pid = 0;
foreach (var (h, side, label) in sources)
{
    if (!strs.ContainsKey(h)) { Out.AppendLine("missing " + label); continue; }
    var s = strs[h]; var far = s[0][0] > s[s.Count - 1][0] == (side == "W") ? 0 : s.Count - 1; // W: east end; G: west end
    if (side == "W") far = s[0][0] > s[s.Count - 1][0] ? 0 : s.Count - 1; else far = s[0][0] < s[s.Count - 1][0] ? 0 : s.Count - 1;
    int src = idx[h + "#" + far];
    var dist = new double[nodes.Count]; var prev = new int[nodes.Count]; for (int i = 0; i < nodes.Count; i++) { dist[i] = double.MaxValue; prev[i] = -1; }
    var pq = new SortedSet<(double d, int n)>(); dist[src] = 0; pq.Add((0, src)); int hit = -1;
    while (pq.Count > 0) { var cur = pq.Min; pq.Remove(cur); if (cur.d > dist[cur.n]) continue; if (ewy.Contains(nodes[cur.n].h)) { hit = cur.n; break; } foreach (var (to, w) in adj[cur.n]) { var nd = cur.d + w; if (nd < dist[to]) { if (dist[to] != double.MaxValue) pq.Remove((dist[to], to)); dist[to] = nd; prev[to] = cur.n; pq.Add((nd, to)); } } }
    if (hit < 0) { Out.AppendLine("no route " + label); continue; }
    var path = new List<int>(); for (int x = hit; x >= 0; x = prev[x]) path.Add(x); path.Reverse(); pid++;
    for (int k = 0; k < path.Count; k++) { var n = nodes[path[k]]; used.Add(n.h); sb.Append(pid + "\t" + side + "\t" + label + "\t" + k + "\t" + n.p[0].ToString("F2", ci) + "\t" + n.p[1].ToString("F2", ci) + "\t" + n.p[2].ToString("F2", ci) + "\t" + n.h + "\n"); }
    Out.AppendLine(label + ": " + dist[hit].ToString("F0") + " m via " + string.Join(" > ", path.Select(x => nameOf[nodes[x].h]).Distinct()));
}
System.IO.File.WriteAllText(@"C:\dswtmp\EWY_paths.tsv", sb.ToString());
System.IO.File.WriteAllText(@"C:\dswtmp\EWY_path_strings.txt", string.Join("\n", used));
return null;
