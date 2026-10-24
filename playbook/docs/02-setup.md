# 02 – Setup

## Software used

| Piece | Purpose |
|---|---|
| Claude desktop app (Code tab) | The AI assistant, with PowerShell access to the PC |
| Deswik CAD 2026.1 + Interactive Scheduler | Design and scheduling |
| **Kennet.DeswikClaude** plugin v0.4.0 (`Kennet.DeswikClaude.dll`, loaded via Deswik Plugin Manager) | Lets the AI query and change Deswik over a named pipe; every change pops up an approval in Deswik |
| Deswik CAD (Claude Assistant) desktop extension (`deswik-claude.mcpb`) | Optional: exposes the plugin tools directly in Claude |
| Microsoft Excel / Word / PowerPoint / OneNote (desktop) | Edited live through COM automation from PowerShell |

Keep the plugin files together in one folder (DLL, Roslyn compiler DLLs, `.mcpb`, `INSTALL.txt`). If a cloud-synced folder replaces the DLL while Deswik is open, restart Deswik.

## How the AI talks to Deswik

The plugin listens on the named pipe `\\.\pipe\KennetDeswikClaude`. A request is one JSON line:

```json
{"op":"call","name":"run_csharp","input":{"description":"...","code":"...","modifies":false}}
```

`scripts/deswik/dcall.ps1` is the client used. It:

- connects with a 5 s timeout and waits for the reply (default 120 s);
- **retries when the call lands in the wrong Deswik window** (see below);
- returns the plugin's text reply.

Tools used: `get_context`, `get_selection`, `run_csharp`, `is_set_task_fields`, `is_add_dependencies`, `is_level_schedule`, `list_layers`, `get_deswik_guide`.

### Two Deswik windows = random target

When two Deswik windows run the plugin they share the same pipe, and each call lands in either one. **Every script starts with a filename guard**:

```csharp
if (!App.Filename.EndsWith("MINE_UG_FY27.dcf")) { Out.AppendLine("WRONG WINDOW"); return null; }
```

`dcall.ps1` retries until the reply doesn't contain `WRONG WINDOW` (and optionally contains an expected word). A call that silently hits the other window can look like "No IS schedule is loaded" – treat that as wrong window too.

### Read-only vs change

- `modifies:false` scripts run without a pop-up (keep them strictly read-only).
- `modifies:true` scripts and `is_*` change tools raise an **approval pop-up in Deswik** – the engineer must click approve in the right window.
- Every script the plugin runs is saved to `%APPDATA%\Kennet.DeswikClaude\Scripts\` with a timestamp – a useful audit trail and source of reusable code.

## How the AI talks to Office

PowerShell 5.1 + COM:

- **Live, open file:** `[Runtime.InteropServices.Marshal]::GetActiveObject('Excel.Application')` (same for `PowerPoint.Application`, `Word.Application`), then find the workbook/presentation by name. Changes appear on screen; the user saves.
- **Closed file:** `New-Object -ComObject Excel.Application` (hidden instance), open read-only for reading, or open, change, save and close when building a new file.
- **OneNote:** `New-Object -ComObject OneNote.Application`, `GetHierarchy` / `GetPageContent` / `UpdatePageContent` (XML).
- **PDFs** (quotes, GA drawings): rendered to PNG with the built-in Windows `Windows.Data.Pdf` API, then read as images.

See [09-lessons-learned.md](09-lessons-learned.md) for the COM gotchas (busy errors, type casts, long paths, encoding).
