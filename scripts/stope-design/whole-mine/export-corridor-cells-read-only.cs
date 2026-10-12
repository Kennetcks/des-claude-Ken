// Export block model cells around all lateral drives and ore away from drives (read-only, writes work files)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)

var inv=System.Globalization.CultureInfo.InvariantCulture;
var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
if (File.Exists(Path.Combine(dir,"export_done.txt"))) File.Delete(Path.Combine(dir,"export_done.txt"));
double OX=7770,OY=62610,OZ=5930,PX=4,PY=10,PZ=10; int NX=213,NY=120,NZ=54;
var mask=new bool[NX*NY*NZ];
// corridor around every lateral non-decline floor string
var segs=new List<double[]>();
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Deleted||f.Layer==null||f.Layer.Name!="DESIGN\\FLOOR STRINGS"||f.asPolyline==null) continue;
  if (Convert.ToString(f.AttributeValueGet("LAT_VERT",""))!="LAT"||Convert.ToString(f.AttributeValueGet("ACT_TYPE",""))=="DEC") continue;
  var pts=f.asPolyline.VertexList.Cast<object>().Select(o=>(Deswik.Graphics.Geometry.Point)o).ToList();
  for (int i=0;i<pts.Count-1;i++) segs.Add(new double[]{pts[i].x,pts[i].y,pts[i].z,pts[i+1].x,pts[i+1].y,pts[i+1].z});
}
double R=12+Math.Sqrt(2*2+5*5);
foreach (var s in segs) {
  int i0=Math.Max(0,(int)((Math.Min(s[0],s[3])-R-OX)/PX)), i1=Math.Min(NX-1,(int)((Math.Max(s[0],s[3])+R-OX)/PX));
  int j0=Math.Max(0,(int)((Math.Min(s[1],s[4])-R-OY)/PY)), j1=Math.Min(NY-1,(int)((Math.Max(s[1],s[4])+R-OY)/PY));
  int k0=Math.Max(0,(int)((Math.Min(s[2],s[5])-2-OZ)/PZ)), k1=Math.Min(NZ-1,(int)((Math.Max(s[2],s[5])+28-OZ)/PZ));
  double dx=s[3]-s[0], dy=s[4]-s[1], L2=dx*dx+dy*dy;
  for (int i=i0;i<=i1;i++) for (int j=j0;j<=j1;j++) {
    double cx=OX+(i+0.5)*PX, cy=OY+(j+0.5)*PY; double t=L2>0?Math.Max(0,Math.Min(1,((cx-s[0])*dx+(cy-s[1])*dy)/L2)):0;
    double ex=s[0]+t*dx-cx, ey=s[1]+t*dy-cy; if (ex*ex+ey*ey>R*R) continue;
    for (int k=k0;k<=k1;k++) mask[k+NZ*(j+NY*i)]=true; }
}
int mc=0; foreach (var b in mask) if (b) mc++; Out.AppendLine("segments "+segs.Count+" parent cells in corridor "+mc);
string path=@"C:\path\to\BLOCK_MODEL.gmdlb";
Type mT=null; foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()){ var t=asm.GetType("Deswik.BlockModel.ManagedGeoModels.BlockModel"); if(t!=null) mT=t; }
object m=Activator.CreateInstance(mT,new object[]{path}); var load=mT.GetMethod("Load"); var st=load.GetParameters()[0].ParameterType;
load.Invoke(m,new object[]{Enum.Parse(st,"OpenForReadNotIndexed"),System.Threading.CancellationToken.None});
object bm=mT.GetProperty("BlockGeoModel").GetValue(m,null); var bmT=bm.GetType();
var gd=(Func<string,long,double>)Delegate.CreateDelegate(typeof(Func<string,long,double>),bm,bmT.GetMethod("GetDoubleValue",new Type[]{typeof(string),typeof(long)}));
var gl=(Func<string,long,long>)Delegate.CreateDelegate(typeof(Func<string,long,long>),bm,bmT.GetMethod("GetLongValue",new Type[]{typeof(string),typeof(long)}));
var gv=bmT.GetMethod("GetValue",new Type[]{typeof(string),typeof(long)});
long N=(long)bmT.GetProperty("RecordCount").GetValue(bm,null);
long nc=0, no=0; var sw=System.Diagnostics.Stopwatch.StartNew();
using (var wc=new BinaryWriter(File.Create(Path.Combine(dir,"corridor_blocks.f32"))))
using (var wo=new BinaryWriter(File.Create(Path.Combine(dir,"ore_outside.f32")))) {
  for (long r=0;r<N;r++) {
    long ijk=gl("_IJK",r);
    if (ijk>=0 && ijk<mask.Length && mask[ijk]) {
      double mined=0; try { mined=Convert.ToDouble(gv.Invoke(bm,new object[]{"MINED",r}),inv); } catch {}
      wc.Write((float)(gd("_XC",r)-OX)); wc.Write((float)(gd("_YC",r)-OY)); wc.Write((float)(gd("_ZC",r)-OZ));
      wc.Write((float)gd("_XSIZE",r)); wc.Write((float)gd("_YSIZE",r)); wc.Write((float)gd("_ZSIZE",r));
      wc.Write((float)gd("AU",r)); wc.Write((float)gd("DENSITY",r)); wc.Write((float)mined); nc++;
    } else {
      double au=gd("AU",r); if (au>=2.5) {
        wo.Write((float)(gd("_XC",r)-OX)); wo.Write((float)(gd("_YC",r)-OY)); wo.Write((float)(gd("_ZC",r)-OZ));
        wo.Write((float)gd("_XSIZE",r)); wo.Write((float)gd("_YSIZE",r)); wo.Write((float)gd("_ZSIZE",r));
        wo.Write((float)au); wo.Write((float)gd("DENSITY",r)); wo.Write(0f); no++; }
    }
  }
}
((IDisposable)m).Dispose();
File.WriteAllText(Path.Combine(dir,"export_done.txt"),"corridor "+nc+" ore_outside "+no+" seconds "+sw.Elapsed.TotalSeconds.ToString("0"));
Out.AppendLine("corridor "+nc+" ore_outside "+no+" s="+sw.Elapsed.TotalSeconds.ToString("0"));
return null;
