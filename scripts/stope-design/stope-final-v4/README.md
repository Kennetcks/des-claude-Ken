# Stope Final V4 and Decline V3: veins the drive-based blocks missed (Kennet 2026-10-13)

Kennet asked why block 3CA4872 (ST314 B73 B01, 13.7 m @ 1.74) took both lodes. B73 is an 11 m ST drive running at
about 90 deg to the HB0143 vein, so walls square to the drive never saw the vein as a lode. Fix: strings along each
missed vein's own strike.

1. `../missed-stopes/scan.py` - ore >= 2.5 outside the live stopes, vein bodies, 2.2 m pieces (screening only; optimistic).
2. `stage_v4.py` - >= 2.6 ore outside the live V3 stopes (+2 m skin) -> 10 m pockets -> one vein string per 22 m level band
   (V2 level series) along the pocket's PCA strike, snapped onto the lode -> design8 v9 blocks with the live V3 stopes as
   forbidden space and fixed winners (run with `CEIL_R=15 HUP=30 HBLIND=26 MODEL=s2`).
3. `v4_bio.py` + the V3 `stopes.py` / `geom.py` / `write_geom.py` (stope-shapes) - same stope rules on the vein blocks.
4. Deswik (2026.1): copy the live V3 stopes to `STOPE FINAL V4`, add the vein stopes and `VEIN STRINGS`, interrogate.
   In 2026.1 `ModelToInterrogate` takes `new Deswik.Graphics.Commands.CommonCadApplicationMethods(App, null)`.
   Stopes interrogating <= 2.5 are removed.
5. `design_v3dec.py` - ore drive on each vein stope group's floor centreline (1 m past the solids) and an access with the
   stage-2 router from the V2 network; copied onto `DECLINE V3` (V2 unchanged), solids by rule.

Result: 20 vein stopes [figure removed] (INT); 18 OD 441 m, 18 ACC 2,210 m, 8 ISP. Most missed ore is ~1 m veins that
dilute below 2.5 at 2.2 m drillable width; long accesses make several of them uneconomic (Kennet to pick).
