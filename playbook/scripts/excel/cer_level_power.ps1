# Update the open CER (level power infrastructure) from the open per-level planning workbook (one tab per level,
# New Purchase lines). Writes only unlocked input cells. Never saves. Names, levels and text below are placeholders.
$ErrorActionPreference = 'Stop'
$xl = [Runtime.InteropServices.Marshal]::GetActiveObject('Excel.Application')
function retry([scriptblock]$b) { for ($t = 1; $t -le 20; $t++) { try { return (& $b) } catch { if ($t -eq 20 -or $_.Exception -is [InvalidCastException]) { throw }; Start-Sleep -Milliseconds 1500 } } }
$cer = retry { $xl.Workbooks.Item('CER - <name>.xlsx') }; $pl = retry { $xl.Workbooks.Item('<Power planning workbook>.xlsx') }
$ov = $cer.Worksheets.Item('CER Overview'); $ce = $cer.Worksheets.Item('Cost Estimation')
$intro = $cer.Worksheets.Item('Project Intro & Background'); $sol = $cer.Worksheets.Item('Solution-Alternatives')
function put($ws, [string]$a, $v) { $c = $ws.Range($a); if ($c.MergeArea.Cells.Item(1, 1).Locked) { throw "$($ws.Name)!$a is locked" }; if ($v -is [string] -and $v.StartsWith('=')) { retry { $c.Formula = [string]$v } } elseif ($v -is [string]) { retry { $c.Value2 = [string]$v } } elseif ($null -eq $v) { retry { $c.MergeArea.ClearContents() | Out-Null } } else { retry { $c.Value2 = [double]$v } } }

# --- level lines (New Purchase only), in level order
$levels = 'G6410', 'G6390', 'G6370', 'G6350', 'G6330'
$lines = New-Object System.Collections.Generic.List[object]
foreach ($lv in $levels) { $ws = $pl.Worksheets.Item($lv)
  for ($r = 8; $r -le 23; $r++) { [double]$q = $ws.Range("E$r").Value2; if ($q -gt 0 -and $ws.Range("I$r").Text -eq 'New Purchase') {
      $lines.Add([pscustomobject]@{ Level = $lv; Item = [string]$ws.Range("B$r").Text; Qty = $q; Unit = [double]$ws.Range("F$r").Value2; Month = [string]$ws.Range("H$r").Text; Comment = [string]$ws.Range("K$r").Text }) } } }
$planTotal = [double]$pl.Worksheets.Item('Budget Summary').Range('E11').Value2
if ($lines.Count -gt 34) { throw "too many lines ($($lines.Count)) for Cost Estimation rows 7-40" }

# --- Cost Estimation: clear inputs B/C/D/F 7:40, then one row per level line (E keeps =D*C)
foreach ($col in 'B', 'C', 'D', 'F') { retry { $ce.Range("${col}7:${col}40").ClearContents() | Out-Null } }
$monthRows = @{}; $levelRows = @{}; $r = 7
foreach ($l in $lines) {
  put $ce "B$r" ("{0} - {1}" -f $l.Level, $l.Item); put $ce "C$r" $l.Qty; put $ce "D$r" $l.Unit
  $cm = "{0}, required {1} (3MS). Supplier quote <number>." -f $l.Level, $l.Month; if ($l.Comment) { $cm += ' ' + $l.Comment.Trim() }
  put $ce "F$r" $cm
  if (-not $monthRows.ContainsKey($l.Month)) { $monthRows[$l.Month] = New-Object System.Collections.Generic.List[string] }; $monthRows[$l.Month].Add("'Cost Estimation'!E$r")
  if (-not $levelRows.ContainsKey($l.Level)) { $levelRows[$l.Level] = @($r, $r) } else { $levelRows[$l.Level][1] = $r }
  $r++ }

# --- cash flow by 3MS month (C37 Jul-26 ... C59 Jun-27, every second row)
$months = 'July', 'August', 'September', 'October', 'November', 'December', 'January', 'February', 'March', 'April', 'May', 'June'
for ($i = 0; $i -lt 12; $i++) { $cell = "C$(37 + 2 * $i)"; $m = $months[$i]
  if ($monthRows.ContainsKey($m)) { put $ov $cell ('=' + ($monthRows[$m] -join '+')) } else { put $ov $cell $null } }

# --- significant cost components: one line per level
$mShort = @{ 'October' = 'Oct-26'; 'November' = 'Nov-26'; 'January' = 'Jan-27'; 'February' = 'Feb-27'; 'May' = 'May-27' }
for ($k = 88; $k -le 98; $k++) { foreach ($col in 'A', 'G') { if (-not $ov.Range("$col$k").MergeArea.Cells.Item(1, 1).Locked) { put $ov "$col$k" $null } } }   # row 98 is a locked spacer
$k = 88; foreach ($lv in $levels) { $a = $levelRows[$lv][0]; $b = $levelRows[$lv][1]
  $ms = ($lines | Where-Object Level -eq $lv | ForEach-Object { $mShort[$_.Month] } | Select-Object -Unique) -join '/'
  put $ov "A$k" ("{0} level power infrastructure ({1})" -f $lv, $ms); put $ov "G$k" "=SUM('Cost Estimation'!E$a`:E$b)"; $k++ }

# --- general information and text
put $ov 'A8' '<Project title (levels covered)>'
put $ov 'G14' ([datetime]'2027-05-31').ToOADate()
put $ov 'A72' '<Budget status and why the equipment is needed earlier>'
put $ov 'A82' '<Description: items per level, ordered to arrive before each level is reached in the 3MS>'
put $intro 'B6' '<Introduction: levels, months, item list from the quote, total capital requested>'
put $intro 'B26' '<Background: what each new level needs before development starts, lead times, risk>'
put $sol 'C5' '<Solution: items per level and month, supply terms, quote validity>'
put $sol 'C21' ("Option 1 - Buy when each level is reached`n<why not>`n`nOption 2 - Move equipment from upper levels`n<why not>`n`nOption 3 - Hire temporary equipment`n<why not>")

$xl.Calculate()
"lines written: $($lines.Count) (Cost Estimation rows 7-$($r - 1))"
"Cost Estimation total E41 = " + $ce.Range('E41').Text + " ; planning Budget Summary E11 = " + ('{0:N2}' -f $planTotal)
"Capital requested C11 = " + $ov.Range('C11').Text + " ; cash flow total C65 = " + $ov.Range('C65').Text + " ; components total G99 = " + $ov.Range('G99').Text + " ; budget I69 = " + $ov.Range('I69').Text
for ($i = 0; $i -lt 12; $i++) { $c = $ov.Range("C$(37 + 2 * $i)"); if ($c.Text.Trim()) { "  cash flow " + $ov.Range("A$(37 + 2 * $i)").Text + " = " + $c.Text } }
for ($k = 88; $k -le 92; $k++) { "  " + $ov.Range("A$k").Text + " = " + $ov.Range("G$k").Text }
"CER saved flag: $($cer.Saved) (not saved)"
