# Builds the LOM-by-FY workbook from %TEMP%\dsw\lom_export.tsv (Deswik "Schedule Export 01 - Underground",
# filter Mine=A+B). Sheet "LOM by FY": one column per financial year + LOM total; sum rows are SUM formulas
# over the "Monthly" sheet, average rows (grades, %, headings) are Deswik's own weighted values.
# Note: labels are written with .Value and bulk arrays with .Value2/.Formula - PowerShell's COM binder caches
# the type of the first value written to a property, so mixing text and arrays on Value2 fails.
param([string]$Out)
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $Out) { "file already exists - stopping so nothing is overwritten"; return }
$rows = @(Import-Csv "$env:TEMP\dsw\lom_export.tsv" -Delimiter "`t" | Where-Object { $_.Field -ne 'Sch_Recovered Au Ounces' })   # no recovered ounces
$cols = $rows[0].PSObject.Properties.Name
$fyCols = @($cols | Where-Object { $_ -match '^FY\d\d$' })
$moCols = @($cols | Where-Object { $_ -match '^[A-Z][a-z]{2,3}-\d\d$' })   # en-AU months include 'Sept'
$inv = [Globalization.CultureInfo]::InvariantCulture
function colL([int]$n) { $s = ''; while ($n -gt 0) { $m = ($n - 1) % 26; $s = [string][char](65 + $m) + $s; $n = [int](($n - $m - 1) / 26) }; $s }
function isSum($r) { $r.Calculate -eq 'PeriodTotals' -and $r.Field -ne '' }
function isAvg($r) { $r.Field -ne '' -and $r.Calculate -ne 'PeriodTotals' }
$nR = $rows.Count; $first = 5

# --- Monthly array (values from Deswik)
$mHdr = @('Row', 'Title', 'Field', 'Filter', 'Calculate') + $moCols
$mArr = New-Object 'object[,]' ($nR + 1), ($mHdr.Count)
for ($c = 0; $c -lt $mHdr.Count; $c++) { $mArr[0, $c] = [string]$mHdr[$c] }
for ($i = 0; $i -lt $nR; $i++) { $r = $rows[$i]; $k = $i + 1
  $mArr[$k, 0] = [double]$r.Row; $mArr[$k, 1] = [string]$r.Title; $mArr[$k, 2] = [string]$r.Field; $mArr[$k, 3] = [string]$r.Filter; $mArr[$k, 4] = [string]$r.Calculate
  if ($r.Field -ne '') { for ($c = 0; $c -lt $moCols.Count; $c++) { $t = [string]$r.($moCols[$c]); if ($t -ne '') { $mArr[$k, (5 + $c)] = [double]::Parse($t, $inv) } } } }

# --- FY array (formulas for sum rows, Deswik values for average rows)
$fyRange = @{}
for ($c = 0; $c -lt $moCols.Count; $c++) { $d = [datetime]::ParseExact('01-' + $moCols[$c].Substring(0, 3) + '-' + $moCols[$c].Substring($moCols[$c].Length - 2), 'dd-MMM-yy', $inv); $y = if ($d.Month -ge 7) { $d.Year + 1 } else { $d.Year }; $f = 'FY' + ($y % 100).ToString('00')
  if (-not $fyRange.ContainsKey($f)) { $fyRange[$f] = @(($c + 6), ($c + 6)) } else { $fyRange[$f][1] = $c + 6 } }
