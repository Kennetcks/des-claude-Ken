# Scripts from the November 2026 session

**Worked examples from late Oct to mid Nov 2026 – adapt before use.** Same rules as [../README.md](../README.md): paths are placeholders (`<SHARE>\...`, `<DESKTOP>\...`, `C:\dswtmp`, `%TEMP%\dsw`), the schedule file is `MINE_UG_FY27.dcf`, mine codes are `MA`/`MB`/`MC`/`MD`, and CAD handles, task IDs, level names and coordinates are from one drawing and will be different in yours. Read a script fully before running it. Run the read-only ones (`probe`, `check`, `compare`, `dump`, `list`) first. Most scripts that change things are a **dry run unless `APPLY = true`** – check that flag yourself, because the plugin's `modifies=false` does not stop a script from changing the drawing.

The guide that goes with these scripts is [../../docs/11-november-2026-session.md](../../docs/11-november-2026-session.md).

- `deswik/` – bodies for the plugin's `run_csharp` tool (`object Run(App, Schedule, Out)`). Many start with a `WRONG WINDOW` guard on `App.Filename`.
- `excel/`, `powerpoint/` – Windows PowerShell 5.1 + Office COM. They work on the **open** file and never save unless the header says so.
- `ventsim/` – build new Ventsim `.vsm` models from Deswik strings (never overwrite the base model).
- `tools/` – the pipe client, `.dsf` report rebuild without Deswik, `.dcf`/`.vdcl` XML readers, PDF text and image tools.

Main families in `deswik/`:

| Prefix | What |
|---|---|
| `cs_mpa07_*`, `cs_dec_*`, `cs_rad_*`, `cs_dsp_*`, `cs_ewd_*` | Level Design Drafter work on one level: decline continuation, RAD / DSP / EWD take-offs, walls, fillets, grades, labels |
| `cs_truck_*`, `cs_*strip*`, `cs_dropdown_*`, `cs_jbox_*`, `cs_fan_strips*` | Truck loading back strips, drop-downs, junction boxes and fan strip standards |
| `cs_replace_*`, `cs_newstrings_*`, `cs_create_tunnels_selected.cs`, `cs_delete_tasks_for_strings.cs`, `cs_*snapshot.cs`, `cs_audit_cut1_ewr_links.cs`, `cs_deps*` | Replacing IS tunnel tasks after string edits (see the guide) |
| `cs_cl_from_task_solids.cs`, `cs_combined_*`, `cs_new_attrs.cs` | Rebuilding centrelines from task solids and matching them to tasks |
| `w390_*`, `w412_*`, `reverse_*`, `split_*` | Moving cuts between jumbos, task rates and reordering (examples) |
| `*filter*`, `slide_filters.cs`, `export_*`, `phys_export.cs`, `inspect_*`, `list_*` | Task filters for deck screenshots, report exports and report API discovery |

## deswik/

