// v7: replace blocks Deswik reports as invalid (Volume -1) with the convex solids from hull_invalid_blocks.py
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)

var inv=System.Globalization.CultureInfo.InvariantCulture;
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
string BASE="0\\06 SITE\\03 ENGINEERING\\DESIGN MASTER\\STOPES\\STOPE DESIGN\\5M BLOCKS MINE V7";
string[] fields={"Name","ACT_TYPE","STO_DEV","LAT_VERT","LODE","LEVEL","DRIVE NUMBER","SOURCE","BLOCK","SPLIT","BREAKTHROUGH","DRILLABLE","DESIGN","CH_FROM","CH_TO","Stope Height","BASE_ABOVE_FLOOR","Stope Length","Stope Thickness","EST_TONNES","EST_AU","IN_PLAN_PCT","MINED_PCT","DECLINE_DIST"};
var geo=new Dictionary<string,string[]>();
foreach (var line in File.ReadAllLines(Path.Combine(dir,"blocks_5m_mine_v7_hulls.txt"))) { if(line.Trim()=="") continue; var p=line.Split('|'); int a=p[0].IndexOf("\"name\":\"")+8; geo[p[0].Substring(a,p[0].IndexOf('"',a)-a)]=p; }
var old=new List<Deswik.Graphics.Primaries.Figure>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if(!f.Deleted&&f.Layer!=null&&f.Layer.Name.StartsWith(BASE)&&f.asPolyface!=null&&f.asPolyface.Volume<0&&geo.ContainsKey(Convert.ToString(f.AttributeValueGet("Name","")))) old.Add(f);
int nb=0, still=0;
foreach (var f in old) {
  var p=geo[Convert.ToString(f.AttributeValueGet("Name",""))];
  var V=p[1].Split(',').Select(x=>double.Parse(x,inv)).ToArray(); var F=p[2].Split(',').Select(x=>int.Parse(x)).ToArray();
  var pf=new Deswik.Graphics.Figures.Polyface(); for(int i=0;i<V.Length;i+=3) pf.AddVertex(V[i],V[i+1],V[i+2]); for(int i=0;i<F.Length;i+=3) pf.AddTriangle(F[i],F[i+1],F[i+2]);
  pf.Layer=f.Layer; App.ModelSpace.Entities.AddItem(pf,null,null,true,false);
  foreach (var fn in fields) pf.AttributeValueSet(fn,f.AttributeValueGet(fn,""));
  pf.AttributeValueSet("DESIGN",Convert.ToString(f.AttributeValueGet("DESIGN",""))+"; rebuilt as a convex solid (original end sections crossed on a bend)");
  f.Deleted=true; nb++; if (pf.Volume<0) still++;
}
App.Redraw(true);
Out.AppendLine("replaced "+nb+" of "+geo.Count+", still invalid "+still);
return null;
