# Build a Ventsim import DXF (R12, 3D polylines) from the W390 RAD string in 'RAD Decline draft.dcf' (decoded to rad_strings.json):
#   RAD_DECLINE      the design string as drawn (RL 6390.7 to 6422.9)
#   RAD_TO_SURFACE   straight extension from the top end on the same bearing at 1:7 up, stopping at $surfaceRL (the portal)
#   VENT_DUCT_1200   duct line from the portal to the RAD face, 1.0 m to the side of the centreline (set as 1.2 m bag in Ventsim)
#   FAN_110KW        point at the portal end of the duct (110 kW - fan curve to be set in Ventsim)
# Writes to the Desktop CAD folder; never touches the .dcf.
param([Parameter(Mandatory)][double]$surfaceRL, [double]$grade = 7.0, [double]$step = 5.0, [double]$ductOffset = 1.0,
  [string]$out = '<DESKTOP>\02 Design & Technical\CAD\SITE - RAD to surface Ventsim import.dxf')
$ErrorActionPreference = 'Stop'
$inv = [Globalization.CultureInfo]::InvariantCulture
$s = (Get-Content "$env:TEMP\dsw\rad_strings.json" -Raw | ConvertFrom-Json) | Where-Object { $_.ACT_TYPE -eq 'RAD' -and $_.N -gt 1 } | Select-Object -First 1
if (-not $s) { throw 'RAD string not found in rad_strings.json - run read_dcf_strings.ps1 first' }
$rad = @($s.Pts | ForEach-Object { , @([double]$_[0], [double]$_[1], [double]$_[2]) })
if ($rad[0][2] -lt $rad[-1][2]) { [array]::Reverse($rad) }   # top end first
$top = $rad[0]; $nx = $rad[1]
if ($surfaceRL -le $top[2]) { throw "Surface RL $surfaceRL is not above the RAD top end ($($top[2]))" }
# unit plan direction pointing up-ramp (away from the second vertex)
$dx = $top[0] - $nx[0]; $dy = $top[1] - $nx[1]; $h = [Math]::Sqrt($dx * $dx + $dy * $dy); $ux = $dx / $h; $uy = $dy / $h
$rise = $surfaceRL - $top[2]; $run = $rise * $grade
$ext = @(); $k = [Math]::Ceiling($run / $step)
for ($i = $k; $i -ge 0; $i--) { $d = [Math]::Min($i * $step, $run); $ext += , @(($top[0] + $ux * $d), ($top[1] + $uy * $d), ($top[2] + $d / $grade)) }
# duct: portal -> along extension and RAD to the face, offset to the right-hand side looking down-ramp
$path = @($ext) + @($rad | Select-Object -Skip 1)
$duct = @(); for ($i = 0; $i -lt $path.Count; $i++) {
  $a = $path[[Math]::Max($i - 1, 0)]; $b = $path[[Math]::Min($i + 1, $path.Count - 1)]
  $tx = $b[0] - $a[0]; $ty = $b[1] - $a[1]; $tl = [Math]::Sqrt($tx * $tx + $ty * $ty); if ($tl -eq 0) { $tl = 1 }
  $duct += , @(($path[$i][0] + $ty / $tl * $ductOffset), ($path[$i][1] - $tx / $tl * $ductOffset), ($path[$i][2] + 1.5)) }
function Len($p) { $l = 0; for ($i = 1; $i -lt $p.Count; $i++) { $a = $p[$i - 1]; $b = $p[$i]; $l += [Math]::Sqrt(($b[0] - $a[0]) * ($b[0] - $a[0]) + ($b[1] - $a[1]) * ($b[1] - $a[1]) + ($b[2] - $a[2]) * ($b[2] - $a[2])) }; $l }
$sb = New-Object Text.StringBuilder
function W($c, $v) { [void]$sb.Append("$c`r`n$v`r`n") }
function F($v) { $v.ToString('0.000', $inv) }
function Poly($layer, $pts) { W 0 'POLYLINE'; W 8 $layer; W 66 1; W 10 '0.0'; W 20 '0.0'; W 30 '0.0'; W 70 8
  foreach ($p in $pts) { W 0 'VERTEX'; W 8 $layer; W 10 (F $p[0]); W 20 (F $p[1]); W 30 (F $p[2]); W 70 32 }; W 0 'SEQEND'; W 8 $layer }
W 0 'SECTION'; W 2 'ENTITIES'
Poly 'RAD_DECLINE' $rad
Poly 'RAD_TO_SURFACE' $ext
Poly 'VENT_DUCT_1200' $duct
W 0 'POINT'; W 8 'FAN_110KW'; W 10 (F $duct[0][0]); W 20 (F $duct[0][1]); W 30 (F $duct[0][2])
W 0 'ENDSEC'; W 0 'EOF'
[IO.File]::WriteAllText($out, $sb.ToString(), [Text.Encoding]::ASCII)
"written: $out"
'RAD_DECLINE     {0,6:F1} m  RL {1:F1} to {2:F1}' -f (Len $rad), $rad[-1][2], $rad[0][2]
'RAD_TO_SURFACE  {0,6:F1} m  RL {1:F1} to {2:F1}, portal E {3:F1} N {4:F1}' -f (Len $ext), $top[2], $ext[0][2], $ext[0][0], $ext[0][1]
'VENT_DUCT_1200  {0,6:F1} m  portal to RAD face' -f (Len $duct)
'FAN_110KW       at E {0:F1} N {1:F1} RL {2:F1}' -f $duct[0][0], $duct[0][1], $duct[0][2]
