# Put the November 3MS (SCH =1, all mines) into the 3MS input block of the open Monthly Tracking Sheet
# ('Actuals and 3MS'!AQ:AX = Nov-Jun). October (AP) and earlier are left as they are.
# -Apply writes into the user's open Excel; without it, only prints old vs new. Never saves.
param([switch]$Apply)
$ErrorActionPreference = 'Stop'
$src = "<SHARE>\3MS\05 Nov\Documentation\MINE_UG_3MS_FY27_NOVEMBER_26_Physicals.xlsm"
# tracking row -> Deswik 3MS Dump row (Nov-26..Jun-27 = dump columns I..P)
# (string keys: an [ordered] dictionary indexes integer keys by position, not by key)
$map = [ordered]@{ '5' = 43; '6' = 32; '7' = 29; '9' = 57; '10' = 54; '11' = 53; '13' = 61; '14' = 62; '15' = 0
  '17' = 18; '18' = 19; '19' = 20; '21' = 13; '22' = 13; '23' = 14; '24' = 15; '26' = 4; '27' = 5; '28' = 6
  '30' = 23; '31' = 133; '32' = 69; '38' = 33 }
$new = @{}
$xs = New-Object -ComObject Excel.Application; $xs.Visible = $false; $xs.DisplayAlerts = $false; $xs.AutomationSecurity = 3
try { $wb = $xs.Workbooks.Open($src, 0, $true); $d = $wb.Worksheets.Item('Deswik 3MS Dump')
  foreach ($k in $map.Keys) { if ($map[$k]) { $new[$k] = @(9..16 | ForEach-Object { [double]$d.Cells.Item($map[$k], $_).Value2 }) } }
  $wb.Close($false) } finally { $xs.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($xs) }
$new['15'] = @(0..7 | ForEach-Object { $new['13'][$_] + $new['14'][$_] })

$xl = [Runtime.InteropServices.Marshal]::GetActiveObject('Excel.Application')
$tw = $null; foreach ($w in $xl.Workbooks) { if ($w.Name -like 'the mine UG_Monthly Tracking*') { $tw = $w } }
if (-not $tw) { throw 'Monthly Tracking Sheet is not open' }
$sh = $tw.Worksheets.Item('Actuals and 3MS')
$cols = 43..50   # AQ..AX
"{0,-42} {1}" -f 'row (old -> new)', (($cols | ForEach-Object { $sh.Cells.Item(3, $_).Text.Trim().Substring(0, 3) }) -join '          ')
foreach ($k in $map.Keys) { $r = [int]$k
  $lab = $sh.Cells.Item($r, 37).Text.Trim(); $fmt = if ($lab -like '*g/t*') { 'N2' } else { 'N0' }
  $cells = for ($i = 0; $i -lt 8; $i++) { $o = [double]$sh.Cells.Item($r, $cols[$i]).Value2; '{0}>{1}' -f $o.ToString($fmt), $new[$k][$i].ToString($fmt) }
  "{0,-42} {1}" -f $lab, ($cells -join '  ') }
if ($Apply) {
  for ($i = 0; $i -lt 8; $i++) { $sh.Cells.Item(2, $cols[$i]).Value2 = 'Nov 3MS' }
  foreach ($k in $map.Keys) { $r = [int]$k; for ($i = 0; $i -lt 8; $i++) { $sh.Cells.Item($r, $cols[$i]).Value2 = $new[$k][$i] } }
  "written into the open workbook (not saved). D7 FY total now: " + $sh.Range('D7').Text + ", D28: " + $sh.Range('D28').Text }
