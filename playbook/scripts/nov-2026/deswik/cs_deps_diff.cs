// Read-only: links in the 105950 snapshot (before the take-off rule) that are gone now, split into expected (parent -> take-off first cut)
// and unexpected; also links added.
var cur = Schedule.Tasks.Where(t => !t.Deleted).ToList(); var uid = cur.ToDictionary(t => t.UniqueId); var byId = cur.ToDictionary(t => t.Id);
string F(Deswik.Scheduler.Engine.Task t, string f) => t.GetFieldValueAndCacheIt(f) ?? "";
var now = new HashSet<string>(Schedule.Dependencies.Where(d => !d.Deleted && uid.ContainsKey(d.From) && uid.ContainsKey(d.To)).Select(d => uid[d.From].Id + ">" + uid[d.To].Id));
var dl = System.IO.File.ReadAllLines(@"C:\dswtmp\Schedule_Dependencies_20261113_105950.tsv").Skip(1).Select(l => l.Split('\t')).ToList();
var before = new HashSet<string>(dl.Select(c => c[1] + ">" + c[6]));
var gone = before.Where(k => !now.Contains(k)).ToList(); var added = now.Where(k => !before.Contains(k)).ToList();
// expected removal: To is cut #1 of a lateral tunnel string and From is on another tunnel string
bool Expected(string k) { var p = k.Split('>'); if (!byId.ContainsKey(p[0]) || !byId.ContainsKey(p[1])) return false; var a = byId[p[0]]; var b = byId[p[1]]; return F(b, "DIS Activity Type") == "Tunnel" && F(b, "DIS Activity Number") == "1" && F(b, "LAT_VERT") == "LAT" && F(a, "DIS Activity Type") == "Tunnel" && F(a, "DIS Parent ID") != F(b, "DIS Parent ID"); }
var unexp = gone.Where(k => !Expected(k)).ToList();
Out.AppendLine("before " + before.Count + "  now " + now.Count + "  gone " + gone.Count + " (expected " + (gone.Count - unexp.Count) + ", UNEXPECTED " + unexp.Count + ")  added " + added.Count);
string N(string id) => byId.TryGetValue(id, out var t) ? t.Name + " #" + F(t, "DIS Activity Number") + " [" + F(t, "DIS Activity Type") + "]" : id + " (missing)";
var sb = new StringBuilder(); foreach (var k in unexp) { var p = k.Split('>'); sb.AppendLine(p[0] + "\t" + p[1] + "\t" + N(p[0]) + " -> " + N(p[1])); }
System.IO.File.WriteAllText(@"C:\dswtmp\Takeoff_Rule_UnexpectedRemovals.tsv", sb.ToString(), new UTF8Encoding(true));
foreach (var g in unexp.GroupBy(k => { var p = k.Split('>'); return (byId.ContainsKey(p[0]) ? F(byId[p[0]], "ACT_TYPE") : "?") + " -> " + (byId.ContainsKey(p[1]) ? F(byId[p[1]], "ACT_TYPE") : "?"); }).OrderByDescending(g => g.Count())) Out.AppendLine("  " + g.Count() + "  " + g.Key);
foreach (var k in unexp.Take(15)) { var p = k.Split('>'); Out.AppendLine("   e.g. " + N(p[0]) + " -> " + N(p[1])); }
return null;
