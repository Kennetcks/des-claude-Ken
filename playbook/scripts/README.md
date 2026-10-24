# Scripts index

**Worked examples from Oct 2026 – adapt before use.** Paths are placeholders (`<SHARE>\...`, `<DOCS>\...`, `%TEMP%\dsw`), the schedule file is `MINE_UG_FY27.dcf`, and document text and figures are `<placeholders>`. CAD handles, task IDs and row numbers are from one schedule and will be different in yours. Read a script fully before running it; run read-only versions first.

- `.cs` files are **bodies for the plugin's `run_csharp` tool** (`object Run(App, Schedule, Out)`), sent with `deswik/dcall.ps1`.
- `.ps1` files are **Windows PowerShell 5.1** scripts using Office COM. Save them as UTF-8 **with BOM** if they contain °, ², • characters.

## deswik/

| Script | What it does | Changes data? |
|---|---|---|
| `dcall.ps1` | Pipe client for the plugin; reply timeout; retries when the call hits the wrong Deswik window | – |
| `export_physicals_reports.cs` | Exports 3 report views (13 months, 30 days, 12 months) to TSV; restores the active report | No |
| `export_report_monthly.cs` | Earlier single-report monthly export | No |
| `discover_view_api.cs` | Lists view/report API members (discovery pattern) | No |
| `why_task_starts_early.cs` | Finds a task from its CAD solid handle and lists predecessors vs the drive cut | No |
| `filter_heading_successors.cs`, `filter_heading_fix.cs` | Create / rewrite a named task filter (where clause) and make it active | Filter only |
| `final_assign_resources.cs`, `assign_jumbo_extension_cuts.cs`, `assign_resources_level_example.cs` | Assign resources to tasks from templates | Yes |
| `final_layer_attributes.cs`, `final_layer_match_act_type.cs`, `fix_layer_attributes_example.cs`, `test2_attributes.cs` | Set / copy scheduler attributes on centreline strings | Yes |
| `de3_radius_options.cs`, `de3_single_loop_options.cs`, `test2_flat_option.cs` | Decline loop design options (radius, single gradient, flat section) | Creates strings |
| `drafter_read_centrelines.cs` | Lists strings with vertices, grades, chainage and attributes | No |
| `drafter_fan_chamber_split.cs`, `drafter_fan_chamber_8_5m.cs` | Splits a centreline at vertices; centres an 8.5 m fan chamber in a flat; no grade change | Yes |
| `level_plan_views.cs` | Plan-view screenshots of levels | No (view only) |
| `mpa_solid_axes.cs`, `mpa005_centrelines.cs`, `mpa005_copy_attribute_settings.cs` | Build floor centrelines from task solids on a new layer; copy attribute definitions | Creates layer |

## excel/

| Script | What it does |
|---|---|
| `build_nov_physicals.ps1` | Builds the month's physicals workbook from a template + TSV exports (refuses to overwrite) |
| `build_nov_physicals_refresh.ps1` | Same, rebuilding from the previous version of the same month (keeps fixes) |
| `tracking_nov3ms.ps1` | Writes a 3MS into the tracking sheet's 3MS block (preview, then `-Apply`) |
| `tracking_equipment_kpi.ps1` | Inserts equipment count + KPI rows |
| `tracking_board_copy.ps1`, `tracking_copy_with_3ms.ps1` | Make a tracking-sheet copy with a given 3MS; current month as month plan |
| `cer_level_power.ps1` | Fills a CER from a per-level planning workbook (cost lines, cash flow, text) |
| `cer_from_quote.ps1` | Fills a CER from a supplier quote (fan + container set) |
| `build_lom_workbook.ps1` | Life-of-mine summary workbook from report exports |

## powerpoint/

| Script | What it does |
|---|---|
| `build_nov_deck.ps1` | New draft deck from last month's approved deck + physicals |
| `update_nov_deck_live.ps1` | Updates the open deck: 12-month table, fleet, comments, SWOT |
| `update_nov_deck_levels.ps1` | Level-slide comment boxes |
| `slide2_grammar.ps1` | Grammar pass that replaces only text after each label |
| `slide19_procurement.ps1` | Plain pending-procurement table |
| `swot_update.ps1` | Adds red SWOT rows and shrinks to fit |
| `deck_ytd_charts.ps1` | Replaces chart pictures with images exported from the tracking sheet |

## word-onenote/

| Script | What it does |
|---|---|
| `vent_survey_memo.ps1` | Secondary ventilation survey memo from a template + survey readings + photos |
| `build_mds_v2.ps1`, `build_mds_devonly.ps1`, `fill_mds_placeholders.ps1` | Mine Design Standard update (tracked changes, development-only version, placeholders) |
| `onenote_handover_update.ps1` | Adds red "UPDATE" lines under matching items on a OneNote page (backs up the page XML first) |
