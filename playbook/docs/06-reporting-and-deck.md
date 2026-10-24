# 06 – Reporting: tracking sheet and 3MS deck

## Monthly tracking sheet (`MINE UG_Monthly Tracking Sheet Budget FY27` in the scripts)

- Tab **Actuals and 3MS**: summary E:P = `IF(actual>0, actual, 3MS)`; actuals block W:AH; **3MS input block AM:AX** (typed values, Jul–Jun); labels in AK.
- **Updating the 3MS:** write the new 3MS into AM:AX only for months it covers (e.g. Nov–Jun from the November 3MS), label row 2/3 "Nov 3MS", leave past months. Map rows by label (not position) – see `tracking_*.ps1`.
- **Show the month plan instead of part-month actuals** (e.g. the current month before month end): set that column's summary formulas to `=AP{row}` and relabel "Planned"; actuals stay untouched in the actuals block.
- **Equipment and KPI rows** (layout copied from another site's sheet): Number of Jumbos / Metres per Jumbo, Longhole drills / Metres per drill, Loaders / Tonnes per loader, Trucks / TKM per truck. Counts go in the 3MS block (from the 3MS *Planned Equipment* tab) with the same `IF(actual>0,…)` formula in E:P; KPIs are `IFERROR(total/count,0)`; column D total = `ROUNDUP(AVERAGE(counts),0)` and `total/SUM(count months)`. Peach fill `RGB(252,213,180)` to match.
- Inserting whole rows keeps all blocks aligned and other tabs' references follow – check for INDIRECT/OFFSET first.
- Many `#REF!` cells on other tabs (Weekly Summary, YTD Tracking…) were **pre-existing** – formulas literally contain `#REF!`. Not caused by row inserts.

## YTD charts into the deck

- Charts are on **YTD Graphs vs Budget**. Export as images (`Chart.Export(png)`), insert at the same position/size, delete the old picture (`deck_ytd_charts.ps1`). Hidden Excel instances refuse CopyPicture – export works.
- **Charts went blank after changing the date headers** because the category (date) axis had a fixed min/max. Fix: `Axes(1).MinimumScaleIsAuto = True; MaximumScaleIsAuto = True`.

## 3MS deck (PowerPoint)

| Slide type | What was automated |
|---|---|
| Comments (slide 2) | Development, ore drives, ounces, fleet lines from the physicals; grammar pass in the engineer's own words |
| 12-month table | Filled from 12 Month Physicals by matching row labels |
| Fleet table | From the 3MS *Planned Equipment* tab (0 shown as "-") |
| Level slides | Right-hand "Level comments": **monthly metres only** (`G6390 MLA: Nov 77 m;`), next three months, heading names bold, 12 pt, clear of the diagram |
| SWOT | New points from the slide comments added **in red**; body shrunk to fit (9 pt) |
| Procurement | Plain table of pending items (Item / Qty / CER / PO / Status) from the procurement schedule |
| Chart slides | YTD chart images |

Tips:
- The engineer edits the deck at the same time – **find slides by SlideID and paragraphs by text**, never by index.
- Replace only the text after a label's colon to keep label formatting.
- `InsertAfter` on a paragraph can merge lines – read back and fix.
- Check text boxes don't overlap other shapes (compare bottom vs the diagram's top).
- Unsaved deck changes are lost if PowerPoint closes – remind the user to save.
