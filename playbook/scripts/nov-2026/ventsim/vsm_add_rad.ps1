# Make a NEW Ventsim model = base .vsm + the W390 RAD string (RAD Decline draft.dcf, decoded to rad_strings.json) with a forcing fan
# and 1.2 m vent bag, built the way the mine model already builds them (a .vsm is gzipped tab-delimited text; airways are rows of
# the MAIN table and join where end coordinates match):
#   RAD    copies RAD airway u1948 (Tunnel Type 6 = RAD preset 5x5, secondary layer RAD, k 0.012); top vertex snapped onto the
#          existing portal node (exit of surface-exit airway u1649) so it opens to surface; face airway carries flag 262144 like the
#          model's other dead-end RADs
#   bag    copies bag airway u2340 (flag 134217728 = duct, round, k 0.003): 2.94 m above the RAD centreline and 1 m to the side
#          (clear of the existing CC610 bag at the portal), dropping straight down into the RAD face node as the outlet
#   fan    copies aux fan u3019 (flags duct + surface exit 4096, Fan Primary -1); open end beside the portal surface node, blowing
#          into the bag; fan DB id 47 = FAN_110KW
#   ties   copies link u1888 (flag 67108864, 0.01 m, fixed R 7.5E+08) from a RAD node up to the bag about every 46 m, as the model does
#   all bag, fan and tie rows get a new Group number (one per bag system, as in the model)
# The base file is never changed.
param(
  [string]$base = '<SHARE>\05 Ventilation\06 Ventsim\Mine Model.vsm',
  [string]$out = '<SHARE>\05 Ventilation\06 Ventsim\Mine Model - RAD 110kW fan 1200 bag.vsm',
  [int]$fanId = 47, [double]$ductDia = 1.2, [double]$ductUp = 2.94, [double]$ductSide = 1.0, [double]$minSeg = 9.5, [double]$tieEvery = 46,
  [switch]$write, [switch]$replace)
$ErrorActionPreference = 'Stop'
$inv = [Globalization.CultureInfo]::InvariantCulture
function N($v) { ([double]$v).ToString('R', $inv) }

$fs = [IO.File]::OpenRead($base); $gz = New-Object IO.Compression.GZipStream($fs, [IO.Compression.CompressionMode]::Decompress)
$sr = New-Object IO.StreamReader($gz, (New-Object Text.UTF8Encoding($false))); $text = $sr.ReadToEnd(); $sr.Close()
$nl = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
$L = New-Object Collections.Generic.List[string]; $L.AddRange([string[]]($text -split "`r?`n"))
$m = -1; for ($i = 0; $i -lt $L.Count; $i++) { if ($L[$i].StartsWith("MAIN`tUnique No")) { $m = $i; break } }
if ($m -lt 0) { throw 'MAIN airway table not found' }
$h = $L[$m] -split "`t"; $ix = @{}; for ($i = 0; $i -lt $h.Count; $i++) { if (-not $ix.ContainsKey($h[$i])) { $ix[$h[$i]] = $i } }
$end = $m + 1; while (-not $L[$end].StartsWith('END')) { $end++ }
$rows = @{}; $maxU = 0; $maxRow = 0; $maxGroup = 0
for ($i = $m + 1; $i -lt $end; $i++) { $c = $L[$i] -split "`t"; $rows[$c[1]] = $c; $maxU = [Math]::Max($maxU, [int]$c[1]); $maxRow = [Math]::Max($maxRow, [int]$c[0]); $maxGroup = [Math]::Max($maxGroup, [int]$c[$ix['Group']]) }
if (-not ($L | Where-Object { $_ -match "^$fanId`t" -and $_ -match 'FAN_110KW' })) { throw "fan id $fanId is not the FAN_110KW in this model" }
$need = @{ '1948' = 'RAD'; '2340' = 'bag'; '3019' = 'aux fan'; '1888' = 'tie link'; '1649' = 'portal surface exit' }
foreach ($k in $need.Keys) { if (-not $rows[$k]) { throw "template u$k ($($need[$k])) not in model" } }
if ($rows['1948'][$ix['Tunnel Type']] -ne '6') { throw 'u1948 is not a RAD (Tunnel Type 6) airway' }
if (([int64]$rows['2340'][27] -band 134217728) -eq 0 -or ([int64]$rows['1888'][27] -band 67108864) -eq 0) { throw 'bag / tie templates do not carry the expected flags' }
$group = $maxGroup + 1

