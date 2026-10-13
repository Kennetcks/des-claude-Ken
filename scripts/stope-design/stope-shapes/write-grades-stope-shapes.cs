// Stope shapes: write interrogation grades (INT_*) onto the stope shapes, colour them with Kennet's '5m blocks AU v2' legend (applied, never edited), export stope_shapes_grades.csv
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
var inv=System.Globalization.CultureInfo.InvariantCulture;
string LAY="DESIGN\\CREATING PLILLAR/STOPES\\5M BLOCKS\\STOPE SHAPES V2", LEG="5m blocks AU v2";
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
var lines=File.ReadAllLines(Path.Combine(dir,"interrogation_stope_shapes.csv"));
Func<string,List<string>> split = s => { var r=new List<string>(); var sb=new StringBuilder(); bool q=false; foreach (char ch in s){ if(ch=='"'){q=!q;continue;} if(ch==','&&!q){r.Add(sb.ToString());sb.Clear();continue;} sb.Append(ch);} r.Add(sb.ToString()); return r; };
var hdr=split(lines[0].TrimStart('﻿')); int iID=hdr.IndexOf("INT_ID");
var map=new Dictionary<string,List<string>>(); for (int i=1;i<lines.Length;i++){ var c=split(lines[i]); if (c.Count>=hdr.Count) map[c[iID]]=c; }
Func<List<string>,string,double> num=(row,col)=>{ int k=hdr.IndexOf(col); double v=0; if(k>=0) double.TryParse(row[k],System.Globalization.NumberStyles.Float,inv,out v); return v; };
string tcol=hdr.Contains("Model Tons")?"Model Tons":(hdr.Contains("Model Tonnes")?"Model Tonnes":"Tonnes");
string date=DateTime.Now.ToString("yyyy-MM-dd"); int upd=0; double T=0,M=0;
var outRows=new List<string>{"name,group,level,drive,source,ch_from,ch_to,length,height,chamfer_start_deg,chamfer_end_deg,int_tonnes,int_au,est_tonnes,est_au,blocks"};
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Deleted||f.Layer==null||f.Layer.Name!=LAY||f.asPolyface==null) continue;
  List<string> row; if(!map.TryGetValue(Convert.ToString(f.AttributeValueGet("INT_ID","")),out row)) continue;
  double t=Math.Round(num(row,tcol),1), au=Math.Round(num(row,"AU"),3);
  f.AttributeValueSet("INT_TONNES",t); f.AttributeValueSet("INT_AU",au); f.AttributeValueSet("INT_VOLUME",Math.Round(num(row,"Volume"),1)); f.AttributeValueSet("INT_VOID_VOLUME",Math.Round(num(row,"Void Volume"),1)); f.AttributeValueSet("INT_DENSITY",Math.Round(num(row,"Density")>0?num(row,"Density"):num(row,"Model Density"),3));
  f.AttributeValueSet("INT_MODEL","BLOCK_MODEL"); f.AttributeValueSet("INT_DATE",date); upd++; T+=t; M+=t*au;
  Func<string,string> S=(k)=>Convert.ToString(f.AttributeValueGet(k,""),inv);
  outRows.Add(string.Join(",",new string[]{S("Name"),S("GROUP"),S("LEVEL"),S("DRIVE NUMBER"),S("SOURCE"),S("CH_FROM"),S("CH_TO"),S("Stope Length"),S("Stope Height"),S("CHAMFER_START_DEG"),S("CHAMFER_END_DEG"),t.ToString("0",inv),au.ToString("0.000",inv),S("EST_TONNES"),S("EST_AU"),S("BLOCKS")}));
}
App.Legends.ApplyToLayer(LAY, LEG);
File.WriteAllLines(Path.Combine(dir,"stope_shapes_grades.csv"),outRows.ToArray());
App.Redraw(true);
Out.AppendLine("updated "+upd+"; "+T.ToString("0")+" t @ "+(M/T).ToString("0.00"));
return null;
