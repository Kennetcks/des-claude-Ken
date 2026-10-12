# Working with scheduler task solids

Common layout of Deswik IS task solids (check with a read-only summary first; names vary by site):

- Layer such as `SCHEDULER\TASK SOLIDS`, figures are `Polyface` solids.
- `STO_DEV` = `DEV` / `STO` (development vs stope).
- `LAT_VERT` = `LAT` / `VERT` (lateral vs vertical development).
- `ACT_TYPE` = activity type (e.g. OD, DEC, MLA, ACC, RAD, SMP, EWR, LH_Stope ...).
- `DIS Parent ID` (DIS parent GUID) = the design drive a solid came from. Group by it to get one drive.

Geometry notes:
- `Polyface.FaceList` is 4 one-based vertex indices + a colour per face.
- Adjacent solids in a drive share exact vertices (round ~5 cm), which gives their order along the drive.
- `Polyface.GetMinZ(x, y, BoundingBox)` returns `double.MaxValue` when (x, y) is outside the solid.

Working style the engineer asked for:
- Proceed with sensible defaults; only ask when the data genuinely can't answer it (e.g. whether two areas of the mine are meant to connect). One short question.
- Report results with counts and examples (handles, coordinates, RLs), and say what you didn't check.
