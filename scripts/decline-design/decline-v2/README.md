# Decline V2: centre Kennet's ore drives on the stopes and add missing access (2026-10-13)

Kennet's ask: "fix the ore drives so they are central to the stopes, extend where needed, add access where there is no ore drive,
always reuse/extend the strings already there; correct attributes; solids by rule; change the colours". Copy his layer first
(DESIGN\DECLINE -> DESIGN\DECLINE V2) and only edit the copy. Stopes = what is on STOPE FINAL V3 now, skipping GROUP = NO.

Run order (work dir holds kd_strings.csv / kd_attrs.csv exported from his layer, v3_live.csv = live stope names):
1. `export-decline-strings-read-only.cs` -> kd_strings.csv, kd_attrs.csv (all layer fields).
2. `stage1_centre_drives.py` - stope floor centrelines (1 m stations, from the stope solids); each stope assigned to the ore drive on its level
   (|dz| < 3 m, median offset < 10 m); drive offset field = stope centre where a stope sits on it, back to his line 8 m away,
   max 0.4 m sideways per metre; extended along the centreline past the drive ends (not past an end that sits on another drive);
   strings touching the drive move with it and every original junction is re-snapped.
3. `stage2_route_access.py` - stopes with no drive, grouped by level + lode, get one ore drive along their centrelines and a connector:
   cheapest of (same-level OD/ACC, 7.5 m perpendicular take-off) or (decline at the stope RL, 10 m take-off, +40 m penalty = new level with ISP);
   straight or one waypoint (8 m grid), turns <= 100 deg, grade <= 1:33, >= 10 m horizontal or 10 m vertical from other decline passes,
   no stope inside +-3 m x 0.5-5.5 m above the floor (except the first 7 m leaving an ore drive). Networks with no access only get one if a stope sits on them.
4. `stage2b_extend_drives.py` - a connector that leaves from the END of one of his ore drives becomes an extension of that drive.
4b. `stage4_tidy.py` - Kennet's review: straight drives where possible (fewest vertices keeping every stope centre within 2 m, runs >= 8 m,
   junction points kept), no ore drive that serves no stope at a dead end, free ends trimmed to the last stope + 1 m.
5. `stage3_attributes.py` - attributes: LEVEL, LODE (GROUP A/B, nearest drive for new strings), COST_CENTRE O for OD / C otherwise,
   LAT_VERT, STO_DEV, DESIGN notes, Transformation Details; cleans duplicate vertices and backtracks (they stop tunnels-by-rule).
6. `rebuild-decline-v2.cs` - clears V2, recopies his strings, applies the CSVs, colours by ACT_TYPE, refreshes the solids.
7. `checks.py` - stope-to-drive offsets and new-access checks.

Result 2026-10-13 (after his review): 141 strings / 141 solids; 116 of 119 stopes within 2 m of a straight drive; 13 new accesses (12 short crosscuts, 1 new level);
5 dead-end ore drives removed, tails trimmed. Kennet: drives must be straight, no tail past the last stope, no ore drive without a stope.
