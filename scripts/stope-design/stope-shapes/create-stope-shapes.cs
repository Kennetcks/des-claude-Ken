// ST strings: create the stope shapes (5 m blocks combined into <=40-50 m stopes with 5 m pillars, end chamfers <=37 deg off vertical, flat top cuts)
// from stope_shapes_geometry.txt (written by stopes.py -> geom.py -> write_geom.py) on DESIGN\CREATING PLILLAR/STOPES\5M BLOCKS\STOPE SHAPES. Blocks are not touched.
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
var inv=System.Globalization.CultureInfo.InvariantCulture;
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
string LAY="DESIGN\\CREATING PLILLAR/STOPES\\5M BLOCKS\\STOPE SHAPES V2"; // v1 kept on ...\\STOPE SHAPES
Deswik.Graphics.Primaries.Layer ly=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if(l.Name==LAY&&!l.Deleted) ly=l;
if(ly==null){ App.Layers.Add(LAY,false,1); foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if(l.Name==LAY&&!l.Deleted) ly=l; }
int have0=0; foreach (var e in ly.Entities){ var f=e as Deswik.Graphics.Primaries.Figure; if(f!=null&&!f.Deleted) have0++; }
if(have0>0){ Out.AppendLine(LAY+" already has "+have0+" figures, stopping"); return null; }
string[] sF={"Name","ACT_TYPE","STO_DEV","LAT_VERT","LEVEL","DRIVE NUMBER","GROUP","Transformation Details","SOURCE","BLOCKS","DESIGN","INT_ID","INT_MODEL","INT_DATE"};
string[] dF={"CH_FROM","CH_TO","Stope Length","Stope Height","CHAMFER_START_DEG","CHAMFER_END_DEG","EST_TONNES","EST_AU","INT_TONNES","INT_AU","INT_VOLUME","INT_VOID_VOLUME","INT_DENSITY"};
{ var at=ly.Attributes; var have=new HashSet<string>(); foreach (var a in (System.Collections.IEnumerable)at) have.Add(Convert.ToString(a.GetType().GetProperty("Name").GetValue(a,null)));
  var addM=at.GetType().GetMethods().First(m=>m.Name=="Add"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string)); var aType=addM.GetParameters()[1].ParameterType;
  foreach (var fn in sF) if(!have.Contains(fn)) addM.Invoke(at,new object[]{fn,Enum.Parse(aType,"String"),""});
  foreach (var fn in dF) if(!have.Contains(fn)) addM.Invoke(at,new object[]{fn,Enum.Parse(aType,"Double"),0.0}); }
string DESIGN="Stope shape (Kennet 2026-10-13): 5 m blocks combined along the drive, max 40 m (50 m when it avoids a short leftover), 5 m pillar between stopes, pieces above 2.4 g/t, stope average above 2.5 g/t, ends vertical unless a chamfer of max 37 deg off vertical picks up a higher grade upper part, flat top cut where the ore is from the floor up";
int nb=0, open_=0; double T=0,M=0;
foreach (var line in File.ReadAllLines(Path.Combine(dir,"stope_shapes_geometry.txt"))) {
  if (line.Trim()=="") continue; var parts=line.Split('|');
  var meta=new Dictionary<string,string>();
  foreach (var kv in parts[0].Trim('{','}').Split(new[]{", \""},StringSplitOptions.None)) { var s=kv.Trim().Trim('"'); int c=s.IndexOf("\":"); meta[s.Substring(0,c).Trim('"')]=s.Substring(c+2).Trim().Trim('"'); }
  var V=parts[1].Split(',').Select(x=>double.Parse(x,inv)).ToArray(); var F=parts[2].Split(',').Select(x=>int.Parse(x)).ToArray();
  var pf=new Deswik.Graphics.Figures.Polyface(); for(int i=0;i<V.Length;i+=3) pf.AddVertex(V[i],V[i+1],V[i+2]); for(int i=0;i<F.Length;i+=3) pf.AddTriangle(F[i],F[i+1],F[i+2]);
  pf.Layer=ly; App.ModelSpace.Entities.AddItem(pf,null,null,true,false);
  Func<string,double> D=(k)=>double.Parse(meta[k],inv);
  pf.AttributeValueSet("Name",meta["name"]); pf.AttributeValueSet("ACT_TYPE","LH_Stope"); pf.AttributeValueSet("STO_DEV","STO"); pf.AttributeValueSet("LAT_VERT","VERT");
  pf.AttributeValueSet("LEVEL",meta["level"]); pf.AttributeValueSet("DRIVE NUMBER",meta["drive"]); pf.AttributeValueSet("GROUP",meta["group"]); pf.AttributeValueSet("Transformation Details","Group "+meta["group"]);
  pf.AttributeValueSet("SOURCE",meta["source"]); pf.AttributeValueSet("BLOCKS",meta["blocks"]); pf.AttributeValueSet("DESIGN",DESIGN);
  pf.AttributeValueSet("CH_FROM",D("ch0")); pf.AttributeValueSet("CH_TO",D("ch1")); pf.AttributeValueSet("Stope Length",D("L")); pf.AttributeValueSet("Stope Height",D("H"));
  pf.AttributeValueSet("CHAMFER_START_DEG",Math.Round(D("angL"),1)); pf.AttributeValueSet("CHAMFER_END_DEG",Math.Round(D("angR"),1)); pf.AttributeValueSet("EST_TONNES",D("t")); pf.AttributeValueSet("EST_AU",D("g"));
  if(pf.Volume<=0) pf.RepairAlternate(true,true,true); // union slivers: Deswik's repair makes them valid solids
  if(!pf.IsClosed||pf.Volume<=0) open_++; nb++; T+=D("t"); M+=D("t")*D("g");
}
App.Redraw(true);
Out.AppendLine("stopes "+nb+" not closed "+open_+"; est "+T.ToString("0")+" t @ "+(M/T).ToString("0.00"));
return null;
