// Study existing stopes: sizes, levels and which drive sits under each (read-only)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)

var inv=System.Globalization.CultureInfo.InvariantCulture;
var stopes=new List<Deswik.Graphics.Primaries.Figure>(); var dev=new List<Deswik.Graphics.Primaries.Figure>();
var other=new Dictionary<string,int>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Deleted || f.Layer==null || f.Layer.Name!="SCHEDULER\\TASK SOLIDS" || f.asPolyface==null) continue;
  var sd=Convert.ToString(f.AttributeValueGet("STO_DEV","")); var at=Convert.ToString(f.AttributeValueGet("ACT_TYPE",""));
  if (sd=="STO") { stopes.Add(f); int c; other.TryGetValue(at,out c); other[at]=c+1; }
  else if (sd=="DEV" && Convert.ToString(f.AttributeValueGet("LAT_VERT",""))=="LAT") dev.Add(f);
}
Out.AppendLine("stope solids "+stopes.Count+" by ACT_TYPE: "+string.Join(", ",other.Select(k=>k.Key+"="+k.Value)));
Func<Deswik.Graphics.Primaries.Figure,double[]> bb=(f)=>{ double[] b={1e18,1e18,1e18,-1e18,-1e18,-1e18}; foreach (var p in f.asPolyface.VertexList){ b[0]=Math.Min(b[0],p.x);b[1]=Math.Min(b[1],p.y);b[2]=Math.Min(b[2],p.z);b[3]=Math.Max(b[3],p.x);b[4]=Math.Max(b[4],p.y);b[5]=Math.Max(b[5],p.z);} return b; };
var devB=dev.Select(d=>bb(d)).ToList();
var sb=new StringBuilder("name,level,act,h_attr,len_attr,thk_attr,vol,minx,miny,minz,maxx,maxy,maxz,below_act,below_gap,below_parent,above_act,above_gap\n");
var belowCount=new Dictionary<string,int>(); var heights=new List<double>();
foreach (var s in stopes) {
  var b=bb(s); double cx=0,cy=0; int n=0; foreach (var p in s.asPolyface.VertexList) if (p.z<b[2]+1.0){cx+=p.x;cy+=p.y;n++;} cx/=n; cy/=n;
  double tx=0,ty=0; int m=0; foreach (var p in s.asPolyface.VertexList) if (p.z>b[5]-1.0){tx+=p.x;ty+=p.y;m++;} tx/=m; ty/=m;
  string ba="NONE", bp=""; double bg=99; string aa="NONE"; double ag=99;
  for (int i=0;i<dev.Count;i++){ var d=devB[i];
    if (cx>=d[0]-1&&cx<=d[3]+1&&cy>=d[1]-1&&cy<=d[4]+1){ double g=b[2]-d[5]; if (g>-6&&g<6&&Math.Abs(g)<Math.Abs(bg)){bg=g;ba=Convert.ToString(dev[i].AttributeValueGet("ACT_TYPE",""));bp=Convert.ToString(dev[i].AttributeValueGet("DIS Parent ID",""));} }
    if (tx>=d[0]-1&&tx<=d[3]+1&&ty>=d[1]-1&&ty<=d[4]+1){ double g=d[2]-b[5]; if (g>-6&&g<6&&Math.Abs(g)<Math.Abs(ag)){ag=g;aa=Convert.ToString(dev[i].AttributeValueGet("ACT_TYPE",""));} } }
  int c; belowCount.TryGetValue(ba,out c); belowCount[ba]=c+1; heights.Add(b[5]-b[2]);
  sb.AppendLine(string.Join(",",new string[]{Convert.ToString(s.AttributeValueGet("Name","")).Replace(","," "),Convert.ToString(s.AttributeValueGet("LEVEL","")),Convert.ToString(s.AttributeValueGet("ACT_TYPE","")),Convert.ToString(s.AttributeValueGet("Stope Height","")),Convert.ToString(s.AttributeValueGet("Stope Length","")),Convert.ToString(s.AttributeValueGet("Stope Thickness","")),Math.Abs(s.asPolyface.Volume).ToString("0",inv),b[0].ToString("0.0",inv),b[1].ToString("0.0",inv),b[2].ToString("0.0",inv),b[3].ToString("0.0",inv),b[4].ToString("0.0",inv),b[5].ToString("0.0",inv),ba,bg.ToString("0.0",inv),bp,aa,ag.ToString("0.0",inv)}));
}
var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine"); Directory.CreateDirectory(dir);
File.WriteAllText(Path.Combine(dir,"existing_stopes.csv"),sb.ToString());
Out.AppendLine("drive type under stope base: "+string.Join(", ",belowCount.OrderByDescending(k=>k.Value).Select(k=>k.Key+"="+k.Value)));
heights.Sort(); Out.AppendLine("bbox height min/med/max "+heights[0].ToString("0.0")+"/"+heights[heights.Count/2].ToString("0.0")+"/"+heights[heights.Count-1].ToString("0.0"));
Out.AppendLine("written "+dir);
return null;
