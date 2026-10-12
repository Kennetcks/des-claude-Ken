// Add ACT_TYPE, LAT_VERT and DIS Parent ID attribute fields to DESIGN\FLOOR STRINGS and re-write the values
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

Deswik.Graphics.Primaries.Layer ls=null; foreach (Deswik.Graphics.Primaries.Layer l in App.Layers) if(l.Name=="DESIGN\\FLOOR STRINGS") ls=l;
var at=ls.Attributes;
var addM=at.GetType().GetMethods().First(m=>m.Name=="Add"&&m.GetParameters().Length==3&&m.GetParameters()[0].ParameterType==typeof(string));
var aType=addM.GetParameters()[1].ParameterType;
object strT=Enum.Parse(aType,"String");
var names=new string[]{"ACT_TYPE","LAT_VERT","DIS Parent ID"};
foreach (var n in names) addM.Invoke(at,new object[]{n,strT,""});
int n2=0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) {
  if (f.Layer!=ls) continue;
  foreach (var n in names) { var v=Convert.ToString(f.AttributeValueGet(n,"")); f.AttributeValueSet(n,v); }
  n2++;
}
App.Legends.ApplyToLayer("DESIGN\\FLOOR STRINGS","Floor Strings ACT_TYPE");
App.Redraw(true);
Out.AppendLine("Layer fields now: "+string.Join(", ",((System.Collections.IEnumerable)at).Cast<object>().Select(x=>x.GetType().GetProperty("Name").GetValue(x,null))));
var cnt=new Dictionary<string,int>(); foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (f.Layer==ls){ string v=Convert.ToString(f.AttributeValueGet("ACT_TYPE","")); int c; cnt.TryGetValue(v,out c); cnt[v]=c+1; }
Out.AppendLine("Strings updated: "+n2+"; ACT_TYPE values: "+string.Join(", ",cnt.OrderBy(k=>k.Key).Select(k=>(k.Key==""?"(blank)":k.Key)+"="+k.Value)));
return null;