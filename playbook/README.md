# AI-Assisted Mine Planning Playbook

Prepared by the mine planning engineer with AI assistance (Claude), October 2026.
Treat everything here as a working draft: check it before you rely on it, and follow your company's policy on using AI.

## What this is

Over two weeks in October 2026 an AI assistant (Claude, in the Claude desktop app) worked alongside an underground planning engineer on a monthly 3MS (three-month schedule) cycle and related work. This folder captures **what worked, how it was done, the scripts that did it, and the mistakes to avoid**, so the same work can be done faster and more safely next time.

Work covered:

| Area | Examples |
|---|---|
| Deswik IS scheduling | Priorities, start constraints, take-off links, levelling, task filters for screenshots, finding why a task starts too early |
| Deswik CAD design | Decline loop options, centreline attributes, fan chamber flat section, scheduler attributes |
| 3MS documentation | Monthly physicals workbook, Dev Dump, Grade Bins, 3MS deck (data slides, level comments, SWOT, procurement), discrepancy review |
| Reporting | Monthly tracking sheet (3MS block, equipment/KPI rows), YTD charts into the deck |
| Approvals / documents | CERs (level power infrastructure, portal fan), secondary ventilation survey memo, HRA feedback, draft emails |
| Daily tools | Weekly plan compliance comments, OneNote handover updates |

## Start here

1. [docs/01-how-we-work.md](docs/01-how-we-work.md) – the rules (approvals, never save unasked, decision support only).
2. [docs/02-setup.md](docs/02-setup.md) – what you need installed and how the AI talks to Deswik and Office.
3. Pick the workflow you need:
   - [docs/03-deswik-scheduling.md](docs/03-deswik-scheduling.md)
   - [docs/04-deswik-design.md](docs/04-deswik-design.md)
   - [docs/05-3ms-monthly-cycle.md](docs/05-3ms-monthly-cycle.md)
   - [docs/06-reporting-and-deck.md](docs/06-reporting-and-deck.md)
   - [docs/07-documents-cer-memos.md](docs/07-documents-cer-memos.md)
4. Before you trust a number: [docs/08-checks-and-reviews.md](docs/08-checks-and-reviews.md).
5. Read [docs/09-lessons-learned.md](docs/09-lessons-learned.md) – every mistake made and how to avoid it.
6. Reusable scripts: [scripts/README.md](scripts/README.md). Example prompts: [docs/10-prompt-examples.md](docs/10-prompt-examples.md).

## Folder layout

```
README.md
docs/          guides, one per topic
scripts/
  deswik/      C# bodies for the Kennet.DeswikClaude run_csharp tool (+ dcall.ps1 pipe client)
  excel/       PowerShell + Excel COM: physicals, tracking sheet, CERs
  powerpoint/  PowerShell + PowerPoint COM: 3MS deck updates
  word-onenote/ PowerShell + Word / OneNote COM: memos, design standard, handover
```

## Important limits

- Scripts are **worked examples**. File names, folders, CAD handles, task IDs and row numbers are placeholders or examples from one schedule (`MINE_UG_FY27.dcf`, `<SHARE>\...`, `<DOCS>\...`); text and figures in documents are `<placeholders>`. Read and adapt them, do not run them blind.
- Nothing here replaces engineering judgement, site procedures, or approval by an authorised, competent person. Schedules, designs, CERs and risk assessments produced with AI are **drafts** until reviewed and signed off.
