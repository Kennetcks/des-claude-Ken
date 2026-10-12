// Write interrogation grades onto the 5 m blocks and colour them with the 5m blocks AU legend
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)

var inv=System.Globalization.CultureInfo.InvariantCulture;
string BASE="0\\06 SITE\\03 ENGINEERING\\DESIGN MASTER\\STOPES\\STOPE DESIGN\\5M BLOCKS MINE", LEG="5m blocks AU";
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
var lines=File.ReadAllLines(Path.Combine(dir,"interrogation_blocks_mine.csv"));
Func<string,List<string>> split = s => { var r=new List<string>(); var sb=new StringBuilder(); bool q=false; foreach (char ch in s){ if(ch=='"'){q=!q;continue;} if(ch==','&&!q){r.Add(sb.ToString());sb.Clear();continue;} sb.Append(ch);} r.Add(sb.ToString()); return r; };
var hdr=split(lines[0].TrimStart('﻿'));
Out.AppendLine("columns: "+string.Join("|",hdr.Take(40)));
int iID=hdr.IndexOf("INT_ID");
var map=new Dictionary<string,List<string>>(); for (int i=1;i<lines.Length;i++){ var c=split(lines[i]); if (c.Count>=hdr.Count) map[c[iID]]=c; }
Func<List<string>,string,double> num=(row,col)=>{ int k=hdr.IndexOf(col); double v=0; if(k>=0) double.TryParse(row[k],System.Globalization.NumberStyles.Float,inv,out v); return v; };
string tcol=hdr.Contains("Model Tons")?"Model Tons":(hdr.Contains("Model Tonnes")?"Model Tonnes":"Tonnes");
var lg=App.Legends.Find(LEG); Out.AppendLine("legend found="+(lg!=null)+" tonnes col="+tcol);
var bands=new List<Tuple<double,System.Drawing.Color>>();
foreach (Deswik.Common.Core.Legends.LegendItem it in lg) { double fr; double.TryParse(it.From,System.Globalization.NumberStyles.Float,inv,out fr); bands.Add(Tuple.Create(fr,it.PenColor)); }
string date=DateTime.Now.ToString("yyyy-MM-dd"); int upd=0, miss=0; double T=0,M=0,ET=0,EM=0;
var outRows=new List<string>{"name,source,lode,level,drive,block,ch_from,ch_to,height_above_back,thickness,int_tonnes,int_au,est_tonnes,est_au,in_plan_pct,void_volume"};
var bySrc=new Dictionary<string,double[]>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Deleted||f.Layer==null||!f.Layer.Name.StartsWith(BASE)||f.asPolyface==null) continue;
  var id=Convert.ToString(f.AttributeValueGet("INT_ID","")); List<string> row; if (!map.TryGetValue(id,out row)) { miss++; continue; }
  double t=num(row,tcol), au=num(row,"AU");
  f.AttributeValueSet("INT_TONNES",Math.Round(t,1)); f.AttributeValueSet("INT_AU",Math.Round(au,3)); f.AttributeValueSet("INT_VOLUME",Math.Round(num(row,"Volume"),1)); f.AttributeValueSet("INT_VOID_VOLUME",Math.Round(num(row,"Void Volume"),1)); f.AttributeValueSet("INT_DENSITY",Math.Round(num(row,"Density")>0?num(row,"Density"):num(row,"Model Density"),3));
  f.AttributeValueSet("INT_MODEL","BLOCK_MODEL"); f.AttributeValueSet("INT_DATE",date);
  System.Drawing.Color c=bands[0].Item2; foreach (var b in bands) if (au>=b.Item1) c=b.Item2; f.PenColor=new Deswik.Graphics.Objects.Color(c);
  double et=Convert.ToDouble(f.AttributeValueGet("EST_TONNES",0.0)), eg=Convert.ToDouble(f.AttributeValueGet("EST_AU",0.0));
  T+=t; M+=t*au; ET+=et; EM+=et*eg; upd++;
  var src=Convert.ToString(f.AttributeValueGet("SOURCE","")); double[] a; if(!bySrc.TryGetValue(src,out a)){a=new double[4];bySrc[src]=a;} a[0]++; a[1]+=t; a[2]+=t*au; if(au>2.6) a[3]+=t;
  outRows.Add(string.Join(",",new string[]{Convert.ToString(f.AttributeValueGet("Name","")),src,Convert.ToString(f.AttributeValueGet("LODE","")),Convert.ToString(f.AttributeValueGet("LEVEL","")),Convert.ToString(f.AttributeValueGet("DRIVE NUMBER","")),Convert.ToString(f.AttributeValueGet("BLOCK","")),
    Convert.ToDouble(f.AttributeValueGet("CH_FROM",0.0)).ToString("0.0",inv),Convert.ToDouble(f.AttributeValueGet("CH_TO",0.0)).ToString("0.0",inv),Convert.ToDouble(f.AttributeValueGet("Stope Height",0.0)).ToString("0.0",inv),Convert.ToDouble(f.AttributeValueGet("Stope Thickness",0.0)).ToString("0.00",inv),
    t.ToString("0",inv),au.ToString("0.000",inv),et.ToString("0",inv),eg.ToString("0.000",inv),Convert.ToString(f.AttributeValueGet("IN_PLAN_PCT","")),num(row,"Void Volume").ToString("0.0",inv)}));
}
foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (!l.Deleted && l.Name.StartsWith(BASE) && !l.Name.EndsWith("TRIAL ORE DRIVES")) App.Legends.ApplyToLayer(l.Name, LEG);
File.WriteAllLines(Path.Combine(dir,"blocks_5m_mine_grades.csv"),outRows.ToArray());
App.Redraw(true);
Out.AppendLine("updated "+upd+" missing "+miss+"  Deswik "+T.ToString("0")+" t @ "+(M/T).ToString("0.00")+"  python est "+ET.ToString("0")+" t @ "+(EM/ET).ToString("0.00"));
foreach (var kv in bySrc) Out.AppendLine("  "+kv.Key+": "+kv.Value[0]+" blocks "+kv.Value[1].ToString("0")+" t @ "+(kv.Value[2]/kv.Value[1]).ToString("0.00")+", above 2.6 g/t: "+kv.Value[3].ToString("0")+" t");
return null;
