// Re-run the full string review after rebuild (read only)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

var S=new List<List<double[]>>(); var H=new List<string>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Layer==null || f.Layer.Name!="DESIGN\\FLOOR STRINGS" || f.asPolyline==null) continue;
  var p=new List<double[]>(); foreach (Deswik.Graphics.Geometry.Point q in f.asPolyline.VertexList) p.Add(new double[]{q.x,q.y,q.z}); S.Add(p); H.Add(f.Handle);
}
var lat=new List<Deswik.Graphics.Primaries.Figure>(); var lbb=new List<double[]>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Layer==null||f.Layer.Name!="SCHEDULER\\TASK SOLIDS"||f.asPolyface==null) continue;
  if (Convert.ToString(f.AttributeValueGet("STO_DEV",""))!="DEV"||Convert.ToString(f.AttributeValueGet("LAT_VERT",""))!="LAT") continue;
  double a=1e18,b=-1e18,c=1e18,d=-1e18; foreach (Deswik.Graphics.Geometry.Point v in f.asPolyface.VertexList){a=Math.Min(a,v.x);b=Math.Max(b,v.x);c=Math.Min(c,v.y);d=Math.Max(d,v.y);} lat.Add(f); lbb.Add(new double[]{a,b,c,d});
}
double G=10; var grid=new Dictionary<string,List<int>>();
for (int i=0;i<lat.Count;i++) for (int gx=(int)Math.Floor(lbb[i][0]/G); gx<=(int)Math.Floor(lbb[i][1]/G); gx++) for (int gy=(int)Math.Floor(lbb[i][2]/G); gy<=(int)Math.Floor(lbb[i][3]/G); gy++) { string k=gx+","+gy; List<int> l; if(!grid.TryGetValue(k,out l)){l=new List<int>();grid[k]=l;} l.Add(i);} 
Func<double,double,double,double> drop = (x,y,z0) => {
  List<int> cl; if (!grid.TryGetValue(Math.Floor(x/G)+","+Math.Floor(y/G),out cl)) return double.NaN;
  var hits=new List<double[]>();
  foreach (int s in cl){ if (x<lbb[s][0]||x>lbb[s][1]||y<lbb[s][2]||y>lbb[s][3]) continue; double lo=double.MaxValue, hi=double.MaxValue;
    try{ lo=lat[s].asPolyface.GetMinZ(x,y,lat[s].BoundingBox); hi=lat[s].asPolyface.GetMaxZ(x,y,lat[s].BoundingBox);}catch{} if (lo>1e9||hi>1e9) continue; if (Math.Abs(hi-lo)<0.2) continue; hits.Add(new double[]{Math.Min(lo,hi),Math.Max(lo,hi)}); }
  if (hits.Count==0) return double.NaN;
  double[] st=null; double bd=double.MaxValue;
  foreach (var h in hits){ double d=(z0>=h[0]-0.3&&z0<=h[1]+0.3)?0:Math.Min(Math.Abs(z0-h[0]),Math.Abs(z0-h[1])); if(d<bd){bd=d;st=h;} }
  if (bd>4) return double.NaN;
  double cur=st[0]; bool ch=true; while(ch){ ch=false; foreach (var h in hits) if (h[1]>=cur-0.3&&h[0]<cur-0.01){cur=h[0];ch=true;} }
  return cur; };
