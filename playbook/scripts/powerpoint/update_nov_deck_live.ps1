# Update the OPEN November draft deck from the rebuilt November physicals (SCH =1, 21/10/2026): slide 5 12-month table,
# slide 6 fleet table, slide 2 comments, slide 15 SWOT, slide 1 notes. Writes into the open PowerPoint; never saves.
$ErrorActionPreference = 'Stop'
$r12 = Get-Content "$env:TEMP\dsw\nov_12m.json" -Raw | ConvertFrom-Json
$pe = Get-Content "$env:TEMP\dsw\nov_pe.json" -Raw | ConvertFrom-Json
function norm($s) { (("$s").ToLower() -replace 'haualge', 'haulage' -replace 'verital', 'vertical' -replace '[^a-z0-9&]', '') }
function V($label) { ($r12 | Where-Object { $_.L -eq $label }).V }
function N($s) { if ("$s" -eq '-' -or "$s" -eq '') { 0 } else { [double]("$s" -replace ',', '') } }
$pp = [Runtime.InteropServices.Marshal]::GetActiveObject('PowerPoint.Application')
$pr = $null; foreach ($p in $pp.Presentations) { if ($p.Name -eq 'MINE_UG_3MS_NOVEMBER_26 Draft.pptx') { $pr = $p } }
if (-not $pr) { throw 'November draft deck is not open' }
$log = New-Object System.Collections.Generic.List[string]
$lat = V 'Total Lateral Dev (m)'; $q = (N $lat[0]) + (N $lat[1]) + (N $lat[2])
$oz = V 'Total Ore Ounces'; $ozq = (N $oz[0]) + (N $oz[1]) + (N $oz[2])
$south = V 'CAPEX Decline South (m)'; $od = V 'OPEX Ore Drive (m)'; $so = V 'Stope Ore (t)'
$months = @('Nov-26','Dec-26','Jan-27','Feb-27','Mar-27','Apr-27','May-27','Jun-27','Jul-27','Aug-27','Sep-27','Oct-27')
function firstMonth($row, [double]$min) { for ($i = 0; $i -lt 12; $i++) { if ((N $row[$i]) -ge $min) { return $i } }; -1 }
$pej = ($pe | Where-Object L -eq 'Jumbo').V; $pel = ($pe | Where-Object L -eq 'Loader').V; $pet = ($pe | Where-Object L -eq 'Truck').V
$j2 = firstMonth $pej 2; $l2 = firstMonth $pel 2; $t2 = firstMonth $pet 2; $st1 = firstMonth $so 1
# --- slide 5: 12-month table
$t = ($pr.Slides.Item(5).Shapes | Where-Object { $_.HasTable } | Select-Object -First 1).Table; $hit = 0; $miss = @()
for ($c = 0; $c -lt 12; $c++) { $t.Cell(1, 3 + $c).Shape.TextFrame.TextRange.Text = $months[$c] }
for ($r = 2; $r -le $t.Rows.Count; $r++) { $lab = $t.Cell($r, 1).Shape.TextFrame.TextRange.Text; if (-not $lab.Trim()) { continue }
  $src = $r12 | Where-Object { (norm $_.L) -eq (norm $lab) } | Select-Object -First 1; if (-not $src) { $miss += $lab; continue }
  if (@($src.V | Where-Object { $_ }).Count -eq 0) { continue }
  for ($c = 0; $c -lt 12; $c++) { $t.Cell($r, 3 + $c).Shape.TextFrame.TextRange.Text = [string]$src.V[$c] }; $hit++ }
$log.Add("slide 5: $hit rows refreshed" + $(if ($miss) { "; not matched: " + ($miss -join ', ') } else { '' }))
# --- slide 6: fleet table (all rows from Planned Equipment; 0 shown as -)
$t6 = ($pr.Slides.Item(6).Shapes | Where-Object { $_.HasTable } | Select-Object -First 1).Table; $hit = 0
for ($c = 0; $c -lt 12; $c++) { $t6.Cell(1, 3 + $c).Shape.TextFrame.TextRange.Text = $months[$c] }
for ($r = 2; $r -le $t6.Rows.Count; $r++) { $k = $t6.Cell($r, 1).Shape.TextFrame.TextRange.Text.Trim(); $w = $pe | Where-Object { $_.L -eq $k } | Select-Object -First 1
  if (-not $w) { $log.Add("slide 6: '$k' not in Planned Equipment - kept"); continue }
  for ($c = 0; $c -lt 12; $c++) { $v = [string]$w.V[$c]; if ($v -eq '0') { $v = '-' }; $t6.Cell($r, 3 + $c).Shape.TextFrame.TextRange.Text = $v }; $hit++ }