# RAD vertices, top (portal) first, thinned to segments of at least $minSeg m (keeps both ends)
$s = (Get-Content "$env:TEMP\dsw\rad_strings.json" -Raw | ConvertFrom-Json) | Where-Object { $_.ACT_TYPE -eq 'RAD' -and $_.N -gt 1 } | Select-Object -First 1
$p = @($s.Pts | ForEach-Object { , @([double]$_[0], [double]$_[1], [double]$_[2]) }); if ($p[0][2] -lt $p[-1][2]) { [array]::Reverse($p) }
function D3($a, $b) { [Math]::Sqrt(($b[0] - $a[0]) * ($b[0] - $a[0]) + ($b[1] - $a[1]) * ($b[1] - $a[1]) + ($b[2] - $a[2]) * ($b[2] - $a[2])) }
$rad = @(, $p[0]); for ($i = 1; $i -lt $p.Count - 1; $i++) { if ((D3 $rad[-1] $p[$i]) -ge $minSeg) { $rad += , $p[$i] } }
if ((D3 $rad[-1] $p[-1]) -lt 3 -and $rad.Count -gt 1) { $rad = $rad[0..($rad.Count - 2)] }; $rad += , $p[-1]
$pc = $rows['1649']; $portal = @([double]$pc[12], [double]$pc[13], [double]$pc[14]); $outside = @([double]$pc[9], [double]$pc[10], [double]$pc[11])
$gap = D3 $portal $rad[0]; if ($gap -gt 0.5) { throw "RAD top is $([Math]::Round($gap,2)) m from the portal node - not snapping" }; $rad[0] = $portal

# bag nodes: above each RAD vertex (side offset to the right looking down-ramp), then the outlet drop into the face node
function Off($pt, $a, $b, $up) { $tx = $b[0] - $a[0]; $ty = $b[1] - $a[1]; $tl = [Math]::Sqrt($tx * $tx + $ty * $ty); , @(($pt[0] + $ty / $tl * $ductSide), ($pt[1] - $tx / $tl * $ductSide), ($pt[2] + $up)) }
$bag = @(); for ($i = 0; $i -lt $rad.Count; $i++) { $bag += , (Off $rad[$i] $rad[[Math]::Max($i - 1, 0)] $rad[[Math]::Min($i + 1, $rad.Count - 1)] $ductUp) }
$bag += , $rad[-1]
$fanOpen = Off $outside $outside $portal $ductUp

