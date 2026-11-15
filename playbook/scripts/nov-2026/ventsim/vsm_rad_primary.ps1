# Make a NEW Ventsim model = Mine Model.vsm + the W390 RAD as the primary exhaust, set up like the reference primary system:
#   intake down the decline, return up the RAR stack, out the RAD through a vent wall of 4 x FAN_MODEL fans near the portal.
#   RAD      the draft string (rad_strings.json) as RAD airways copied from u1948 (Tunnel Type 6 = RAD 5x5). It gets its own portal:
#            the draft starts on the main decline portal node, so the top node is moved 5 m to the side away from the decline and the
#            first airway is a surface exit (4096), like the decline portal airway u1649. Otherwise the exhaust would re-enter the decline.
#   hole     the RAD face is carried flat ~30 m into RAR u1776 at the face RL; u1776 is split there (bottom part keeps its dead-end flag)
#   fan wall RAD segment 2 from the portal widened to 9.7 x 5.7 and given Fan Numbers 4 of the reference "FAN_MODEL | 1 fan"
#            curve (copied into this model's fan list from the reference model, as the mine's own curves for that fan differ); airway drawn
#            inby -> outby so the fans exhaust
#   wall     6417 link from the RAR top to the decline (u1948) set to a fixed 1000 Ns2/m8 (value of the model's Concrete Wall preset)
# Existing fans and bags are left as they are. The base file is never changed.
param(
  [string]$base = '<SHARE>\05 Ventilation\06 Ventsim\Mine Model.vsm',
  [string]$refModel = "$env:TEMP\dsw\vsm\reference vent model.txt",
  [string]$out = '<SHARE>\05 Ventilation\06 Ventsim\Mine Model - RAD primary 4 fans.vsm',
  [int]$refFanId = 4, [int]$fans = 4, [double]$fanW = 9.7, [double]$fanH = 5.7, [double]$portalSide = 5.0, [double]$minSeg = 9.5,
  [string]$rarU = '1776', [string]$wallU = '1948', [double]$wallR = 1000, [switch]$write)
$ErrorActionPreference = 'Stop'
$inv = [Globalization.CultureInfo]::InvariantCulture
function N($v) { ([double]$v).ToString('R', $inv) }
function D3($a, $b) { [Math]::Sqrt(($b[0] - $a[0]) * ($b[0] - $a[0]) + ($b[1] - $a[1]) * ($b[1] - $a[1]) + ($b[2] - $a[2]) * ($b[2] - $a[2])) }

$fs = [IO.File]::OpenRead($base); $gz = New-Object IO.Compression.GZipStream($fs, [IO.Compression.CompressionMode]::Decompress)
$sr = New-Object IO.StreamReader($gz, (New-Object Text.UTF8Encoding($false))); $text = $sr.ReadToEnd(); $sr.Close()
$nl = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
$L = New-Object Collections.Generic.List[string]; $L.AddRange([string[]]($text -split "`r?`n"))

# --- fan: copy the reference FAN_MODEL record (header row + its curve rows) into this model's FANS list under a new id
$R = [IO.File]::ReadAllLines($refModel); $rf = [array]::IndexOf($R, 'FANS'); $blk = @()
for ($i = $rf + 1; $R[$i] -ne 'FANPICS'; $i++) { $c = $R[$i] -split "`t"; if ($c.Count -gt 30 -and $c[0] -eq "$refFanId") { $blk += $R[$i]; $j = $i + 1; while (($R[$j] -split "`t").Count -le 30 -and $R[$j] -ne 'FANPICS') { $blk += $R[$j]; $j++ }; break } }
if (-not $blk -or $blk[0] -notmatch 'FAN_MODEL') { throw "reference fan $refFanId is not the FAN_MODEL" }
$wf = $L.IndexOf('FANS'); $wfEnd = $L.IndexOf('FANPICS'); $maxFan = 0
for ($i = $wf + 1; $i -lt $wfEnd; $i++) { $c = $L[$i] -split "`t"; if ($c.Count -gt 30) { $maxFan = [Math]::Max($maxFan, [int]$c[0]) } }
$fanId = $maxFan + 1; $h0 = $blk[0] -split "`t"; $h0[0] = "$fanId"; $h0[1] = 'FAN_MODEL (reference curve)'
for ($k = 0; $k -lt $h0.Count; $k++) { if ($h0[$k] -match '^[0-9a-f]{8}-[0-9a-f]{4}-' -and $h0[$k] -ne '00000000-0000-0000-0000-000000000000') { $h0[$k] = [guid]::NewGuid().ToString() } }
# reference (Ventsim 6.0.2) fan rows lack the last 3 fields of the mine (6.0.4); same layout otherwise, so pad with the mine's defaults
$wCols = ($L[$wf + 1] -split "`t"); if ($h0.Count -gt $wCols.Count) { throw 'reference fan record is longer than the mine''s - not copying' }
$h0 = @($h0) + @($wCols[$h0.Count..($wCols.Count - 1)])
$fanBlock = @(($h0 -join "`t")) + @($blk | Select-Object -Skip 1)
$wCurve = ($L[$wf + 2] -split "`t").Count; foreach ($cl in ($fanBlock | Select-Object -Skip 1)) { if (($cl -split "`t").Count -ne $wCurve) { throw 'fan curve row layout differs between models - not copying' } }

