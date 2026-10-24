# Update the open secondary ventilation survey memo (built from another site's template). Text and readings below are placeholders.
# Works bottom-up so paragraph numbers above each edit stay valid. Writes into the open Word document; never saves.
$ErrorActionPreference = 'Stop'
$img = "$env:TEMP\dsw\vent"
$fig = @("$img\8.jpg", "$img\9.jpg", "$img\10.jpg")
foreach ($f in $fig) { if (-not (Test-Path -LiteralPath $f)) { throw "photo missing: $f" } }
$wd = [Runtime.InteropServices.Marshal]::GetActiveObject('Word.Application')
$d = $null; foreach ($x in $wd.Documents) { if ($x.Name -eq 'VM_0001 Secondary Survey.docx') { $d = $x } }
if (-not $d) { throw 'memo is not open' }
function P([int]$n) { $d.Paragraphs.Item($n) }
function txt([int]$n) { ((P $n).Range.Text -replace "[\r\a]", '').Trim() }
function setText([int]$n, [string]$s) { $r = (P $n).Range; [void]$r.MoveEnd(1, -1); $r.Text = $s }
function delParas([int]$a, [int]$b) { $d.Range((P $a).Range.Start, (P $b).Range.End).Delete() | Out-Null }
function cell($t, [int]$r, [int]$c, [string]$s) { $t.Cell($r, $c).Range.Text = $s }
# --- check the layout is still the one previewed
$expect = @{ 18 = 'Introduction'; 21 = 'Results'; 27 = 'Summary'; 43 = 'Comparisons To Last Survey'; 50 = 'Aggregate Airflow Requirements'; 69 = 'Recommendations'; 84 = 'Requirements'; 100 = 'Sign Off' }
foreach ($k in $expect.Keys) { if ((txt $k) -ne $expect[$k]) { throw "paragraph $k is '$(txt $k)', expected '$($expect[$k])' - nothing changed" } }
$log = New-Object System.Collections.Generic.List[string]
$capStyle = $null; try { $capStyle = $d.Styles.Item('Caption') } catch {}
function addFigure([int]$n, [string]$file, [string]$caption) {   # paragraph n becomes the picture; a caption paragraph follows
  $p = P $n; $r = $p.Range; [void]$r.MoveEnd(1, -1); $r.Text = ''
  $pic = $d.InlineShapes.AddPicture($file, $false, $true, $r); $ratio = $pic.Height / $pic.Width; $pic.Height = 260; $pic.Width = [single](260 / $ratio)
  $p.Alignment = 1; $p.Range.InsertParagraphAfter(); $c = P ($n + 1); $cr = $c.Range; [void]$cr.MoveEnd(1, -1); $cr.Text = $caption
  if ($capStyle) { $c.Style = $capStyle }; $c.Alignment = 1 }

# 1. sign-off table
$t2 = $d.Tables.Item(2); cell $t2 2 1 '<Author>'; cell $t2 2 3 '<dd/mm/yyyy>'; cell $t2 3 1 ''; cell $t2 4 1 ''; $log.Add('sign-off table updated')
# 2. remove the template's site-only 'Requirements' section (84-99)
delParas 84 99; $log.Add('removed Requirements section')
# 3. recommendations (70 kept, 71-83 empty)
delParas 72 83
setText 71 ("•`t<action 1>`r" +
  "•`t<action 2>`r")
addFigure 75 $fig[2] 'Figure 3: <caption>'
$log.Add('recommendations written, Figure 3 added')
# 4. remove 'Aggregate Airflow Requirements' (50-68)
delParas 50 68; $log.Add('removed Aggregate Airflow Requirements section')
# 5. comparison to last survey
delParas 45 45
setText 44 'This is the first secondary ventilation survey. The readings in this memo are the baseline for the next surveys.'
setText 43 'Comparison To Last Survey'; $log.Add('comparison section written')
# 6. summary: figures in the empty paragraphs 38-42, conclusions 33-37, readings 29-32
delParas 42 42
addFigure 40 $fig[1] 'Figure 2: <caption>'
addFigure 38 $fig[0] 'Figure 1: <caption>'
delParas 37 37
setText 36 '<conclusion: temperatures>'
setText 35 '<conclusion>'
setText 34 '<conclusion: fan performance>'
setText 33 'The following conclusions can be made from these measurements:'
delParas 32 32
setText 31 '•	Both stations: WB <x> °C, DB <x> °C, O₂ <x> %, gases <readings>.'
setText 30 '•	<Station 2>: <Q> m³/s (<v> m/s over <A> m²).'
setText 29 '•	<Station 1>: <Q> m³/s (<v> m/s over <A> m²).'
$log.Add('summary written, Figures 1-2 added')
# 7. results table in paragraph 24
setText 22 'The following readings were obtained on <date> at the survey stations.'
$tr = (P 24).Range; $t = $d.Tables.Add($tr, 3, 10)
$hdr = @('Station', 'Location', 'WB (°C)', 'DB (°C)', 'Area (m²)', 'Velocity (m/s)', 'Quantity (m³/s)', 'O₂ (%)', 'CO / NO₂ (ppm)', 'CH₄ (%)')
$r1 = @('VS01', '<location>', '', '', '', '', '', '', '', '')
$r2 = @('VS02', '<location>', '', '', '', '', '', '', '', '')
for ($c = 1; $c -le 10; $c++) { cell $t 1 $c $hdr[$c - 1]; cell $t 2 $c $r1[$c - 1]; cell $t 3 $c $r2[$c - 1] }
$t.Borders.Enable = $true; $t.Range.Font.Size = 9; $t.Range.ParagraphFormat.Alignment = 1; $t.Range.ParagraphFormat.SpaceAfter = 0
$t.Rows.Item(1).Range.Font.Bold = $true; $t.Rows.Item(1).Shading.BackgroundPatternColor = 14277081; $t.AutoFitBehavior(1)
$log.Add('results table added')
# 8. introduction and header table
setText 19 'On <date>, a secondary ventilation survey was conducted. This involved checking the airflow velocity, quantity and dry/wet bulb temperatures in travel ways and work areas that form the secondary ventilation circuit. This memo summarises the results of the survey, notes any insufficiencies present and recommends actions to be taken to rectify shortcomings.'
$t1 = $d.Tables.Item(1)
cell $t1 1 1 'Memo Number: <number>'
cell $t1 2 1 'To: <roles>'
cell $t1 4 1 'From: <author>'
cell $t1 5 1 'Date: <dd/mm/yyyy>'
cell $t1 6 1 'Secondary Ventilation Survey <Month yyyy>'
$log.Add('introduction and header updated')
# --- report
$log.Add("now: paragraphs=$($d.Paragraphs.Count) tables=$($d.Tables.Count) pictures=$($d.InlineShapes.Count) pages=$($d.ComputeStatistics(2)) saved=$($d.Saved)")
$i = 0; foreach ($p in $d.Paragraphs) { $i++; $s = ($p.Range.Text -replace "[\r\a]", ' ').Trim(); if ($p.Range.ListFormat.ListString -and -not $p.Range.Information(12)) { $log.Add(("  {0} {1}" -f $p.Range.ListFormat.ListString, $(if ($s.Length -gt 90) { $s.Substring(0, 90) } else { $s }))) } }
$log
