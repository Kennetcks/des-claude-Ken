# READ ONLY: monthly LOM physicals from lom_tasks.tsv (written by lom_by_fy.ps1): lateral dev m, stope t, total t, haul tkm and the
# schedule resources busy each month (share of the month with at least one task on that resource). Writes lom_monthly.tsv.
param([string]$in = "$env:TEMP\dsw\lom_tasks.tsv", [string]$out = "$env:TEMP\dsw\lom_monthly.tsv")
$ErrorActionPreference = 'Stop'
$inv = [Globalization.CultureInfo]::InvariantCulture
function Num($v) { $d = 0.0; if ([double]::TryParse("$v", [Globalization.NumberStyles]::Float, $inv, [ref]$d)) { $d } else { 0.0 } }
$tasks = Import-Csv $in -Delimiter "`t"
$mo = @{}
foreach ($k in $tasks) {
  $st = [datetime]::MinValue; $fi = [datetime]::MinValue
  if (-not [datetime]::TryParse($k.Start, $inv, [Globalization.DateTimeStyles]::None, [ref]$st) -or -not [datetime]::TryParse($k.Finish, $inv, [Globalization.DateTimeStyles]::None, [ref]$fi)) { continue }
  $dur = ($fi - $st).TotalDays; $m = Num $k.'Sch_metres'; $tn = Num $k.'Sch_Diluted tonnes'; $tkm = Num $k.'Sch_Haul_tkm'
  $c = [datetime]::new($st.Year, $st.Month, 1)
  while ($c -le $fi) {
    $n = $c.AddMonths(1); $lo = if ($st -gt $c) { $st } else { $c }; $hi = if ($fi -lt $n) { $fi } else { $n }
    $f = if ($dur -le 0) { 1 } else { [Math]::Max(0, ($hi - $lo).TotalDays) / $dur }
    $key = $c.ToString('yyyy-MM'); if (-not $mo[$key]) { $mo[$key] = @{ lat = 0.0; sto = 0.0; tot = 0.0; tkm = 0.0; res = @{} } }; $r = $mo[$key]
    if ($f -gt 0) {
      if ($k.STO_DEV -eq 'DEV' -and $k.LAT_VERT -eq 'LAT') { $r.lat += $m * $f }
      if ($k.STO_DEV -eq 'STO') { $r.sto += $tn * $f }; $r.tot += $tn * $f; $r.tkm += $tkm * $f
      if ($k.Resource -and ($m -gt 0 -or $tn -gt 0)) { $days = ($hi - $lo).TotalDays; $r.res[$k.Resource] = [double]$r.res[$k.Resource] + $days } }
    $c = $n } }
$resNames = $tasks | Where-Object { $_.Resource } | ForEach-Object Resource | Sort-Object -Unique
$lines = @("Month`tLatDev_m`tStope_t`tTotal_t`tHaul_tkm`t" + ($resNames -join "`t"))
foreach ($key in ($mo.Keys | Sort-Object)) { $r = $mo[$key]; $dim = [datetime]::DaysInMonth([int]$key.Substring(0, 4), [int]$key.Substring(5, 2))
  $lines += "$key`t" + ('{0:F0}' -f $r.lat) + "`t" + ('{0:F0}' -f $r.sto) + "`t" + ('{0:F0}' -f $r.tot) + "`t" + ('{0:F0}' -f $r.tkm) + "`t" + (($resNames | ForEach-Object { '{0:F2}' -f ([Math]::Min(9.99, [double]$r.res[$_] / $dim)) }) -join "`t") }
$lines | Set-Content $out -Encoding UTF8
$lines | ForEach-Object { $_ }
