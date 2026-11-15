# Build a small new .dcf holding single and double fan strip standards with 5 m transitions, on the layer
# 0\REFERENCE\STRIP STANDARDS\FAN STRIP TRANSITIONS, so it can be merged into the Level Design Drafter.
# Template document: RAD Decline draft.dcf (read only). Template entities: the drafter's existing Double Fan Strip
# polyline (h63433982) and label (h63433975), taken from strip_standards.xmlents (vdcl_layer_entities.ps1).
# Without -write it only prints the geometry and the clash check.
param([string]$out = '<SHARE>\03 Planning\01 Development\Draft Level Designer\SITE - Fan Strip Standards.dcf',
  [string]$tmplDcf = "<DESKTOP>\02 Design & Technical\CAD\RAD Decline draft.dcf",
  [string]$ents = "$env:TEMP\dsw\ldd\strip_standards.xmlents", [string]$layerName = 'FAN STRIP TRANSITIONS',
  [double]$rise = 1.5, [double]$trans = 5.0, [double]$width = 5.5, [switch]$write)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$inv = [Globalization.CultureInfo]::InvariantCulture
function F($v) { $v.ToString('R', $inv) }

# --- frame: the existing Double Fan Strip (h63433982) sits on a 1:7 grade; reuse its corner, bearing and grade
$P1 = @(8041.17, 62914.04, 6427.80); $u = @(-0.4679, 0.8838); $v = @(0.8850, 0.4656); $grade = 1 / 7.0
function W($a, $b, $up) { @(($P1[0] + $a * $u[0] + $b * $v[0]), ($P1[1] + $a * $u[1] + $b * $v[1]), ($P1[2] + $a * $grade + $up)) }

# --- strips: [name, high length, along-start]; new column sits 12 m across from the existing column
$v0 = 12.0
$strips = @(@('Single', 8.0, -24.0), @('Double', 15.0, 0.0))
$polys = @(); $labels = @()
foreach ($s in $strips) {
  $L = $s[1]; $a0 = $s[2]; $a1 = $a0 + $trans; $a2 = $a1 + $L; $a3 = $a2 + $trans; $b0 = $v0; $b1 = $v0 + $width
  # three planar panels: ramp up, high flat, ramp down (closed, so they hatch like the existing standards)
  $polys += , @((W $a0 $b0 0), (W $a1 $b0 $rise), (W $a1 $b1 $rise), (W $a0 $b1 0))
  $polys += , @((W $a1 $b0 $rise), (W $a2 $b0 $rise), (W $a2 $b1 $rise), (W $a1 $b1 $rise))
  $polys += , @((W $a2 $b0 $rise), (W $a3 $b0 0), (W $a3 $b1 0), (W $a2 $b1 $rise))
  $txt = "$($s[0]) Fan Strip&#xD;&#xA;{0:F1} mL x {1:F1}mW x {2:F1}mH&#xD;&#xA;+ {3:F1}m transition each end (0 to {2:F1}mH)&#xD;&#xA;Total {4:F1}m" -f $L, $width, $rise, $trans, ($L + 2 * $trans)
  $labels += , @($txt, (W ($a3 + 1.8) ($b1 + 5.0) $rise))
}
'--- panels (E, N, RL):'
foreach ($p in $polys) { ($p | ForEach-Object { '{0:F2},{1:F2},{2:F2}' -f $_[0], $_[1], $_[2] }) -join ' | ' }

# --- clash check against everything already on STRIP STANDARDS (in the same along/across frame)
$lines = Get-Content $ents -Encoding UTF8
$hit = 0
foreach ($x in $lines) {
  $pts = @(); $s = [regex]::Match($x, 'F="VertexList" T="Vertexes"><P F="streamed" V="([^"]+)"').Groups[1].Value
  if ($s) { $b = [Convert]::FromBase64String($s); $n = [BitConverter]::ToInt32($b, 0); $st = [int](($b.Length - 4) / [Math]::Max($n, 1)); for ($i = 0; $i -lt $n; $i++) { $o = 4 + $i * $st; $pts += , @([BitConverter]::ToDouble($b, $o), [BitConverter]::ToDouble($b, $o + 8)) } }
  $ip = [regex]::Match($x, 'F="InsertionPoint" T="gPoint" V="([^"]*)"').Groups[1].Value; if ($ip) { $c = $ip -split ','; $pts += , @([double]$c[0], [double]$c[1]) }
  foreach ($q in $pts) { $dx = $q[0] - $P1[0]; $dy = $q[1] - $P1[1]; $a = $dx * $u[0] + $dy * $u[1]; $bb = $dx * $v[0] + $dy * $v[1]
    if ($bb -gt ($v0 - 2) -and $bb -lt ($v0 + $width + 12) -and $a -gt -30 -and $a -lt 30) { $hit++; '  clash: {0:F1} along, {1:F1} across' -f $a, $bb } } }
"clash points: $hit"
if (-not $write) { return }
if (Test-Path -LiteralPath $out) { throw "refusing to overwrite $out" }

