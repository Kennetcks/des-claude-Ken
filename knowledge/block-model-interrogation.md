# Batch interrogating solids against a block model

When the user asks for grade and tonnes of solids ("batch interrogate"), run Deswik's own batch interrogation from `run_csharp`. It's the same engine as the Batch Interrogation dialog. Verified on Deswik 2024.2 with a Deswik `.gmdlb` block model on 513 tunnel solids (~1 minute).

## Finding the model
- Ask or search: block models are often `.gmdlb` (Deswik block GeoModel). Others: Datamine `.dm`, Surpac, Vulcan, etc.
- Read the model's fields before choosing grade/density fields (see the script). Text fields (LITH, DOMAIN, RESCAT…) can be grouping fields; numeric ones (AU, DENSITY) are interrogated.

## API (Deswik.Graphics.Interrogation, verified 2024.2)
```csharp
var asm = typeof(Deswik.Graphics.Application).Assembly;
var rulesT = asm.GetType("Deswik.Graphics.Interrogation.InterrogationRules");
var mtT = asm.GetType("Deswik.Graphics.Interrogation.InterrogationRules+ModelToInterrogate");
var rules = Activator.CreateInstance(rulesT, new object[]{ App.Legends });
var m = Activator.CreateInstance(mtT, new object[]{ App.CommonSettings });
mtT.GetProperty("FileName").SetValue(m, modelPath, null);
mtT.GetProperty("Use").SetValue(m, true, null);
var mtProp = mtT.GetProperty("ModelType");          // must be set: .gmdlb = BlockGeoModel
mtProp.SetValue(m, Enum.Parse(mtProp.PropertyType, "BlockGeoModel"), null);
var models = rulesT.GetProperty("Models").GetValue(rules, null);
// DENSITY: set it on the model's own settings, or Deswik uses DefaultDensity (2.8) for every block
var ms = mtT.GetField("ModelSettings").GetValue(m);
ms.GetType().GetProperty("DensityFieldName").SetValue(ms, "DENSITY", null);
models.GetType().GetMethod("Add").Invoke(models, new object[]{ m });
// fields: rulesT.GetMethod("GetAllModelFields").Invoke(rules, new object[]{ numericOnly, App })

var ib = new Deswik.Graphics.Interrogation.InterrogateBatch(App);
ib.Rules = (Deswik.Graphics.Interrogation.InterrogationRules)rules;
var p = new Deswik.Graphics.Interrogation.FieldsToInterrogate.Parameters(); p.InterrogateSelected = true;
var fld = new Deswik.Graphics.Interrogation.FieldsToInterrogate.Parameters.InterrogationFields();
fld.Name = "AU"; fld.DestinationName = "AU";
fld.FieldHandling = new Deswik.Interrogation.InterrogationFieldHandling(
    Deswik.Interrogation.InterrogationFieldHandling.FieldHandlingTypeConstants.MassWeighted, 0);
p.Fields.Add(fld); ib.FieldsToInterrogate = p;
ib.CalcModelFields = true;              // ModelDensityName is only the output column name
ib.CreateNewID = true; ib.NewIDField = "INT_ID";      // without an ID you get ONE combined row
ib.Selection = sel; ib.RebuildSelection = false;      // Deswik.Graphics.Collections.Selection of live solids
ib.ShowProgressAndMessages = false; ib.ShowMessageBox = false; ib.ShowResultsOnCompletion = false; ib.ShowPivotOnCompletion = false;
ib.AddResultsToGraphics = false; ib.SaveAtEnd = false; ib.PromptIfNoEntitiesMatching = false;
ib.CreateOutputCSVFile = true; ib.OutputCSVFile = csvPath; ib.CloseModelOnCompletion = true;
ib.Execute();
```

## Gotchas
- **Density: set `ModelToInterrogate.ModelSettings.DensityFieldName`.** It defaults to `<None>`, and Deswik then silently uses `DefaultDensity` (2.8) for every block. `InterrogateBatch.ModelDensityName` is just the output column name. Check that the "Model Density" column varies before reporting tonnes, and spot-check one solid against the user's own interrogation.
- **ModelType defaults to None** and then the model has no fields. Set it (`BlockGeoModel` for `.gmdlb`).
- **No ID field = one total row.** Set `CreateNewID`/`NewIDField` for one row per solid. Deswik writes that ID onto each solid, so match CSV rows back by it, and check the volume matches. The other solid attributes only appear in the CSV on the first run.
- `AddFigureAttributes = true` did **not** write anything onto the solids in this setup. Write the results yourself from the CSV, and **add the attribute fields to the layer first** (see `attributes-and-legends.md`). Names used: INT_TONNES, INT_AU, INT_VOLUME, INT_MODEL_VOLUME, INT_VOID_VOLUME, INT_DENSITY, INT_MODEL, INT_DATE.
- CSV columns: Volume (solid), Tonnes (solid × density), Model Volume / Model Tons (inside the model), Void Volume (outside the model), the interrogated fields, Model. **Report Model Tons and its mass-weighted grade**, and state the void volume separately. In the combined row the grade is diluted by the void at zero.
- `Execute` on a big model takes longer than the tool's 60 s wait. The call keeps running in Deswik; check the CSV file instead of re-running.
- Solids on a tunnels-by-rule layer lose these attributes when the layer is refreshed; tell the user to re-interrogate after a refresh.
- Report total tonnes and grade, the split by ACT_TYPE, how much is above a cut-off, and the void volume. Deliver the per-solid CSV.

See `scripts/interrogation/`.

- **Deswik 2026.1:** `InterrogationRules+ModelToInterrogate` no longer takes `App.CommonSettings`; its only constructor takes an `ICommonCadApplicationMethods`. Pass `Activator.CreateInstance(asm.GetType("Deswik.Graphics.Commands.CommonCadApplicationMethods"), new object[]{App, null})`. The rest of the batch setup is unchanged (checked 2026-10-13, 24 stopes, density 2.84-2.94).
