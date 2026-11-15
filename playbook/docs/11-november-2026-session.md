# 11 – November 2026 session: what was learned

A second long session (late October to mid November 2026) added level design in the Level Design Drafter, replacing IS tunnel tasks, report rebuilds without Deswik, Ventsim models built from Deswik strings, and a daily report automation. The scripts are in [../scripts/nov-2026/](../scripts/nov-2026/README.md). This page keeps the rules and the gotchas.

## Never do (unless asked again)

- **Do not run `IS_UpdateGraphicsAttributesFromSchedAll`** (or any "update graphics attributes from schedule" step) after a re-level. It opens the assign-attribute dialog and interrupts the engineer, who runs it themselves. Say that the CAD solid dates stay stale until they do.
- **Do not refresh `YEAR_MONTH` with a script.** The engineer keeps it with an IS "Update Fields Using Formula" rule. The formula that works in IS is `Year([Start]) & "-" & Format(Month([Start]),"00")`. `Format([Start],"yyyy-MM")`, `TEXT()` and `ToString()` do not work, because Start is numeric. Creating the rule through the API failed, so it is made in the IS dialog.
- **Do not re-run `cs_cl_from_task_solids.cs` with APPLY on a file whose tunnel tasks were created from its output.** Once tasks are re-created from those centrelines, the string handles are the tasks' `DIS Parent ID`; rewriting the strings orphans every tunnel task.
- **Never bulk-delete on a layer by area alone.** The engineer draws sketch lines on the same layers.
- **Never save over an original.** Back up first (see below), write new files under new names.

## Backups

Before editing any of the engineer's files (physicals `.xlsm`, tracking sheet `.xlsx`, deck `.pptx`, `.vsm`), save a copy to one backup folder on the Desktop, `<DESKTOP>\...\3MS Backups`, named `<original name>_prev_<dd-MM_HHmm><ext>` (`SaveCopyAs` for open Office files). Not beside the original on the shared drive. Create the folder if it is missing; do not move or tidy older backups.

## Driving the plugin over the pipe

When the Deswik tools are not loaded in a chat, the `Kennet.DeswikClaude` plugin still listens on `\\.\pipe\KennetDeswikClaude`, one JSON line each way, one request per connection:

- `{"op":"ping"}` returns the open file; `{"op":"list"}` the tool list; `{"op":"call","name":"<tool>","input":{...}}` returns `{content:[{text}],isError}`.
- Check `Get-Process Deswik.CAD` and `[IO.Directory]::GetFiles('\\.\pipe\')` first.
- With several Deswik windows open, each serves the pipe and a call lands on one at random. `tools/deswik_pipe.ps1 -docMatch <file>` adds a guard on `App.Filename` and resends until the call reaches the right window.
- **`modifies=false` does not stop a script from changing the drawing.** A dry run must check its own `APPLY` flag before every edit. (A "dry run" once changed 7 Design Master strips this way.) With auto-approve on, say what will change before running.

## Deswik CAD API gotchas

- Polylines and MText on reference layers come back as plain `Figure`: use `f.IsPolyline` / `f.asPolyline`, `f.IsMText` / `f.asMText.TextString`.
- Delete with `f.Deleted = true` (`Entities` has no `RemoveItem`). Deleted figures stay in `Entities`, so always skip `f.Deleted`.
- **A hatched polyline edited in place keeps its old hatch.** Neither `ResetPersistentVertexList` nor `Update` refreshes it. Clone the figure, set the vertices on the clone, `AddItem` it, mark the old one deleted. Most "rebuild" scripts here work this way and save the old vertices to `C:\dswtmp\*_backup.txt` first.
- Hatch transparency: `HatchProperties.FillBackColor.Transparency` (0–255) and `Solid2DTransparency`; 50% = 128.
- `MText.Rotation` is in degrees, `RotationRad` in radians; `Figure.BoundingBox.Midpoint` is the text-box centre.
- Report pictures without a screen grab: `App.ViewToBitmap(Color.White, 2400, 1500, 200f, true)` after `SetView(...)` and `ViewSize`. Hide the survey floor and stope layers for pictures, and put the user's view and layers back afterwards.

## Level design rules learned

- **Moving a take-off "x m along the decline":** measure the old and the new position the same way – the arc length along the decline centreline to the point's perpendicular foot. Print "moved X m from the original" from that same function, and read it back before applying. Mixing a wall chainage with a centreline chainage made a "0.30 m" move really 0.21 m on a curve. If the draft has already been edited, the original is the Design Master copy.
- **Truck loading back strip drop-down width = drive width − 0.5 m** (5.0 m drive → 4.5 m, 5.5 m → 5.0 m). The rest follows the site's truck loading strip standard (flat section above the decline backs, ramps at each end). Strip style used: ramps hatched grey (colour 253) at 50%, the rest red.
- Decline continuations were rebuilt like the Deswik decline tool: tangent arc from the last segment, straight, end arc, vertices evenly spaced, RLs exactly on the design grade from the joint. Dependants (walls, back strips, take-off centrelines) keep their chainage / offset / height to the decline.

## Replacing IS tunnel tasks after string edits

Used when edited strings must replace existing scheduled tunnel tasks without losing settings and links:

