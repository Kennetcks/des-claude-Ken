# Finding the Deswik API

Deswik CAD is a .NET application, so the classes a `run_csharp` script can use (`Deswik.Graphics.Primaries.Figure`,
`Polyface`, `Polyline`, layers, attributes...) can be discovered instead of guessed.

## First choice: reflection inside Deswik

Run `scripts/api-discovery/list-type-members-read-only.cs` through `run_csharp`. It changes nothing in the drawing.
Set `TypeName` to the class and, for big classes, `Filter` to a word such as `Attribute` or `Intersect`.

What it showed in Deswik CAD 2024.2 (build 2024.2.1423):

- 192 `Deswik.*` assemblies are loaded, installed in `C:\Program Files\Deswik\Deswik.Suite 2024.2\`
  (for example `Deswik.Graphics`, `Deswik.CAD.Core`, `Deswik.BlockModel`, `Deswik.Common.Algorithms`).
- `Figure` lives in `Deswik.Graphics` and derives from `Deswik.Graphics.Objects.Primary`.
- It has typed views `asPolyline`, `asPolyface`, `asPoint`, `asText`... with matching `IsPolyline`-style flags.
- Useful methods: `AttributeValueGet/Set`, `AttributeDoubleValueGet`, `AttributesCopyFrom`, `Clone`,
  `GetClosestPoint`, `GetCOGPoint`, `IntersectWith`, `HasPossibleIntersection2D/3D`, `ClipToBox`,
  `LegendApply`, `MinimalBoundingArea`, `EntityPropertyOrAttributeValueGet/Set`.

Reflection only shows names and signatures, not what a method does; test new calls on a copy or a small selection first.

## Second choice: REA (static .NET inspection)

[REA](https://github.com/morluto/rea) (MIT, forked to `Kennetcks/rea`) is a reverse-engineering MCP server/CLI. Its
managed-code tools read .NET DLLs without loading them:

- `inspect_managed_artifact`: assembly identity, framework, references.
- `inspect_managed_members` (`--type Namespace.Class` to limit output): types, signatures, CIL bodies, calls.
- `compare_managed_members`: what changed between two Deswik versions (useful when upgrading from 2024.2).

Setup: `npx rea-agents setup` (Node.js 22.19+ / 24.11+). REA documents Linux/macOS as its supported hosts; Windows
works for agent setup but managed inspection on Windows is not listed as verified. It can also run in a cloud
session on a DLL copied from the Deswik install folder.

Use it when reflection is not enough: seeing which methods a method calls, finding non-public helpers, or comparing
versions. It does not decompile to C# by itself (that needs ILSpy).

Check Deswik's licence terms before decompiling or inspecting their binaries in depth; listing the public API through
reflection from a script is the lighter option.
