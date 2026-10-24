# Add equipment and KPI rows (reference-site layout) to 'Actuals and 3MS' in the open Monthly Tracking Sheet.
# Counts: Nov-Jun from the November 3MS 'Planned Equipment' tab; Jul-Oct from the October 3MS (Oct-26 column).
# Whole rows are inserted bottom-up so the actuals (U:AH) and 3MS (AM:AX) blocks stay aligned. Never saves.
$ErrorActionPreference = 'Stop'
$peach = 11851260   # RGB(252,213,180), the orange rows in the reference-site sheet
#            Jul Aug Sep Oct Nov Dec Jan Feb Mar Apr May Jun
$jumbo  = @(1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2)   # example counts - take them from the 3MS 'Planned Equipment' tab
$drill  = @(0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1)
$loader = @(1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2)
$truck  = @(1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2)
$xl = [Runtime.InteropServices.Marshal]::GetActiveObject('Excel.Application')
$tw = $null; foreach ($w in $xl.Workbooks) { if ($w.Name -like 'the mine UG_Monthly Tracking*') { $tw = $w } }
if (-not $tw) { throw 'Monthly Tracking Sheet is not open' }
$sh = $tw.Worksheets.Item('Actuals and 3MS')
$chk = @{ 7 = 'Total Lateral Development (m)'; 15 = 'Total Longhole Meters (m)'; 31 = 'Total Movement (t)'; 32 = 'Total Truck tkm' }
foreach ($k in $chk.Keys) { if ($sh.Cells.Item($k, 3).Text.Trim() -ne $chk[$k]) { throw "row $k is not '$($chk[$k])' - layout changed, nothing done" } }
# after row, count label, KPI label, counts, KPI numerator row (before insert)
$adds = @(
  @{ after = 32; n = 'Number of Trucks';  kpi = 'TKM per Truck';           v = $truck;  src = 32 },
  @{ after = 31; n = 'Number of Loaders'; kpi = 'Tonnes per Loader';       v = $loader; src = 31 },
  @{ after = 15; n = 'Number of Longhole Drills'; kpi = 'Metres per Longhole Drill'; v = $drill; src = 15 },
  @{ after = 7;  n = 'Number of Jumbos';  kpi = 'Metres per Jumbo';        v = $jumbo;  src = 7 })
foreach ($a in $adds) {
  $r1 = $a.after + 1; $r2 = $a.after + 2
  # resumable: a block already inserted by an earlier, interrupted run is filled, not inserted again
  if ($sh.Cells.Item($r1, 3).Text.Trim() -ne $a.n) { [void]$sh.Range("$r1`:$r2").EntireRow.Insert(-4121, 0) }   # shift down, format from above
  $sh.Cells.Item($r1, 3).Value2 = $a.n; $sh.Cells.Item($r2, 3).Value2 = $a.kpi
  for ($i = 0; $i -lt 12; $i++) { $c = 5 + $i; $col = $sh.Cells.Item(1, $c).Address($false, $false) -replace '\d', ''
    $sh.Cells.Item($r1, $c).Value2 = [double]($a['v'][$i])
    $sh.Cells.Item($r2, $c).Formula = "=IFERROR($col$($a.src)/$col$r1,0)" }
  $sh.Range("D$r1`:D$r2").ClearContents()
  $rg = $sh.Range("C$r1`:P$r2"); $rg.Interior.Color = $peach; $rg.Font.Bold = $true
  $sh.Range("E$r1`:P$r1").NumberFormat = '0'; $sh.Range("E$r2`:P$r2").NumberFormat = '#,##0'
  "added rows $r1-$r2 ($($a.n) / $($a.kpi)) under row $($a.after)" }
foreach ($r in 1..50) { $t = $sh.Cells.Item($r, 3).Text.Trim(); if ($t -match 'Number of|per ') { "{0,-28} {1}" -f $t, ((5..16 | ForEach-Object { $sh.Cells.Item($r, $_).Text.Trim() }) -join ' | ') } }
"saved flag: $($tw.Saved) (not saved)"