Func<double[],double[],double[],double> turn = (a,b,c) => { double ax=b[0]-a[0], ay=b[1]-a[1], bx=c[0]-b[0], by=c[1]-b[1]; double la=Math.Sqrt(ax*ax+ay*ay), lb=Math.Sqrt(bx*bx+by*by); if(la<0.05||lb<0.05) return 0; return Math.Acos(Math.Max(-1,Math.Min(1,(ax*bx+ay*by)/(la*lb))))*180/Math.PI; };
var cats=new Dictionary<string,List<string>>(); Action<string,string> add=(k,v)=>{ List<string> l; if(!cats.TryGetValue(k,out l)){l=new List<string>();cats[k]=l;} l.Add(v); };
int latCount=0, rCount=0;
for (int c=0;c<S.Count;c++){ var p=S[c]; string h=H[c];
  double hz=0,vz=0; for(int k=1;k<p.Count;k++){hz+=Math.Sqrt((p[k][0]-p[k-1][0])*(p[k][0]-p[k-1][0])+(p[k][1]-p[k-1][1])*(p[k][1]-p[k-1][1]));vz+=Math.Abs(p[k][2]-p[k-1][2]);}
  bool raise = vz>hz; if (raise) { rCount++; continue; } latCount++;
  for(int k=1;k<p.Count;k++){ double d=Math.Sqrt((p[k][0]-p[k-1][0])*(p[k][0]-p[k-1][0])+(p[k][1]-p[k-1][1])*(p[k][1]-p[k-1][1])); if (d<0.1) add("near-duplicate points",h+" v"+k); if (d>8) add("long segment >8 m",h+" v"+k+string.Format(" {0:0.0} m",d)); }
  for(int k=1;k+1<p.Count;k++){ double t=turn(p[k-1],p[k],p[k+1]);
    bool endK = (k==1 && p.Count>=4 && turn(p[1],p[2],p[3])<5) || (k==p.Count-2 && p.Count>=4 && turn(p[k-2],p[k-1],p[k])<5);
    if (t>10 && endK) add("end bends off the drive line",h+string.Format(" v{0} {1:0} deg",k,t));
    else if (t>120) add("doubles back",h+string.Format(" v{0} {1:0} deg",k,t));
    else if (t>45) add("sharp turn 45-120 deg",h+string.Format(" v{0} {1:0} deg",k,t)); }
  for(int k=1;k<p.Count;k++){ double hh=Math.Sqrt((p[k][0]-p[k-1][0])*(p[k][0]-p[k-1][0])+(p[k][1]-p[k-1][1])*(p[k][1]-p[k-1][1])); double dz=p[k][2]-p[k-1][2]; if (hh>0.3 && Math.Abs(dz)/hh>0.25 && Math.Abs(dz)>0.3) add("grade steeper than 1:4",h+string.Format(" v{0} {1:0}%",k,100*dz/hh)); }
  for(int k=1;k+1<p.Count;k++){ var a=p[k-1]; var b=p[k+1]; var m=p[k]; double la=Math.Sqrt((m[0]-a[0])*(m[0]-a[0])+(m[1]-a[1])*(m[1]-a[1])), lb=Math.Sqrt((b[0]-m[0])*(b[0]-m[0])+(b[1]-m[1])*(b[1]-m[1])); if (la+lb<1e-6) continue; double zi=a[2]+(b[2]-a[2])*la/(la+lb); if (Math.Abs(m[2]-zi)>0.5) add("bump/dip >0.5 m",h+string.Format(" v{0} {1:0.00} m",k,m[2]-zi)); }
  for(int k=0;k<p.Count;k++){ var nb = k==0?p[1]:p[k-1]; double dx=nb[0]-p[k][0], dy=nb[1]-p[k][1]; double L=Math.Sqrt(dx*dx+dy*dy); double z=double.NaN;
    foreach (double ins in new double[]{0.3,0}) { if (ins>0 && L<0.6) continue; double xx=p[k][0]+(L>0?dx/L*ins:0), yy=p[k][1]+(L>0?dy/L*ins:0); z=drop(xx,yy,p[k][2]); if(!double.IsNaN(z)) break; }
    if (double.IsNaN(z)) add("point outside lateral solids",h+" v"+k);
    else if (Math.Abs(z-p[k][2])>0.3) add("point off the floor >0.3 m",h+string.Format(" v{0} {1:0.00} m",k,p[k][2]-z)); }
}
Out.AppendLine("Strings: "+latCount+" lateral, "+rCount+" raise");
if (cats.Count==0) Out.AppendLine("No issues found");
foreach (var kv in cats.OrderByDescending(k=>k.Value.Count)) { Out.AppendLine(kv.Key+": "+kv.Value.Count); foreach (var s in kv.Value.Take(6)) Out.AppendLine("    "+s); }
return null;