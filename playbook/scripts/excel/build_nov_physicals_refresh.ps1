# Builds MINE_UG_3MS_FY27_NOVEMBER_26_Physicals.xlsm as a copy of the October workbook, then refreshes the dump tabs
# with A+B data exported from the current 3MS (%TEMP%\dsw\phys_*.tsv). October file is not touched.
# Labels are written with .Value, numbers as 2D double arrays with .Value2 (COM binder caches the first type used).
$ErrorActionPreference = 'Stop'
$docs = "<SHARE>\3MS\05 Nov\Documentation"
$src = "$env:TEMP\dsw\MINE_UG_3MS_FY27_NOVEMBER_26_Physicals_SCH1_prev_21-10.xlsm"   # previous November build (keeps the month-offset and decline fixes)
$dst = Join-Path $docs 'MINE_UG_3MS_FY27_NOVEMBER_26_Physicals.xlsm'
$dir = "$env:TEMP\dsw"
if (Test-Path -LiteralPath $dst) { "November file already exists - stopping so nothing is overwritten"; return }
$inv = [Globalization.CultureInfo]::InvariantCulture
function num($s) { if ($null -eq $s -or "$s" -eq '' -or "$s" -eq 'ERR') { return $null }; [double]::Parse("$s", $inv) }
function arr($rows, $cols) { New-Object 'object[,]' $rows, $cols }
$main = @(Import-Csv "$dir\phys_main.tsv" -Delimiter "`t"); $daily = @(Import-Csv "$dir\phys_daily.tsv" -Delimiter "`t"); $blend = @(Import-Csv "$dir\phys_blend.tsv" -Delimiter "`t")
$mainP = @($main[0].PSObject.Properties.Name | Select-Object -Skip 4); $dayP = @($daily[0].PSObject.Properties.Name | Select-Object -Skip 4); $blP = @($blend[0].PSObject.Properties.Name | Select-Object -Skip 4)
$dev = @(Get-Content "$dir\phys_dev.tsv" | Where-Object { $_ } | ForEach-Object { , ($_ -split "`t") })
Copy-Item -LiteralPath $src -Destination $dst
$xl = New-Object -ComObject Excel.Application; $xl.Visible = $false; $xl.DisplayAlerts = $false; $xl.AutomationSecurity = 3
$log = New-Object System.Collections.Generic.List[string]
try {
  $wb = $xl.Workbooks.Open($dst)
  # --- Deswik 3MS Dump: E3:E199 titles must match the report row order
  $ws = $wb.Worksheets.Item('Deswik 3MS Dump'); $n = $main.Count; $bad = 0
  for ($i = 0; $i -lt $n; $i++) { $t = [string]$ws.Cells.Item(3 + $i, 5).Value2; if ($t.Trim() -ne ([string]$main[$i].Title).Trim()) { $bad++; if ($bad -le 3) { $log.Add("  title mismatch row $(3+$i): sheet '$t' vs report '$($main[$i].Title)'") } } }
  if ($bad) { throw "Deswik 3MS Dump: $bad title mismatches - stopping" }
  $h = arr 1 13; for ($c = 0; $c -lt 13; $c++) { $h[0, $c] = [double]([datetime]::ParseExact($mainP[$c], 'yyyy-MM-dd', $inv)).ToOADate() }
  $ws.Range('I2:U2').Value2 = $h
  $v = arr $n 13; for ($i = 0; $i -lt $n; $i++) { for ($c = 0; $c -lt 13; $c++) { $x = num $main[$i].($mainP[$c]); if ($null -ne $x) { $v[$i, $c] = $x } } }
  $ws.Range("I3:U$(2 + $n)").ClearContents(); $ws.Range("I3:U$(2 + $n)").Value2 = $v
  $log.Add("Deswik 3MS Dump: $n rows x 13 months (Nov-26 to Nov-27)")
  # "Total UG Haualge - tkm" (formula row) comes back 0 from the export: fill it as Ore & LG tkm + Waste tkm, with a note
  if ([string]$ws.Range('E69').Value2 -like 'Total UG Haualge - tkm*' -and -not ($v[66, 0])) {
    $t = arr 1 13; for ($c = 0; $c -lt 13; $c++) { $t[0, $c] = [double]([double]$v[67, $c] + [double]$v[68, $c]) }; $ws.Range('I69:U69').Value2 = $t
    if ($ws.Range('I69').Comment) { $ws.Range('I69').Comment.Delete() }
    [void]$ws.Range('I69').AddComment("17/10/2026: Deswik returned 0 for this formula row in the export, so I69:U69 = Ore & LG tkm (row 70) + Waste tkm (row 71). Check against the 3MP report in Deswik.")
    $log.Add("  Total UG Haulage tkm filled as Ore & LG + Waste") }
  # --- For Daily Report: A3:A36 titles must match
  $ws = $wb.Worksheets.Item('For Daily Report'); $n = $daily.Count; $bad = 0
  for ($i = 0; $i -lt $n; $i++) { $t = [string]$ws.Cells.Item(3 + $i, 1).Value2; if ($t.Trim() -ne ([string]$daily[$i].Title).Trim()) { $bad++; if ($bad -le 3) { $log.Add("  daily title mismatch row $(3+$i): '$t' vs '$($daily[$i].Title)'") } } }
  # labels on this tab were hand-edited (same rows, different wording, checked 17/10/2026) - keep them, write by position
  if ($n -ne 34) { throw "For Daily Report: report has $n rows, expected 34 - stopping" }
  if ($bad) { $log.Add("  For Daily Report: $bad labels worded differently from the report - sheet labels kept") }
  $ws.Range('D1:NQ37').ClearContents()
  $nd = $dayP.Count; $h1 = arr 1 $nd; $h2 = arr 1 $nd; $m0 = ([datetime]'2026-11-01').ToOADate()
  for ($c = 0; $c -lt $nd; $c++) { $h1[0, $c] = [double]$m0; $h2[0, $c] = [double]([datetime]::ParseExact($dayP[$c], 'yyyy-MM-dd', $inv)).ToOADate() }
  $lastCol = $ws.Cells.Item(1, 3 + $nd).Address(0, 0) -replace '\d', ''
  $ws.Range("D1:${lastCol}1").Value2 = $h1; $ws.Range("D2:${lastCol}2").Value2 = $h2
  $v = arr $n $nd; for ($i = 0; $i -lt $n; $i++) { for ($c = 0; $c -lt $nd; $c++) { $x = num $daily[$i].($dayP[$c]); if ($null -ne $x) { $v[$i, $c] = $x } } }
  $ws.Range("D3:${lastCol}$(2 + $n)").Value2 = $v
  $ws.Range("D1:${lastCol}2").NumberFormat = $ws.Range('D2').NumberFormat
  $log.Add("For Daily Report: $n rows x $nd days (1-30 Nov 2026)")
  # --- Grade Bins: rewrite names and values from the Blend Forecast report (12 months from Oct-26)
  $ws = $wb.Worksheets.Item('Grade Bins'); $n = $blend.Count
  $old = @(); for ($i = 0; $i -lt 85; $i++) { $old += [string]$ws.Cells.Item(2 + $i, 1).Value2 }
  $same = 0; for ($i = 0; $i -lt $n; $i++) { if ($old[$i].Trim() -eq ([string]$blend[$i].Title).Trim()) { $same++ } }
  $log.Add("Grade Bins: $same of $n row titles already in the same place")
  $ws.Range('A2:P86').ClearContents()
  $h = arr 1 12; for ($c = 0; $c -lt 12; $c++) { $h[0, $c] = [double]([datetime]::ParseExact($blP[$c], 'yyyy-MM-dd', $inv)).ToOADate() }
  $ws.Range('E1:P1').Value2 = $h
  for ($i = 0; $i -lt $n; $i++) { $ws.Cells.Item(2 + $i, 1).Value = [string]$blend[$i].Title }
  $v = arr $n 12; for ($i = 0; $i -lt $n; $i++) { for ($c = 0; $c -lt 12; $c++) { $x = num $blend[$i].($blP[$c]); if ($null -ne $x) { $v[$i, $c] = $x } } }
  $ws.Range("E2:P$(1 + $n)").Value2 = $v
  $log.Add("Grade Bins: $n rows x 12 months (Oct-26 to Sep-27)")
  # --- Dev Dump: lateral dev metres by heading, Nov-26 to Jan-27, Total row with SUM formulas
  $ws = $wb.Worksheets.Item('Dev Dump'); $fmt = $ws.Range('B2').NumberFormat
  $ws.Range('A2:L200').ClearContents()
  $b1 = $ws.Range('B1').Value2
  if ($b1 -is [double]) { $h = arr 1 3; $h[0,0] = [double]([datetime]'2026-11-01').ToOADate(); $h[0,1] = [double]([datetime]'2026-12-01').ToOADate(); $h[0,2] = [double]([datetime]'2027-01-01').ToOADate(); $ws.Range('B1:D1').Value2 = $h }
  else { $ws.Range('B1').Value = 'Nov-26'; $ws.Range('C1').Value = 'Dec-26'; $ws.Range('D1').Value = 'Jan-27' }
  $nh = $dev.Count
  for ($i = 0; $i -lt $nh; $i++) { $ws.Cells.Item(2 + $i, 1).Value = [string]$dev[$i][0] }
  $v = arr $nh 3; for ($i = 0; $i -lt $nh; $i++) { for ($c = 0; $c -lt 3; $c++) { $v[$i, $c] = [double]::Parse($dev[$i][1 + $c], $inv) } }
  $ws.Range("B2:D$(1 + $nh)").Value2 = $v; $ws.Range("B2:D$(1 + $nh)").NumberFormat = $fmt
  $tr = 2 + $nh; $ws.Cells.Item($tr, 1).Value = 'Total'
  foreach ($c in 'B','C','D') { $ws.Range("$c$tr").Formula = "=SUM(${c}2:$c$($tr - 1))" }; $ws.Range("B${tr}:D$tr").NumberFormat = $fmt
  $log.Add("Dev Dump: $nh headings, Total row $tr")
  # --- Stope Bogging / Stope Drilling / C2P: no A+B stoping before Feb 2027 -> headers only for this period
  $ws = $wb.Worksheets.Item('Stope Bogging'); $ws.Range('A2:F46').ClearContents(); $h = arr 1 3; $h[0,0] = [double]([datetime]'2026-11-01').ToOADate(); $h[0,1] = [double]([datetime]'2026-12-01').ToOADate(); $h[0,2] = [double]([datetime]'2027-01-01').ToOADate(); $ws.Range('D1:F1').Value2 = $h; $ws.Range('D1:F1').NumberFormat = $ws.Range('D1').NumberFormat
  $ws = $wb.Worksheets.Item('Stope Drilling'); $ws.Range('A2:AJ97').ClearContents(); $ws.Range('C1:AJ1').ClearContents(); $ws.Range('C1:E1').Value2 = $h
  $ws = $wb.Worksheets.Item('C2P'); $lr = $ws.Cells.Item($ws.Rows.Count, 1).End(-4162).Row; $ws.Range("A3:AL$([math]::Max($lr, 3))").ClearContents(); $ws.Range('J1:AL2').ClearContents()
  $h1 = arr 1 30; $h2 = arr 1 30; for ($c = 0; $c -lt 30; $c++) { $h1[0, $c] = [double]$m0; $h2[0, $c] = [double]([datetime]'2026-11-01').AddDays($c).ToOADate() }; $ws.Range('J1:AM1').Value2 = $h1; $ws.Range('J2:AM2').Value2 = $h2
  $log.Add("Stope Bogging, Stope Drilling, C2P: headers rolled to November; no A+B stope tasks before Feb 2027")
  $xl.CalculateFull()
  $s12 = $wb.Worksheets.Item('12 Month Physicals'); $log.Add("12 Month Physicals check: E5 " + $s12.Range('E5').Text + ", Total lateral E20..G20 " + $s12.Range('E20').Text + " / " + $s12.Range('F20').Text + " / " + $s12.Range('G20').Text + ", Total ore ounces E27..G27 " + $s12.Range('E27').Text + " / " + $s12.Range('F27').Text + " / " + $s12.Range('G27').Text)
  $s3 = $wb.Worksheets.Item('3 Month Schedule'); $log.Add("3 Month Schedule check: E5 " + $s3.Range('E5').Text + ", lateral " + $s3.Range('E20').Text + " / " + $s3.Range('F20').Text + " / " + $s3.Range('G20').Text)
  $errs = 0; foreach ($w in $wb.Worksheets) { try { foreach ($c in $w.UsedRange.SpecialCells(-4123, 16)) { $errs++ } } catch {} }; $log.Add("formula cells showing errors: $errs")
  $wb.Save(); $wb.Close($false); $log.Add("saved $dst")
} catch { $log.Add("ERROR: " + $_.Exception.Message); try { $wb.Close($false) } catch {}; if (Test-Path -LiteralPath $dst) { Remove-Item -LiteralPath $dst; $log.Add("removed the incomplete copy") } }
finally { $xl.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($xl) }
$log
