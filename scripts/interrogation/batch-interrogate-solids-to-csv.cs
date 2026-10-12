// Batch interrogate each solid on a layer against a block model (AU mass-weighted) to a per-solid CSV. AddFigureAttributes did not write attributes here; use write-interrogation-attributes.cs
// Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out)
// Usings: 

var asm = typeof(Deswik.Graphics.Application).Assembly;
string bm = @"C:\path\to\BLOCK_MODEL.gmdlb";
string LAY = "DESIGN\\FLOOR STRINGS\\SOLIDS";
string csv = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kennet.DeswikClaude", "interrogation_floor_string_solids_by_solid.csv");
if (File.Exists(csv)) File.Delete(csv);
var mtT = asm.GetType("Deswik.Graphics.Interrogation.InterrogationRules+ModelToInterrogate");
var rulesT = asm.GetType("Deswik.Graphics.Interrogation.InterrogationRules");
var rules = Activator.CreateInstance(rulesT, new object[]{App.Legends});
var m = Activator.CreateInstance(mtT, new object[]{App.CommonSettings});
mtT.GetProperty("FileName").SetValue(m, bm, null);
mtT.GetProperty("Use").SetValue(m, true, null);
var mtProp = mtT.GetProperty("ModelType"); mtProp.SetValue(m, Enum.Parse(mtProp.PropertyType,"BlockGeoModel"), null);
var models = rulesT.GetProperty("Models").GetValue(rules,null);
models.GetType().GetMethod("Add").Invoke(models,new object[]{m});

var ib = new Deswik.Graphics.Interrogation.InterrogateBatch(App);
ib.Rules = (Deswik.Graphics.Interrogation.InterrogationRules)rules;
var p = new Deswik.Graphics.Interrogation.FieldsToInterrogate.Parameters();
p.InterrogateSelected = true;
var fld = new Deswik.Graphics.Interrogation.FieldsToInterrogate.Parameters.InterrogationFields();
fld.Name = "AU"; fld.DestinationName = "AU";
fld.FieldHandling = new Deswik.Interrogation.InterrogationFieldHandling(Deswik.Interrogation.InterrogationFieldHandling.FieldHandlingTypeConstants.MassWeighted, 0);
p.Fields.Add(fld);
ib.FieldsToInterrogate = p;
ib.ModelDensityName = "DENSITY";
ib.CalcModelFields = true;
ib.CreateNewID = true; ib.NewIDField = "INT_ID"; ib.UseExistingIDField = false;
var sel = new Deswik.Graphics.Collections.Selection();
int n=0;
foreach (Deswik.Graphics.Primaries.Figure f in App.ModelSpace.Entities) if (!f.Deleted && f.Layer!=null && f.Layer.Name==LAY && f.asPolyface!=null) { sel.Add(f); n++; }
ib.Selection = sel; ib.RebuildSelection = false;
ib.ShowProgressAndMessages = false; ib.ShowMessageBox = false; ib.ShowResultsOnCompletion = false; ib.ShowPivotOnCompletion = false;
ib.AddResultsToGraphics = false; ib.AddFigureAttributes = true; ib.SaveAtEnd = false; ib.PromptIfNoEntitiesMatching = false;
ib.CreateOutputCSVFile = true; ib.OutputCSVFile = csv; ib.CloseModelOnCompletion = true;
Out.AppendLine("solids selected: "+n+" Tonnes='"+ib.TonnesName+"' Volume='"+ib.VolumeName+"' ModelTonnes='"+ib.ModelTonnesName+"' group='"+ib.AttributeGroup+"'");
try { ib.Execute(); } catch (Exception e) { Out.AppendLine("execute: "+(e.InnerException??e).ToString().Substring(0,800)); }
Out.AppendLine("success="+ib.Success);
if (File.Exists(csv)) { var lines=File.ReadAllLines(csv); Out.AppendLine("csv lines="+lines.Length); foreach (var l in lines.Take(4)) Out.AppendLine(l.Length>300?l.Substring(0,300):l); }
foreach (Deswik.Graphics.Primaries.Figure f in sel) { foreach (var o in (System.Collections.IEnumerable)f.XProperties) { var nm=Convert.ToString(o.GetType().GetProperty("Name").GetValue(o,null)); if (nm.StartsWith("_dw_Legend")) continue; Out.AppendLine("  "+nm+" = "+Convert.ToString(o.GetType().GetProperty("Value").GetValue(o,null))); } break; }
return null;