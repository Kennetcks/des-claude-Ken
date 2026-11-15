# Rewrites the Nov 3MS block (AQ:AX = Nov-26..Jun-27) on 'Actuals and 3MS' in the OPEN MC+MD tracking sheet from the OPEN
# Physicals workbook (Deswik 3MS Dump I:P and Planned Equipment E:L). Same row mapping as tracking_mcmd_nov3ms.ps1. No save.
$ErrorActionPreference = 'Stop'
$map = [ordered]@{ '6' = 43; '7' = 32; '8' = 29; '12' = 57; '13' = 54; '14' = 53; '16' = 61; '17' = 62; '18' = 0
  '22' = 18; '23' = 19; '24' = 20; '26' = 13; '27' = 13; '28' = 14; '29' = 15; '31' = 4; '32' = 5; '33' = 6
  '35' = 23; '36' = 133; '39' = 69; '47' = 33 }
$equip = [ordered]@{ '9' = 4; '19' = 5; '37' = 6; '40' = 7 }
$labels = @{ '6' = 'Operating Development (m)'; '8' = 'Total Lateral Development (m)'; '9' = 'Number of Jumbos'; '19' = 'Number of Longhole Drills'; '33' = 'Total Ounces (Oz)'; '37' = 'Number of Loaders'; '39' = 'Total Truck tkm'; '40' = 'Number of Trucks'; '47' = 'Decline Advance (m)' }
$xl = [Runtime.InteropServices.Marshal]::GetActiveObject('Excel.Application')
$wo = $xl.Workbooks | Where-Object { $_.Name -eq 'MINE_UG_3MS_FY27_NOVEMBER_26_Physicals.xlsm' }
$wb = $xl.Workbooks | Where-Object { $_.Name -eq 'MINE UG_Monthly Tracking Sheet Budget FY27 - MC+MD.xlsx' }
if (-not $wo -or -not $wb) { throw 'Both workbooks must be open' }
$d = $wo.Worksheets.Item('Deswik 3MS Dump'); $pe = $wo.Worksheets.Item('Planned Equipment')
if ($d.Cells.Item(2, 9).Text -ne '1/11/2026' -or $pe.Cells.Item(3, 5).Text -ne 'Nov-26') { throw "November 3MS columns not where expected ($($d.Cells.Item(2, 9).Text), $($pe.Cells.Item(3, 5).Text))" }
$sh = $wb.Worksheets.Item('Actuals and 3MS')
foreach ($k in $labels.Keys) { if ($sh.Range("AK$k").Text.Trim() -ne $labels[$k]) { throw "AK$k is '$($sh.Range("AK$k").Text)', expected '$($labels[$k])' - nothing written" } }
if ($sh.Cells.Item(4, 43).Text -notlike '*Nov*' -and [datetime]::FromOADate($sh.Cells.Item(4, 43).Value2).Month -ne 11) { throw 'AQ4 is not November - nothing written' }
$before = @{ D8 = $sh.Range('D8').Text.Trim(); D33 = $sh.Range('D33').Text.Trim(); D39 = $sh.Range('D39').Text.Trim(); D47 = $sh.Range('D47').Text.Trim() }
$new = @{}
foreach ($k in $map.Keys) { if ($map[$k]) { $a = New-Object 'double[]' 8; for ($i = 0; $i -lt 8; $i++) { $a[$i] = [double]$d.Cells.Item($map[$k], 9 + $i).Value2 }; $new[$k] = $a } }
$a = New-Object 'double[]' 8; for ($i = 0; $i -lt 8; $i++) { $a[$i] = $new['16'][$i] + $new['17'][$i] }; $new['18'] = $a
foreach ($k in $equip.Keys) { $a = New-Object 'double[]' 8; for ($i = 0; $i -lt 8; $i++) { $a[$i] = [double]$pe.Cells.Item($equip[$k], 5 + $i).Value2 }; $new[$k] = $a }
foreach ($k in @($map.Keys) + @($equip.Keys)) { [int]$r = $k; for ($i = 0; $i -lt 8; $i++) { $sh.Cells.Item($r, 43 + $i).Value2 = [double]$new[$k][$i] } }
$xl.Calculate()
"Nov 3MS block AQ:AX (Nov-26..Jun-27) rewritten: $(@($map.Keys).Count + @($equip.Keys).Count) rows"
"FY27 lateral (D8):  $($before.D8) -> $($sh.Range('D8').Text.Trim())"
"FY27 ounces (D33):  $($before.D33) -> $($sh.Range('D33').Text.Trim())"
"FY27 truck tkm (D39): $($before.D39) -> $($sh.Range('D39').Text.Trim())"
"Decline advance (D47): $($before.D47) -> $($sh.Range('D47').Text.Trim())"
"Lateral Nov-Jun (AQ:AX): " + ((43..50 | ForEach-Object { $sh.Cells.Item(8, $_).Text.Trim() }) -join ' | ')
"Jumbos  Nov-Jun (AQ:AX): " + ((43..50 | ForEach-Object { $sh.Cells.Item(9, $_).Text.Trim() }) -join ' | ')