| Script | What it does |
|---|---|
| `activate_filter.cs` | Make a task filter active (name read from C:\dswtmp\filter_to_activate.txt). No task data changes. File not saved. |
| `add_names_to_filter.cs` | Add task names to an existing "Name list" filter (first line of C:\dswtmp\filter_add.txt = filter name, other lines = task names), |
| `bins_vs_ore.cs` | Read-only: why HG + MG + LG tonnes (Blend Forecast bins: Diluted Tonnage, SCHEDULED = 1, Diluted Au_Grade > 1) differ from |
| `blend_export_only.cs` | Read-only: export ONLY the Blend Forecast report view (12 months from 1 Oct 2026) to %TEMP%\dsw\phys_blend_fix.tsv. |
| `check_active_filter.cs` | Read-only: count tasks the active filter lets through, by MINE, with date span. |
| `check_slide_filters.cs` | Read-only: count tasks in each "3MS S1x" filter (after a cache refresh) and show its where clause start and Nov-Jan metres. |
| `cs_audit_cut1_ewr_links.cs` | 1) Headings whose cut #1 has no predecessor: external links that landed on a later cut are moved to cut #1. |
| `cs_cable_labels.cs` | CB labels on CABLEBOLTS to the highest RL of their cable group: cables within 10 m in plan and 6 m in height of the |
| `cs_cables_height.cs` | Cable bolt collars (points on CABLEBOLTS) to the right height: on the strip surface where the point lies inside a strip |
| `cs_cables_probe.cs` | Read-only: everything on the CABLEBOLTS layer, with the nearest drive centreline (floor z there, profile height) and the |
| `cs_capture_views.cs` | Render Design Master views to PNG for the design standard. Each shot: hide the clutter layers, set the view |
| `cs_centre_labels.cs` | Centre each fan / truck strip label on its strip: panels sharing vertices form one strip; each label belongs to the |
| `cs_cl_from_task_solids.cs` | MINE_UG_FY27 (06 Dec): centrelines for the SCHEDULED = 1 task solids (SCHEDULER\TASK SOLIDS, DIS Activity Type Tunnel), one |
| `cs_cl_qa.cs` | Body of: object Run(Deswik.Graphics.Application App, Deswik.Scheduler.Engine.Schedule Schedule, StringBuilder Out) |
| `cs_cog_lines.cs` | Level Design Drafter: a red COG line across the drive (6 m, Design Master style) with "COG" text along the drive, at |
| `cs_cog_notes.cs` | Read-only: NOTES polylines within 4 m of the RAD's old grade breaks, and the two COG texts' position/rotation. |
| `cs_cog_probe.cs` | Read-only: anything "COG" - layers, attribute fields, text - and what of it sits near the W6390 RAD. |
| `cs_cog_update.cs` | MPA 07 W6390 RAD COG markers (NOTES: a 6 m line across the drive + "COG" text) for the new grade - one break, 1:8.8 to |
| `cs_combined_dxf.cs` | Read-only: write the COMBINED 12.11 strings to an R12 ASCII DXF (3D polylines, one layer per ACT_TYPE) for Ventsim. |
| `cs_combined_match_tasks.cs` | Make COMBINED 12.11 hold exactly the task-solid parent strings (tunnel tasks): |
| `cs_copy_to_standards.cs` | Copy one single fan strip, one double fan strip and one of each truck loading strip onto STRIP STANDARDS, in a new |
| `cs_create_tunnels_selected.cs` | IS Create Tunnels without the dialog, for the strings whose handles are listed in C:\dswtmp\create_tunnels_handles.txt. |
| `cs_dec_cont_detail.cs` | Read-only: vertex-by-vertex geometry of 44BD9C0 and the draft MAIN DEC (4587A08): chainage, segment, azimuth, turn, |
| `cs_dec_cont_probe.cs` | Read-only: figure 44BD9C0 (layer, attributes, ends, spacing, grade) and every figure on 0\DESIGN DRAFT (layer, name, |
| `cs_dec_cont_rebuild.cs` | MPA 07 draft MAIN DEC (4587A08) rebuilt like the Deswik decline tool, as a continuation of 44BD9C0 (DESIGN MASTER): |
| `cs_dec_grade_check.cs` | MPA 07 MAIN DEC: check every segment is exactly 1:7 down (plan length / drop); if FIX, reset each vertex RL to |
| `cs_dec_on_solids.cs` | MPA 07 draft MAIN DEC (4587A08) rebuilt smooth (like 44BD9C0) and fitted to the MPA 07 sketch solids (decline cuts): |
| `cs_dec_readback.cs` | Read-only after the rebuild: new MAIN DEC (joint, spacing, turns, grade), and the ties between decline walls, |
| `cs_dec_simplify.cs` | MPA 07 MAIN DEC: fewer vertices. Plan simplification (Douglas-Peucker) keeping the line within TOL of the current |
| `cs_decline_stockpile_strip.cs` | Build a truck loading strip on a real decline: select the decline centreline and the stockpile centreline, then run. |
| `cs_decline_walls_clean.cs` | Drafted decline walls redrawn clean from the decline CL: exactly 2.75 m each side, a vertex every ~0.5 m, end ties kept. Dry run unless APPLY |
| `cs_decline_walls_clpts.cs` | Drafted decline walls with one vertex per decline CL vertex (mitred 2.75 m offsets), end ties kept. Dry run unless APPLY |
| `cs_delete_tasks_for_strings.cs` | For the string handles in C:\dswtmp\create_tunnels_handles.txt: write their task solids' boxes to C:\dswtmp\TaskSolids_RebuildN.tsv |
| `cs_demo_probe.cs` | Read-only: current state of the MPA 07 W6390 RAD / DSP in the Level Design Drafter - every polyline on 0\DESIGN DRAFT |
| `cs_demo_rad_dsp.cs` | MPA 07 (Level Design Drafter), 3 Nov 2026: |
| `cs_deps2.cs` | Rebuild dependencies to match the old schedule (Map2.tsv). |
| `cs_deps_diff.cs` | Read-only: links in the 105950 snapshot (before the take-off rule) that are gone now, split into expected (parent -> take-off first cut) |
| `cs_diff_sched_fields.cs` | Read-only: for matched pairs, count differences in scheduling input fields (current state vs old). |
| `cs_diff_writable.cs` | Read-only: writable Task properties that are also snapshot columns; count old-vs-current differences on paired tasks. |
| `cs_direction_check.cs` | Read-only: for each COMBINED string, compare the mining direction of the new cuts with the old cuts of its source string. |
| `cs_dm_sub_hazards2.cs` | MA UG Design Master.duf: replace the G6390 SUB hazard strings (458373F, 4583740) and BHID labels (4583742, 4583741), |
| `cs_dm_sub_replace3.cs` | MA UG Design Master.duf: replace the old G6390 SUB design with the new draft from the Level Design Drafter (exported to |
| `cs_dropdown_probe.cs` | Read-only: the MPA 07 truck-loading drop-down box (4-point strip panel) edge lengths and fall, and the 4-point panels |
| `cs_dropdown_widen.cs` | MPA 07 W6390 DSP truck-loading drop-down: widen the 4-point box from 4.5 m to WIDTH, 0.25 m each side, kept centred on |
| `cs_dsp_check.cs` | Read-only: after the EWD -> DSP move. Lists the W6390 DSP figures, checks the drive wall ends meet the decline wall |
| `cs_dsp_gss.cs` | MPA 07 W6390 DSP: GSS R 3.0 -> R 2.0 (Design Master DSPs at 5.5 m use R 2.0) on every DSP figure, and the GSS label text. |
| `cs_dsp_mouth_dump.cs` | Read-only: DSP mouth in detail. Every wall vertex near the DSP (drafted WALLS and Drafter CENTERLINES\WALLS, decline and |
| `cs_dsp_move.cs` | MPA 07: W6390 DSP (was EWD; OLD = current name), moved MOVE m down the decline, 5.5 x 5.5, same azimuth and grade, 20.0 m long. |
| `cs_dsp_probe.cs` | Read-only: how DSPs are attributed in the Design Master (Description, GSS, PROFILE), and what the EWD grade arrow is |
| `cs_dsp_square.cs` | Square a DSP to the decline: rotate its CL about its start to 90 deg to the decline tangent, same length and grade |
| `cs_dsp_strip_move.cs` | MPA 07: move the truck loading strip that sits on the old W6390 EWD mouth down the decline with the W6390 DSP (MOVE m). |
| `cs_dsp_strip_move2.cs` | MPA 07: move the truck loading strip that sits on the W6390 DSP mouth down the decline with the W6390 DSP (MOVE m). |
| `cs_dsp_strip_probe.cs` | Read-only: every figure on 0\DESIGN DRAFT\STRIPPING (and sublayers) within 25 m of the old EWD / new DSP mouths, with |
| `cs_dsp_strip_std.cs` | MPA 07 W6390 DSP truck loading strip -> "Truck Loading Strip (1:7 down) 8.0 mL x 6.0mW x 1.5mH" (5.0 m drop-down): |
| `cs_esc_rename.cs` | Apply the agreed escapeway mapping: rename/relevel 4 NEW strings, write the final Replace_Plan, delete the 12 orphan tasks left on replaced parents. |
| `cs_ewd_probe.cs` | Read-only: every figure tied to the MPA 07 W6390 EWD (by Name/attribute, or text mentioning EWD), plus anything within |
| `cs_ewd_to_dsp.cs` | MPA 07: W6390 EWD -> W6390 DSP, moved MOVE m down the decline, 5.5 x 5.5, same azimuth and grade, 20.0 m long. |
| `cs_ewy_export.cs` | Read-only: export the EWY strings, the old EWY strings and all dev strings within 70 m of them, plus schedule dates per string. |
| `cs_ewy_levels.cs` | Display-only screenshots: long section of the top four EWY pieces, and plan views per level showing all development solids |
| `cs_ewy_paths.cs` | Read-only: walking routes along the dev centrelines from each W ACC / G MLA far end to the EWY (shortest path). |
| `cs_ewy_shots.cs` | Screenshots of the EWY solids in the current Deswik legend. Display-only: everything outside a box around the EWY is hidden for the capture. |
| `cs_fan_strips.cs` | Single and double fan strip standards with 5 m transitions (0 -> 1.5 m above backs), cloned from the existing |
| `cs_fan_strips_down.cs` | Single and double fan strip standards with 5 m transitions (0 -> 1.5 m above backs), cloned from the existing |
| `cs_fan_strips_grey.cs` | Recolour the 5 m transition panels on FAN STRIP TRANSITIONS light grey (pen and hatch). High sections stay as they are. |
| `cs_fan_strips_up.cs` | Single and double fan strip standards with 5 m transitions (0 -> 1.5 m above backs), cloned from the existing |
| `cs_fix_cuddies.cs` | Snap the cuddy outlines on STRIP STANDARDS to their labels: the opening on the drive wall stays put; the back wall is |
| `cs_fix_scheduled.cs` | Set SCHEDULED back to the old value where a current task maps to an old task that had SCHEDULED 0. |
| `cs_fix_strip_standards.cs` | Snap the STRIP STANDARDS outlines to their labelled sizes (plan dimensions), keeping position, bearing and right angles. |
| `cs_fix_w346ewr.cs` | W346 EWR - (string 45931DF): links from the drives at its top (W368 EWD W, 45933B9) and bottom (G350 DSP, 458EDDC) into cut #1, |
| `cs_grade_label_count.cs` | Read-only: CENTERLINES\GRADE texts per drive Name (text, position) and each drive's centreline vertex count / attributes |
| `cs_heading_shift.cs` | Read-only: headings whose first cut moved > 30 d vs old. For each: metres old/new, first-cut predecessors old vs now (with their dates), priority, ... |
| `cs_jbox_add.cs` | MPA 07: add a J/Box (jumbo box) strip on the decline's left wall (looking down), 20 m down the decline from the W6390 DSP, |
| `cs_jbox_check.cs` | Read-only: the MPA 07 J/Box strip(s) and label(s) on STRIPPING. |
| `cs_jbox_existing.cs` | Read-only: existing Design Master J/Box strips - the strip string next to each label, its attributes, and where it sits |
| `cs_jbox_probe.cs` | Read-only: the J/Box Strip standard (polylines near its label, with the nearest decline-like reference string), the |
| `cs_jumbo_probe.cs` | Read-only: the strip standard labels, the jumbo box standard(s) with their nearby polylines, and where the W6390 DSP |
| `cs_labels_like_user.cs` | Copy the user's adjusted label (Double Fan Strip (1:7 up)) to the other strip labels: same rotation and justification, |
| `cs_labels_top.cs` | Put each fan / truck strip label above its strip: centred along the strip, 1 m beyond the strip's top edge (the |
| `cs_layers_near.cs` | Read-only: every text within 60 m of MPA 07 whose text mentions Length, with layer, handle and type. |
| `cs_level_compare.cs` | Read-only: compare levelled schedule with the pre-replace snapshot (monthly physicals, heading first cuts, stope starts). |
| `cs_map2.cs` | Read-only: fresh map from current tasks to old tasks. |
| `cs_master_inspect.cs` | Read-only: describe the current selection, and every strip on STRIPPING (tagged rebuilt ones grouped by shared corners) |
| `cs_master_probe_sel.cs` | Read-only: the selected strip's shape, the centrelines near it (with profile and distance), and labels within 25 m. |
| `cs_master_strips_v3.cs` | Rebuild the four Design Master strips cleanly. Removes the panels generated earlier (tagged CLAUDE_DECLINE_STRIP), |
| `cs_mine_grid.cs` | New mine grid: LEVEL -> 6000 + LEVEL, and the level number in names (<letters><number>...) -> 6000 + number. |
| `cs_mpa07_cables.cs` | MPA 07: CB 02 cables at each take-off, laid out as the GSS sheet: 3 rows x 3 at 2 m, centred on the side drive's |
| `cs_mpa07_dec_extend.cs` | MPA 07: extend the MAIN DEC centreline (DESIGN DRAFT, CLAUDE_MPA07) through the solids added on 0\SKETCH LAYER\MPA 07 |
| `cs_mpa07_dec_label.cs` | MPA 07: update the MAIN DEC label length (and any length attribute on the tagged centreline) to the extended string. |
| `cs_mpa07_design.cs` | MPA 07 decline extension in the Level Design Drafter, from the cut solids on 0\SKETCH LAYER\MPA 07. |
| `cs_mpa07_ewd_trim.cs` | MPA 07: trim or extend one drive (DRIVE) at its far end so the centreline is 20.0 m (3D) along its last segment, grade kept; move the two end-face ... |
| `cs_mpa07_ext_probe.cs` | Read-only: MPA 07 decline centreline end, its walls, and the solids on 0\SKETCH LAYER\MPA 07 relative to the decline end. |
| `cs_mpa07_length_labels.cs` | MPA 07: set the "Length: x.xm" labels (0\DESIGN DRAFT\CENTERLINES\LENGTH) of MAIN DEC, W6390 RAD and W6390 EWD to the |
| `cs_mpa07_offset_probe.cs` | Read-only: for every MPA 07 sketch solid, the signed plan offset of its cut centre (_CPX/_CPY) and of its true |
| `cs_mpa07_one_grade.cs` | MPA 07: one grade per drive. G6390 EWD and G6390 RAD run at +1:50 from their take-off on the decline (the decline is |
| `cs_mpa07_rad_bt.cs` | MPA 07: W6390 RAD breaks through floor to floor into W390 RAD W (the scheduled drive, from its task solids on |
| `cs_mpa07_rad_bt_walls.cs` | MPA 07: finish the W6390 RAD walls at the B/T into W390 RAD W. Each RAD side wall runs to the W390 RAD W wall on the |
| `cs_mpa07_rebuild.cs` | MPA 07: north drive -> W6390 RAD, north-east drive -> W6390 EWD, both 5.0mW x 5.0mH SA. Walls rebuilt from the |
| `cs_mpa07_recentre.cs` | MPA 07: put the MAIN DEC centreline (DESIGN DRAFT, CLAUDE_MPA07) on the centre of the cut solids on |
| `cs_mpa07_rename_truck.cs` | MPA 07: north drive -> W6390 RAD, north-east drive -> W6390 EWD, both 5.0mW x 5.0mH SA. Walls rebuilt from the |
| `cs_mpa07_review.cs` | Read-only review of the MPA 07 draft (Level Design Drafter): every figure on 0\DESIGN DRAFT near the W6390 decline / |
| `cs_mpa07_solids_vs_dec.cs` | Read-only: MPA 07 sketch solids (0\SKETCH LAYER\MPA 07) - cut centre, azimuth, floor RL (lowest vertex near the centre) |
| `cs_mpa07_square_lobes.cs` | Level Design Drafter: (1) remove the angled-leg example added by mistake to the strip standards (everything in that |
| `cs_mpa07_trim.cs` | MPA 07, Design Master style: the EWD and RAD centrelines start on the decline wall (half the decline width off its |
| `cs_mpa07_walls_truck.cs` | MPA 07 (Level Design Drafter): cut the walls at the take-offs with 3 m fillets, Design Master style - open wall |
| `cs_new_attrs.cs` | Fix attributes on COMBINED 12.11\NEW strings and decide replace vs new, using the current task solids. |
| `cs_new_strings_check.cs` | Read-only: check strings on COMBINED 12.11\NEW — attributes, and which existing COMBINED 12.11 strings they overlap (replace) or not (new). |
| `cs_newstrings_apply.cs` | After Create Tunnels on COMBINED 12.11\NEW: map new tasks to the old tasks they replace (Replace_Plan2.tsv), copy settings + resources, |
| `cs_newstrings_orphans.cs` | Fix new tasks (on Replace_Plan2 strings) that have no predecessor: |
| `cs_orphans.cs` | Read-only: tasks that now have no predecessor but whose old equivalent had one. Also the old predecessors' names. |
| `cs_orphans2.cs` | Read-only: tasks that now have no predecessor but whose old equivalent had one. Also the old predecessors' names. |
| `cs_pit_face_probe.cs` | Read-only: what a face of the pit surface 4589044 looks like (type, properties), so RL on the surface can be found. |
| `cs_pmap_probe.cs` | Read-only: what the open Deswik window exposes about process maps - App members named *Process*/*Map*/*Document*, |
| `cs_polyface_api.cs` | Read-only: how a Figure wraps a Polyface (constructors / setters), and where solids already are in the drawing. |
| `cs_polyface_faces.cs` | Read-only: the types behind Polyface.VertexList / FaceList and their Add methods, and the raw face list of one strip solid. |
| `cs_probe_strip.cs` | Read-only probe of a strip polyline (vertices and attributes) |
| `cs_rad_annot_probe.cs` | Read-only: every non-polyline figure (text, leader, point, block) within 15 m of the W6390 RAD on DESIGN DRAFT layers, |
| `cs_rad_drop_vertex.cs` | MPA 07 W6390 RAD centreline: drop the leftover vertex at plan 6.99 m (old grade break) so the 1:8.8 is one segment. |
| `cs_rad_dropdown_level.cs` | MPA 07 W6390 RAD drop-down: put its top edge on the high section (same height above the decline as the high |
| `cs_rad_fillet_fix.cs` | MPA 07, W6390 RAD mouth at the joint with the existing decline: |
| `cs_rad_fillets.cs` | RAD mouth fillets made exactly 3 m x 3 m against the decline wall |
| `cs_rad_grade.cs` | Read-only: W6390 RAD centreline(s) - vertices, plan/3D length, segment grades, and the grade needed before a flat last 8 m. |
| `cs_rad_grade_dedupe.cs` | W6390 RAD: delete duplicate CENTERLINES\GRADE labels - a text (and the leader generated with it) identical in text and |
| `cs_rad_grade_labels.cs` | Read-only: every W6390 RAD figure on CENTERLINES sublayers (GRADE etc.) and every W6390 RAD centreline anywhere, |
| `cs_rad_grade_text.cs` | Read-only: the W6390 RAD's CENTERLINES\GRADE / CONTROL / NOTES figures - their kind (Is* flags) and any text, by reflection. |
| `cs_rad_mouth_probe.cs` | Read-only: wall strings within 12 m of the MPA 07 joint (existing MAIN DEC end), with every vertex near the joint. |
| `cs_rad_regrade.cs` | MPA 07 W6390 RAD: regrade the centreline to a constant grade up from its start (decline wall) and a FLAT m flat section to the end, start and end RL |
| `cs_rad_square.cs` | W6390 RAD take-off squared to the new decline (4588F43): the CL keeps its inner points v1/v2 (on the MPA 07 RAD |
| `cs_rad_straight.cs` | W6390 RAD straightened: keep start v0 and grade break v1 (both on the square take-off line), v2 = v1 + same flat length on |
| `cs_rad_strip_compare.cs` | Read-only: truck loading strip with a drop-down - the reference standards vs the MPA 07 DSP one, in a local frame: |
| `cs_rad_strip_std.cs` | MPA 07 W6390 RAD truck loading strip -> truck loading standard (1:7 down), as done for the DSP (cs_dsp_strip_std.cs): |
| `cs_rad_wall_check.cs` | Read-only: W6390 RAD walls against its centreline - each wall vertex's chainage, offset and height above the centreline, |
| `cs_rad_wall_end_probe.cs` | Read-only: what the RAD wall's left fillet end (8080.717,63114.799) touches - any polyline (not SOLIDS) passing within |
| `cs_rad_wall_rebuild.cs` | MPA 07 W6390 RAD drafted wall (DESIGN DRAFT\WALLS): rebuild on the current centreline (1:8.8 to the COG, flat after). |
| `cs_ramps_2p5.cs` | Shorten the end ramps (ramp up / ramp down along the drive) from 5 m to 2.5 m on the strips in the new STRIP STANDARDS |
| `cs_ramps_2p5_all.cs` | Shorten every 5 m ramp (end ramps and truck front ramps) to 2.5 m on the fan and truck transition layers and on the |
| `cs_ramps_len.cs` | Set every ramp (end ramps and truck front ramps) to TARGET m on the fan and truck transition layers and on the strip |
| `cs_rebuild_master_strips.cs` | Rebuild the four Design Master strips cleanly. Removes the panels generated earlier (tagged CLAUDE_DECLINE_STRIP), |
| `cs_replace_fill_rest.cs` | For new tunnel tasks still without a resource: copy planner fields + assignment from the nearest old cut on the same source string (no distance cap). |
| `cs_replace_master_strips.cs` | Replace the selected strips on 0\DESIGN MASTER\STRIPPING with the 8 m decline-following strip design. |
| `cs_replace_match.cs` | Read-only: match new tunnel tasks to old ones (same source string + ACT_TYPE, nearest solid centre), diff task fields, list missing dependencies. |
| `cs_replace_restore_deps.cs` | Re-add old dependencies onto the new tasks. Old->new task map: unchanged IDs, Replace_Map.tsv (tunnels), derived via their parent tunnel + ACT_TYPE. |
| `cs_replace_restore_fields.cs` | Restore planner inputs from the old tasks onto the matched new tunnel tasks (Replace_Map.tsv). |
| `cs_restore2.cs` | Restore task settings from old tasks onto mapped new tunnel + derived tasks (Map2.tsv): |
| `cs_schedule_snapshot.cs` | Read-only: export all IS tasks (every field) and all dependencies to C:\dswtmp. Nothing in Deswik changes. |
| `cs_sheet_update_truck.cs` | Update the two truck loading strips on the STRIP STANDARDS sheet (the 8 m and 10 m 1:50 up copies in the new band, |
| `cs_sortorder.cs` | Put tasks in the old schedule's order: SortId ranked by the mapped old task's SortId (ties by cut number). |
| `cs_square_lobes.cs` | Design Master: square the truck strip lobe and front ramp to the stockpile drive they open into. The lobe edge and the |
| `cs_starts_raise.cs` | MPA 07: put the W6390 DSP and RAD centreline starts on the decline grade (they sit 0.058 m below it). |
| `cs_starts_vs_decline.cs` | Read-only: W6390 DSP and RAD centreline starts against the decline - the decline centreline RL projected square to the |
| `cs_std_from_decline.cs` | Replace the 8 m 1:7 down / 1:7 up truck loading standards with the decline-following 8 m strip built by the decline |
| `cs_stope_cut.cs` | Cut stope solids by the scheduled development solids they overlap (CutFirst), replacing the mesh in place (handle + attributes kept). |
| `cs_stope_cut_test.cs` | Read-only test: on a copy of one stope (not added to the drawing), subtract its overlapping dev solids with CutFirst and TrimFirst; compare volumes. |
| `cs_stope_overlap.cs` | Read-only: for each LH_Stope task solid, find overlapping development (Tunnel) task solids and the overlap volume. |
| `cs_strip_narrow.cs` | Truck loading back strips rebuilt to the standard (8 m flat, 1.5 m high, 3.5 m ramps, drop-down = drive width - 0.5 m) |
| `cs_strip_solids.cs` | Level Design Drafter: solids of the strip standards on a NEW layer 0\REFERENCE\STRIP STANDARDS\SOLIDS, same place as |
| `cs_strip_solids_check.cs` | Strip solids layer: the faces were added with 0-based vertex numbers but Deswik's face list is 1-based, so shift every |
| `cs_strip_std_probe.cs` | Level Design Drafter, read-only: list everything on the strip standards layers - labels and closed panels |
| `cs_strip_vs_std.cs` | Read-only: the RAD-side and DSP-side back strips against the strip standards. Each strip set is shown in its drive's |
| `cs_sub_cables.cs` | G6390 SUB take-off (Level Design Drafter): add CB 02 cable bolts on 0\DESIGN DRAFT\CABLEBOLTS, laid out as the MPA 07 |
| `cs_sub_move_fillets8.cs` | G6390 SUB (Level Design Drafter): draft centreline 458948B moved MOVE m UP the Design Master decline 44BD9C0 (along the |
| `cs_takeoff_check.cs` | Take-off check for the strings in Replace_Plan2: for each string, find the cuts of OTHER strings that touch its first cut |
| `cs_takeoff_fix.cs` | Fix take-offs: (string handle of the new drive, handle of the drive it comes off). Junction cut = the drive's cut whose centre is nearest |
| `cs_takeoff_rule.cs` | Take-off rule: for every lateral tunnel string whose first cut sits on another lateral string (the parent) at junction cut j: |
| `cs_tasksolid_compare.cs` | Read-only: compare current SCHEDULER\TASK SOLIDS against the latest snapshot in C:\dswtmp. Writes a report TSV. Nothing in Deswik changes. |
| `cs_tasksolid_snapshot.cs` | Read-only: export every SCHEDULER\TASK SOLIDS figure's attributes to C:\dswtmp (TSV). Nothing in Deswik changes. |
| `cs_text_refresh.cs` | Refresh the display of the three MPA 07 length labels after their text was changed: call the figure's own |
| `cs_tidy_truck_layer.cs` | Tidy TRUCK LOADING TRANSITIONS into a grid. Strips = panels sharing vertices. Each strip's high-section length, grade |
| `cs_tl_compare.cs` | Read-only: truck loading strip with a drop-down - the reference standards vs the MPA 07 DSP one, in a local frame: |
| `cs_truck10_width6.cs` | Rebuild the straight 8 m truck loading standards (labels "8.0 mL", not "decline") with a 6.0 m wide high section (5.0 m drive + 1.0 m into the ... |
| `cs_truck8_width6.cs` | Rebuild the straight 8 m truck loading standards (labels "8.0 mL", not "decline") with a 6.0 m wide high section (5.0 m drive + 1.0 m into the ... |
| `cs_truck_10m.cs` | Rebuild the 10.0 m truck loading strip (1:50 up) with fixed dimensions: 5.0 m wide drive part, 5.0 m stockpile |
| `cs_truck_angled_leg.cs` | Strip standards (Level Design Drafter): truck loading strip into an angled 4.5 m leg, 1:7 down decline. 8 m high |
| `cs_truck_curved_6w.cs` | Rebuild the two curved (decline-style) 8 m truck loading standards on an ideal decline: 25 m radius arc at 1:7 (down / |
| `cs_truck_leg5_example.cs` | Strip standards (Level Design Drafter): truck loading strip into an angled 4.5 m leg, 1:7 down decline. 8 m high |
| `cs_truck_lobe.cs` | Truck loading strips with a stockpile opening, fixed dimensions: 5.0 m wide drive part, 5.0 m opening centred on the |
| `cs_truck_lobe_17.cs` | Straight 8 m truck loading strips at 1:7 down / 1:7 up: 5.0 m wide drive part, 5.0 m opening flush with the start of |
| `cs_truck_set.cs` | Fill out the truck loading set: replace the duplicate 10 m 1:50 up strip (along -24, far wall 49) with an 8 m strip in |
| `cs_truck_strips3.cs` | Second truck loading standard ("Stripping Truck Loading 7.2H x 5.5W x 8.0L", 5-point outline with a chamfered corner): |
| `cs_truck_strips3_mirror.cs` | Second truck loading standard ("Stripping Truck Loading 7.2H x 5.5W x 8.0L", 5-point outline with a chamfered corner): |
| `cs_truck_strips3_otherend.cs` | Second truck loading standard ("Stripping Truck Loading 7.2H x 5.5W x 8.0L", 5-point outline with a chamfered corner): |
| `cs_updated_vs_solids.cs` | Read-only: strings on COMBINED 15.11\UPDATED vs the scheduler. For each: task count, whether its centreline still sits in its own task |
| `cs_vs_addfields.cs` | Ventilation Plan Master: give 0\PRIMARY STATIONS\LABELS the station layer's attribute fields, trying the API calls in turn. |
| `cs_vs_attr_api.cs` | Read-only (Ventilation Plan Master): how to put attribute fields on a layer and set values on a figure. |
| `cs_vs_attr_probe.cs` | Read-only (Ventilation Plan Master): attribute names/values on the vent station polyline (via the document's attribute |
| `cs_vs_labels.cs` | Ventilation Plan Master: label every vent station (closed section polylines on 0\PRIMARY STATIONS) at the side of the |
| `cs_vs_probe.cs` | Read-only (Ventilation Plan Master): the PRIMARY STATIONS figures - kind (Is* flags), vertices/position, all attributes, |
| `cs_vs_run_attrs.cs` | Ventilation Plan Master: run the "Vent Station Labels" macro, then read back each label figure's text and attributes. |
| `cs_vs_state.cs` | Read-only (Ventilation Plan Master): stations on 0\PRIMARY STATIONS (id, vertices, closed) and the LABELS layer's attribute fields. |
| `cs_w390rad_rl_readback.cs` | Read-only: new strings against the backup (XY moved? Z change) and the W412 RAD attributes / vertices. |
| `cs_w390rad_rl_w412.cs` | W390 RAD strings 458905E / 458905F / 4589060: RL only (XY untouched) dropped by DZ so they sit on the W390 RAD W solid |
| `cs_w412_probe3.cs` | Read-only: (1) each vertex of 458905E/5F/60 against the W390 RAD W solid floor (CP chain, projected in plan) and the pit |
| `cs_w6390_list.cs` | Read-only: every polyline whose Name starts with W6390, by layer, with vertex count, attributes and plan extent. |
| `daily_report_dump.cs` | Read-only: rebuild the "Daily Report Dump" report by day for Nov 2026. Each row uses the same field and filter as the report view; |
| `decline_links.cs` | Read-only: list every heading whose name contains "DEC", with its round count, dates and resource, |
| `decline_rule.cs` | Read-only. the mine decline rule at each access, measured in decline rounds from the round that releases the access (UG Dev-74 link, round k): |
| `decline_tasks.cs` | Read-only: scheduled decline tasks (any MINE, ACT_TYPE DEC) starting 25/12/2026 - 15/02/2027, in start order. |
| `dev_dump.cs` | Read-only: lateral development metres (filter DEV_Lateral, Sch_metres) grouped by task Name for Nov 2026, Dec 2026 and Jan 2027, |
| `ewr_breakthrough.cs` | For every H EWR heading: the lowest EWR solid (the bottom lift), the scheduled development solid nearest the rise bottom |
| `export_report.cs` | Read-only: export the active IS cross-tab report (monthly, Nov 2026 - Nov 2027) to a text file for the Physicals workbook. |
| `export_view.cs` | Export one IS cross-tab report view (rows x months) to C:\dswtmp\view_<n>.tsv using the report's optional-view override, then clear the override. |
| `figure_to_task.cs` | Read-only: for the CAD figure with handle in C:\dswtmp\find_text.txt, print key attributes and the schedule task(s) it belongs to. |
| `filter_mine_ab_sep26_jan27.cs` | Add/refresh task filter "Claude - MA+MB Sep26-Jan27": MINE = W or MB*, SCHEDULED = 1, task active any time 01/09/2026 - 31/01/2027. |
| `find_reports.cs` | Read-only: find report / indicator definitions held by the schedule. |
| `find_target_start.cs` | Read-only: find where the schedule target start / status date lives. |
| `find_text.cs` | Read-only: find tasks whose Id, UniqueId, Name or any field contains the text in C:\dswtmp\find_text.txt (case-insensitive). |
| `h_accesses.cs` | Read-only: every scheduled heading on the H side that is not the decline. For each, its first task (no same-name |
| `handle_range.cs` | Read-only: figures with handle between the two hex handles in C:\dswtmp\find_text.txt ("lo,hi"), grouped by MINE/LEVEL/ACT_TYPE, |
| `handle_tasks_by_id.cs` | Read-only (MINE_UG_FY27): for each handle in HS, the figure's ID attribute -> its task (rate, resource, dates) and the |
| `handles_tasks.cs` | Read-only: for each handle in C:\dswtmp\find_text.txt (comma separated), print the figure's attributes and its task, |
| `heading_metres.cs` | Read-only: lateral development metres (DEV_Lateral, Sch_metres) by task Name and MINE for Nov 2026 - Jan 2027, |
| `inspect_crosstabs.cs` | Read-only: list cross-tab report views and report-data methods that could produce or export their values. |
| `inspect_days_scale.cs` | Read-only: compare the Days and Months time scales and list TimeScaling methods. |
| `inspect_filter_fields.cs` | Read-only: find schedule-level caches that hold compiled task filters. |
| `inspect_report_data.cs` | Read-only: show the structure of the IS report data (Report tab) and the current report view. |
| `inspect_view5_rows.cs` | Read-only: show how each row of the "Daily Report Dump" report view is defined. |
| `list_crosstab_views.cs` | Read-only: list the cross-tab (report) views with their row counts and first row names. |
| `list_filters.cs` | Read-only: list task filters with their items and where clauses, and the active one. |
| `list_timescalings.cs` | Read-only: list report time scalings and how the active one is chosen. |
| `may_metres_by_jumbo.cs` | Read-only: May-2027 development metres (Sch_metres, proportional by time) by resource and filter group, |
| `mla_compare.cs` | Read-only: for every MLA heading (W/G side and H side) show resource, priority, constraint and every link of its first round |
| `mla_rule.cs` | Read-only. the mine decline rule at each access, measured in decline rounds from the round that releases the access (UG Dev-74 link, round k): |
| `od_compare.cs` | Read-only: every ore drive heading (ACT_TYPE OD): first round, priority, links to other headings on the first round |
| `od_priority.cs` | H ore drives (ACT_TYPE OD, name starting H) released by an MLA, or by an OD that is itself MLA-fed: set every round to Priority 1. |
| `path_between_handles.cs` | Read-only (MINE_UG_FY27): tasks of handles A and B (via the graphic's ID), and the successor path between them (either way). |
| `phys_export.cs` | Export 3 report views for the November physicals (active filter SCH =1; restores active report; no schedule data changed) |
| `probe_dep_res.cs` | Read-only: show the members of dependency and resource-assignment objects, and how MA Jumbo #1 is assigned on one task. |
| `refresh_slide_filters.cs` | Tell Deswik the five "3MS S1x" filters were edited (Schedule.UpdateFilter, as the filter dialog does), then recount them. |
| `report_filter_info.cs` | Read-only: which task filter is active, and which filter the report views use. |
| `reverse_44CC6AB.cs` | Reverse string 44CC6AB (W390 RAD W, rounds #1..#16) so it mines #16 -> #1 uphill, add 44CC65D #21 -> 44CC6AB #16, |
| `revert_decline_rule.cs` | Undo decline_rule.cs: delete every link listed as "ADD hold:" / "ADD jstp:" in C:\dswtmp\h_dec_links_added.txt ("... [fromId] -> ... [toId]"). |
| `review_rad_overlaps.cs` | Read-only (MINE_UG_FY27): W390/W412 RAD W cut span and resources, and whether MA / MC Jumbo #1 have overlapping tasks |
| `rise_breakthrough.cs` | Read-only: for the rise named in C:\dswtmp\find_text.txt (e.g. "H360 EWR"), list its tasks with links, the rise solid's bottom, |
| `selected_figure_info.cs` | Read-only: show the selected CAD figure(s) - layer, handle/id and all attribute values - and search figures on TASK SOLIDS for "44A92F2". |
| `set_resource_path.cs` | Set PhysicalResource to the resource named on line 2 of C:\dswtmp\path_ends.txt for every task on the successor path |
| `set_target_start_27oct.cs` | Move the schedule target start from 16/10/2026 06:00 to 27/10/2026 06:00 (11 days). Nothing else changes. File not saved. |
| `set_target_start_28oct.cs` | Move the schedule target start from 27/10/2026 06:00 to 28/10/2026 06:00 (1 day). Nothing else changes. File not saved. |
| `slide_filter_names.cs` | Read-only: write the task names in each "3MS S1x" filter to C:\dswtmp\slide_filter_names.tsv (filter, name). |
| `slide_filters.cs` | Add/refresh one task filter per 3MS deck level slide (13-17): Name = <each task name in that picture>, joined with Or. |
| `split_44CC6AB.cs` | String 44CC6AB (W390 RAD W #1..#16), currently all mined up #16 -> #1 on MA Jumbo #1. Change to: |
| `stope_order.cs` | Read-only: for each figure handle in C:\dswtmp\find_text.txt, list the tasks tied to it (same Description/MINE/LEVEL/ACT_TYPE |
| `task_path.cs` | Read-only: shortest successor path between the two task Ids in C:\dswtmp\path_ends.txt ("fromId,toId"), |
| `view_row_names.cs` | Read-only: write every report view's row names to C:\dswtmp\view_names.tsv (view index, view name, row name). |
| `w390_44CC6AB_revert.cs` | Undo w390_44CC6AB_to_ma.cs: put the 10 string-44CC6AB cuts back to the assignment saved in |
| `w390_44CC6AB_to_ma.cs` | W390 RAD W cuts from string 44CC6AB, uid 9896-9900 and 9907-9912: MC Jumbo #1 -> MA Jumbo #1, rate and units kept. |
| `w390_44CC6B5_to_mc.cs` | W390 RAD W cuts uid 9901-9906 (handles 44CC6B5..44CC6BF): MA Jumbo #1 -> MC Jumbo #1, rate and units kept. |
| `w390_link_check.cs` | Read-only (MINE_UG_FY27): every attribute on the 44B8F36..44B8F4C task graphics whose name or value looks like a task link |
| `w390_rad_ma_50.cs` | W390 RAD W cuts uid 9843-9853 (handles 44B8F38..44B8F4C): MC Jumbo #1 -> MA Jumbo #1 at a 50 m/mo task rate |
| `w390_rad_range.cs` | Read-only (MINE_UG_FY27): figures with handles 44B8F36..44B8F40, each one's task (matched on MINE/LEVEL/ACT_TYPE and |
| `w390_rad_to_ma.cs` | W390 RAD W cuts uid 9843-9847 (handles 44B8F38..44B8F40): MC Jumbo #1 -> MA Jumbo #1, rate and units kept. |
| `w390_rad_to_ma_b.cs` | W390 RAD W cuts uid 9848-9853 (handles 44B8F42..44B8F4C): MC Jumbo #1 -> MA Jumbo #1, rate and units kept. |
| `w390_rate60.cs` | W390 RAD W cuts on the path 44B8F32 -> 44CC6B3 (uid 9840-9853, 9896, 9898-9900) to a 60 m/mo task rate: |
| `w390_rate70.cs` | W390 RAD W cuts 44B8F32 -> 44B8F38 (uid 9840-9843) to a 70 m/mo task rate: |
| `w412_w390_rate50_reorder.cs` | The RAD cuts from 44ABDAC down to 44B8F38 go to a 50 m/mo task rate (Rate/TaskRate 50S_m/mo, MA Jumbo units 0.125): |

## excel/

| Script | What it does |
|---|---|
| `build_nov_physicals_ab.ps1` | New MINE_UG_3MS_FY27_NOVEMBER_26_Physicals.xlsm (MB + SITE only) = copy of the approved November workbook with the four dump tabs |
| `check_open_workbooks.ps1` | Read-only: compares the open Physicals and MC+MD tracking workbooks against a fresh Deswik export (%TEMP%\dsw\phys_*.tsv, |
| `lom_by_fy.ps1` | READ ONLY: LOM physicals by financial year (Jul-Jun) from an extracted IS .dsf (tmp.dxf Tasks section): development metres, |
| `lom_monthly.ps1` | READ ONLY: monthly LOM physicals from lom_tasks.tsv (written by lom_by_fy.ps1): lateral dev m, stope t, total t, haul tkm and the |
| `refresh_open_physicals.ps1` | Refreshes the dump tabs of the Physicals workbook that is OPEN in Excel (no open/save/close) from the latest Deswik export: |
| `refresh_open_tracking.ps1` | Rewrites the Nov 3MS block (AQ:AX = Nov-26..Jun-27) on 'Actuals and 3MS' in the OPEN MC+MD tracking sheet from the OPEN |
| `xlsx_dump.ps1` | READ ONLY: dump every non-empty cell (values, plus formulas when -formulas) and cell comments of an .xlsx without opening Excel. |

## powerpoint/

| Script | What it does |
|---|---|
| `deck_charts_open.ps1` | Slides 8-11 (IDs 453-456) of the OPEN November deck: replace the chart picture with a PNG export of the matching chart on |
| `deck_charts_vector.ps1` | Slides 8-11 (IDs 453-456) of the OPEN November deck: replace the chart picture with a vector (enhanced metafile) copy of the |

## ventsim/

| Script | What it does |
|---|---|
| `rad_to_surface_dxf.ps1` | Build a Ventsim import DXF (R12, 3D polylines) from the W390 RAD string in 'RAD Decline draft.dcf' (decoded to rad_strings.json): |
| `vsm_add_rad.ps1` | Make a NEW Ventsim model = base .vsm + the W390 RAD string (RAD Decline draft.dcf, decoded to rad_strings.json) with a forcing fan |
| `vsm_rad_primary.ps1` | Make a NEW Ventsim model = Mine Model.vsm + the W390 RAD as the primary exhaust, set up like the reference primary system: |
| `VsmBuild.cs` | New Ventsim model = existing tunnels of a reference .vsm (no bags, fans, tie links; one stage) + the COMBINED 12.11 DXF drives, |
| `VsmFromDxf.cs` | Build Ventsim airways from the COMBINED 12.11 DXF (3D polylines, layer = ACT_TYPE). |

## tools/

| Script | What it does |
|---|---|
| `deswik_pipe.ps1` | Call a tool on the Kennet.DeswikClaude plugin running inside Deswik, through its local named pipe (one JSON line each way, |
| `DsfReports.cs` | Rebuild Deswik IS crosstab report rows from an unzipped .dsf (no Deswik needed). |
| `dxf_near.ps1` | READ ONLY: for each DXF, report the extents and the points within $r m (plan) of X,Y, grouped by layer with Z range. |
| `fan_strip_dcf.ps1` | Build a small new .dcf holding single and double fan strip standards with 5 m transitions, on the layer |
| `pdf_obj_images.ps1` | READ ONLY: save PDF image objects by object number (JPEG as-is, Flate RGB/Gray to PNG) and build a labelled contact sheet. |
| `pdf_page_images.ps1` | READ ONLY: save the images drawn on PDF pages whose text contains "Page <n>" (footer), JPEG as .jpg, Flate RGB/Gray as .png. |
| `pdf_pages.ps1` | Render PDF pages to PNG with the Windows built-in PDF API (no extra tools), then tile them into contact sheets. |
| `pdf_text_search.ps1` | READ ONLY: inflate every FlateDecode stream in a PDF, pull the text-showing operators out, and print hits for a regex with context. |
| `read_dcf_strings.ps1` | READ ONLY: list every string in an extracted Deswik .dcf (VectorDraw XML) with layer, key attributes and decoded vertices. |
| `vdcl_layer_entities.ps1` | READ ONLY: stream a gunzipped Deswik .vdcl/.dcf XML and write out the full XML of every entity on the given layer handles |
| `vdcl_layers.ps1` | READ ONLY: stream a gunzipped Deswik .vdcl/.dcf XML (too big to load) and list the layers (handle, name, every simple property), |
