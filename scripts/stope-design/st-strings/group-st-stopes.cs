// ST strings: tag every 5 m stope with GROUP (A-F) and "Transformation Details" = "Group X" from stope_groups.csv,
// then create the "Stope groups" legend (Field GROUP, one colour per group) and show it on the two stope layers.
// Never edits "5m blocks AU v2"; switch a layer back to it from the layer's legend list.
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
var inv=System.Globalization.CultureInfo.InvariantCulture;
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
string BASE="DESIGN\\CREATING PLILLAR/STOPES\\5M BLOCKS"; string NAME="Stope groups";
// stope_groups.csv: drive,group. drive = DRIVE NUMBER (ST handle or X###); intersections are INT@x,y (plan centre)
var map=new Dictionary<string,string>(); var ints=new List<double[]>(); var intG=new List<string>();
foreach (var l in File.ReadAllLines(Path.Combine(dir,"stope_groups.csv")).Skip(1)) { if(l.Trim()=="") continue; int c=l.LastIndexOf(','); string k=l.Substring(0,c), g=l.Substring(c+1).Trim();
  if(k.StartsWith("INT@")){ var p=k.Substring(4).Split(','); ints.Add(new double[]{double.Parse(p[0],inv),double.Parse(p[1],inv)}); intG.Add(g);} else map[k]=g; }
var layers=new List<Deswik.Graphics.Primaries.Layer>();
foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if(!l.Deleted&&(l.Name==BASE+"\\STOPES"||l.Name==BASE+"\\EXTRA STRINGS")) layers.Add(l);
foreach (var ly in layers) { var at=ly.Attributes; var have=new HashSet<string>(); foreach (var a in (System.Collections.IEnumerable)at) have.Add(Convert.ToString(a.GetType().GetProperty("Name").GetValue(a,null)));
  var addM=at.GetType().GetMethods().First(m=>m.Name=="Add"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string)); var aType=addM.GetParameters()[1].ParameterType;
  foreach (var fn in new[]{"GROUP","Transformation Details"}) if(!have.Contains(fn)) addM.Invoke(at,new object[]{fn,Enum.Parse(aType,"String"),""}); }
var cnt=new Dictionary<string,int>(); var miss=new List<string>();
foreach (var ly in layers) foreach (var e in ly.Entities) { var f=e as Deswik.Graphics.Primaries.Figure; if(f==null||f.Deleted||f.asPolyface==null) continue;
  string dn=Convert.ToString(f.AttributeValueGet("DRIVE NUMBER","")); string g=null;
  if(dn.Contains("/")){ double sx=0,sy=0; int n=0; foreach (var q in f.asPolyface.VertexList){ sx+=q.x; sy+=q.y; n++; } sx/=n; sy/=n; double best=1e18; for(int i=0;i<ints.Count;i++){ double d=Math.Pow(ints[i][0]-sx,2)+Math.Pow(ints[i][1]-sy,2); if(d<best){best=d; g=intG[i];} } if(best>400) g=null; }
  else if(map.ContainsKey(dn)) g=map[dn];
  if(g==null){ miss.Add(Convert.ToString(f.AttributeValueGet("Name",""))); continue; }
  f.AttributeValueSet("GROUP",g); f.AttributeValueSet("Transformation Details","Group "+g); int cc; cnt.TryGetValue(g,out cc); cnt[g]=cc+1; }
if(!App.Legends.CheckExists(NAME)){
  var tmpl=App.Legends.Find("Act_Type"); // any value legend works as the item template
  var lg=new Deswik.Common.Core.Legends.Legend(NAME,tmpl.Filters,tmpl.GlobalConstants);
  var cols=new[]{System.Drawing.Color.FromArgb(230,25,75),System.Drawing.Color.FromArgb(0,130,200),System.Drawing.Color.FromArgb(60,180,75),System.Drawing.Color.FromArgb(255,165,0),System.Drawing.Color.FromArgb(145,30,180),System.Drawing.Color.FromArgb(0,200,200)};
  string[] desc={"Group A - East lode","Group B - South upper","Group C - North upper","Group D - East lower","Group E - West deep","Group F - Main middle"};
  for(int i=0;i<6;i++){ var it=new Deswik.Common.Core.Legends.LegendItem(tmpl[0]); it.Description=desc[i]; it.Field="GROUP"; it.Value=((char)('A'+i)).ToString();
    it.PenColor=cols[i]; it.FillColor=cols[i]; it.FillBkColor=cols[i]; it.Transparency=255; lg.Add(it); }
  App.Legends.Add(lg);
}
foreach (var ly in layers) App.Legends.ApplyToLayer(ly,NAME);
App.Redraw(true);
foreach (var k in cnt.Keys.OrderBy(x=>x)) Out.AppendLine(k+": "+cnt[k]+" blocks");
Out.AppendLine("unmatched "+miss.Count+": "+string.Join(" | ",miss.Take(20)));
return null;
