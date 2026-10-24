# 01 – How we work with the AI

These rules came out of real use. They keep the engineer in control and stop the AI from damaging live files.

## Ground rules

1. **The engineer decides; the AI drafts.** Schedules, designs, CERs, memos and risk assessments are decision support only. Final approval stays with an authorised, competent person.
2. **Show before change.** For anything that edits a file or the schedule, the AI states what will change (items, fields, old → new, counts) and waits for a yes. Deswik also shows its own approval pop-up for every change.
3. **Never save unasked.** The AI writes into open files (Excel, Word, PowerPoint, Deswik) but does not save them. The engineer saves. Exception: building a *new* file you asked for (e.g. a new physicals workbook) – it is saved, and the old one is backed up first.
4. **Back up before overwrite.** Copy the existing file to `%TEMP%\dsw\...` (or a backup folder) before replacing it.
5. **Read first, then write.** Every job starts with read-only checks: which file/window is open, what the layout is, what the current values are.
6. **Report honestly.** If a check fails, a number does not tie, or a step was skipped, the AI says so. "Nothing was changed" must be true.
7. **Don't invent.** No made-up names, figures, policy requirements or sources. Where a guess is needed (e.g. a design standard placeholder) it is labelled as a guess.
8. **Protect information.** Use only the data needed. No passwords, tokens or credentials in prompts or scripts. Do not send company information to unapproved AI services.

## Writing conventions

- The site's spelling convention, metric units.
- Say **"ounces"**, never "recovered ounces".
- Months as `Nov-26`; dates as `dd/mm/yyyy`.
- Level names follow the site convention (e.g. G6390 = RL 6390); check how the Deswik `LEVEL` attribute relates to RL on your site (it can be RL minus a fixed offset).
- Keep the engineer's own wording when editing their text – fix grammar and spelling only, unless asked to rewrite.
- Mark AI edits visibly when the engineer asks (e.g. **red text** in a SWOT or OneNote handover).

## A good working loop

1. Ask for the job in plain words ("update slide 10 with the level's metres, next three months only").
2. The AI reads the file and the data, then shows a short plan or a before/after table.
3. You say yes / change it.
4. The AI writes, then **reads back** the result and reports what changed.
5. You check in the application and save.

## Things the AI cannot (or should not) do

- Bypass sheet passwords or protection – ask the owner to unprotect.
- Read the *new* Outlook (olk.exe) – only classic Outlook exposes automation. Paste the email or save it as a file instead.
- Send emails, post, or submit anything on your behalf without your explicit OK.
- Make final safety, HRA, ventilation or ground-support decisions.
