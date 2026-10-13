// Draw decline_level_strings.csv (from dec_design.py) as polylines on sublayers of DESIGN\DECLINE, with ACT_TYPE, LEVEL, LODE, DESIGN.
// Stops if a target sublayer already has figures (delete them first for a re-run). Never touches Kennet's DESIGN\DECLINE string.
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)

var inv=System.Globalization.CultureInfo.InvariantCulture;
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
string B="DESIGN\\DECLINE";
var LAY=new Dictionary<string,string>{{"DEC",B+"\\DECLINE EXTENSION"},{"ACC",B+"\\ACCESS"},{"OD",B+"\\ORE DRIVES"},{"RAR",B+"\\RAR"},{"RAD",B+"\\RAR"},{"EWR",B+"\\ESCAPEWAY"},{"EWD",B+"\\ESCAPEWAY"},{"ISP",B+"\\STOCKPILES"},{"DSP",B+"\\STOCKPILES"},{"SMP",B+"\\SUMPS"}};
var DES=new Dictionary<string,string>{
 {"DEC","Decline extension from the end of DESIGN\\DECLINE (no gap), 1:7, stacked 22 m loops, axis stepping with the ore as central as the 25 m stope standoff allows (Claude concept 2026-10-13)"},
 {"ACC","Level access: 90 deg take-off from the decline, 12 m straight, rising ~1:50 to the ore drive, routed clear of other stopes and 10 m off the decline; short ACC between drives of the same level group = crosscut"},
 {"OD","Ore drive on the stope floor (floor centre of the 5 m blocks), continuous along strike"},
 {"RAR","Return air raise beside the decline, straight, ~66 deg, all levels"},{"RAD","Return air drive from the level access to the RAR"},
 {"EWR","Escapeway raise beside the decline, opposite side to the RAR, straight, ~66 deg, all levels"},{"EWD","Escapeway drive from the level access to the escapeway raise"},
 {"ISP","Level stockpile, 15 m off the access, +1:50"},{"DSP","Decline stockpile, 22 m on the outside of the loop, +1:50, about every 150 m of decline"},{"SMP","Decline sump, 10 m on the inside of the loop at -1:7, about every 30 m vertical, plus one at the bottom"}};
Func<string,Deswik.Graphics.Primaries.Layer> getL=(nm)=>{ Deswik.Graphics.Primaries.Layer r=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if(l.Name==nm&&!l.Deleted) r=l; if(r==null){ App.Layers.Add(nm,false,1); foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if(l.Name==nm&&!l.Deleted) r=l; } return r; };
var lays=new Dictionary<string,Deswik.Graphics.Primaries.Layer>(); foreach(var kv in LAY) lays[kv.Key]=getL(kv.Value);
foreach (var ly in lays.Values.Distinct()) { int c=0; foreach (var e in ly.Entities){ var f=e as Deswik.Graphics.Primaries.Figure; if(f!=null&&!f.Deleted) c++; } if(c>0){ Out.AppendLine(ly.Name+" already has "+c+" figures, stopping"); return null; } }
string[] sF={"Name","ACT_TYPE","STO_DEV","LAT_VERT","LEVEL","LODE","DESIGN"};
foreach (var ly in lays.Values.Distinct()) {
  var at=ly.Attributes; var have=new HashSet<string>(); foreach (var a in (System.Collections.IEnumerable)at) have.Add(Convert.ToString(a.GetType().GetProperty("Name").GetValue(a,null)));
  var addM=at.GetType().GetMethods().First(m=>m.Name=="Add"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string)); var aType=addM.GetParameters()[1].ParameterType;
  foreach (var fn in sF) if(!have.Contains(fn)) addM.Invoke(at,new object[]{fn,Enum.Parse(aType,"String"),""});
}
var rows=File.ReadAllLines(Path.Combine(dir,"decline_level_strings.csv")).Skip(1).Select(l=>l.Split(',')).ToList();
var cnt=new Dictionary<string,int>();
foreach (var g in rows.GroupBy(r=>r[0])) {
  var f0=g.First(); string t=f0[1];
  var pl=new Deswik.Graphics.Figures.Polyline(); foreach (var r in g) pl.AddVertex(double.Parse(r[4],inv),double.Parse(r[5],inv),double.Parse(r[6],inv));
  pl.Layer=lays[t]; App.ModelSpace.Entities.AddItem(pl,null,null,true,false);
  pl.AttributeValueSet("Name",t+" "+(f0[2]==""?"":"L"+f0[2]+" ")+g.Key); pl.AttributeValueSet("ACT_TYPE",t); pl.AttributeValueSet("STO_DEV","DEV"); pl.AttributeValueSet("LAT_VERT",(t=="RAR"||t=="EWR")?"VERT":"LAT");
  pl.AttributeValueSet("LEVEL",f0[2]); pl.AttributeValueSet("LODE",f0[3]); pl.AttributeValueSet("DESIGN",DES[t]);
  int c; cnt.TryGetValue(t,out c); cnt[t]=c+1;
}
App.Redraw(true);
Out.AppendLine(string.Join(", ",cnt.Select(k=>k.Key+"="+k.Value))); return null;
