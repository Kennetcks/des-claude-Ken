// v8: write interrogation grades onto the 5 m blocks v8 and colour them with Kennet's '5m blocks AU v2' legend.
// The legend is Kennet's own (he edited it): read it, never rebuild it. First matching item wins (From <= AU < To).
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)

var inv=System.Globalization.CultureInfo.InvariantCulture;
string BASE="0\\06 SITE\\03 ENGINEERING\\DESIGN MASTER\\STOPES\\STOPE DESIGN\\5M BLOCKS MINE V8", LEG="5m blocks AU v2";
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
var lines=File.ReadAllLines(Path.Combine(dir,"interrogation_blocks_mine_v8.csv"));
Func<string,List<string>> split = s => { var r=new List<string>(); var sb=new StringBuilder(); bool q=false; foreach (char ch in s){ if(ch=='"'){q=!q;continue;} if(ch==','&&!q){r.Add(sb.ToString());sb.Clear();continue;} sb.Append(ch);} r.Add(sb.ToString()); return r; };
var hdr=split(lines[0].TrimStart('﻿'));
Out.AppendLine("columns: "+string.Join("|",hdr.Take(40)));
int iID=hdr.IndexOf("INT_ID");
var map=new Dictionary<string,List<string>>(); for (int i=1;i<lines.Length;i++){ var c=split(lines[i]); if (c.Count>=hdr.Count) map[c[iID]]=c; }
Func<List<string>,string,double> num=(row,col)=>{ int k=hdr.IndexOf(col); double v=0; if(k>=0) double.TryParse(row[k],System.Globalization.NumberStyles.Float,inv,out v); return v; };
string tcol=hdr.Contains("Model Tons")?"Model Tons":(hdr.Contains("Model Tonnes")?"Model Tonnes":"Tonnes");
var lg=App.Legends.Find(LEG);
Out.AppendLine("legend "+LEG+" items="+lg.Count+" tonnes col="+tcol);
var bands=new List<Tuple<double,double,System.Drawing.Color>>();
foreach (Deswik.Common.Core.Legends.LegendItem it in lg) { double fr,to; double.TryParse(it.From,System.Globalization.NumberStyles.Float,inv,out fr); if(!double.TryParse(it.To,System.Globalization.NumberStyles.Float,inv,out to)) to=1e9; bands.Add(Tuple.Create(fr,to,it.PenColor)); }
string date=DateTime.Now.ToString("yyyy-MM-dd"); int upd=0, miss=0; double T=0,M=0,ET=0,EM=0;
var outRows=new List<string>{"name,source,lode,level,drive,block,split,breakthrough,ch_from,ch_to,stope_height,base_above_floor,thickness,int_tonnes,int_au,est_tonnes,est_au,in_plan_pct,void_volume"};
var bySrc=new Dictionary<string,double[]>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Deleted||f.Layer==null||!f.Layer.Name.StartsWith(BASE)||f.asPolyface==null) continue;
  var id=Convert.ToString(f.AttributeValueGet("INT_ID","")); List<string> row;
  if (map.TryGetValue(id,out row)) {
    double t0=num(row,tcol), au0=num(row,"AU");
    f.AttributeValueSet("INT_TONNES",Math.Round(t0,1)); f.AttributeValueSet("INT_AU",Math.Round(au0,3)); f.AttributeValueSet("INT_VOLUME",Math.Round(num(row,"Volume"),1)); f.AttributeValueSet("INT_VOID_VOLUME",Math.Round(num(row,"Void Volume"),1)); f.AttributeValueSet("INT_DENSITY",Math.Round(num(row,"Density")>0?num(row,"Density"):num(row,"Model Density"),3));
    f.AttributeValueSet("INT_MODEL","BLOCK_MODEL"); f.AttributeValueSet("INT_DATE",date); upd++;
  }
  if (Convert.ToString(f.AttributeValueGet("INT_DATE",""))=="") { miss++; continue; }
  // every interrogated block (this run or an earlier one) is coloured and listed from its attributes
  double t=Convert.ToDouble(f.AttributeValueGet("INT_TONNES",0.0)), au=Convert.ToDouble(f.AttributeValueGet("INT_AU",0.0));
  System.Drawing.Color c=bands[0].Item3; foreach (var b in bands) if (au>=b.Item1&&au<b.Item2) { c=b.Item3; break; } f.PenColor=new Deswik.Graphics.Objects.Color(c);
  double et=Convert.ToDouble(f.AttributeValueGet("EST_TONNES",0.0)), eg=Convert.ToDouble(f.AttributeValueGet("EST_AU",0.0));
  T+=t; M+=t*au; ET+=et; EM+=et*eg;
  var src=Convert.ToString(f.AttributeValueGet("SOURCE","")); string grp=Convert.ToString(f.AttributeValueGet("DRIVE NUMBER","")).StartsWith("T")?"NEW TRIAL DRIVES (v7)":src; double[] a; if(!bySrc.TryGetValue(grp,out a)){a=new double[5];bySrc[grp]=a;} a[0]++; a[1]+=t; a[2]+=t*au; if(au>2.6) a[3]+=t; if(au<2) a[4]++;
  outRows.Add(string.Join(",",new string[]{Convert.ToString(f.AttributeValueGet("Name","")),src,Convert.ToString(f.AttributeValueGet("LODE","")),Convert.ToString(f.AttributeValueGet("LEVEL","")),Convert.ToString(f.AttributeValueGet("DRIVE NUMBER","")),Convert.ToString(f.AttributeValueGet("BLOCK","")),Convert.ToString(f.AttributeValueGet("SPLIT","")),Convert.ToString(f.AttributeValueGet("BREAKTHROUGH","")),
    Convert.ToDouble(f.AttributeValueGet("CH_FROM",0.0)).ToString("0.0",inv),Convert.ToDouble(f.AttributeValueGet("CH_TO",0.0)).ToString("0.0",inv),Convert.ToDouble(f.AttributeValueGet("Stope Height",0.0)).ToString("0.0",inv),Convert.ToDouble(f.AttributeValueGet("BASE_ABOVE_FLOOR",0.0)).ToString("0.0",inv),Convert.ToDouble(f.AttributeValueGet("Stope Thickness",0.0)).ToString("0.00",inv),
    t.ToString("0",inv),au.ToString("0.000",inv),et.ToString("0",inv),eg.ToString("0.000",inv),Convert.ToString(f.AttributeValueGet("IN_PLAN_PCT","")),Convert.ToDouble(f.AttributeValueGet("INT_VOID_VOLUME",0.0)).ToString("0.0",inv)}));
}
foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if (!l.Deleted && l.Name.StartsWith(BASE) && !l.Name.EndsWith("TRIAL ORE DRIVES")) App.Legends.ApplyToLayer(l.Name, LEG);
File.WriteAllLines(Path.Combine(dir,"blocks_5m_mine_v8_grades.csv"),outRows.ToArray());
App.Redraw(true);
Out.AppendLine("updated "+upd+" blocks still not interrogated "+miss+" ; all blocks  Deswik "+T.ToString("0")+" t @ "+(M/T).ToString("0.00")+"  python est "+ET.ToString("0")+" t @ "+(EM/ET).ToString("0.00"));
foreach (var kv in bySrc) Out.AppendLine("  "+kv.Key+": "+kv.Value[0]+" blocks "+kv.Value[1].ToString("0")+" t @ "+(kv.Value[2]/kv.Value[1]).ToString("0.00")+", above 2.6 g/t: "+kv.Value[3].ToString("0")+" t, below 2 g/t blocks: "+kv.Value[4]);
return null;
