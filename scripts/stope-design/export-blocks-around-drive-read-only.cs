// Export the block model sub-blocks around one ore drive, plus the drive's floor string, to CSV for design_stopes.py (read only)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 
// Set the model, the drive (DIS Parent ID of its floor string) and a box around the drive that includes the stope height.

string path=@"C:\path\to\model.gmdlb";
string outDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes"); Directory.CreateDirectory(outDir);
string FLOOR_LAYER="DESIGN\\FLOOR STRINGS", PID="34A6267";
double X0=8075,X1=8225,Y0=62790,Y1=62960,Z0=6410,Z1=6455;
var inv=System.Globalization.CultureInfo.InvariantCulture;
// floor string of the drive
foreach (var l in App.Layers) if (l.Name==FLOOR_LAYER) foreach (var e in l.Entities){ var f=e as Deswik.Graphics.Primaries.Figure; if(f==null||f.asPolyline==null||Convert.ToString(f.AttributeValueGet("DIS Parent ID",""))!=PID) continue;
  var sb=new StringBuilder("x,y,z\n"); foreach (Deswik.Graphics.Geometry.Point v in f.asPolyline.VertexList) sb.AppendLine(string.Format(inv,"{0},{1},{2}",v.x,v.y,v.z)); File.WriteAllText(Path.Combine(outDir,"floor_string.csv"),sb.ToString()); Out.AppendLine("floor string points "+f.asPolyline.VertexList.Count); }
// open the .gmdlb read only through the managed wrapper (Deswik.GeoModel.BlockModel.Open returned null here)
Type mT=null; foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()){ var t=asm.GetType("Deswik.BlockModel.ManagedGeoModels.BlockModel"); if(t!=null) mT=t; }
object m=Activator.CreateInstance(mT,new object[]{path}); var load=mT.GetMethod("Load"); var st=load.GetParameters()[0].ParameterType;
if(!(bool)load.Invoke(m,new object[]{Enum.Parse(st,"OpenForReadNotIndexed"),System.Threading.CancellationToken.None})){ Out.AppendLine("model did not open"); return null; }
object bm=mT.GetProperty("BlockGeoModel").GetValue(m,null); var bmT=bm.GetType();
var md=bmT.GetProperty("Metadata").GetValue(bm,null); var mdT=md.GetType();
double[] O=(double[])mdT.GetProperty("Origin").GetValue(md,null); double SX=Convert.ToDouble(mdT.GetProperty("XSize").GetValue(md,null)), SY=Convert.ToDouble(mdT.GetProperty("YSize").GetValue(md,null)), SZ=Convert.ToDouble(mdT.GetProperty("ZSize").GetValue(md,null));
int NY=Convert.ToInt32(mdT.GetProperty("NY").GetValue(md,null)), NZ=Convert.ToInt32(mdT.GetProperty("NZ").GetValue(md,null));
Out.AppendLine("origin "+string.Join(",",O)+" parent "+SX+"x"+SY+"x"+SZ+" rotated "+mdT.GetProperty("IsRotated").GetValue(md,null));
var gd=bmT.GetMethod("GetDoubleValue",new Type[]{typeof(string),typeof(long)}); var gv=bmT.GetMethod("GetValue",new Type[]{typeof(string),typeof(long)});
long N=(long)bmT.GetProperty("RecordCount").GetValue(bm,null);
// records are sorted by _IJK = k + NZ*(j + NY*i) (k fastest): binary search each (i,j) column
Func<long,long> ijkAt=(r)=>Convert.ToInt64(gv.Invoke(bm,new object[]{"_IJK",r}));
Func<long,long> lower=(target)=>{ long lo=0,hi=N; while(lo<hi){ long mid=(lo+hi)/2; if(ijkAt(mid)<target) lo=mid+1; else hi=mid; } return lo; };
int i0=(int)((X0-O[0])/SX), i1=(int)((X1-O[0])/SX), j0=(int)((Y0-O[1])/SY), j1=(int)((Y1-O[1])/SY), k0=(int)((Z0-O[2])/SZ), k1=(int)((Z1-O[2])/SZ);
string[] F={"_XC","_YC","_ZC","_XSIZE","_YSIZE","_ZSIZE","AU","DENSITY"};
var sbB=new StringBuilder("x,y,z,dx,dy,dz,au,dens,domain,mined\n"); int n=0;
for (int i=i0;i<=i1;i++) for (int j=j0;j<=j1;j++){ long r0=lower(k0+NZ*((long)j+NY*(long)i)), r1=lower(k1+1+NZ*((long)j+NY*(long)i));
  for (long r=r0;r<r1;r++){ var vals=F.Select(fn=>((double)gd.Invoke(bm,new object[]{fn,r})).ToString(inv)); sbB.AppendLine(string.Join(",",vals)+","+gv.Invoke(bm,new object[]{"DOMAIN",r})+","+gv.Invoke(bm,new object[]{"MINED",r})); n++; } }
File.WriteAllText(Path.Combine(outDir,"blocks.csv"),sbB.ToString());
Out.AppendLine("blocks written: "+n+" to "+outDir);
((IDisposable)m).Dispose(); return null;
