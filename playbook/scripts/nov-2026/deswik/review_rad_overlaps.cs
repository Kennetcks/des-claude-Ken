// Read-only (MINE_UG_FY27): W390/W412 RAD W cut span and resources, and whether MA / MC Jumbo #1 have overlapping tasks
// Nov-26 to Mar-27 (a sign the schedule is not levelled).
var rad = Schedule.Tasks.Where(t => !t.Deleted && (t.Name == "W390 RAD W" || t.Name == "W412 RAD W")).OrderBy(t => t.Start).ToList();
var last = rad.OrderBy(t => t.Finish).Last();
Out.AppendLine($"RAD W cuts: {rad.Count}, first {rad.First().Start:dd/MM/yy HH:mm}, last finish {last.Finish:dd/MM/yy HH:mm} (uid {last.UniqueId})");
foreach (var g in rad.GroupBy(t => t.PhysicalResource ?? "")) Out.AppendLine($"  on [{g.Key}]: {g.Count()} cuts {g.Min(t => t.Start):dd/MM/yy}-{g.Max(t => t.Finish):dd/MM/yy}");
foreach (var rn in new[] { "MA Jumbo #1", "MC Jumbo #1" }) {
  var ts = Schedule.Tasks.Where(t => !t.Deleted && t.PhysicalResource == rn && t.Finish > new System.DateTime(2026, 11, 1) && t.Start < new System.DateTime(2027, 4, 1) && t.Finish > t.Start).OrderBy(t => t.Start).ToList();
  int ov = 0; string ex = ""; var pmax = new System.DateTime(2000, 1, 1);
  foreach (var t in ts) { if (t.Start < pmax.AddMinutes(-5)) { ov++; if (ex.Length < 500) ex += $" {t.Name}@{t.Start:dd/MM HH:mm};"; } if (t.Finish > pmax) pmax = t.Finish; }
  Out.AppendLine($"{rn}: {ts.Count} tasks Nov-Mar, first {(ts.Count > 0 ? ts[0].Start.ToString("dd/MM/yy") : "-")}, {ov} overlap the one before:{ex}");
}
return null;
