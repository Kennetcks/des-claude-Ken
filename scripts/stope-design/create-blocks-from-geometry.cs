// Create 5 m blocks along a drive from blocks_5m_geometry.txt (blocks_along_drive.py) on STOPE DESIGN\5M BLOCKS, tagged like LH_Stope task solids
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

string LAY="0\\06 SITE\\03 ENGINEERING\\DESIGN MASTER\\STOPES\\STOPE DESIGN\\5M BLOCKS";
string src=@"C:\Users\User\AppData\Roaming\Kennet.DeswikClaude\work\stopes298\blocks_5m_geometry.txt";
string PREFIX="G410 S100 B", LEVEL="410", DRIVE="100";
string DESIGN="5 m block along G410 OD S100 (INT_ID 298), walls follow >=2.5 g/t lode, base 1 m inside OD";
var inv=System.Globalization.CultureInfo.InvariantCulture;
var lines=File.ReadAllLines(src).Where(x=>x.Trim()!="").ToArray();
Deswik.Graphics.Primaries.Layer ly=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if(l.Name==LAY) ly=l;
if (ly==null) { App.Layers.Add(LAY,false,1); foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if(l.Name==LAY) ly=l; }
int existing=0; foreach (var e in ly.Entities){ var f=e as Deswik.Graphics.Primaries.Figure; if(f!=null&&!f.Deleted) existing++; } if(existing>0){ Out.AppendLine("layer already has "+existing+" solids, stopping"); return null; }
// layer fields first, or the user can't see the attributes
var at=ly.Attributes; var have=new HashSet<string>(); foreach (var a in (System.Collections.IEnumerable)at) have.Add(Convert.ToString(a.GetType().GetProperty("Name").GetValue(a,null)));
var addM=at.GetType().GetMethods().First(m=>m.Name=="Add"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string)); var aType=addM.GetParameters()[1].ParameterType;
foreach (var n in new[]{"Name","ACT_TYPE","STO_DEV","LAT_VERT","LEVEL","DRIVE NUMBER","BLOCK","DESIGN","INT_ID","INT_MODEL","INT_DATE"}) if(!have.Contains(n)) addM.Invoke(at,new object[]{n,Enum.Parse(aType,"String"),""});
foreach (var n in new[]{"CH_FROM","CH_TO","Stope Height","Stope Length","Stope Thickness","INT_TONNES","INT_AU","INT_VOLUME","INT_VOID_VOLUME","INT_DENSITY"}) if(!have.Contains(n)) addM.Invoke(at,new object[]{n,Enum.Parse(aType,"Double"),0.0});
int s=0, bad=0; double vol=0;
foreach (var line in lines){ s++; var parts=line.Split('|'); var meta=parts[0].Split(',').Select(x=>double.Parse(x,inv)).ToArray(); var V=parts[1].Split(',').Select(x=>double.Parse(x,inv)).ToArray(); var F=parts[2].Split(',').Select(x=>int.Parse(x)).ToArray();
  var pf=new Deswik.Graphics.Figures.Polyface(); for(int i=0;i<V.Length;i+=3) pf.AddVertex(V[i],V[i+1],V[i+2]); for(int i=0;i<F.Length;i+=3) pf.AddTriangle(F[i],F[i+1],F[i+2]);
  pf.Layer=ly; App.ModelSpace.Entities.AddItem(pf,null,null,true,false); string b=s.ToString("00");
  pf.AttributeValueSet("Name",PREFIX+b); pf.AttributeValueSet("BLOCK",b); pf.AttributeValueSet("ACT_TYPE","LH_Stope"); pf.AttributeValueSet("STO_DEV","STO"); pf.AttributeValueSet("LAT_VERT","VERT"); pf.AttributeValueSet("LEVEL",LEVEL); pf.AttributeValueSet("DRIVE NUMBER",DRIVE);
  pf.AttributeValueSet("DESIGN",DESIGN);
  pf.AttributeValueSet("CH_FROM",meta[0]); pf.AttributeValueSet("CH_TO",meta[1]); pf.AttributeValueSet("Stope Height",20.0); pf.AttributeValueSet("Stope Length",meta[1]-meta[0]); pf.AttributeValueSet("Stope Thickness",meta[4]);
  if(!pf.IsClosed) bad++; vol+=pf.Volume; }
Out.AppendLine("blocks created: "+s+" not closed: "+bad+" total volume "+vol.ToString("0.0")+" on "+LAY);
return null;
