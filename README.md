# desw-claude-plugin

Shared knowledge for the **Claude Assistant for Deswik CAD** plugin (Kennet.DeswikClaude), so every install starts with what has already been learned in real sessions.

- `knowledge/` — guides Claude should read before working in Deswik: rules the engineer settled on, API details verified in Deswik 2024.2, and pitfalls.
- `scripts/` — working `run_csharp` examples behind those guides.

- `plugin/` — source of the Deswik plugin (C#) and the Claude desktop app extension. See [plugin/README.md](plugin/README.md) to install and build.

## How the plugin uses it
From v0.3.0 the plugin builds every file in `knowledge/` into its DLL, and `get_deswik_guide` (and the panel chat) returns them after the built-in API guide. So a fresh install already knows them. To add or try a guide without rebuilding, drop a `.md` file in `%AppData%\Kennet.DeswikClaude\Knowledge` and restart Deswik; a file there replaces the built-in one with the same name. The **Guides** button in the panel opens that folder.

| Guide | Covers |
|---|---|
| [floor-strings.md](knowledge/floor-strings.md) | Floor strings from development solids: lateral = floor middle, vertical = centre line, skip stopes, one plain line per drive, end heights read inside the face |
| [attributes-and-legends.md](knowledge/attributes-and-legends.md) | Attribute fields must exist on the layer; legends and line weight API |
| [tunnels-by-rule.md](knowledge/tunnels-by-rule.md) | Refreshable solids from strings with Section Along Polyline Rules; sections per ACT_TYPE |
| [decline-and-level-design.md](knowledge/decline-and-level-design.md) | Decline and level concept design: 1:7 stacked loops, standoffs, one ACC per group, take-offs, RAR clearance, checks and delivery; .dcf file format notes |
| [block-model-interrogation.md](knowledge/block-model-interrogation.md) | Batch interrogating solids against a block model for grade and tonnes; API, gotchas, what to report |
| [stope-design.md](knowledge/stope-design.md) | Stopes along an ore drive from a block model: the engineer's rules, how existing stopes look, the method, reading a `.gmdlb` block model, loading layers |
| [task-solids.md](knowledge/task-solids.md) | Task solid attributes and geometry notes, working style |
| [mine-design-standards.md](knowledge/mine-design-standards.md) | Reference design values from site guidelines in the engineer's library (Site A, Site B, Site C, Site D, Site E): profiles, gradients, fillets, equipment, standoffs, vertical development, stope limits, D&B basics, sequencing |
| [drill-and-blast-design.md](knowledge/drill-and-blast-design.md) | Rises, slots and ring design rules of thumb from site D&B guidelines and the AMPS course: void ratio, burden / spacing by stope width, standoffs, timing, ring naming, Rhino rig, development faces, QAQC |
| [backfill-and-paste.md](knowledge/backfill-and-paste.md) | Fill types, paste strength targets, paste walls and pours, fill / breather / drain hole design, backfill hazards |
| [deswik-scheduling.md](knowledge/deswik-scheduling.md) | Monthly 3MS update in Deswik.IS / Sched: replacing development and stope tasks, slot flags, dependencies, resources, levelling, outputs |
| [deswik-design-practice.md](knowledge/deswik-design-practice.md) | How sites organise design in Deswik: master layers and versions, centreline attributes and naming, polyline direction, walls and fillets, stripping, solids and derived tasks, plan content and HOLD/STOP controls |

## Adding to it
When something new is learned in a session (a rule the user settled on, an API call that works, a pitfall), add it to the matching file in `knowledge/` and keep it general: no drawing-specific handles.
