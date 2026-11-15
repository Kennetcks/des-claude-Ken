# Refreshes the dump tabs of the Physicals workbook that is OPEN in Excel (no open/save/close) from the latest Deswik export:
# %TEMP%\dsw\phys_main.tsv / phys_daily.tsv / phys_blend.tsv and C:\dswtmp\dev_dump.tsv. Same layout rules as build_nov_physicals_refresh.ps1.
# Labels are written with .Value, numbers as 2D double arrays with .Value2.
$ErrorActionPreference = 'Stop'
$inv = [Globalization.CultureInfo]::InvariantCulture
function num($s) { if ($null -eq $s -or "$s" -eq '' -or "$s" -eq 'ERR') { return $null }; [double]::Parse("$s", $inv) }
function arr($rows, $cols) { New-Object 'object[,]' $rows, $cols }
$dir = "$env:TEMP\dsw"
$main = @(Import-Csv "$dir\phys_main.tsv" -Delimiter "`t"); $daily = @(Import-Csv "$dir\phys_daily.tsv" -Delimiter "`t"); $blend = @(Import-Csv "$dir\phys_blend.tsv" -Delimiter "`t")
$mainP = @($main[0].PSObject.Properties.Name | Select-Object -Skip 4); $dayP = @($daily[0].PSObject.Properties.Name | Select-Object -Skip 4); $blP = @($blend[0].PSObject.Properties.Name | Select-Object -Skip 4)
$dev = @(Get-Content 'C:\dswtmp\dev_dump.tsv' | Where-Object { $_ } | ForEach-Object { , ($_ -split "`t") })
$xl = [Runtime.InteropServices.Marshal]::GetActiveObject('Excel.Application')
$wb = $xl.Workbooks | Where-Object { $_.Name -like 'MINE_UG_3MS_FY27_NOVEMBER_26_Physicals.xlsm' }
if (-not $wb) { throw 'Physicals workbook is not open' }
$log = New-Object System.Collections.Generic.List[string]
# --- Deswik 3MS Dump: E3:E199 titles must match the report row order
$ws = $wb.Worksheets.Item('Deswik 3MS Dump'); $n = $main.Count; $bad = 0
for ($i = 0; $i -lt $n; $i++) { if (([string]$ws.Cells.Item(3 + $i, 5).Value2).Trim() -ne ([string]$main[$i].Title).Trim()) { $bad++ } }
if ($bad) { throw "Deswik 3MS Dump: $bad title mismatches - nothing written" }
$h = arr 1 13; for ($c = 0; $c -lt 13; $c++) { $h[0, $c] = [double]([datetime]::ParseExact($mainP[$c], 'yyyy-MM-dd', $inv)).ToOADate() }
$ws.Range('I2:U2').Value2 = $h
$v = arr $n 13; for ($i = 0; $i -lt $n; $i++) { for ($c = 0; $c -lt 13; $c++) { $x = num $main[$i].($mainP[$c]); if ($null -ne $x) { $v[$i, $c] = $x } } }
$ws.Range("I3:U$(2 + $n)").ClearContents(); $ws.Range("I3:U$(2 + $n)").Value2 = $v
$log.Add("Deswik 3MS Dump: $n rows x 13 months (Nov-26 to Nov-27)")
if ([string]$ws.Range('E69').Value2 -like 'Total UG Haualge - tkm*' -and -not ($v[66, 0])) {
  $t = arr 1 13; for ($c = 0; $c -lt 13; $c++) { $t[0, $c] = [double]([double]$v[67, $c] + [double]$v[68, $c]) }; $ws.Range('I69:U69').Value2 = $t
  if ($ws.Range('I69').Comment) { $ws.Range('I69').Comment.Delete() }
  [void]$ws.Range('I69').AddComment("29/10/2026: Deswik returned 0 for this formula row in the export, so I69:U69 = Ore & LG tkm (row 70) + Waste tkm (row 71). Check against the 3MP report in Deswik.")
  $log.Add('  Total UG Haulage tkm filled as Ore & LG + Waste') }
