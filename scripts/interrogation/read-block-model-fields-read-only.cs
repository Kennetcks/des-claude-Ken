// Read the block model fields as a BlockGeoModel (read-only)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

var asm = typeof(Deswik.Graphics.Application).Assembly;
string bm = @"C:\path\to\BLOCK_MODEL.gmdlb";
var mtT = asm.GetType("Deswik.Graphics.Interrogation.InterrogationRules+ModelToInterrogate");
var rulesT = asm.GetType("Deswik.Graphics.Interrogation.InterrogationRules");
var rules = Activator.CreateInstance(rulesT, new object[]{App.Legends});
var m = Activator.CreateInstance(mtT, new object[]{App.CommonSettings});
mtT.GetProperty("FileName").SetValue(m, bm, null);
mtT.GetProperty("Use").SetValue(m, true, null);
var mtProp = mtT.GetProperty("ModelType");
mtProp.SetValue(m, Enum.Parse(mtProp.PropertyType,"BlockGeoModel"), null);
var models = rulesT.GetProperty("Models").GetValue(rules,null);
models.GetType().GetMethod("Add").Invoke(models,new object[]{m});
try {
  var all = ((System.Collections.IEnumerable)rulesT.GetMethod("GetAllModelFields").Invoke(rules,new object[]{false,App})).Cast<object>().Select(x=>x.ToString()).ToList();
  var num = new HashSet<string>(((System.Collections.IEnumerable)rulesT.GetMethod("GetAllModelFields").Invoke(rules,new object[]{true,App})).Cast<object>().Select(x=>x.ToString()));
  Out.AppendLine("fields: "+all.Count);
  Out.AppendLine(string.Join(", ", all.Select(f=>f+(num.Contains(f)?"":"(txt)"))));
} catch (Exception e) { Out.AppendLine("fields err: "+(e.InnerException??e).ToString().Substring(0,600)); }
try { var bf=(System.Collections.IEnumerable)rulesT.GetMethod("GetBlockModelFields").Invoke(rules,null); Out.AppendLine("blockmodel fields: "+string.Join(", ",bf.Cast<object>())); } catch (Exception e) { Out.AppendLine("bmf err: "+(e.InnerException??e).Message); }
return null;