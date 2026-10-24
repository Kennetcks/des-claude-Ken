# 03 – Deswik Interactive Scheduler work

## Planning rules used (engineer's rules)

These were applied in one FY schedule. Confirm with your own site rules.

- **Decline take-off rule:** the decline advances ≥ 10 m (3 cuts at 3.5 m) past an intersection, then waits while the new heading does its cable stop plus one cut, then the decline continues. Implemented as **two FS links per take-off**: decline cut *k+3* → heading `JSTP_CBL`, and heading cut #1 → decline cut *k+4*.
- **Nothing starts on a heading before the drive reaches it.** Raises (RAR), slots (SLOT) and LH drilling must be linked to the drive cut at the raise.
- Decline 1:7, 22 m radius stacked loops; perpendicular take-offs with ≥ 10 m straight; accesses about 1:50; RAR ≥ 10 m from access/ISP/OD; ore drive ≥ 1 m past the last stope.
- **Start-No-Earlier-Than (SNET) holds work; priority does not.** A started heading keeps its jumbo, so a low priority won't stop it. Use SNET to hold a heading to a date.
- Holding only the first cut of a heading (SNET on #1, cleared on the rest) delays the heading without delaying unrelated work.

## Common jobs and how they were done

| Job | Method |
|---|---|
| Change priority / constraints | `is_set_task_fields` with `{task_id, field, value}`; constraint values `Start No Earlier Than` / `As Soon As Possible`; dates as `"1 July 2027 06:00"` (ISO dates were misread) |
| Add links | `is_add_dependencies` `{from_id, to_id, link_type:'FS', lag}` |
| Remove a link | `run_csharp`: find `Schedule.Dependencies` where `From`/`To` match the tasks' `UniqueId`, then `Schedule.Delete(dep, false, false)` |
| Re-level | `is_level_schedule` – only when asked; it moves dates mine-wide and takes minutes |
| Why does task X start early? | Find the task from its CAD solid handle (`ID` attribute), list `PredecessorDetails` with each predecessor's finish, compare with the drive cut it should follow (`scripts/deswik/why_task_starts_early.cs`) |
| Mine-wide check | Scan all tasks of a type for missing predecessors (e.g. every raise SLOT with no predecessor) before fixing one |
| Export tasks | Task table to TSV with ~115 fields via `GetNullaleFieldFullValue` (see `scripts/deswik/`) |
| Export reports | Switch the active report view by reflection, `GetReportData()`, `GetRowTotal(row, DateRangeF)` per period, restore the active view (`export_physicals_reports.cs`) |
| Filters for screenshots | Create a named task filter by reflection, write its `where` clause, `SetAsActiveTaskFilter(name)`, refresh with `HandleFiltersUpdated()` (`filter_heading_*.cs`) |

## Useful filter syntax (where clause)

```
(NumericCustomField["SCHEDULED"]=1) And ((Name="G390 MLA -") Or (Id="<task id>"))
(CustomField["DIS Parent GUID"]="<guid>")          -- a whole string (e.g. the whole decline)
IsMatch(Name, "^G370.*$")                           -- all tasks of a level
```

After setting the clause: `recalculateWhereClause = false`, `SetAsActiveTaskFilter(name)`, refresh the filter cache, `HandleFiltersUpdated()`. Always tell the user which filter was active before (to switch back).

## Gotchas

- `is_list_fields` may wrongly say "no schedule" – use `Schedule.GetTaskFields()` via run_csharp.
- Setting `Constraint` resets `ConstraintDate` to the task start – set both.
- Report data and filter membership only refresh after a re-level or a filter-cache update (`matchingFilterCache.Update()`).
- The date-range filter (`m_taskFilterDateRangeActive`) silently cuts months from exports – check it is **off** before exporting; it can switch back on after a crash.
- **Unsaved schedule changes are lost if Deswik crashes** – after an important change + re-level, save.
- `Schedule.ScheduleCalculator.CalculateSchedule(...)` in scripts cleared a manual SNET twice – avoid calling it.
- Never bulk-dump all getters on levelling objects by reflection – it broke the levelling window once.
