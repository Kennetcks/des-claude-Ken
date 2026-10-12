// Write interrogated tonnes, volume, density and AU onto each floor-string solid
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

string LAY = "DESIGN\\FLOOR STRINGS\\SOLIDS";
string csv = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kennet.DeswikClaude", "interrogation_floor_string_solids_by_solid.csv");
var lines = File.ReadAllLines(csv);
Func<string,string[]> split = s => s.Split(',').Select(x=>x.Trim('"','﻿')).ToArray();
var hdr = split(lines[0]).ToList();
int iId=hdr.IndexOf("DIS Parent ID");
var map = new Dictionary<string,string[]>();
for (int i=1;i<lines.Length;i++){ var c=split(lines[i]); if (c.Length>=hdr.Count) map[c[iId]]=c; }
// csv column -> attribute name
var cols = new string[][]{ new[]{"Volume","INT_VOLUME"}, new[]{"Model Volume","INT_MODEL_VOLUME"}, new[]{"Void Volume","INT_VOID_VOLUME"}, new[]{"Model Tons","INT_TONNES"}, new[]{"Density","INT_DENSITY"}, new[]{"AU","INT_AU"} };
Deswik.Graphics.Primaries.Layer ly=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (l.Name==LAY) ly=l;
var at=ly.Attributes;
var addM=at.GetType().GetMethods().First(mm=>mm.Name=="Add"&&mm.GetParameters().Length==3&&mm.GetParameters()[0].ParameterType==typeof(string));
var aType=addM.GetParameters()[1].ParameterType; var tn=Enum.GetNames(aType);
string numName = tn.Contains("Double")?"Double":(tn.Contains("Real")?"Real":(tn.Contains("Float")?"Float":"String"));
var known = new HashSet<string>(((System.Collections.IEnumerable)at).Cast<object>().Select(x=>Convert.ToString(x.GetType().GetProperty("Name").GetValue(x,null))));
foreach (var c in cols) if (!known.Contains(c[1])) addM.Invoke(at,new object[]{c[1], Enum.Parse(aType,numName), numName=="String"?(object)"":(object)0.0});
if (!known.Contains("INT_MODEL")) addM.Invoke(at,new object[]{"INT_MODEL", Enum.Parse(aType,"String"), ""});
if (!known.Contains("INT_DATE")) addM.Invoke(at,new object[]{"INT_DATE", Enum.Parse(aType,"String"), ""});
string model = "BLOCK_MODEL"; string date = DateTime.Now.ToString("yyyy-MM-dd");
int ok=0, miss=0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Deleted||f.Layer!=ly) continue;
  string id=Convert.ToString(f.AttributeValueGet("DIS Parent ID",""));
  string[] row; if (!map.TryGetValue(id,out row)) { miss++; continue; }
  foreach (var c in cols) { double v; double.TryParse(row[hdr.IndexOf(c[0])], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v); f.AttributeValueSet(c[1], Math.Round(v, c[1]=="INT_AU"?3:2)); }
  f.AttributeValueSet("INT_MODEL", model); f.AttributeValueSet("INT_DATE", date);
  ok++;
}
Out.AppendLine("attr type used: "+numName+"; solids updated="+ok+" missing="+miss);
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) { if (f.Deleted||f.Layer!=ly) continue; Out.AppendLine("example "+f.Handle+": "+string.Join(", ",cols.Select(c=>c[1]+"="+Convert.ToString(f.AttributeValueGet(c[1],""))))); break; }
return null;