$new = New-Object Collections.Generic.List[string]; $u = $maxU; $r = $maxRow
function Add($tpl, $a, $b, $w, $flags, $grp, [hashtable]$set) {
  $c = [string[]]$rows[$tpl].Clone(); $script:u++; $script:r++
  $len = D3 $a $b; $k = [double]$c[$ix['kFactor']]; $round = ($c[$ix['Shape Type']] -eq '1')
  if ($round) { $area = [Math]::PI * $w * $w / 4; $per = [Math]::PI * $w; $c[$ix['Width']] = N $w; $c[$ix['Height']] = N $w; $c[$ix['Area']] = N $area; $c[$ix['Perimeter']] = N $per }
  else { $area = [double]$c[$ix['Area']]; $per = [double]$c[$ix['Perimeter']] }
  if ($c[$ix['Resistance Type']] -eq '0') { $res = $k * $len * $per / [Math]::Pow($area, 3); $c[$ix['Resistance']] = N $res; $c[$ix['Resistance Total']] = N $res }
  $c[0] = "$script:r"; $c[1] = "$script:u"; $c[$ix['GUID']] = [guid]::NewGuid().ToString(); $c[$ix['Group']] = "$grp"
  $c[$ix['X1']] = N $a[0]; $c[$ix['Y1']] = N $a[1]; $c[$ix['Z1']] = N $a[2]; $c[$ix['X2']] = N $b[0]; $c[$ix['Y2']] = N $b[1]; $c[$ix['Z2']] = N $b[2]
  $c[$ix['Length']] = N $len; $c[$ix['Branch Flags']] = "$flags"; $c[$ix['Quantity']] = '0'; $c[$ix['Stage']] = '1'; $c[$ix['newstage']] = '1'
  if ($set) { foreach ($k2 in $set.Keys) { $c[$ix[$k2]] = $set[$k2] } }
  $script:new.Add(($c -join "`t")) }
# RAD (group 0 like every tunnel), face airway flagged like the model's other dead-end RADs
for ($i = 1; $i -lt $rad.Count; $i++) { Add '1948' $rad[$i - 1] $rad[$i] 5.0 $(if ($i -eq $rad.Count - 1) { 262144 } else { 0 }) 0 $null }
# fan: open end (surface) -> first bag node
Add '3019' $fanOpen $bag[0] $ductDia 134221824 $group @{ 'Fan Type' = "$fanId"; 'Fan Numbers' = '1'; 'Fan Off' = 'False' }
# bag airways and outlet drop
for ($i = 1; $i -lt $bag.Count; $i++) { Add '2340' $bag[$i - 1] $bag[$i] $ductDia 134217728 $group $null }
# tie links RAD node -> bag node every ~$tieEvery m (not at the portal or the outlet)
$ch = 0; $last = 0; $ties = 0
for ($i = 1; $i -lt $rad.Count - 1; $i++) { $ch += D3 $rad[$i - 1] $rad[$i]; if ($ch - $last -ge $tieEvery) { Add '1888' $rad[$i] $bag[$i] 0.01 67108864 $group $null; $last = $ch; $ties++ } }

function Len($q) { $l = 0; for ($i = 1; $i -lt $q.Count; $i++) { $l += D3 $q[$i - 1] $q[$i] }; $l }
'base: {0} ({1} airways, bag groups up to {2})' -f $base, ($end - $m - 1), $maxGroup
'RAD:  {0} airways, {1:F1} m, RAD preset 5x5, RL {2:F1} (portal node, joins surface-exit u1649) to {3:F1} (face)' -f ($rad.Count - 1), (Len $rad), $rad[0][2], $rad[-1][2]
'fan:  FAN_110KW (id {0}), open end E {1:F1} N {2:F1} RL {3:F1}, group {4}' -f $fanId, $fanOpen[0], $fanOpen[1], $fanOpen[2], $group
'bag:  {0} airways, {1:F1} m of {2} m bag, {3} m above the RAD, outlet dropping into the face node' -f ($bag.Count - 1), (Len $bag), $ductDia, $ductUp
"ties: $ties links RAD -> bag"
"new unique numbers u$($maxU + 1) to u$u"
if (-not $write) { 'dry run - nothing written (use -write)'; return }
if ((Test-Path -LiteralPath $out) -and -not $replace) { throw "$out already exists - not overwriting" }
$L.InsertRange($end, $new)
$ms = New-Object IO.MemoryStream; $gz = New-Object IO.Compression.GZipStream($ms, [IO.Compression.CompressionMode]::Compress)
$bytes = (New-Object Text.UTF8Encoding($false)).GetBytes(($L -join $nl)); $gz.Write($bytes, 0, $bytes.Length); $gz.Close()
[IO.File]::WriteAllBytes($out, $ms.ToArray())
"written: $out"
