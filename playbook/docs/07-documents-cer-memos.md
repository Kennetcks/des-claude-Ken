# 07 – Documents: CERs, memos, risk assessments, emails

## CER (company capital expenditure request template)

- Sheets are protected but the **input cells are unlocked** – write only those; never remove a password. Formatting (merging cells) needs the owner to unprotect.
- Fill: Cost Estimation B/C/D/F (E = D×C), Overview title, dates, explanation (A72), description (A82), cost components (A88:G97 → `='Cost Estimation'!E7` …), cash flow (C37…C59, one per month), Introduction (B6), Background (B26), Solution (C5), Alternatives (C21).
- Every total must tie: Cost Estimation E41 = capital requested C11 = cash flow total C65 = components G99.
- **Budget status**: if unbudgeted, say so in the explanation and keep "In budget? No".
- Read quotes from the PDF (render to image); list quote number, revision, date, validity, ex GST, ex works, lead time, "budget pricing only". Flag what the quote excludes (freight, installation, starters).
- When the quote is revised, update **values only** if asked, and flag text that now mismatches (part numbers, revision).
- Cash flow by month the spend happens, not when the item is used.

Examples: level power infrastructure (one line per item and level, from the per-level planning sheet, comments carried into Cost Estimation F) and a portal fan (spare fan + container/skid/frame for a new portal).

## Secondary ventilation survey memo

Built from another site's template: header table, Introduction, Results (readings table from the survey Excel: station, WB/DB, area, velocity, quantity, gases), Summary (conclusions), Comparison (first survey = baseline), Recommendations (actions), photos as labelled figures, Sign-off. Sections that only applied to the template's site were removed. Names of people not known → roles, not invented names.

## Risk assessment feedback (HRA)

Useful review points (from a reactive ground HRA review):
- Consequence column should be the **outcome** (fatality, toxic exposure), not the mechanism.
- Show **inherent and residual** risk.
- If test results are pending, state the ground is **treated as reactive** meanwhile and link to any engineering hold.
- Make controls measurable (max sleep time, testing frequency, sulphide trigger).
- Consider hole temperature checks, warning signs and response, emergency response, named gases (SO₂, H₂S, NOx, CO), critical controls and their verification.
- Every action needs an owner and due date.
Final ratings and controls are for the competent team, not the AI.

## Emails and handovers

- Draft emails in chat; never send. Tone: plain, professional, flag uncertainty clearly ("draft", "TBC", "high risk").
- OneNote handover: add updates **under** the matching item in red ("UPDATE dd/mm:"), keep the original text, back up the page XML first.
- Weekly plan compliance: comments = the **variance reasons** from the Daily Report compliance log (on the DS row when the log is per day); leave metres to the engineer.
