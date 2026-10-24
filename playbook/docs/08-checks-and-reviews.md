# 08 – Checks and reviews

## Always do these before calling a job done

1. **Read back** what was written (values, formulas, text) – don't trust "no error".
2. **Tie totals** across places that should agree (report ↔ workbook ↔ deck ↔ charts).
3. **Labels vs data:** values must sit against the right row label; blank rows stay blank.
4. **Units and periods:** month boundaries (midnight-based), FY vs calendar year, ex GST.
5. **Saved flag:** report whether the file is saved. Remind the user to save.

## Deck discrepancy review (method used)

1. Extract every slide's text, tables and pictures to a text file (read-only).
2. Cross-check numbers against the physicals, the task export and the tracking sheet.
3. List: what ties (✓), and a table of discrepancies with *where / issue / suggested fix*.
4. **Change nothing** – the engineer decides.
5. On re-check, diff the extraction against the previous one to show what changed.

Typical findings: placeholder text ("x metres"), ranges that ignore a low month, comments that contradict the schedule (service hole dates, a stop that is not modelled), names that differ between slides for the same heading, fleet table vs workbook, stale titles (last month's name on this month's deck), chart showing plan vs actual.

## Schedule checks

- After a link/constraint change: read back the predecessors and dates; re-level only when asked; compare key outputs (ounces, metres by month) before vs after.
- Mine-wide pattern check: one bad task often means a pattern (all raise SLOTs had no predecessor).
