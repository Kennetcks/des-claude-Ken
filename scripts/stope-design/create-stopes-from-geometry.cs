// Create stope solids from design_stopes.py output and tag them like the existing LH_Stope task solids
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 
// Paste the stV/stF lines from geom.cs below. Each stope: one ring of 2*NL wall points per section (FW bottom->top, HW top->bottom), triangles with 1-based indices.

string LAY="0\\06 SITE\\03 ENGINEERING\\DESIGN MASTER\\STOPES\\STOPE DESIGN";
var stV=new List<double[]>(); var stF=new List<int[]>();
// stV.Add(new double[]{...}); stF.Add(new int[]{...});   <- from geom.cs
string[] nm={"G410 S100 -01","G410 S100 -02"}; double[] len={17,40}; double[] thk={2.2,2.3}; string LEVEL="410", DRIVE="100", DIR="S";
Deswik.Graphics.Primaries.Layer ly=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if(l.Name==LAY) ly=l;
var at=ly.Attributes; var have=new HashSet<string>(); foreach (var a in (System.Collections.IEnumerable)at) have.Add(Convert.ToString(a.GetType().GetProperty("Name").GetValue(a,null)));
var addM=at.GetType().GetMethods().First(m=>m.Name=="Add"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string)); var aType=addM.GetParameters()[1].ParameterType;
string[] sF={"Name","ACT_TYPE","STO_DEV","LAT_VERT","LEVEL","DRIVE NUMBER","DIRECTION","STOPE TYPE","DESIGN","Description"}; string[] dF={"Stope Height","Stope Length","Stope Thickness"};
foreach (var n in sF) if(!have.Contains(n)) addM.Invoke(at,new object[]{n,Enum.Parse(aType,"String"),""});
foreach (var n in dF) if(!have.Contains(n)) addM.Invoke(at,new object[]{n,Enum.Parse(aType,"Double"),0.0});
for (int s=0;s<stV.Count;s++){ var pf=new Deswik.Graphics.Figures.Polyface(); var V=stV[s]; for(int i=0;i<V.Length;i+=3) pf.AddVertex(V[i],V[i+1],V[i+2]); var F=stF[s]; for(int i=0;i<F.Length;i+=3) pf.AddTriangle(F[i],F[i+1],F[i+2]);
  pf.Layer=ly; App.ModelSpace.Entities.AddItem(pf,null,null,true,false);
  pf.AttributeValueSet("Name",nm[s]); pf.AttributeValueSet("ACT_TYPE","LH_Stope"); pf.AttributeValueSet("STO_DEV","STO"); pf.AttributeValueSet("LAT_VERT","VERT"); pf.AttributeValueSet("LEVEL",LEVEL); pf.AttributeValueSet("DRIVE NUMBER",DRIVE); pf.AttributeValueSet("DIRECTION",DIR); pf.AttributeValueSet("STOPE TYPE","OPEN"); pf.AttributeValueSet("Description","LH Stope");
  pf.AttributeValueSet("DESIGN","Concept from block model, base inside the ore drive, to be cut");
  pf.AttributeValueSet("Stope Height",20.0); pf.AttributeValueSet("Stope Length",len[s]); pf.AttributeValueSet("Stope Thickness",thk[s]);
  Out.AppendLine(nm[s]+": verts "+pf.VertexList.Count+" closed "+pf.IsClosed+" volume "+pf.Volume.ToString("0.0")); }
App.Redraw(true); return null;
