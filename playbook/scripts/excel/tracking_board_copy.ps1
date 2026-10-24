# Board 2 = copy of board 1 (layout, equipment rows, KPIs) with the October 3MS in the 3MS block for Oct-Jun (AP:AX).
# Jul-Sep (AM:AO) keep board 1's values. The previous board 2 is backed up to %TEMP%\dsw first.
$ErrorActionPreference = 'Stop'
$dir = "<SHARE>\Reporting\Monthly Tracking Sheet\FY27"
$b1 = "$dir\MINE UG_Monthly Tracking Sheet Budget FY27_board.xlsx"
$b2 = "$dir\MINE UG_Monthly Tracking Sheet Budget FY27 - board 2.xlsx"
$oct = "<SHARE>\3MS\04 Oct\Documentation\MINE_UG_3MS_FY27_OCTOBER_26_Physicals.xlsm"
$bak = "$env:TEMP\dsw\MINE UG_Monthly Tracking Sheet Budget FY27 - board 2_before_19-10.xlsx"
# board row (Actuals and 3MS, AK label) -> October dump row; equipment rows use the Planned Equipment tab
$map = [ordered]@{ '6' = 43; '7' = 32; '8' = 29; '12' = 57; '13' = 54; '14' = 53; '16' = 61; '17' = 62; '18' = 0
  '22' = 18; '23' = 19; '24' = 20; '26' = 13; '27' = 13; '28' = 14; '29' = 15; '31' = 4; '32' = 5; '33' = 6
  '35' = 23; '36' = 133; '39' = 69; '47' = 33 }
$equip = [ordered]@{ '9' = 4; '19' = 5; '37' = 6; '40' = 7 }   # board row -> Planned Equipment row (Jumbo, Prod Drills, Loader, Truck)
$labels = @{ '6' = 'Operating Development (m)'; '8' = 'Total Lateral Development (m)'; '9' = 'Number of Jumbos'; '19' = 'Number of Longhole Drills'
  '24' = 'Development Ounces (Oz)'; '33' = 'Total Ounces (Oz)'; '37' = 'Number of Loaders'; '39' = 'Total Truck tkm'; '40' = 'Number of Trucks'; '47' = 'Decline Advance (m)' }

$xs = New-Object -ComObject Excel.Application; $xs.Visible = $false; $xs.DisplayAlerts = $false; $xs.AskToUpdateLinks = $false; $xs.AutomationSecurity = 3
try {
  # read the October 3MS (Oct-Jun = dump columns I..Q, Planned Equipment columns E..M)
  $wo = $xs.Workbooks.Open($oct, 0, $true); $d = $wo.Worksheets.Item('Deswik 3MS Dump'); $pe = $wo.Worksheets.Item('Planned Equipment')
  $new = @{}
  foreach ($k in $map.Keys) { if ($map[$k]) { $a = New-Object 'double[]' 9; for ($i = 0; $i -lt 9; $i++) { $a[$i] = [double]$d.Cells.Item($map[$k], 9 + $i).Value2 }; $new[$k] = $a } }
  $a = New-Object 'double[]' 9; for ($i = 0; $i -lt 9; $i++) { $a[$i] = $new['16'][$i] + $new['17'][$i] }; $new['18'] = $a
  foreach ($k in $equip.Keys) { $a = New-Object 'double[]' 9; for ($i = 0; $i -lt 9; $i++) { $a[$i] = [double]$pe.Cells.Item($equip[$k], 5 + $i).Value2 }; $new[$k] = $a }
  if ($pe.Cells.Item(3, 5).Text -ne 'Oct-26' -or $d.Cells.Item(2, 9).Text -ne '1/10/2026') { throw 'October 3MS columns are not where expected' }
  $wo.Close($false)

  # board 2 = board 1
  if (-not (Test-Path -LiteralPath $bak)) { Copy-Item -LiteralPath $b2 -Destination $bak }
  Copy-Item -LiteralPath $b1 -Destination $b2 -Force
  $wb = $xs.Workbooks.Open($b2); $sh = $wb.Worksheets.Item('Actuals and 3MS')
  foreach ($k in $labels.Keys) { if ($sh.Range("AK$k").Text.Trim() -ne $labels[$k]) { $wb.Close($false); Copy-Item -LiteralPath $bak -Destination $b2 -Force; throw "AK$k is '$($sh.Range("AK$k").Text)', expected '$($labels[$k])' - board 2 restored" } }
  "row (AK label)                            Oct..Jun: board 1 (Nov 3MS) -> October 3MS"
  foreach ($k in @($map.Keys) + @($equip.Keys)) { [int]$r = $k
    $old = (42..50 | ForEach-Object { $sh.Cells.Item($r, $_).Text.Trim() }) -join ','
    for ($i = 0; $i -lt 9; $i++) { $sh.Cells.Item($r, 42 + $i).Value2 = [double]$new[$k][$i] }
    "{0,-42} {1}  ->  {2}" -f $sh.Range("AK$r").Text.Trim(), $old, ((42..50 | ForEach-Object { $sh.Cells.Item($r, $_).Text.Trim() }) -join ',') }
  for ($c = 42; $c -le 50; $c++) { $sh.Cells.Item(3, $c).Value2 = 'Oct 3MS' }
  $xs.CalculateFull()
  "FY totals (D): lateral " + $sh.Range('D8').Text.Trim() + ", ounces " + $sh.Range('D33').Text.Trim() + ", tkm " + $sh.Range('D39').Text.Trim() + ", jumbos " + $sh.Range('D9').Text.Trim() + ", m/jumbo " + $sh.Range('D10').Text.Trim()
  $wb.Save(); $wb.Close($false); "saved $b2 ; previous board 2 backed up to $bak"
} finally { $xs.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($xs) }