1. **Snapshot** the schedule and task solids: `cs_schedule_snapshot.cs` and `cs_tasksolid_snapshot.cs` (the solids one can take several minutes; use a long timeout).
2. **Plan** the replacement: match old parents to new strings by MINE / LEVEL / ACT_TYPE and location against the solids snapshot (tasks may not be parented to the handles you expect). Write the plan as a TSV (new handle, old, donor cut, kind).
3. **Delete** the old tasks and their solids: `cs_delete_tasks_for_strings.cs`. `Schedule.Delete(task)` does not delete the task solid – mark the `SCHEDULER\TASK SOLIDS` figures deleted yourself. It refuses tasks with progress.
4. **Create Tunnels from a script**: `cs_create_tunnels_selected.cs`, with the string handles in `C:\dswtmp\create_tunnels_handles.txt`. It uses the project's saved Create Tunnels settings with *Designs to use = Selected* and refuses if any string already has tasks. Create Tunnels names tasks from LEVEL + ACT_TYPE + DIRECTION and needs STOPE TYPE `-` and DIRECTION in `-, N, S, E, W`.
5. **Copy settings and relink** with `cs_newstrings_apply.cs` (resources, rate, priority, calendar, constraints, links).
6. **Audit before saying it is done**: the mapper matches cuts by position, so when a new string has fewer cuts an external link can land on cut #2 or #3 and cut #1 starts at project start. Check every new heading's cut #1 has a predecessor, and link escapeway raises to the drive cut whose solid holds each raise end (`cs_audit_cut1_ewr_links.cs`).
7. Brand-new strings: copy settings from a similar heading; predecessors = the last cut of the drives they join.

Respect exclusions the engineer names mid-task.

## IS reports without Deswik

`tools/DsfReports.cs` rebuilds IS crosstab report rows straight from an unzipped `.dsf` (C# 5, loaded with PowerShell 5.1 `Add-Type`):

- Tasks, task filters, production-field weights and the view's rows come from the `.tmp.dxf` text; each row's filter and expression from the `.tmp.dsf` UTF-16 XML.
- An optional global filter (e.g. `Mine=MB+MA`) is ANDed onto every row. In filters AND binds tighter than OR.
- Quantities are spread over Start–Finish in elapsed time. Stored task text needs `__w__` → `_`.
- Checked against a Deswik export it matched about 99% of cells (monthly, daily and grade-bin tabs). Known gap: a row-reference formula that adds a low-grade development row is counted differently by Deswik's export.

## Excel COM from PowerShell 5.1

- `$range.Value2 = x` **inside a function** (range passed as a parameter) silently writes nothing.
- A top-level line that writes a double after a string (or the other way round) throws "Unable to cast … to System.String": the binder caches the first type per call site.
- So write every cell at top level as `.Formula = <invariant string>`; formatting (Font, Interior, AddComment) inside functions is fine.
- Build new workbooks in a separate hidden `New-Object -ComObject Excel.Application` so the user's open workbooks are untouched.
- Avoid `[ordered]` hashtables with integer keys (they index by position).

## Ventsim models from Deswik

- Ventsim DESIGN 6 has no automation API, but a `.vsm` is gzip of the tab-delimited TXT model. Airways are rows of the `MAIN` table (about 287 columns, ends at `END\tMAIN`); airways join where end coordinates match. The fan database is the `FANS` section; an airway's Fan Type is the fan id.
- Airway `Stage` is a bitmask of the stages it belongs to; branch flag 4096 = connect to surface (free end), 262144 = closed end (dead-end face, no warning).
- Scripts: `ventsim/vsm_add_rad.ps1` (add a drive with a forcing fan and vent bag), `vsm_rad_primary.ps1` (a drive as the primary exhaust with a fan wall), `VsmBuild.cs` / `VsmFromDxf.cs` (whole development plan from a DXF of centrelines, ends snapped together), `rad_to_surface_dxf.ps1` (import DXF).
- Copy template rows from the model itself (tunnel types, bag convention) and **always write a new `.vsm`**; never overwrite the base model.

## Daily report automation (pattern)

A scheduled PowerShell script filled a site daily report workbook from the previous day's emails (a cut sheet `.xlsx`, a plant status PDF and a 24-hour report PDF, read through classic Outlook COM; PDF text positions found from the header labels). What made it safe to run live:

- Dry run by default; `-Live` writes. Back up before every live write. Highlighting written cells is optional and was turned off.
- **Hard-guarded "never write" cells** (month-to-date box, a comment cell the user owns) are blocked in code, not just by convention.
- Input cells are written only if empty; differences are flagged, never overwritten.
- The user's own text is never overwritten: a state file records what the script wrote, so it can tell its own text from theirs. Where both are needed, the script appends (e.g. `<their text> | Downtime: <main-fleet list>`).
- Unknown activity types stop the run unless they reconcile with the 24-hour report.
- Emails can be filed into subfolders by mail rules – search the Inbox and all subfolders.
- The scheduled task needs the PC on and the user logged in; the workbook does not need to be open.

## Files from this session that are not in the repo

The session export also had the full conversation transcript, the raw session logs and some script outputs. They were left out because they hold site data (designs, schedules, names and email drafts) that can't be reliably cleaned. Scripts that only rewrote one deck's or one standard's wording (slide statements, SWOT, design standard sections, scenario KPI workbooks with plan figures) and superseded script versions were also left out.