# --- For Daily Report: 34 rows by position (sheet labels kept), 30 days of November
$ws = $wb.Worksheets.Item('For Daily Report'); $n = $daily.Count
if ($n -ne 34) { throw "For Daily Report: report has $n rows, expected 34 - stopped after the 3MS Dump" }
$nd = $dayP.Count; $m0 = ([datetime]'2026-11-01').ToOADate(); $h1 = arr 1 $nd; $h2 = arr 1 $nd
for ($c = 0; $c -lt $nd; $c++) { $h1[0, $c] = [double]$m0; $h2[0, $c] = [double]([datetime]::ParseExact($dayP[$c], 'yyyy-MM-dd', $inv)).ToOADate() }
$lastCol = $ws.Cells.Item(1, 3 + $nd).Address(0, 0) -replace '\d', ''
$fmt = $ws.Range('D2').NumberFormat
$ws.Range('D1:NQ37').ClearContents()
$ws.Range("D1:${lastCol}1").Value2 = $h1; $ws.Range("D2:${lastCol}2").Value2 = $h2; $ws.Range("D1:${lastCol}2").NumberFormat = $fmt
$v = arr $n $nd; for ($i = 0; $i -lt $n; $i++) { for ($c = 0; $c -lt $nd; $c++) { $x = num $daily[$i].($dayP[$c]); if ($null -ne $x) { $v[$i, $c] = $x } } }
$ws.Range("D3:${lastCol}$(2 + $n)").Value2 = $v
$log.Add("For Daily Report: $n rows x $nd days (1-30 Nov 2026)")
# --- Grade Bins: names and values from Blend Forecast (12 months from Oct-26)
$ws = $wb.Worksheets.Item('Grade Bins'); $n = $blend.Count
$ws.Range('A2:P86').ClearContents()
$h = arr 1 12; for ($c = 0; $c -lt 12; $c++) { $h[0, $c] = [double]([datetime]::ParseExact($blP[$c], 'yyyy-MM-dd', $inv)).ToOADate() }
$ws.Range('E1:P1').Value2 = $h
for ($i = 0; $i -lt $n; $i++) { $ws.Cells.Item(2 + $i, 1).Value = [string]$blend[$i].Title }
$v = arr $n 12; for ($i = 0; $i -lt $n; $i++) { for ($c = 0; $c -lt 12; $c++) { $x = num $blend[$i].($blP[$c]); if ($null -ne $x) { $v[$i, $c] = $x } } }
$ws.Range("E2:P$(1 + $n)").Value2 = $v
$log.Add("Grade Bins: $n rows x 12 months (Oct-26 to Sep-27)")
# --- Dev Dump: lateral dev metres by heading Nov-26 to Jan-27 + Total row
$ws = $wb.Worksheets.Item('Dev Dump'); $fmt = $ws.Range('B2').NumberFormat
$ws.Range('A2:L200').ClearContents()
$nh = $dev.Count
for ($i = 0; $i -lt $nh; $i++) { $ws.Cells.Item(2 + $i, 1).Value = [string]$dev[$i][0] }
$v = arr $nh 3; for ($i = 0; $i -lt $nh; $i++) { for ($c = 0; $c -lt 3; $c++) { $v[$i, $c] = [double]::Parse($dev[$i][1 + $c], $inv) } }
$ws.Range("B2:D$(1 + $nh)").Value2 = $v; $ws.Range("B2:D$(1 + $nh)").NumberFormat = $fmt
$tr = 2 + $nh; $ws.Cells.Item($tr, 1).Value = 'Total'
foreach ($c in 'B','C','D') { $ws.Range("$c$tr").Formula = "=SUM(${c}2:$c$($tr - 1))" }; $ws.Range("B${tr}:D$tr").NumberFormat = $fmt
$log.Add("Dev Dump: $nh headings, Total row $tr")
$xl.Calculate()
$s12 = $wb.Worksheets.Item('12 Month Physicals')
$log.Add('12 Month Physicals: lateral (row 20) ' + ((5..12 | ForEach-Object { $s12.Cells.Item(20, $_).Text.Trim() }) -join ' | '))
$log.Add('12 Month Physicals: ounces (row 27)  ' + ((5..12 | ForEach-Object { $s12.Cells.Item(27, $_).Text.Trim() }) -join ' | '))
$errs = 0; foreach ($w in $wb.Worksheets) { try { foreach ($c in $w.UsedRange.SpecialCells(-4123, 16)) { $errs++ } } catch {} }; $log.Add("formula cells showing errors: $errs")
$log
