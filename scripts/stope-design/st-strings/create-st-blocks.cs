// ST strings: create the 5 m blocks (st_blocks_geometry.txt from st_geom.py) on DESIGN\CREATING PLILLAR/STOPES\5M BLOCKS\{ST ORE DRIVES, ST ACCESSES, EXTRA STRINGS, INTERSECTIONS} and the extra strings on DESIGN\CREATING PLILLAR/STOPES\EXTRA STRINGS
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)

var inv=System.Globalization.CultureInfo.InvariantCulture;
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
string BASE="DESIGN\\CREATING PLILLAR/STOPES\\5M BLOCKS";
var LAYS=new Dictionary<string,string>{{"ST ORE DRIVE",BASE+"\\ST ORE DRIVES"},{"ST ACCESS",BASE+"\\ST ACCESSES"},{"EXTRA STRING",BASE+"\\EXTRA STRINGS"},{"INTERSECTION",BASE+"\\INTERSECTIONS"}};
string LINES="DESIGN\\CREATING PLILLAR/STOPES\\EXTRA STRINGS";
Func<string,Deswik.Graphics.Primaries.Layer> getL=(nm)=>{ Deswik.Graphics.Primaries.Layer r=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if(l.Name==nm&&!l.Deleted) r=l; if(r==null){ App.Layers.Add(nm,false,1); foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if(l.Name==nm&&!l.Deleted) r=l; } return r; };
var lays=new Dictionary<string,Deswik.Graphics.Primaries.Layer>(); foreach (var kv in LAYS) lays[kv.Key]=getL(kv.Value); var ll=getL(LINES);
foreach (var ly in lays.Values.Concat(new[]{ll})) { int c=0; foreach (var e in ly.Entities){ var f=e as Deswik.Graphics.Primaries.Figure; if(f!=null&&!f.Deleted) c++; } if(c>0){ Out.AppendLine(ly.Name+" already has "+c+" figures, stopping"); return null; } }
string[] sF={"Name","ACT_TYPE","STO_DEV","LAT_VERT","LODE","LEVEL","DRIVE NUMBER","SOURCE","BLOCK","SPLIT","BREAKTHROUGH","DRILLABLE","DESIGN","INT_ID","INT_MODEL","INT_DATE"};
string[] dF={"CH_FROM","CH_TO","Stope Height","BASE_ABOVE_FLOOR","Stope Length","Stope Thickness","EST_TONNES","EST_AU","IN_PLAN_PCT","MINED_PCT","DECLINE_DIST","INT_TONNES","INT_AU","INT_VOLUME","INT_VOID_VOLUME","INT_DENSITY"};
foreach (var ly in lays.Values.Concat(new[]{ll})) {
  var at=ly.Attributes; var have=new HashSet<string>(); foreach (var a in (System.Collections.IEnumerable)at) have.Add(Convert.ToString(a.GetType().GetProperty("Name").GetValue(a,null)));
  var addM=at.GetType().GetMethods().First(m=>m.Name=="Add"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string)); var aType=addM.GetParameters()[1].ParameterType;
  foreach (var fn in sF) if(!have.Contains(fn)) addM.Invoke(at,new object[]{fn,Enum.Parse(aType,"String"),""});
  foreach (var fn in dF) if(!have.Contains(fn)) addM.Invoke(at,new object[]{fn,Enum.Parse(aType,"Double"),0.0});
}
string DESIGN="5 m block on Kennet's ST strings (DESIGN\\CREATING PLILLAR/STOPES\\ST), 2026-10-13, v8 rules: no block overlaps another, close lodes together when the span is > 2.6 g/t else the better lode, full height floor to floor (or 22 m), walls on >=2.5 g/t, drillable (convex, >=50 deg, <=1 m change per 5 m), split by height for a piece > 2.6 then > 2.0, ore drive blocks kept with any grade except blocks below 0.5 g/t that cannot be split into a piece above 2.0 and a lower piece (Kennet 2026-10-13), accesses only > 1.5 g/t, 15 m off the ramp";
int nb=0, bad=0; var inval=new List<string>(); var cnt=new Dictionary<string,int>();
foreach (var line in File.ReadAllLines(Path.Combine(dir,"st_blocks_geometry.txt"))) {
  if (line.Trim()=="") continue; var parts=line.Split('|');
  var meta=new Dictionary<string,string>();
  foreach (var kv in parts[0].Trim('{','}').Split(',')) { int c=kv.IndexOf(':'); meta[kv.Substring(0,c).Trim('"')]=kv.Substring(c+1).Trim('"'); }
  var V=parts[1].Split(',').Select(x=>double.Parse(x,inv)).ToArray(); var F=parts[2].Split(',').Select(x=>int.Parse(x)).ToArray();
  var pf=new Deswik.Graphics.Figures.Polyface(); for(int i=0;i<V.Length;i+=3) pf.AddVertex(V[i],V[i+1],V[i+2]); for(int i=0;i<F.Length;i+=3) pf.AddTriangle(F[i],F[i+1],F[i+2]);
  string src=meta["source"]; pf.Layer=lays[src]; App.ModelSpace.Entities.AddItem(pf,null,null,true,false);
  Func<string,double> D=(k)=>double.Parse(meta[k],inv);
  pf.AttributeValueSet("Name",meta["name"]); pf.AttributeValueSet("ACT_TYPE","LH_Stope"); pf.AttributeValueSet("STO_DEV","STO"); pf.AttributeValueSet("LAT_VERT","VERT");
  pf.AttributeValueSet("LODE",meta["lode"]); pf.AttributeValueSet("LEVEL",meta["level"]); pf.AttributeValueSet("DRIVE NUMBER",meta["drive"]); pf.AttributeValueSet("SOURCE",src); pf.AttributeValueSet("BLOCK",meta["block"]); pf.AttributeValueSet("SPLIT",meta["split"]); pf.AttributeValueSet("BREAKTHROUGH",meta["bt"]); pf.AttributeValueSet("DRILLABLE",meta["drill"]);
  pf.AttributeValueSet("DESIGN",src=="INTERSECTION"?"Intersection solid: fills the junction between two drive ends that face each other, convex hull of the two end blocks' faces, full height":src=="EXTRA STRING"?DESIGN+". On a conceptual extra string through >=2.6 g/t ore within 50 m of the ST strings that the ST blocks miss":DESIGN);
  pf.AttributeValueSet("CH_FROM",D("ch0")); pf.AttributeValueSet("CH_TO",D("ch1")); pf.AttributeValueSet("Stope Height",D("ht")); pf.AttributeValueSet("BASE_ABOVE_FLOOR",D("zlo")); pf.AttributeValueSet("Stope Length",D("ch1")-D("ch0")); pf.AttributeValueSet("Stope Thickness",D("w"));
  pf.AttributeValueSet("EST_TONNES",D("t")); pf.AttributeValueSet("EST_AU",D("g")); pf.AttributeValueSet("MINED_PCT",D("mined")); pf.AttributeValueSet("DECLINE_DIST",D("dec"));
  if(!pf.IsClosed) bad++; if(pf.Volume<0) inval.Add(meta["name"]); nb++; int cc; cnt.TryGetValue(src,out cc); cnt[src]=cc+1;
}
var rows=File.ReadAllLines(Path.Combine(dir,"st_extra_strings.csv")).Skip(1).Select(l=>l.Split(',')).ToList(); int nl=0;
foreach (var g in rows.GroupBy(r=>r[0])) {
  var pl=new Deswik.Graphics.Figures.Polyline(); foreach (var r in g) pl.AddVertex(double.Parse(r[3],inv),double.Parse(r[4],inv),double.Parse(r[5],inv));
  pl.Layer=ll; App.ModelSpace.Entities.AddItem(pl,null,null,true,false);
  var f0=g.First(); pl.AttributeValueSet("Name","Extra string "+g.Key+" L"+f0[1]); pl.AttributeValueSet("ACT_TYPE","OD"); pl.AttributeValueSet("LEVEL",f0[1]); pl.AttributeValueSet("DRIVE NUMBER",g.Key); pl.AttributeValueSet("SOURCE","EXTRA STRING");
  pl.AttributeValueSet("DESIGN","Conceptual ore drive floor line through >=2.6 g/t ore within 50 m of the ST strings that the ST blocks miss, on the nearest ST ore drive's 22 m level series; "+f0[2]+" t of that ore along it"); nl++;
}
App.Redraw(true);
Out.AppendLine("blocks "+nb+" not closed "+bad+" by layer: "+string.Join(", ",cnt.Select(k=>k.Key+"="+k.Value))+"; extra strings "+nl+"; invalid solids "+inval.Count+": "+string.Join(" | ",inval));
return null;