# --- airways
$m = -1; for ($i = 0; $i -lt $L.Count; $i++) { if ($L[$i].StartsWith("MAIN`tUnique No")) { $m = $i; break } }
$h = $L[$m] -split "`t"; $ix = @{}; for ($i = 0; $i -lt $h.Count; $i++) { if (-not $ix.ContainsKey($h[$i])) { $ix[$h[$i]] = $i } }
$end = $m + 1; while (-not $L[$end].StartsWith('END')) { $end++ }
$rows = @{}; $line = @{}; $maxU = 0; $maxRow = 0
for ($i = $m + 1; $i -lt $end; $i++) { $c = $L[$i] -split "`t"; $rows[$c[1]] = $c; $line[$c[1]] = $i; $maxU = [Math]::Max($maxU, [int]$c[1]); $maxRow = [Math]::Max($maxRow, [int]$c[0]) }
foreach ($k in '1948', '1649', $rarU, $wallU) { if (-not $rows[$k]) { throw "airway u$k not in model" } }
if ($rows['1948'][$ix['Tunnel Type']] -ne '6') { throw 'u1948 is not a RAD airway' }
if ($rows[$rarU][$ix['Tunnel Type']] -ne '7') { throw "u$rarU is not a RAR airway" }

$s = (Get-Content "$env:TEMP\dsw\rad_strings.json" -Raw | ConvertFrom-Json) | Where-Object { $_.ACT_TYPE -eq 'RAD' -and $_.N -gt 1 } | Select-Object -First 1
$p = @($s.Pts | ForEach-Object { , @([double]$_[0], [double]$_[1], [double]$_[2]) }); if ($p[0][2] -lt $p[-1][2]) { [array]::Reverse($p) }
$rad = @(, $p[0]); for ($i = 1; $i -lt $p.Count - 1; $i++) { if ((D3 $rad[-1] $p[$i]) -ge $minSeg) { $rad += , $p[$i] } }
if ((D3 $rad[-1] $p[-1]) -lt 3 -and $rad.Count -gt 1) { $rad = $rad[0..($rad.Count - 2)] }; $rad += , $p[-1]
# own portal: move the top node sideways, away from the decline (right-hand side looking down the RAD)
$dx = $rad[1][0] - $rad[0][0]; $dy = $rad[1][1] - $rad[0][1]; $dl = [Math]::Sqrt($dx * $dx + $dy * $dy)
$rad[0] = @(($rad[0][0] + $dy / $dl * $portalSide), ($rad[0][1] - $dx / $dl * $portalSide), $rad[0][2])
$decl = $rows['1650']; $toDecl = @(([double]$decl[12] - [double]$decl[9]), ([double]$decl[13] - [double]$decl[10]))
if (($dy / $dl * $toDecl[0] - $dx / $dl * $toDecl[1]) -gt 0) { throw 'portal offset would move towards the decline - check side' }
# hole into the RAR at the RAD face RL
$rr = $rows[$rarU]; $ra = @([double]$rr[9], [double]$rr[10], [double]$rr[11]); $rb = @([double]$rr[12], [double]$rr[13], [double]$rr[14])
$zf = $rad[-1][2]; $t = ($zf - $ra[2]) / ($rb[2] - $ra[2]); if ($t -le 0 -or $t -ge 1) { throw "RAD face RL $zf is outside RAR u$rarU" }
$hole = @(($ra[0] + $t * ($rb[0] - $ra[0])), ($ra[1] + $t * ($rb[1] - $ra[1])), $zf)

$new = New-Object Collections.Generic.List[string]; $u = $maxU; $r = $maxRow
function Row($tpl, $a, $b, $flags, [hashtable]$set, [switch]$keepId) {
  $c = [string[]]$rows[$tpl].Clone()
  if (-not $keepId) { $script:u++; $script:r++; $c[0] = "$script:r"; $c[1] = "$script:u"; $c[$ix['GUID']] = [guid]::NewGuid().ToString() }
  $c[$ix['X1']] = N $a[0]; $c[$ix['Y1']] = N $a[1]; $c[$ix['Z1']] = N $a[2]; $c[$ix['X2']] = N $b[0]; $c[$ix['Y2']] = N $b[1]; $c[$ix['Z2']] = N $b[2]
  $len = D3 $a $b; $c[$ix['Length']] = N $len; $c[$ix['Branch Flags']] = "$flags"; $c[$ix['Quantity']] = '0'
  if ($set) { foreach ($k in $set.Keys) { $c[$ix[$k]] = $set[$k] } }
  if ($c[$ix['Resistance Type']] -eq '0') { $res = [double]$c[$ix['kFactor']] * $len * [double]$c[$ix['Perimeter']] / [Math]::Pow([double]$c[$ix['Area']], 3); $c[$ix['Resistance']] = N $res; $c[$ix['Resistance Total']] = N $res }
  , $c }
