# New tracking sheet "…FY27 - COPY.xlsx" = copy of Board 1, with the November 3MS (all mines, SCHEDULED = 1, 21/10/2026)
# in the 3MS block Nov-Jun (AQ:AX) incl. equipment, and October (column H) showing the month plan (3MS block AP) instead of
# part-month actuals. Board 1 is not touched; refuses to overwrite an existing COPY file.
$ErrorActionPreference = 'Stop'
$dir = "<SHARE>\Reporting\Monthly Tracking Sheet\FY27"
$b1 = "$dir\Board\MINE UG_Monthly Tracking Sheet Budget FY27_board.xlsx"
$dst = "$dir\MINE UG_Monthly Tracking Sheet Budget FY27 - COPY.xlsx"
$nov = "<SHARE>\3MS\05 Nov\Documentation\MINE_UG_3MS_FY27_NOVEMBER_26_Physicals.xlsm"
if (Test-Path -LiteralPath $dst) { "COPY file already exists - stopping so nothing is overwritten"; return }
# board row (Actuals and 3MS) -> November dump row (same mapping as the October build of board 2)
$map = [ordered]@{ '6' = 43; '7' = 32; '8' = 29; '12' = 57; '13' = 54; '14' = 53; '16' = 61; '17' = 62; '18' = 0
  '22' = 18; '23' = 19; '24' = 20; '26' = 13; '27' = 13; '28' = 14; '29' = 15; '31' = 4; '32' = 5; '33' = 6
  '35' = 23; '36' = 133; '39' = 69; '47' = 33 }
$equip = [ordered]@{ '9' = 4; '19' = 5; '37' = 6; '40' = 7 }   # Jumbo, Prod Drills, Loader, Truck (Planned Equipment rows)
$labels = @{ '6' = 'Operating Development (m)'; '8' = 'Total Lateral Development (m)'; '9' = 'Number of Jumbos'; '19' = 'Number of Longhole Drills'; '33' = 'Total Ounces (Oz)'; '37' = 'Number of Loaders'; '39' = 'Total Truck tkm'; '40' = 'Number of Trucks'; '47' = 'Decline Advance (m)' }
$xs = New-Object -ComObject Excel.Application; $xs.Visible = $false; $xs.DisplayAlerts = $false; $xs.AskToUpdateLinks = $false; $xs.AutomationSecurity = 3
try {
  $wo = $xs.Workbooks.Open($nov, 0, $true); $d = $wo.Worksheets.Item('Deswik 3MS Dump'); $pe = $wo.Worksheets.Item('Planned Equipment')
  if ($d.Cells.Item(2, 9).Text -ne '1/11/2026' -or $pe.Cells.Item(3, 5).Text -ne 'Nov-26') { throw "November 3MS columns not where expected ($($d.Cells.Item(2, 9).Text), $($pe.Cells.Item(3, 5).Text))" }
  $new = @{}
  foreach ($k in $map.Keys) { if ($map[$k]) { $a = New-Object 'double[]' 8; for ($i = 0; $i -lt 8; $i++) { $a[$i] = [double]$d.Cells.Item($map[$k], 9 + $i).Value2 }; $new[$k] = $a } }
  $a = New-Object 'double[]' 8; for ($i = 0; $i -lt 8; $i++) { $a[$i] = $new['16'][$i] + $new['17'][$i] }; $new['18'] = $a
  foreach ($k in $equip.Keys) { $a = New-Object 'double[]' 8; for ($i = 0; $i -lt 8; $i++) { $a[$i] = [double]$pe.Cells.Item($equip[$k], 5 + $i).Value2 }; $new[$k] = $a }
  $wo.Close($false)
  Copy-Item -LiteralPath $b1 -Destination $dst
  $wb = $xs.Workbooks.Open($dst); $sh = $wb.Worksheets.Item('Actuals and 3MS')
  foreach ($k in $labels.Keys) { if ($sh.Range("AK$k").Text.Trim() -ne $labels[$k]) { $wb.Close($false); Remove-Item -LiteralPath $dst; throw "AK$k is '$($sh.Range("AK$k").Text)', expected '$($labels[$k])' - COPY file removed" } }
  foreach ($k in @($map.Keys) + @($equip.Keys)) { [int]$r = $k; for ($i = 0; $i -lt 8; $i++) { $sh.Cells.Item($r, 43 + $i).Value2 = [double]$new[$k][$i] } }
  for ($c = 43; $c -le 50; $c++) { $sh.Cells.Item(3, $c).Value2 = 'Nov 3MS' }
  # October summary (column H): month plan from the 3MS block instead of part-month actuals
  $nOct = 0
  for ($r = 6; $r -le 47; $r++) { $f = [string]$sh.Range("H$r").Formula; if ($f -eq "=IF(Z$r>0,Z$r,AP$r)") { $sh.Range("H$r").Formula = "=AP$r"; $nOct++ } }
  $sh.Range('H3').Value2 = 'Planned'
  $xs.CalculateFull()
  "3MS block AQ:AX (Nov-Jun) written from the November 3MS; October column H now = 3MS plan on $nOct rows"
  "FY totals (D): lateral " + $sh.Range('D8').Text.Trim() + ", ounces " + $sh.Range('D33').Text.Trim() + ", tkm " + $sh.Range('D39').Text.Trim() + ", decline " + $sh.Range('L47').Text.Trim()
  "Oct (H): lateral " + $sh.Range('H8').Text.Trim() + ", ounces " + $sh.Range('H33').Text.Trim() + ", decline " + $sh.Range('H47').Text.Trim()
  "E:P lateral: " + ((5..16 | ForEach-Object { $sh.Cells.Item(8, $_).Text.Trim() }) -join ' | ')
  "E:P ounces:  " + ((5..16 | ForEach-Object { $sh.Cells.Item(33, $_).Text.Trim() }) -join ' | ')
  $wb.Save(); $wb.Close($false); "saved $dst"
} finally { $xs.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($xs) }