$fHdr = @('Title', 'Field', 'Filter', 'Calculate') + $fyCols + @('LOM total')
$fArr = New-Object 'object[,]' ($nR + 1), ($fHdr.Count)
for ($c = 0; $c -lt $fHdr.Count; $c++) { $fArr[0, $c] = [string]$fHdr[$c] }
$lc = 4 + $fyCols.Count
for ($i = 0; $i -lt $nR; $i++) { $r = $rows[$i]; $k = $i + 1; $xr = $first + $i
  $fArr[$k, 0] = [string]$r.Title; $fArr[$k, 1] = [string]$r.Field; $fArr[$k, 2] = [string]$r.Filter; $fArr[$k, 3] = [string]$r.Calculate
  for ($c = 0; $c -lt $fyCols.Count; $c++) { $f = $fyCols[$c]
    if (isSum $r) { $g = $fyRange[$f]; $fArr[$k, (4 + $c)] = [string]("=SUM(Monthly!" + (colL $g[0]) + $xr + ":" + (colL $g[1]) + $xr + ")") }
    elseif (isAvg $r) { $t = [string]$r.$f; if ($t -ne '') { $fArr[$k, (4 + $c)] = [double]::Parse($t, $inv) } } }
  if (isSum $r) { $fArr[$k, $lc] = [string]("=SUM(" + (colL 5) + $xr + ":" + (colL (4 + $fyCols.Count)) + $xr + ")") }
  elseif (isAvg $r) { $t = [string]$r.LOM; if ($t -ne '') { $fArr[$k, $lc] = [double]::Parse($t, $inv) } } }