$log.Add("slide 6: $hit fleet rows refreshed from Planned Equipment")
# --- slide 2: comments
$box = $pr.Slides.Item(2).Shapes | Where-Object { $_.HasTextFrame -and $_.TextFrame.TextRange.Text -like 'Development:*' } | Select-Object -First 1
$tr = $box.TextFrame.TextRange
function setPara($startsWith, $new) { for ($i = 1; $i -le $tr.Paragraphs().Count; $i++) { $p = $tr.Paragraphs($i); if ($p.Text.TrimStart().StartsWith($startsWith)) { $len = $p.Text.TrimEnd("`r").Length; $p.Characters(1, $len).Text = $new; return $true } }; $false }
function addAfter($startsWith, $new) { for ($i = 1; $i -le $tr.Paragraphs().Count; $i++) { $p = $tr.Paragraphs($i); if ($p.Text.TrimStart().StartsWith($startsWith)) { $len = $p.Text.TrimEnd("`r").Length; [void]$p.Characters(1, $len).InsertAfter("`r" + $new); return $true } }; $false }
$dev = "Development: {0} m in Nov-26, {1} m in Dec-26 and {2} m in Jan-27, {3:N0} m for the quarter (all mines, SCHEDULED = 1), ramping up as the second jumbo arrives;" -f $lat[0], $lat[1], $lat[2], $q
if (setPara 'Development:' $dev) { $log.Add('slide 2: development line') }
if (setPara 'First ore drive:' ("Ore drives: {0} m in Nov-26, {1} m in Dec-26 and {2} m in Jan-27;" -f $od[0], $od[1], $od[2])) { $log.Add('slide 2: ore drive line') }
if (setPara 'Ore:' ("Ore: {0:N0} oz for the quarter from development ore ({1} / {2} / {3} oz). First stope ore in {4} ({5} t);" -f $ozq, $oz[0], $oz[1], $oz[2], $months[$st1], $so[$st1])) { $log.Add('slide 2: ore line') }
$sm = firstMonth $south 1
if ($sm -ge 0 -and -not ($tr.Text -like '*Second decline*')) { if (addAfter 'Development:' ("Second decline: starts in {0} with {1} m;" -f $months[$sm], $south[$sm])) { $log.Add('slide 2: second decline line added') } }
if (-not ($tr.Text -like '*Fleet:*')) { if (addAfter 'Ore:' ("Fleet: second jumbo and second loader from {0}, second truck from {1}; first production drill and Rhino from {2};" -f $months[$j2], $months[$t2], $months[(firstMonth (($pe | Where-Object L -eq 'Prod Drills').V) 1)])) { $log.Add('slide 2: fleet line added') } }
# --- slide 15: SWOT
$tt = ($pr.Slides.Item(15).Shapes | Where-Object { $_.HasTable } | Select-Object -First 1).Table
for ($r = 1; $r -le $tt.Rows.Count; $r++) { $c = $tt.Cell($r, 1).Shape.TextFrame.TextRange
  if ($c.Text -like '*m of lateral development planned for the quarter*') { $c.Text = "{0:N0} m of lateral development planned for the quarter ({1} / {2} / {3} m);" -f $q, $lat[0], $lat[1], $lat[2]; $log.Add('slide 15: strength updated') }
  if ($r -eq 12 -and -not $c.Text.Trim()) { $c.Text = "Development rises from {0} m in Nov-26 to {1} m in Jan-27 - relies on the extra jumbo and loader arriving in {2};" -f $lat[0], $lat[2], $months[$j2]; $log.Add('slide 15: weakness added (row 12)') } }
# --- slide 1 notes
$pr.Slides.Item(1).NotesPage.Shapes.Placeholders.Item(2).TextFrame.TextRange.Text = 'DRAFT - data slides updated 21/10/2026 from the November physicals (MINE_UG_FY27, all mines, SCHEDULED = 1, re-levelled 21/10/2026) with AI assistance. Comments on slides 2 and 15 are suggestions to review; pictures, level and geology slides still to be updated before approval.'
$log.Add('slide 1: notes updated'); $log.Add("deck saved flag: $($pr.Saved) (not saved)")
$log
