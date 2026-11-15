# New MINE_UG_3MS_FY27_NOVEMBER_26_Physicals.xlsm (MB + SITE only) = copy of the approved November workbook with the four dump tabs
# refilled from C:\dswtmp\nov_dsf\phys_*_gpwh.tsv, which DsfReports.cs rebuilt straight from 05 Nov\MINE_UG_FY27.dsf with the
# Deswik task filter "Mine=MB+MA" ANDed onto every report row (Deswik not needed). Labels via .Value, numbers as 2D arrays via .Value2.
$ErrorActionPreference = 'Stop'
$docs = '<SHARE>\03 Planning\03 Scheduling\03 3MS\05 Nov\Documentation'
$src = Join-Path $docs 'Jumbo from 1 Feb 2027 Approved\MINE_UG_3MS_FY27_NOVEMBER_26_Physicals - Approved.xlsm'
$dst = Join-Path $docs 'MINE_UG_3MS_FY27_NOVEMBER_26_Physicals.xlsm'
if (Test-Path -LiteralPath $dst) { throw "already exists, not overwriting: $dst" }
$inv = [Globalization.CultureInfo]::InvariantCulture
function num($s) { if ($null -eq $s -or "$s" -eq '' -or "$s" -eq 'ERR') { return $null }; [double]::Parse("$s", $inv) }
$main = @(Import-Csv 'C:\dswtmp\nov_dsf\phys_main_gpwh.tsv' -Delimiter "`t"); $daily = @(Import-Csv 'C:\dswtmp\nov_dsf\phys_daily_gpwh.tsv' -Delimiter "`t"); $blend = @(Import-Csv 'C:\dswtmp\nov_dsf\phys_blend_gpwh.tsv' -Delimiter "`t")
$mainP = @($main[0].PSObject.Properties.Name | Select-Object -Skip 4); $dayP = @($daily[0].PSObject.Properties.Name | Select-Object -Skip 4); $blP = @($blend[0].PSObject.Properties.Name | Select-Object -Skip 4)
$dev = @(Get-Content 'C:\dswtmp\nov_dsf\phys_dev_gpwh.tsv' | Where-Object { $_ } | ForEach-Object { , ($_ -split "`t") })
$note = '14/11/2026: rebuilt from 05 Nov\MINE_UG_FY27.dsf outside Deswik (task filter Mine=MB+MA on every row). Check against the Deswik report.'
Copy-Item -LiteralPath $src -Destination $dst
$xl = New-Object -ComObject Excel.Application; $xl.Visible = $false; $xl.DisplayAlerts = $false; $xl.AutomationSecurity = 3
$log = New-Object System.Collections.Generic.List[string]
try {
  $wb = $xl.Workbooks.Open($dst)
  # --- Deswik 3MS Dump: E3:E199 titles must match, I2:U2 months, I3:U199 values
  $ws = $wb.Worksheets.Item('Deswik 3MS Dump'); $n = $main.Count; $bad = 0
  for ($i = 0; $i -lt $n; $i++) { if (([string]$ws.Cells.Item(3 + $i, 5).Value2).Trim() -ne ([string]$main[$i].Title).Trim()) { $bad++ } }
  if ($bad) { throw "Deswik 3MS Dump: $bad title mismatches - stopping" }
  $h = New-Object 'object[,]' 1, 13; for ($c = 0; $c -lt 13; $c++) { $h[0, $c] = [double]([datetime]::ParseExact($mainP[$c], 'yyyy-MM-dd', $inv)).ToOADate() }
  $ws.Range('I2:U2').Value2 = $h
  $v = New-Object 'object[,]' $n, 13; for ($i = 0; $i -lt $n; $i++) { for ($c = 0; $c -lt 13; $c++) { $x = num $main[$i].($mainP[$c]); if ($null -ne $x) { $v[$i, $c] = $x } } }
  $ws.Range("I3:U$(2 + $n)").ClearContents(); $ws.Range("I3:U$(2 + $n)").Value2 = $v
  if ($ws.Range('I69').Comment) { $ws.Range('I69').Comment.Delete() }
  if ($ws.Range('I2').Comment) { $ws.Range('I2').Comment.Delete() }; [void]$ws.Range('I2').AddComment($note)
  $log.Add("Deswik 3MS Dump: $n rows x 13 months (Nov-26 to Nov-27)")
  # --- For Daily Report: 34 rows by position, 30 days
  $ws = $wb.Worksheets.Item('For Daily Report'); $n = $daily.Count; if ($n -ne 34) { throw "daily has $n rows, expected 34" }
  $nd = $dayP.Count; $m0 = ([datetime]'2026-11-01').ToOADate(); $h1 = New-Object 'object[,]' 1, $nd; $h2 = New-Object 'object[,]' 1, $nd
  for ($c = 0; $c -lt $nd; $c++) { $h1[0, $c] = [double]$m0; $h2[0, $c] = [double]([datetime]::ParseExact($dayP[$c], 'yyyy-MM-dd', $inv)).ToOADate() }
  $lastCol = $ws.Cells.Item(1, 3 + $nd).Address(0, 0) -replace '\d', ''; $fmt = $ws.Range('D2').NumberFormat
  $ws.Range('D1:NQ37').ClearContents(); $ws.Range("D1:${lastCol}1").Value2 = $h1; $ws.Range("D2:${lastCol}2").Value2 = $h2; $ws.Range("D1:${lastCol}2").NumberFormat = $fmt
  $v = New-Object 'object[,]' $n, $nd; for ($i = 0; $i -lt $n; $i++) { for ($c = 0; $c -lt $nd; $c++) { $x = num $daily[$i].($dayP[$c]); if ($null -ne $x) { $v[$i, $c] = $x } } }
  $ws.Range("D3:${lastCol}$(2 + $n)").Value2 = $v
  $log.Add("For Daily Report: $n rows x $nd days (1-30 Nov 2026)")
  # --- Grade Bins: names + 12 months from Oct-26
  $ws = $wb.Worksheets.Item('Grade Bins'); $n = $blend.Count; $ws.Range('A2:P86').ClearContents()
  $h = New-Object 'object[,]' 1, 12; for ($c = 0; $c -lt 12; $c++) { $h[0, $c] = [double]([datetime]::ParseExact($blP[$c], 'yyyy-MM-dd', $inv)).ToOADate() }
  $ws.Range('E1:P1').Value2 = $h
  for ($i = 0; $i -lt $n; $i++) { $ws.Cells.Item(2 + $i, 1).Value = [string]$blend[$i].Title }
  $v = New-Object 'object[,]' $n, 12; for ($i = 0; $i -lt $n; $i++) { for ($c = 0; $c -lt 12; $c++) { $x = num $blend[$i].($blP[$c]); if ($null -ne $x) { $v[$i, $c] = $x } } }
  $ws.Range("E2:P$(1 + $n)").Value2 = $v
  $log.Add("Grade Bins: $n rows x 12 months (Oct-26 to Sep-27)")
  # --- Dev Dump: lateral metres by heading Nov-26..Jan-27 + Total
  $ws = $wb.Worksheets.Item('Dev Dump'); $fmt = $ws.Range('B2').NumberFormat; $ws.Range('A2:L200').ClearContents(); $nh = $dev.Count
  for ($i = 0; $i -lt $nh; $i++) { $ws.Cells.Item(2 + $i, 1).Value = [string]$dev[$i][0] }
  $v = New-Object 'object[,]' $nh, 3; for ($i = 0; $i -lt $nh; $i++) { for ($c = 0; $c -lt 3; $c++) { $v[$i, $c] = [double]::Parse($dev[$i][1 + $c], $inv) } }
  $ws.Range("B2:D$(1 + $nh)").Value2 = $v; $ws.Range("B2:D$(1 + $nh)").NumberFormat = $fmt
  $tr = 2 + $nh; $ws.Cells.Item($tr, 1).Value = 'Total'; foreach ($c in 'B', 'C', 'D') { $ws.Range("$c$tr").Formula = "=SUM(${c}2:$c$($tr - 1))" }; $ws.Range("B${tr}:D$tr").NumberFormat = $fmt
  $log.Add("Dev Dump: $nh headings, Total row $tr")
  $xl.CalculateFull()
  $s12 = $wb.Worksheets.Item('12 Month Physicals')
  $log.Add('12 Month Physicals row 20 (lateral): ' + ((5..16 | ForEach-Object { $s12.Cells.Item(20, $_).Text.Trim() }) -join ' | '))
  $log.Add('12 Month Physicals row 27 (ounces):  ' + ((5..16 | ForEach-Object { $s12.Cells.Item(27, $_).Text.Trim() }) -join ' | '))
  $errs = 0; foreach ($w in $wb.Worksheets) { try { foreach ($c in $w.UsedRange.SpecialCells(-4123, 16)) { $errs++ } } catch {} }; $log.Add("formula cells showing errors: $errs")
  $wb.Save(); $wb.Close($false)
} catch { $log.Add("FAILED: $($_.Exception.Message)"); try { $wb.Close($false) } catch {}; Remove-Item -LiteralPath $dst -ErrorAction SilentlyContinue; $log.Add('partial file removed') }
finally { $xl.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($xl) }
$log
