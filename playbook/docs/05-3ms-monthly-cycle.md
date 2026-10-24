# 05 – 3MS monthly cycle (physicals workbook)

## Inputs

- The levelled schedule (`MINE_UG_FY27.dcf` in the scripts), with the right **task filter** active (e.g. `SCH =1` = `SCHEDULED=1`, all mines; or a filter for a subset of mines) and the **date-range filter off**.
- Last month's physicals workbook (`MINE_UG_3MS_FY27_<MONTH>_26_Physicals.xlsm`) as the template.

## Steps

1. **Check the filter** (read-only script): active filter name, conditions, date-range on/off.
2. **Export the report views** (`export_physicals_reports.cs`):
   - *Schedule Export 01 - Underground* – 13 months → Deswik 3MS Dump tab (I2:U199)
   - *Daily Report Dump* – 30 days → For Daily Report tab (D1:AG36, labels kept)
   - *Blend Forecast* – 12 months → Grade Bins tab (A2:P54)
3. **Export the task table** (`tasks.tsv`) and build the **Dev Dump**: lateral metres per heading for the first three months, prorated by time across month boundaries, `SCHEDULED=1`, `LAT_VERT=LAT`, `Sch_metres`. **Check the totals equal the report's "Lateral Dev – metres"** (they matched exactly, to two decimals).
4. **Build the workbook** (`build_nov_physicals.ps1` / `_refresh.ps1`): copy the template, fill the dump tabs, roll headers, refuse to overwrite an existing file. To keep earlier fixes, rebuild **from the last version of the same month**, not from the previous month's file.
5. **Recalculate and check**: 12 Month Physicals and 3 Month Schedule lateral must equal the report; count formula errors (and know which ones were inherited from the template).
6. Back up the previous version before replacing it.

## Fixes made to the template (carry them forward)

- **Own-month offset:** ounces / development grade rows on 12 Month Physicals referenced the wrong month column (dozens of formulas fixed).
- **Decline double count:** the main CAPEX decline line already included a second decline, so the main decline = CAPEX decline − second decline (e.g. `='Deswik 3MS Dump'!I33-'Deswik 3MS Dump'!I34`).
- **Total UG Haulage tkm** comes back 0 from the export (formula row) → filled as Ore & LG tkm + Waste tkm, with a cell comment.
- **Cost centre:** some decline cuts had `COST_CENTRE = O`; corrected to `C` so they report as CAPEX.

## Grade Bins – watch out

The Blend Forecast export once returned **other rows' figures for every month after the first** (ounces in "All ore – Tonnes", heading counts in "Dev ore – Tonnes", numbers in blank rows). A fresh export fixed it. Always:

- compare a few Grade Bins values with the 12 Month Physicals (all-ore tonnes, ounces, first stope month);
- keep blank-title rows empty;
- fill the four **G/T** rows (HG, MG, LG, MG 0.5–1) as ounces × 31.1035 ÷ tonnes – they have no field in Deswik, so the export returns 0;
- check every column A label matches the Deswik row title.

## Numbers to sanity-check every month

| Check | Should match |
|---|---|
| Dev Dump total per month | Report "Lateral Dev – metres" |
| Sum of heading metres on the level slides | Total lateral on the 12-month table |
| OPEX ore drive line | Sum of the level OD lines |
| Total ounces | Dev ounces + stope ounces; first stope month |
| Decline chart | Each decline counted once (check whether one CAPEX line already contains another decline) |
| Fleet | Within the assumptions (m/jumbo, tkm/truck, t/loader, m/LH drill) |
