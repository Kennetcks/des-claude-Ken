# READ ONLY: LOM physicals by financial year (Jul-Jun) from an extracted IS .dsf (tmp.dxf Tasks section): development metres,
# tonnes, ounces, haul tkm, deepest RL reached, and the resources used (from PhysicalResourceId -> Resources section).
# Quantities are split across years in proportion to each task's duration inside the year.
param([string]$dxf = (Get-ChildItem "$env:TEMP\dsw\jj_dsf\x\*.tmp.dxf" | Select-Object -First 1).FullName, [string]$outTsv = "$env:TEMP\dsw\lom_tasks.tsv")
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName Microsoft.VisualBasic
$L = [IO.File]::ReadAllLines($dxf)
function Csv($line) { $p = New-Object Microsoft.VisualBasic.FileIO.TextFieldParser((New-Object IO.StringReader($line))); $p.SetDelimiters(','); $p.HasFieldsEnclosedInQuotes = $true; $r = $p.ReadFields(); $p.Close(); , $r }
# resources: id -> name
$res = @{}; $s = [array]::IndexOf($L, '1,Resources'); for ($i = $s + 1; -not $L[$i].StartsWith('1,'); $i++) { if ($L[$i].StartsWith("`t3,")) { $c = Csv $L[$i].Substring(3); $res[$c[0]] = $c[1] } }
$t = [array]::IndexOf($L, '1,Tasks'); $hdr = Csv ($L[$t + 1].TrimStart("`t").Substring(2)); $ix = @{}; for ($i = 0; $i -lt $hdr.Count; $i++) { if (-not $ix.ContainsKey($hdr[$i])) { $ix[$hdr[$i]] = $i } }
$want = 'Name', 'Start', 'Finish', 'Is Summary', 'MINE', 'LEVEL', 'ACT_TYPE', 'LAT_VERT', 'STO_DEV', 'ORE_FLAG', 'Sch_metres', 'Sch_Diluted tonnes', 'Sch_Diluted Au Ounces', 'Sch_Haul_tkm', 'PhysicalResourceId'
foreach ($w in $want) { if (-not $ix.ContainsKey($w)) { throw "column '$w' not in Tasks" } }
$tasks = New-Object Collections.Generic.List[object]
for ($i = $t + 2; -not $L[$i].StartsWith('1,'); $i++) {
  if (-not $L[$i].StartsWith("`t3,")) { continue }
  $c = Csv $L[$i].Substring(3); if ($c[$ix['Is Summary']] -ne '0') { continue }
  $o = [ordered]@{}; foreach ($w in $want) { $o[$w] = if ($ix[$w] -lt $c.Count) { $c[$ix[$w]] } else { '' } }
  $o['Resource'] = $res[$o['PhysicalResourceId']]; $tasks.Add([pscustomobject]$o) }
$tasks | Export-Csv $outTsv -Delimiter "`t" -NoTypeInformation -Encoding UTF8
function Num($v) { $d = 0.0; if ([double]::TryParse("$v", [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$d)) { $d } else { 0.0 } }
$fy = @{}
foreach ($k in $tasks) {
  $st = [datetime]::MinValue; $fi = [datetime]::MinValue
  if (-not [datetime]::TryParse($k.Start, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref]$st)) { continue }
  if (-not [datetime]::TryParse($k.Finish, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref]$fi)) { continue }
  $dur = ($fi - $st).TotalDays; $m = Num $k.'Sch_metres'; $tn = Num $k.'Sch_Diluted tonnes'; $oz = Num $k.'Sch_Diluted Au Ounces'; $tkm = Num $k.'Sch_Haul_tkm'
  $y0 = if ($st.Month -ge 7) { $st.Year + 1 } else { $st.Year }; $y1 = if ($fi.Month -ge 7) { $fi.Year + 1 } else { $fi.Year }
  for ($y = $y0; $y -le $y1; $y++) {
    $a = [datetime]::new($y - 1, 7, 1); $b = [datetime]::new($y, 7, 1); $lo = if ($st -gt $a) { $st } else { $a }; $hi = if ($fi -lt $b) { $fi } else { $b }
    $f = if ($dur -le 0) { if ($st -ge $a -and $st -lt $b) { 1 } else { 0 } } else { [Math]::Max(0, ($hi - $lo).TotalDays) / $dur }; if ($f -le 0) { continue }
    if (-not $fy[$y]) { $fy[$y] = @{ lat = 0.0; vert = 0.0; dec = 0.0; dev_t = 0.0; sto_t = 0.0; oz = 0.0; tkm = 0.0; minRL = 99999.0; res = @{} } }
    $r = $fy[$y]
    if ($k.STO_DEV -eq 'DEV' -and $k.LAT_VERT -eq 'LAT') { $r.lat += $m * $f }; if ($k.LAT_VERT -eq 'VERT') { $r.vert += $m * $f }; if ($k.ACT_TYPE -eq 'DEC') { $r.dec += $m * $f }
    if ($k.STO_DEV -eq 'STO') { $r.sto_t += $tn * $f } else { $r.dev_t += $tn * $f }; $r.oz += $oz * $f; $r.tkm += $tkm * $f
    if ($m -gt 0 -or $tn -gt 0) { $lv = Num $k.LEVEL; if ($lv -gt 0 -and $lv -lt $r.minRL) { $r.minRL = $lv } }
    if ($k.Resource) { $r.res[$k.Resource] = 1 } } }
'FY    | lat dev m | vert m | decline m | dev t   | stope t  | oz     | haul tkm  | deepest LEVEL | resources used'
foreach ($y in ($fy.Keys | Sort-Object)) { $r = $fy[$y]; 'FY{0} | {1,9:N0} | {2,6:N0} | {3,9:N0} | {4,7:N0} | {5,8:N0} | {6,6:N0} | {7,9:N0} | {8,5} | {9}' -f ($y % 100), $r.lat, $r.vert, $r.dec, $r.dev_t, $r.sto_t, $r.oz, $r.tkm, $(if ($r.minRL -lt 99999) { $r.minRL } else { '-' }), (($r.res.Keys | Sort-Object) -join ', ') }
"tasks: $($tasks.Count) (written to $outTsv)"
