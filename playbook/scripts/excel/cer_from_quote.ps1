# Rewrite the open CER for a ventilation fan purchase from a supplier quote (ex GST). Example values below are placeholders.
# Not-in-budget case: explain it in the CER text; cash flow in the month the spend happens. Unlocked cells only. Never saves.
$ErrorActionPreference = 'Stop'
$xl = [Runtime.InteropServices.Marshal]::GetActiveObject('Excel.Application')
$w = $null; foreach ($x in $xl.Workbooks) { if ($x.Name -like 'CER - <name>*') { $w = $x } }
if (-not $w) { throw 'CER is not open' }
$ov = $w.Worksheets.Item('CER Overview'); $ce = $w.Worksheets.Item('Cost Estimation'); $intro = $w.Worksheets.Item('Project Intro & Background'); $sol = $w.Worksheets.Item('Solution-Alternatives')
function put($ws, [string]$a, $v) { $c = $ws.Range($a).MergeArea; if ($c.Cells.Item(1, 1).Locked) { throw "$($ws.Name)!$a is locked" }
  if ($null -eq $v) { $c.ClearContents() | Out-Null } elseif ($v -is [string] -and $v.StartsWith('=')) { $c.Cells.Item(1, 1).Formula = [string]$v } elseif ($v -is [string]) { $c.Cells.Item(1, 1).Value2 = [string]$v } else { $c.Cells.Item(1, 1).Value2 = [double]$v } }
$Q = 'Supplier quote <number> (<date>)'
# --- Cost Estimation
foreach ($col in 'B', 'C', 'D', 'F') { $ce.Range("${col}7:${col}40").ClearContents() | Out-Null }
$lines = @(
  @('<Fan model>', 1, 0, "$Q. <what the quote includes>"),
  @('<Mounting skid>', 1, 0, "$Q"),
  @('<Support frame>', 1, 0, "$Q"),
  @('<Sea container>', 1, 0, "$Q. Mounting base for the fan"))
for ($i = 0; $i -lt $lines.Count; $i++) { $r = 7 + $i; put $ce "B$r" $lines[$i][0]; put $ce "C$r" ([double]$lines[$i][1]); put $ce "D$r" ([double]$lines[$i][2]); put $ce "F$r" $lines[$i][3] }
# --- Overview: general information
put $ov 'A8' '<Project title>'
put $ov 'A14' ([datetime]'2026-10-21').ToOADate(); put $ov 'D14' ([datetime]'2026-12-01').ToOADate(); put $ov 'G14' ([datetime]'2027-03-31').ToOADate()
put $ov 'A72' '<Budget status: e.g. not in the budget because the work was brought forward; month the cost is incurred>'
put $ov 'A82' '<Description of what is purchased and why>'
# components and cash flow
for ($k = 88; $k -le 97; $k++) { foreach ($col in 'A', 'G') { if (-not $ov.Range("$col$k").MergeArea.Cells.Item(1, 1).Locked) { put $ov "$col$k" $null } } }
$comp = @('<Fan>', '<Skid>', '<Frame>', '<Container>')
for ($i = 0; $i -lt 4; $i++) { put $ov "A$(88 + $i)" $comp[$i]; put $ov "G$(88 + $i)" "='Cost Estimation'!E$(7 + $i)" }
for ($i = 0; $i -lt 12; $i++) { put $ov "C$(37 + 2 * $i)" $null }
put $ov 'C47' "='Cost Estimation'!E41"   # Dec-26
# --- text tabs
put $intro 'B6' '<Introduction: what is requested, why, total from the Cost Estimation tab, quote reference and validity>'
put $intro 'B26' '<Background: the need, applicable regulations and site plans, what happens without it, lead time>'
put $sol 'C5' '<Solution: items, deployment, supply terms (ex works, freight and installation)>'
put $sol 'C21' ("Option 1 – Do nothing`n<why not>`n`nOption 2 – <alternative>`n<why not>`n`nOption 3 – <alternative>`n<why not>")
$xl.Calculate()
"Cost Estimation total E41 = " + $ce.Range('E41').Text.Trim() + " ; capital requested C11 = " + $ov.Range('C11').Text.Trim() + " ; components G99 = " + $ov.Range('G99').Text.Trim() + " ; cash flow C65 = " + $ov.Range('C65').Text.Trim()
for ($i = 0; $i -lt 12; $i++) { $r = 37 + 2 * $i; $t = $ov.Range("C$r").Text.Trim(); if ($t) { "  cash flow " + $ov.Range("A$r").Text + " = $t" } }
"dates: initiation " + $ov.Range('A14').Text + ", start " + $ov.Range('D14').Text + ", completion " + $ov.Range('G14').Text + " ; in budget: " + $ov.Range('E69').Text + " " + $ov.Range('I69').Text
"saved flag: $($w.Saved) (not saved)"
