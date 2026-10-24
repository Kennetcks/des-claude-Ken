# 09 – Lessons learned (mistakes and fixes)

Every item below actually happened.

## Data and logic

| What went wrong | Fix / rule |
|---|---|
| Grade Bins filled with other rows' numbers after month 1 | Re-export; spot-check against the 12-month table; never write an export without a check |
| A second decline counted twice in the 12-month table | Main decline = CAPEX decline − second decline |
| Ounces/grade rows pointed at the wrong month | Check one month's formula against the dump by hand |
| Date-range filter cut months off the export | Check the filter is off before exporting (it came back on after a crash) |
| Charts went blank after changing dates | Axis min/max were fixed – set to automatic |
| A task appeared to start before its drive | It had no predecessor (ALAP floated it) – scan for the pattern mine-wide |
| Links lost after Deswik crashed | Re-check, re-apply, re-level, then **save** |
| Wrong Deswik window answered | Filename guard + retry in every script |

## PowerShell / COM gotchas

| Symptom | Cause | Fix |
|---|---|---|
| Value written to the wrong row | `[ordered]@{ 5 = … }` – an integer key indexes an OrderedDictionary **by position** | Use string keys (`'5'`) and cast `[int]` when using as a row |
| `Unable to cast Int32/Double to String` setting `.Value2` | PowerShell-wrapped numbers from the pipeline | Cast explicitly: `.Value2 = [double]$v` |
| 2-D array read became flat | A helper function returned the array and PowerShell unrolled it | Read `Range.Value2` directly, not through a function |
| `Specified cast is not valid` on `Font.Size` | int vs single | `[single]$size` |
| Script fails to parse (`missing terminator`) | Script saved UTF-8 without BOM, with °, ², • characters | Save `.ps1` as **UTF-8 with BOM** |
| `Test-Path` false / file not found under `%TEMP%\claude\…` | Path over 260 characters | Copy with `\\?\` prefix to a short folder first |
| `0x800AC472` / `RPC_E_CALL_REJECTED` | Excel busy (user typing or clicking) | Retry loop with short sleeps |
| `$t` became `1` | Retry helper used `$t` as its loop variable – same name as the caller's | Use unique variable names in helpers |
| Variables collide (`$s` and `$S`) | PowerShell names are case-insensitive | Use distinct names |
| `-like '*[x]*'` misses | `[` is a wildcard in `-like` | Use `.Contains()` |
| Word paragraphs merged | Replacing a paragraph range including its mark | Move the range end back one character first |
| Word Find/Replace fails | Replacement over 255 characters | Set the range text directly |
| Merged cell write error | Writing to a non-anchor cell | Write to `MergeArea.Cells(1,1)` |
| Protected sheet change silently ignored | Sheet has a password | Don't bypass – ask the owner to unprotect |
| PowerPoint inserted text joined two lines | `InsertAfter` on the last paragraph | Read back, split at the join |
| PDF tool can't render | No poppler on the PC | Use `Windows.Data.Pdf` via PowerShell |
| New Outlook can't be read | olk.exe has no COM | Paste the email or use classic Outlook |

## Working-style lessons

- Ask one clear question when scope is genuinely ambiguous (e.g. what a short name for a set of mines means, which base file); otherwise take the sensible default and say so.
- Users edit the same file while you work – match by IDs and text, re-read before writing.
- Keep the engineer's voice; mark AI changes when asked (red).
- Short replies with a table beat long explanations.
