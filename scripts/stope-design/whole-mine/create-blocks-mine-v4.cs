// v4: create the whole-mine 5 m blocks (full height floor to floor or 22 m, horizontal splits for >2.6 then >2.0, all blocks kept) and trial ore drive lines on new STOPE DESIGN\5M BLOCKS MINE V4 layers
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)

var inv=System.Globalization.CultureInfo.InvariantCulture;
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
string BASE="0\\06 SITE\\03 ENGINEERING\\DESIGN MASTER\\STOPES\\STOPE DESIGN\\5M BLOCKS MINE V4";
var LAYS=new Dictionary<string,string>{{"OD",BASE+"\\ORE DRIVES"},{"OTHER",BASE+"\\OTHER DRIVES"},{"NO DRIVE",BASE+"\\NO DRIVE"}};
string LINES=BASE+"\\NO DRIVE\\TRIAL ORE DRIVES";
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
string DESIGN="5 m block v4, whole-mine run 2026-10-12: base on the drive floor, always full height (floor to floor when a drive is above within 22 m, else 22 m). Walls on >=2.5 g/t, drillable (convex, >=55 deg, <=1 m change per 5 m). A block below 2.6 g/t is split once with a horizontal cut (by height, 2.5 m steps, pieces >=5 m) when one piece is above 2.6 g/t, else above 2.0 g/t. Ore drive and trial drive blocks kept even with no grade, other drives only above 1.5 g/t";
int nb=0, bad=0; var inval=new List<string>(); var cnt=new Dictionary<string,int>(); double vol=0;
foreach (var line in File.ReadAllLines(Path.Combine(dir,"blocks_5m_mine_v4_geometry.txt"))) {
  if (line.Trim()=="") continue; var parts=line.Split('|');
  var meta=new Dictionary<string,string>();
  foreach (var kv in parts[0].Trim('{','}').Split(',')) { int c=kv.IndexOf(':'); meta[kv.Substring(0,c).Trim('"')]=kv.Substring(c+1).Trim('"'); }
  var V=parts[1].Split(',').Select(x=>double.Parse(x,inv)).ToArray(); var F=parts[2].Split(',').Select(x=>int.Parse(x)).ToArray();
  var pf=new Deswik.Graphics.Figures.Polyface(); for(int i=0;i<V.Length;i+=3) pf.AddVertex(V[i],V[i+1],V[i+2]); for(int i=0;i<F.Length;i+=3) pf.AddTriangle(F[i],F[i+1],F[i+2]);
  string src=meta["source"]; string key=src=="OD"?"OD":(src=="NO DRIVE"?"NO DRIVE":"OTHER");
  pf.Layer=lays[key]; App.ModelSpace.Entities.AddItem(pf,null,null,true,false);
  Func<string,double> D=(k)=>double.Parse(meta[k],inv);
  pf.AttributeValueSet("Name",meta["name"]); pf.AttributeValueSet("ACT_TYPE","LH_Stope"); pf.AttributeValueSet("STO_DEV","STO"); pf.AttributeValueSet("LAT_VERT","VERT");
  pf.AttributeValueSet("LODE",meta["lode"]); pf.AttributeValueSet("LEVEL",meta["level"]); pf.AttributeValueSet("DRIVE NUMBER",meta["drive"]); pf.AttributeValueSet("SOURCE",src); pf.AttributeValueSet("BLOCK",meta["block"]); pf.AttributeValueSet("SPLIT",meta["split"]); pf.AttributeValueSet("BREAKTHROUGH",meta["bt"]); pf.AttributeValueSet("DRILLABLE",meta["drill"]); pf.AttributeValueSet("DESIGN",DESIGN);
  pf.AttributeValueSet("CH_FROM",D("ch0")); pf.AttributeValueSet("CH_TO",D("ch1")); pf.AttributeValueSet("Stope Height",D("ht")); pf.AttributeValueSet("BASE_ABOVE_FLOOR",D("zlo")); pf.AttributeValueSet("Stope Length",D("ch1")-D("ch0")); pf.AttributeValueSet("Stope Thickness",D("w"));
  pf.AttributeValueSet("EST_TONNES",D("t")); pf.AttributeValueSet("EST_AU",D("g")); pf.AttributeValueSet("IN_PLAN_PCT",D("inplan")); pf.AttributeValueSet("MINED_PCT",D("mined")); pf.AttributeValueSet("DECLINE_DIST",D("dec"));
  if(!pf.IsClosed) bad++; if(pf.Volume<0) inval.Add(meta["name"]); vol+=Math.Abs(pf.Volume); nb++; int cc; cnt.TryGetValue(key,out cc); cnt[key]=cc+1;
}
var rows=File.ReadAllLines(Path.Combine(dir,"virtual_drive_lines_v4.csv")).Skip(1).Select(l=>l.Split(',')).ToList(); int nl=0;
foreach (var g in rows.GroupBy(r=>r[0])) {
  var pl=new Deswik.Graphics.Figures.Polyline(); foreach (var r in g) pl.AddVertex(double.Parse(r[3],inv),double.Parse(r[4],inv),double.Parse(r[5],inv));
  pl.Layer=ll; App.ModelSpace.Entities.AddItem(pl,null,null,true,false);
  var f0=g.First(); pl.AttributeValueSet("Name","Trial OD "+g.Key+" L"+f0[1]); pl.AttributeValueSet("ACT_TYPE","OD"); pl.AttributeValueSet("LEVEL",f0[1]); pl.AttributeValueSet("DRIVE NUMBER",g.Key); pl.AttributeValueSet("SOURCE","NO DRIVE");
  pl.AttributeValueSet("DESIGN","Trial ore drive floor line through >=2.5 g/t ore no existing drive reaches; not a design, for the blocks only"); nl++;
}
App.Redraw(true);
Out.AppendLine("blocks "+nb+" not closed "+bad+" volume "+vol.ToString("0")+" by layer: "+string.Join(", ",cnt.Select(k=>k.Key+"="+k.Value))+"; trial drive lines "+nl+"; invalid solids "+inval.Count+": "+string.Join(" | ",inval));
return null;
