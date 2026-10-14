// Stope shapes step 1: export the 5 m blocks on 5M BLOCKS\STOPES and \EXTRA STRINGS with attributes and geometry to st_blocks_export.tsv (read-only)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// FaceList is a flat Int32Array: 5 ints per face (v1,v2,v3,v4,-1; one-based; v4==v1 for a triangle), padded with zeros.
var inv=System.Globalization.CultureInfo.InvariantCulture;
string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine");
string BASE="DESIGN\\CREATING PLILLAR/STOPES\\5M BLOCKS";
string[] ks={"Name","SOURCE","LEVEL","DRIVE NUMBER","BLOCK","SPLIT","GROUP","CH_FROM","CH_TO","BASE_ABOVE_FLOOR","Stope Height","Stope Thickness","INT_TONNES","INT_AU","INT_DENSITY","INT_VOLUME"};
var sb=new StringBuilder(); int n=0;
foreach (Deswik.Graphics.Primaries.Layer ly in App.Layers) { if(ly.Deleted||!(ly.Name==BASE+"\\STOPES"||ly.Name==BASE+"\\EXTRA STRINGS")) continue;
 foreach (var e in ly.Entities){ var f=e as Deswik.Graphics.Primaries.Figure; if(f==null||f.Deleted||f.asPolyface==null) continue; var pf=f.asPolyface;
  sb.Append(ly.Name.Substring(BASE.Length+1)); foreach (var k in ks){ sb.Append('\t'); sb.Append(Convert.ToString(f.AttributeValueGet(k,""),inv)); }
  sb.Append('\t'); var vl=new List<string>(); foreach (var q in pf.VertexList) vl.Add(q.x.ToString("0.###",inv)+","+q.y.ToString("0.###",inv)+","+q.z.ToString("0.###",inv)); sb.Append(string.Join(",",vl));
  sb.Append('\t'); var fl=new List<string>(); foreach (var i in pf.FaceList) fl.Add(i.ToString()); sb.Append(string.Join(",",fl));
  sb.AppendLine(); n++; } }
File.WriteAllText(Path.Combine(dir,"st_blocks_export.tsv"),sb.ToString()); Out.AppendLine("figures "+n);
return null;