# RAD: airway 1 is the portal (surface exit at the open top node); airway 2 is the fan wall, drawn inby -> outby
$tA = [double]$rows['1948'][$ix['Area']]; $tP = [double]$rows['1948'][$ix['Perimeter']]; $tW = [double]$rows['1948'][15]; $tH = [double]$rows['1948'][16]
$fanSet = @{ 'Width' = (N $fanW); 'Height' = (N $fanH); 'Area' = (N ($tA * $fanW * $fanH / ($tW * $tH))); 'Perimeter' = (N ($tP * ($fanW + $fanH) / ($tW + $tH)))
  'Fan Type' = "$fanId"; 'Fan Numbers' = "$fans"; 'Fan Off' = 'False'; 'Curve' = '1'; 'Sim Curve' = '1'; 'Fan Primary' = '0' }
for ($i = 1; $i -lt $rad.Count; $i++) {
  if ($i -eq 1) { $new.Add(((Row '1948' $rad[0] $rad[1] 4096) -join "`t")) }
  elseif ($i -eq 2) { $new.Add(((Row '1948' $rad[2] $rad[1] 0 $fanSet) -join "`t")) }
  else { $new.Add(((Row '1948' $rad[$i - 1] $rad[$i] 0) -join "`t")) } }
$new.Add(((Row '1948' $rad[-1] $hole 0) -join "`t"))
# split the RAR: existing row keeps its id from the top junction down to the hole; new row from the hole down keeps the dead-end flag
$rarFlags = $rr[27]; $topEnd = if ($ra[2] -gt $rb[2]) { $ra } else { $rb }; $botEnd = if ($ra[2] -gt $rb[2]) { $rb } else { $ra }
$L[$line[$rarU]] = (Row $rarU $topEnd $hole 0 -keepId) -join "`t"
$new.Add(((Row $rarU $hole $botEnd $rarFlags) -join "`t"))
# vent wall on the 6417 link
$wc = [string[]]$rows[$wallU].Clone(); $wc[$ix['Resistance Type']] = '1'; $wc[$ix['Resistance']] = N $wallR; $wc[$ix['Resistance Total']] = N $wallR; $wc[$ix['Quantity']] = '0'
$L[$line[$wallU]] = $wc -join "`t"

function Len($q) { $l = 0; for ($i = 1; $i -lt $q.Count; $i++) { $l += D3 $q[$i - 1] $q[$i] }; $l }
'base: {0} ({1} airways)' -f $base, ($end - $m - 1)
'fan:  reference "FAN_MODEL | 1 fan" copied in as fan id {0} ({1} curve rows); {2} fans on a {3} x {4} airway, {5:F1} m, RAD segment 2' -f $fanId, ($fanBlock.Count - 1), $fans, $fanW, $fanH, (D3 $rad[1] $rad[2])
'RAD:  {0} airways + {1:F1} m hole-through, {2:F1} m in all; own portal at E {3:F1} N {4:F1} RL {5:F1} ({6} m off the decline portal node)' -f ($rad.Count - 1), (D3 $rad[-1] $hole), ((Len $rad) + (D3 $rad[-1] $hole)), $rad[0][0], $rad[0][1], $rad[0][2], $portalSide
'hole: RAR u{0} split at E {1:F1} N {2:F1} RL {3:F1}' -f $rarU, $hole[0], $hole[1], $hole[2]
'wall: u{0} (6417 link to the decline) fixed {1} Ns2/m8' -f $wallU, $wallR
"new unique numbers u$($maxU + 1) to u$u"
if (-not $write) { 'dry run - nothing written (use -write)'; return }
if (Test-Path -LiteralPath $out) { throw "$out already exists - not overwriting" }
$L.InsertRange($end, $new); $L.InsertRange($wfEnd, [string[]]$fanBlock)
$ms = New-Object IO.MemoryStream; $gz = New-Object IO.Compression.GZipStream($ms, [IO.Compression.CompressionMode]::Compress)
$bytes = (New-Object Text.UTF8Encoding($false)).GetBytes(($L -join $nl)); $gz.Write($bytes, 0, $bytes.Length); $gz.Close()
[IO.File]::WriteAllBytes($out, $ms.ToArray())
"written: $out"
