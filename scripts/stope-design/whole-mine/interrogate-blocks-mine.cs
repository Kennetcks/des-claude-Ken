// Batch interrogate the whole-mine 5 m blocks against the block model (writes a CSV)
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)

var asm = typeof(Deswik.Graphics.Application).Assembly;
string bm = @"C:\path\to\BLOCK_MODEL.gmdlb";
string BASE="0\\06 SITE\\03 ENGINEERING\\DESIGN MASTER\\STOPES\\STOPE DESIGN\\5M BLOCKS MINE";
string csv = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Kennet.DeswikClaude","work","stopes_mine","interrogation_blocks_mine.csv");
if (File.Exists(csv)) File.Delete(csv);
var mtT = asm.GetType("Deswik.Graphics.Interrogation.InterrogationRules+ModelToInterrogate");
var rulesT = asm.GetType("Deswik.Graphics.Interrogation.InterrogationRules");
var rules = Activator.CreateInstance(rulesT, new object[]{App.Legends});
var m = Activator.CreateInstance(mtT, new object[]{App.CommonSettings});
mtT.GetProperty("FileName").SetValue(m, bm, null); mtT.GetProperty("Use").SetValue(m, true, null);
var mtProp = mtT.GetProperty("ModelType"); mtProp.SetValue(m, Enum.Parse(mtProp.PropertyType,"BlockGeoModel"), null);
var ms = mtT.GetField("ModelSettings").GetValue(m); ms.GetType().GetProperty("DensityFieldName").SetValue(ms, "DENSITY", null);
var models = rulesT.GetProperty("Models").GetValue(rules,null); models.GetType().GetMethod("Add").Invoke(models,new object[]{m});
var ib = new Deswik.Graphics.Interrogation.InterrogateBatch(App);
ib.Rules = (Deswik.Graphics.Interrogation.InterrogationRules)rules;
var p = new Deswik.Graphics.Interrogation.FieldsToInterrogate.Parameters(); p.InterrogateSelected = true;
var fld = new Deswik.Graphics.Interrogation.FieldsToInterrogate.Parameters.InterrogationFields(); fld.Name = "AU"; fld.DestinationName = "AU";
fld.FieldHandling = new Deswik.Interrogation.InterrogationFieldHandling(Deswik.Interrogation.InterrogationFieldHandling.FieldHandlingTypeConstants.MassWeighted, 0);
p.Fields.Add(fld); ib.FieldsToInterrogate = p;
ib.ModelDensityName = "DENSITY"; ib.CalcModelFields = true;
ib.CreateNewID = true; ib.NewIDField = "INT_ID"; ib.UseExistingIDField = false;
var sel = new Deswik.Graphics.Collections.Selection(); int n=0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer!=null && f.Layer.Name.StartsWith(BASE) && f.asPolyface!=null) { sel.Add(f); n++; }
ib.Selection = sel; ib.RebuildSelection = false;
ib.ShowProgressAndMessages = false; ib.ShowMessageBox = false; ib.ShowResultsOnCompletion = false; ib.ShowPivotOnCompletion = false;
ib.AddResultsToGraphics = false; ib.AddFigureAttributes = false; ib.SaveAtEnd = false; ib.PromptIfNoEntitiesMatching = false;
ib.CreateOutputCSVFile = true; ib.OutputCSVFile = csv; ib.CloseModelOnCompletion = true;
Out.AppendLine("solids selected: "+n);
try { ib.Execute(); } catch (Exception e) { Out.AppendLine("execute: "+(e.InnerException??e).Message); }
Out.AppendLine("success="+ib.Success+" csv="+File.Exists(csv));
return null;
