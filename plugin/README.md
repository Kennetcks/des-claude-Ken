# Claude Assistant for Deswik CAD 2024.2 (v0.2)

Lets Claude work on your open Deswik drawing and Interactive Scheduler (IS) schedule. There are two ways to chat:

- **A. Claude desktop app** (uses your Claude subscription, no API key). You chat in the Claude app and Claude works in Deswik.
- **B. The panel inside Deswik** (needs an Anthropic API key, billed separately).

In both cases, **every change pops up an approval window inside Deswik first.**

---

## 1. Install the Deswik plugin (needed for both)
1. Copy `bin\Kennet.DeswikClaude.dll` to a folder, e.g. `C:\DeswikPlugins\`. **Don't rename it.**
2. **Unblock it:** right-click the DLL > Properties > tick **Unblock** > OK. Or run in PowerShell:
   `Get-ChildItem C:\DeswikPlugins -Recurse | Unblock-File`
   Without this, Plugin Manager says "does not have a valid Deswik.CAD load and unload interface".
3. Deswik CAD > **Plugin Manager** > **+** > pick the DLL > **Load**. Tick *Load on startup* if you want it every time.
4. The *Claude Assistant* panel appears and shows: **"Claude desktop app link is on"**.

## 2A. Connect the Claude desktop app
1. In the Claude desktop app: **Settings > Extensions > Advanced settings > Install Extension…**
2. Choose `claude-app-extension\deswik-claude.mcpb` and confirm.
3. Start a new chat. Under the **+** button > Connectors, *Deswik CAD (Claude Assistant)* should be listed and switched on.
4. Try: *"Use the Deswik tools: what's loaded in my Deswik session?"*
5. When Claude wants to change something, Deswik's approval window jumps to the front. Approve or cancel it there.

Notes:
- Deswik must be open with the plugin loaded. If it isn't, the tools reply "Deswik is not reachable".
- The Claude app may also ask its own permission the first time each tool is used. That's normal.
- Approval windows wait for you, so don't leave them open for long or the Claude app may give up on that call.

## 2B. Chat inside Deswik (optional)
Click **API key…** in the panel and paste a key from https://console.anthropic.com.

## What Claude can do
| Read (no approval) | Change (approval every time) |
|---|---|
| API guide, what's open, layers, current selection | Edit task fields. **Undo last AI change** in the panel puts them back |
| Schedule fields, filtered task tables | Add dependencies (native scheduler undo) |
| One task's details + predecessors and successors | Resource levelling |
| IS command list | IS / CAD commands |
| | **Write and run C# inside Deswik** (saved to `%AppData%\Kennet.DeswikClaude\Scripts`) |

## How the link works
The Claude app runs a small Node.js bridge (inside the .mcpb). The bridge talks to the plugin over the local named pipe `\\.\pipe\KennetDeswikClaude`. Only your Windows user can connect, and network access to the pipe is denied. Nothing leaves your PC except what Claude normally sees in your chat.

## Build from source
`src\DeswikClaude.csproj` (Visual Studio, .NET Framework 4.7.2; set `<DeswikDir>` if Deswik is elsewhere).
Extension source: `claude-app-extension\src`. Repack with `npx @anthropic-ai/mcpb pack`.

## Safety
Save before big changes: code Claude writes and runs can't be undone automatically. The plugin relies on IS internals that aren't an official Deswik API, so re-test after Deswik upgrades.
