# Updating a Deswik schedule (3MS / LOM task replacement)

How sites in the engineer's library run the monthly three-month schedule (3MS) and replace tasks in Deswik.CAD + Deswik.IS + Deswik.Sched (Site K 3MS how-to and task replacement guides). Use it when asked to add designs to a schedule, replace tasks or produce schedule outputs. Follow the open project's own layers, attributes and resource names over these. Attribute and solid rules are in [task-solids.md](task-solids.md) and [deswik-design-practice.md](deswik-design-practice.md).

## Set up the month

- New folder `YY.MM Month YY`; copy the last approved (or latest forecast) CAD and Sched files; open the CAD file and link the new Sched file. Check in Deswik.IS > Project Options that the right IS project is linked before changing anything.
- Add a layer `YY.MM 3MS UPDATES` with children `STRINGS TO ADD TO SCHEDULE` and `STOPES TO ADD`. Bring approved centrelines from the design master (a reference layer such as `ALL LEVELS APPROVED`, reloaded from source) and approved / concept stope and firing shapes from the stope design master.
- Link block models in Deswik.IS > Project Options > Interrogation Settings; only one active model at a time.

## Replace development tasks

1. Decide whether to replace at all: if metres and timing are close, leave the old task.
2. Import the approved strings, strip their attributes (avoid carrying typos and design-only names into the schedule), then copy the required attributes from the old schedule string with *copy attributes*. Set drive numbers and ACT types to match the design.
3. Remove the old tasks first (Deswik.IS > Manage Activities > Remove Activities from Schedule; filter `STO_DEV = DEV`), so new tunnels aren't trimmed by them. Setting `Scheduled = 0` keeps a task without scheduling it.
4. Deswik.IS > Create Tunnels on the attributed centrelines. Escapeways and RARs are development tasks with `LAT_VERT = VERT`; RARs also `SLOT_FLG = 1`, `SLOT_TYPE = Rhino`.
5. Check names, priorities, rates and hold dates; assign resources; then link the new tasks to the existing ones (colour the new tasks to see them).

## Add or replace stopes

- Use firing shapes when they exist; otherwise cut the stope solid into ≈ 15–20 m strike pieces, each independent with no overlaps.
- The first firing (F1) gets `SLOT_FLG = 1` and `SLOT_TYPE = Rhino` or `LHR`; later firings `SLOT_FLG = 0`. Copy attributes from the stope being replaced, then run the site's "stope parameters to schedule" formulas (*Attributes > Set from formula*).
- Deswik.IS > Create From Solids interrogates against the active model; re-interrogate later with Batch Updates.
- Link LH-to-LH and slot-to-slot (predrilling order) and stopes above / below (mining sequence).
- For partly drilled stopes, edit the in-situ tonnes or drill metres on the task rather than a percent complete.

## Dependencies, resources and levelling

- Run the development dependency rules first, check them, then the stope rules (Deswik.IS > Dependency Creation; manual tab for special cases). Check sequence in Animation View, especially stopes near the access.
- Assign resources in Deswik.Sched > Resources > Assign Resources: one resource or pool per task. Development → the jumbo resource (assignment "Effort"); stope bogging → the stoping pool with driving quantity "required" (Site K default 650 t/day per stope, ~90 % of stopes at the default; lower it for long trams). Derived LH drilling and Rhino slot tasks pick up their resources automatically: verify by filtering `ACT_TYPE`. Selecting stope solids in model space also selects derived tasks, so don't overwrite their resources.
- Actualise: set Current Time to 06:00 on the actualisation date, complete or reduce tasks, then Scheduling > Schedule Updates > Update Schedule, then Resource Leveling > Level. Push areas with priorities (Batch Updates from graphics for many tasks, the priority column for a few) and re-level; avoid too many high priorities.

## Outputs

- Task solids for the 3-month window exported to `.dtm`, stopes and development separately (date filter, then `STO_DEV`).
- Physicals: monthly report copied into the 3MS physicals workbook; contractor daily breakdown (06:00 to 06:00), weekly development by heading (weeks starting Wednesday, `LAT_VERT = LAT`); daily grade-bin breakdown for geology / processing.
- Level plots from plane definitions with a layer preset and level filters per level (development and task-solid layers); for a new level create the filter, *View > Plane Definitions > Create from Current View*, a layer preset, copy the annotation layer, then add it to the plot viewport. Long sections A3 and A0. Production Gantt filtered to `STO` for the month.
- Meetings: inputs mid-month (assumptions, rates, dilution / recovery, interactions, models, infrastructure), draft on or before the 26th.
