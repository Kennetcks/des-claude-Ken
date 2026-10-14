// STOPE FINAL V5 (Kennet 2026-10-13, 3CA5476): create the V5 stopes (one lode per stope) + vein stopes from stope_final_v5_geometry.txt
// (stp/write_geom.py output joined with vein/write_geom.py, GROUP from the live V3 stope each one overlaps). V4 is never touched.
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
var inv=System.Globalization.CultureInfo.InvariantCulture;
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
string LAY="DESIGN\\CREATING PLILLAR/STOPES\\STOPE FINAL V5";
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
string DESIGN="STOPE FINAL V5 (Kennet 2026-10-13, 3CA5476): walls follow ONE lode per stope, the one with the most gold above 2.6 g/t, instead of a span over two lodes with low grade between; V3 rules otherwise (<=40-50 m, 5 m pillar, pieces >2.4, average >2.5, 5 m vertical then <=40 deg chamfer, >=3 m flat top)";
int nb=0, open_=0; double T=0,M=0;
foreach (var line in File.ReadAllLines(Path.Combine(dir,"stope_final_v5_geometry.txt"))) {
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
