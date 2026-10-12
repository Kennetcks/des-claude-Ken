// Export floor strings and existing stopes, test block model read speed (read-only)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)

var inv=System.Globalization.CultureInfo.InvariantCulture;
var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine"); Directory.CreateDirectory(dir);
var sb=new StringBuilder("pid,act,latvert,ph,x,y,z\n"); int ns=0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Deleted||f.Layer==null||f.Layer.Name!="DESIGN\\FLOOR STRINGS"||f.asPolyline==null) continue; ns++;
  string pid=Convert.ToString(f.AttributeValueGet("DIS Parent ID","")), act=Convert.ToString(f.AttributeValueGet("ACT_TYPE","")), lv=Convert.ToString(f.AttributeValueGet("LAT_VERT","")), ph=Convert.ToString(f.AttributeValueGet("PROFILE_H",""));
  foreach (var p in f.asPolyline.VertexList) sb.AppendLine(pid+","+act+","+lv+","+ph+","+p.x.ToString("0.###",inv)+","+p.y.ToString("0.###",inv)+","+p.z.ToString("0.###",inv));
}
File.WriteAllText(Path.Combine(dir,"floor_strings.csv"),sb.ToString());
// existing stope meshes
var so=new StringBuilder(); int nst=0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Deleted||f.Layer==null||f.Layer.Name!="SCHEDULER\\TASK SOLIDS"||f.asPolyface==null||Convert.ToString(f.AttributeValueGet("STO_DEV",""))!="STO") continue; nst++;
  var pf=f.asPolyface; so.Append(Convert.ToString(f.AttributeValueGet("Name","")).Replace("|"," ")+"|");
  so.Append(string.Join(",",pf.VertexList.Cast<object>().Select(o=>{var p=(Deswik.Graphics.Geometry.Point)o; return p.x.ToString("0.##",inv)+","+p.y.ToString("0.##",inv)+","+p.z.ToString("0.##",inv);}))+"|");
  so.AppendLine(string.Join(",",pf.FaceList.Cast<object>().Select(o=>Convert.ToString(o))));
}
File.WriteAllText(Path.Combine(dir,"existing_stopes_mesh.txt"),so.ToString());
Out.AppendLine("strings "+ns+" stopes "+nst);
// block model speed test
string path=@"C:\path\to\BLOCK_MODEL.gmdlb";
Type mT=null; foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()){ var t=asm.GetType("Deswik.BlockModel.ManagedGeoModels.BlockModel"); if(t!=null) mT=t; }
object m=Activator.CreateInstance(mT,new object[]{path}); var load=mT.GetMethod("Load"); var st=load.GetParameters()[0].ParameterType;
load.Invoke(m,new object[]{Enum.Parse(st,"OpenForReadNotIndexed"),System.Threading.CancellationToken.None});
object bm=mT.GetProperty("BlockGeoModel").GetValue(m,null); var bmT=bm.GetType();
var names=(System.Collections.IEnumerable)bmT.GetMethod("GetAttributeNames").Invoke(bm,new object[]{true,true,true});
foreach (var n in names) Out.Append(n+"("+(((bool)bmT.GetMethod("AttributeIsNumeric").Invoke(bm,new object[]{n}))?"num":"txt")+") ");
Out.AppendLine();
var gd=(Func<string,long,double>)Delegate.CreateDelegate(typeof(Func<string,long,double>),bm,bmT.GetMethod("GetDoubleValue",new Type[]{typeof(string),typeof(long)}));
var gl=(Func<string,long,long>)Delegate.CreateDelegate(typeof(Func<string,long,long>),bm,bmT.GetMethod("GetLongValue",new Type[]{typeof(string),typeof(long)}));
var sw=System.Diagnostics.Stopwatch.StartNew(); double s=0; long cnt=0;
for (long r=0;r<1000000;r++){ s+=gl("_IJK",r); }
Out.AppendLine("1M _IJK reads ms="+sw.ElapsedMilliseconds); sw.Restart();
for (long r=0;r<1000000;r++){ double a=gd("AU",r); if (a>=2.5) cnt++; }
Out.AppendLine("1M AU reads ms="+sw.ElapsedMilliseconds+" ore="+cnt);
Out.AppendLine("MINED sample: "+bmT.GetMethod("GetValue",new Type[]{typeof(string),typeof(long)}).Invoke(bm,new object[]{"MINED",500000L}));
((IDisposable)m).Dispose(); return null;
