# STOPE FINAL V5: one lode per stope (2026-10-13)

Kennet on 3CA5476 (ST336 3CA0CEB S01): "you chose two lodes, and there is one higher grade". The low-grade zone between
two lodes is not mined, so a stope must sit on the better lode instead of a span over both.

Cause in design8 (v8/v9): per 1 m station and level, `lode_span` took the span with the most ore tonnes whose diluted grade
was > 2.6, then the wall nudges added any slice with >= 15 % ore. A wide 2.5-4 g/t zone beat a narrow > 4 g/t lode.

Fix (design8 `SPAN_PICK=shape`, v10d):
- Per station, each candidate shape is one width (2.2-14 m) and a straight dip line up the column (centre leans <= 1.2 m per
  level). It is scored by the gold above `SHAPE_REF` (2.6 = cut-off) in the whole column.
- A Viterbi along strike on 5 m bins (the section spacing) lets the centre move <= 1 m and the width one step per bin.
  These are the same limits the drillable sections hold, so smoothing and strike_limit do not smear the walls across lodes.
- Wall nudges in this mode add only slices whose own grade >= `SHAPE_REF`. There is no ore-share rule.
- `SPAN_PICK=ore` (the default) is unchanged, so older runs reproduce. `value` and `track` were tried and did not fix 3CA5476.

Run: `run.sh` builds the ST blocks, then stp/stopes.py, geom.py and write_geom.py from the stope-shapes folder with
`v5_bio.py` as the loader. `compare.py` matches the stopes to the live V3 stopes: it assigns GROUP, drops stopes that only
sit where Kennet deleted V3 stopes, and writes v3_vs_v5.csv. `stage_v5vein.py` redoes the vein strings with V5 as no-go
space. In Deswik, run create-stope-final-v5.cs, then interrogate-stope-final-v5.cs and write-grades-stope-final-v5.cs.
Then delete the stopes with INT_AU <= 2.5 (6 this time).

Result (INT): 147 stopes [figure removed] = [ounces removed]; gold above 2.6 = 1,094 kg. STOPE FINAL V4 was 146 stopes [figure removed]
= [ounces removed]; [gold removed] above 2.6. 3CA5476: [figure removed] became [figure removed].
With SHAPE_REF=3.0 the result was [figure removed] (python estimate), fewer ounces, so 2.6 was kept.

Gotcha: a stope that is watertight in trimesh can still come out of Deswik as Volume=-1, and RepairAlternate does not
fix it. Round-trip it through manifold3d `Manifold(mesh).simplify(0.05)` and recreate it.

## V5B: lean with the lode (2026-10-13)

Kennet on 3CA56D4 (ST101 3CA0D17 S05): V5 capped the centre lean at 1.2 m per level, but that lode leans about 2.4 m per
level (~66 deg). From ch 140 the top half fell off the lode, so the stope came out 10 m high @ 3.96 where V4 had 26 m @ 4.55.
v10e lets the dip line lean up to the MIN_DIP (50 deg) drillable limit per station. 3CA0D17 S04 is now full height,
[figure removed] INT.
Layer STOPE FINAL V5B: 157 stopes [figure removed] INT = [ounces removed]; [gold removed] above 2.6. That beats V4 ([ounces removed], 976 kg)
on both. 5 stopes <= 2.5 were deleted. `all.sh` runs the whole chain.
The ore drive strings (STOPE FINAL V5B\ORE DRIVES, 77 strings, 5.1 km, no solids) are drawn centred under the stope floors
at the lowest floor RL, >= 1 m past the end stopes. Kennet asked for strings only this time; the solids can come later.

End spikes (Kennet 2026-10-13, 3CA5752 and 3CA5758): a drive-end block beyond the end of the string above took the blind
height (26 m) while the rest of the stope stops at the floor above (~21.5 m), which left a spike at the top end.
`fix_end_spikes.py <all stopes pkl> <out.txt>` trims those stopes flat at the neighbour's height. Two stopes were affected
in V5B: ST260 3CA0CFD S06 is now [figure removed] and ST238 3CA0D01 S04 is [figure removed]. Both were replaced in Deswik and
re-interrogated.