$xl = New-Object -ComObject Excel.Application; $xl.Visible = $false; $xl.DisplayAlerts = $false
$wb = $null
try {
  $wb = $xl.Workbooks.Add(); while ($wb.Worksheets.Count -lt 3) { [void]$wb.Worksheets.Add([Type]::Missing, $wb.Worksheets.Item($wb.Worksheets.Count)) }
  $fy = $wb.Worksheets.Item(1); $fy.Name = 'LOM by FY'
  $mo = $wb.Worksheets.Item(2); $mo.Name = 'Monthly'
  $nt = $wb.Worksheets.Item(3); $nt.Name = 'Notes'

  # bulk writes first (header row 4 + data)
  $rg = $mo.Range($mo.Cells.Item(4, 1), $mo.Cells.Item(4 + $nR, $mHdr.Count)); $rg.Value2 = $mArr
  $rg = $fy.Range($fy.Cells.Item(4, 1), $fy.Cells.Item(4 + $nR, $fHdr.Count)); $rg.Formula = $fArr
  # month headers as text (stop Excel turning Oct-26 into a date)
  for ($c = 0; $c -lt $moCols.Count; $c++) { $mo.Cells.Item(4, 6 + $c).NumberFormat = '@'; $mo.Cells.Item(4, 6 + $c).Value = [string]$moCols[$c] }

  foreach ($ws in $fy, $mo, $nt) { $ws.Cells.Font.Name = 'Arial'; $ws.Cells.Font.Size = 9 }
  $mo.Cells.Item(1, 1).Value = 'MINE_UG_FY27 - Schedule Export 01 - Underground - monthly (filter Mine=A+B)'
  $mo.Cells.Item(2, 1).Value = 'Values calculated by Deswik (report row calculation). Months run 1st 06:00 to 1st 06:00.'
  $fy.Cells.Item(1, 1).Value = 'MINE_UG_FY27 - Life of mine by financial year (Schedule Export 01 - Underground, filter Mine=A+B)'
  $fy.Cells.Item(2, 1).Value = 'FY = 1 July 06:00 to 1 July 06:00. FY27 starts at the schedule start, 16/10/2026. Sum rows add up the Monthly sheet; grade, % and heading rows are Deswik weighted values.'
  foreach ($ws in $fy, $mo) { $ws.Cells.Item(1, 1).Font.Bold = $true; $ws.Cells.Item(1, 1).Font.Size = 12 }

  # formatting
  foreach ($p in @(@($fy, $fHdr.Count), @($mo, $mHdr.Count))) { $ws = $p[0]; $n = [int]$p[1]
    $h = $ws.Range($ws.Cells.Item(4, 1), $ws.Cells.Item(4, $n)); $h.Font.Bold = $true; $h.Interior.Color = 64 + 59 * 256 + 98 * 65536; $h.Font.Color = 16777215; $h.WrapText = $true }
  for ($i = 0; $i -lt $nR; $i++) { $r = $rows[$i]; $xr = $first + $i
    if ($r.Field -eq '' -and $r.Title -ne '') {
      $fy.Rows.Item($xr).Font.Bold = $true; $mo.Rows.Item($xr).Font.Bold = $true
      $fy.Range($fy.Cells.Item($xr, 1), $fy.Cells.Item($xr, $fHdr.Count)).Interior.Color = 245 + 237 * 256 + 226 * 65536
      $mo.Range($mo.Cells.Item($xr, 1), $mo.Cells.Item($xr, 5)).Interior.Color = 245 + 237 * 256 + 226 * 65536 }
    elseif ($r.Field -ne '') { $fmt = if ([string]::IsNullOrWhiteSpace($r.Format)) { '#,##0' } else { [string]$r.Format }
      $fy.Range($fy.Cells.Item($xr, 5), $fy.Cells.Item($xr, $fHdr.Count)).NumberFormat = $fmt
      $mo.Range($mo.Cells.Item($xr, 6), $mo.Cells.Item($xr, $mHdr.Count)).NumberFormat = $fmt } }
  $fy.Range($fy.Cells.Item(4, $fHdr.Count), $fy.Cells.Item(4 + $nR, $fHdr.Count)).Font.Bold = $true
  $fy.Columns.Item(1).ColumnWidth = 46; $fy.Columns.Item(2).ColumnWidth = 24; $fy.Columns.Item(3).ColumnWidth = 24; $fy.Columns.Item(4).ColumnWidth = 12
  for ($c = 5; $c -le $fHdr.Count; $c++) { $fy.Columns.Item($c).ColumnWidth = 12 }
  $mo.Columns.Item(1).ColumnWidth = 5; $mo.Columns.Item(2).ColumnWidth = 46; $mo.Columns.Item(3).ColumnWidth = 22; $mo.Columns.Item(4).ColumnWidth = 22; $mo.Columns.Item(5).ColumnWidth = 11
  for ($c = 6; $c -le $mHdr.Count; $c++) { $mo.Columns.Item($c).ColumnWidth = 10 }

  $notes = @(
    'Source: <schedule file> (Deswik.Sched 2026.1), open session state at <date time>.',
    'Report: "Schedule Export 01 - Underground", task filter <filter name>.',
    'Values: calculated by Deswik itself (report row calculation per period) and exported read-only with AI assistance (Claude). Check before use.',
    'Periods: months 1st 06:00 to 1st 06:00; financial years 1 July 06:00 to 1 July 06:00. A report FY column that ends 30 June 05:59 will be about one day lower.',
    'The first FY covers the schedule from its start date only (no actuals before that).',
    'Sum rows on "LOM by FY" are SUM formulas over the Monthly sheet; grade, recovery % and heading-count rows are Deswik weighted/averaged values (not formulas).',
    'The "Recovered - Au Ounces" row is left out on purpose; ounces are reported as ounces (Sch_Diluted Au Ounces).',
    'Schedule state includes any unsaved what-if changes unless the file has been saved since.')
  for ($i = 0; $i -lt $notes.Count; $i++) { $nt.Cells.Item($i + 1, 1).Value = [string]$notes[$i] }
  $nt.Columns.Item(1).ColumnWidth = 150; $nt.Columns.Item(1).WrapText = $true

  $mo.Activate(); $xl.ActiveWindow.SplitRow = 4; $xl.ActiveWindow.SplitColumn = 2; $xl.ActiveWindow.FreezePanes = $true
  $fy.Activate(); $xl.ActiveWindow.SplitRow = 4; $xl.ActiveWindow.SplitColumn = 1; $xl.ActiveWindow.FreezePanes = $true
  $xl.Calculate()
  $wb.SaveAs($Out, 51)
  "saved $Out"
} finally { if ($wb) { $wb.Close($false) }; $xl.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($xl) }