# --- entity XML from the drafter templates
$tPoly = $lines | Where-Object { $_ -match '^<O F="_Item" T="dwPolyline"><P F="Handle" T="vdHandle" V="63433982"' } | Select-Object -First 1
$tText = $lines | Where-Object { $_ -match '^<O F="_Item" T="vdMText"><P F="Handle" T="vdHandle" V="63433975"' } | Select-Object -First 1
if (-not $tPoly -or -not $tText) { throw 'template entities not found' }
function Clean($x, $h) {
  $x = $x -replace 'F="Handle" T="vdHandle" V="\d+"', "F=`"Handle`" T=`"vdHandle`" V=`"$h`""
  $x = $x -replace 'F="HandleGuid" V="[^"]*"', "F=`"HandleGuid`" V=`"$([guid]::NewGuid())`""
  $x = $x -replace '<H F="Layer" V="\d+" />', '<H F="Layer" V="112" />'
  $x = $x -replace '<H F="LineType" V="\d+" />', '<H F="LineType" V="27" />'
  foreach ($k in '_dw_ParentLeaderGuid', '_PARENTHANDLE', '_dw_GroupName') { $x = $x -replace "(<P F=`"Name`" V=`"$k`" /><P F=`"PropValue`")(?: T=`"\w+`")? V=`"[^`"]*`"", '$1 V=""' }
  $x }
$h = 200; $xml = New-Object Text.StringBuilder
foreach ($p in $polys) {
  $h++; $b = New-Object byte[] (4 + 32 * $p.Count); [BitConverter]::GetBytes([int]$p.Count).CopyTo($b, 0)
  for ($i = 0; $i -lt $p.Count; $i++) { for ($k = 0; $k -lt 3; $k++) { [BitConverter]::GetBytes([double]$p[$i][$k]).CopyTo($b, 4 + 32 * $i + 8 * $k) } }
  $x = Clean $tPoly $h
  $x = $x -replace 'F="VertexList" T="Vertexes"><P F="streamed" V="[^"]+"', "F=`"VertexList`" T=`"Vertexes`"><P F=`"streamed`" V=`"$([Convert]::ToBase64String($b))`""
  $x = $x -replace '<P F="Vertex\[\d+\]" V="[^"]*" />', ''
  [void]$xml.Append($x) }
foreach ($l in $labels) {
  $h++; $x = Clean $tText $h
  $x = $x -replace 'F="TextString" V="[^"]*"', "F=`"TextString`" V=`"$($l[0])`""
  $x = $x -replace 'F="InsertionPoint" T="gPoint" V="[^"]*"', "F=`"InsertionPoint`" T=`"gPoint`" V=`"$(F $l[1][0]),$(F $l[1][1]),$(F $l[1][2])`""
  $x = $x -replace 'F="BoxWidth" T="Double" V="[^"]*"', 'F="BoxWidth" T="Double" V="26"'
  [void]$xml.Append($x) }
$nEnt = $polys.Count + $labels.Count

# --- document: keep layers 0, 110, 111, 112 (renamed), drop 113-117
$zin = [IO.Compression.ZipFile]::OpenRead($tmplDcf); $base = [IO.Path]::GetFileName($tmplDcf); $nb = [IO.Path]::GetFileName($out)
$read = { param($e) $sr = New-Object IO.StreamReader($e.Open(), [Text.Encoding]::UTF8); $t = $sr.ReadToEnd(); $sr.Close(); $t }
$main = & $read ($zin.Entries | Where-Object FullName -eq $base)
$doc = New-Object Xml.XmlDocument; $doc.PreserveWhitespace = $true; $doc.LoadXml($main)
$lay = $doc.SelectSingleNode('//O[@F="Layers" and @T="vdLayers"]')
$names = @{ '110' = '0\REFERENCE'; '111' = '0\REFERENCE\STRIP STANDARDS'; '112' = "0\REFERENCE\STRIP STANDARDS\$layerName" }
foreach ($it in @($lay.SelectNodes('O[@T="vdLayer"]'))) {
  $hv = $it.SelectSingleNode('P[@F="Handle"]').GetAttribute('V')
  if ($hv -in '113', '114', '115', '116', '117') { [void]$lay.RemoveChild($it); continue }
  if ($names.ContainsKey($hv)) { $it.SelectSingleNode('P[@F="Name"]').SetAttribute('V', $names[$hv]) } }
$lay.SelectSingleNode('P[@F="_Count"]').SetAttribute('V', [string]$lay.SelectNodes('O[@T="vdLayer"]').Count)
$sw = New-Object IO.StringWriter; $xw = New-Object Xml.XmlTextWriter($sw); $doc.Save($xw); $main2 = $sw.ToString() -replace '^<\?xml version="1.0" encoding="utf-16"\?>', '<?xml version="1.0" encoding="utf-8"?>'
$empty = & $read ($zin.Entries | Where-Object FullName -eq "${base}_0^^06 SITE")
$part = $empty -replace '<O T="vdEntities"><P F="_Count" T="Int32" V="0" /></O>', "<O T=`"vdEntities`"><P F=`"_Count`" T=`"Int32`" V=`"$nEnt`" />$($xml.ToString())</O>"
if ($part -eq $empty) { throw 'empty part pattern not found' }
$parts = [ordered]@{ "${nb}_0" = (& $read ($zin.Entries | Where-Object FullName -eq "${base}_0")); "${nb}_0^^REFERENCE" = $empty; "${nb}_0^^REFERENCE^^STRIP STANDARDS" = $empty
  "${nb}_0^^REFERENCE^^STRIP STANDARDS^^$layerName" = $part; "${nb}__DW_SETTINGS" = (& $read ($zin.Entries | Where-Object FullName -eq "${base}__DW_SETTINGS")); $nb = $main2 }
$zin.Dispose()
$fs = [IO.File]::Open($out, 'CreateNew'); $zo = New-Object IO.Compression.ZipArchive($fs, [IO.Compression.ZipArchiveMode]::Create)
$utf8 = New-Object Text.UTF8Encoding($true)
foreach ($k in $parts.Keys) { $e = $zo.CreateEntry($k, [IO.Compression.CompressionLevel]::Optimal); $w = New-Object IO.StreamWriter($e.Open(), $utf8); $w.Write(($parts[$k] -replace '^\uFEFF', '')); $w.Close() }
$zo.Dispose(); $fs.Close()
"written $out ($nEnt entities, handles 201-$h)"
