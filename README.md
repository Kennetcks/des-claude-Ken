# desw-claude-plugin

Shared knowledge for the **Claude Assistant for Deswik CAD** plugin (Kennet.DeswikClaude), so every install starts with what has already been learned in real sessions.

- `knowledge/` — guides Claude should read before working in Deswik: rules the engineer settled on, API details verified in Deswik 2024.2, and pitfalls.
- `scripts/` — working `run_csharp` examples behind those guides.

## Using it with the plugin
At the start of a Deswik conversation, give Claude the files in `knowledge/` (or point it at this repository) along with `get_deswik_guide`. The plugin's built-in guide covers the general API; these files add the task-specific rules.

| Guide | Covers |
|---|---|
| [floor-strings.md](knowledge/floor-strings.md) | Floor strings from development solids: lateral = floor middle, vertical = centre line, skip stopes, one plain line per drive, end heights read inside the face |
| [attributes-and-legends.md](knowledge/attributes-and-legends.md) | Attribute fields must exist on the layer; legends and line weight API |
| [tunnels-by-rule.md](knowledge/tunnels-by-rule.md) | Refreshable solids from strings with Section Along Polyline Rules; sections per ACT_TYPE |
| [decline-and-level-design.md](knowledge/decline-and-level-design.md) | Decline and level concept design: 1:7 stacked loops, standoffs, one ACC per group, take-offs, RAR clearance, checks and delivery; .dcf file format notes |
| [task-solids.md](knowledge/task-solids.md) | Task solid attributes and geometry notes, working style |

## Adding to it
When something new is learned in a session (a rule the user settled on, an API call that works, a pitfall), add it to the matching file in `knowledge/` and keep it general: no drawing-specific handles.
