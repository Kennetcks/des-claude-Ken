# Read-only: compares the open Physicals and MC+MD tracking workbooks against a fresh Deswik export (%TEMP%\dsw\phys_*.tsv,
# C:\dswtmp\dev_dump.tsv). Prints cell counts and the largest difference per tab.
$inv = [Globalization.CultureInfo]::InvariantCulture
$xl = [Runtime.InteropServices.Marshal]::GetActiveObject('Excel.Application')
$ph = $xl.Workbooks | Where-Object { $_.Name -eq 'MINE_UG_3MS_FY27_NOVEMBER_26_Physicals.xlsm' }
function cmp($rows, $periods, $range, $skipRow) {
  $v = $range.Value2; $n = 0; $diff = 0; $max = 0; $where = ''
  for ($i = 0; $i -lt $rows.Count; $i++) {
    if ($skipRow -contains $i) { continue }
    for ($c = 0; $c -lt $periods.Count; $c++) {
      $s = $rows[$i].($periods[$c]); $a = 0.0; if ($s -and $s -ne 'ERR') { $a = [double]::Parse($s, $inv) }
      $cell = $v.GetValue($i + 1, $c + 1); $b = 0.0; if ($cell -is [double]) { $b = [double]$cell }
      $n++; $d = [math]::Abs($a - $b)
      if ($d -gt 0.001) { $diff++; if ($d -gt $max) { $max = $d; $where = "row $($i + 1) '$($rows[$i].Title)' col $($c + 1): export $a vs sheet $b" } }
    }
  }
  "$n cells compared, $diff differ" + $(if ($diff) { ", largest: $where" } else { '' })
}
$m = @(Import-Csv "$env:TEMP\dsw\phys_main.tsv" -Delimiter "`t"); $mp = @($m[0].PSObject.Properties.Name | Select-Object -Skip 4)
'Deswik 3MS Dump (haulage tkm row skipped, it is filled by sum): ' + (cmp $m $mp $ph.Worksheets.Item('Deswik 3MS Dump').Range("I3:U$(2 + $m.Count)") @(66))
$d = @(Import-Csv "$env:TEMP\dsw\phys_daily.tsv" -Delimiter "`t"); $dp = @($d[0].PSObject.Properties.Name | Select-Object -Skip 4)
'For Daily Report: ' + (cmp $d $dp $ph.Worksheets.Item('For Daily Report').Range('D3:AG36') @())
$b = @(Import-Csv "$env:TEMP\dsw\phys_blend.tsv" -Delimiter "`t"); $bp = @($b[0].PSObject.Properties.Name | Select-Object -Skip 4)
'Grade Bins: ' + (cmp $b $bp $ph.Worksheets.Item('Grade Bins').Range("E2:P$(1 + $b.Count)") @())
$ws = $ph.Worksheets.Item('Deswik 3MS Dump'); $t = $ws.Range('I69:P71').Value2; $ok = $true
for ($c = 1; $c -le 8; $c++) { if ([math]::Abs($t.GetValue(1, $c) - $t.GetValue(2, $c) - $t.GetValue(3, $c)) -gt 0.01) { $ok = $false } }
"Haulage tkm row = Ore & LG + Waste: $ok"
$dev = @(Get-Content C:\dswtmp\dev_dump.tsv | Where-Object { $_ }); $dd = $ph.Worksheets.Item('Dev Dump')
$tot = $dd.Range('B78:D78').Value2
'Dev Dump totals: sheet ' + ((1..3 | ForEach-Object { [math]::Round($tot.GetValue(1, $_), 1) }) -join ' / ') + ' vs tasks 348.0 / 364.6 / 481.6; headings sheet ' + (@($dd.Range('A2:A200').Value2) | Where-Object { $_ -and $_ -ne 'Total' } | Measure-Object).Count + " vs export $($dev.Count)"
$tr = ($xl.Workbooks | Where-Object { $_.Name -like '*MC+MD.xlsx' }).Worksheets.Item('Actuals and 3MS')
$map = [ordered]@{ '6' = 43; '7' = 32; '8' = 29; '12' = 57; '13' = 54; '14' = 53; '16' = 61; '17' = 62; '22' = 18; '23' = 19; '24' = 20; '26' = 13; '27' = 13; '28' = 14; '29' = 15; '31' = 4; '32' = 5; '33' = 6; '35' = 23; '36' = 133; '39' = 69; '47' = 33 }
$bad = 0; $n = 0
foreach ($k in $map.Keys) { for ($i = 0; $i -lt 8; $i++) { $n++; if ([math]::Abs([double]$tr.Cells.Item([int]$k, 43 + $i).Value2 - [double]$ws.Cells.Item($map[$k], 9 + $i).Value2) -gt 0.001) { $bad++ } } }
"Tracking AQ:AX vs Physicals dump: $n cells, $bad differ"
'Tracking lateral row 8 (AQ:AX): ' + ((43..50 | ForEach-Object { $tr.Cells.Item(8, $_).Text.Trim() }) -join ' | ')
