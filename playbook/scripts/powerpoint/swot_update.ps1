# SWOT (slide ID 444): add points taken from the slide comments as new rows in RED under each heading. Existing rows untouched.
# If the table then runs past the slide, body text is reduced until it fits. Writes into the open deck; never saves.
$ErrorActionPreference = 'Stop'
$add = [ordered]@{
  'Strengths' = @('<new strength taken from the slide comments>;')
  'Weaknesses' = @('<new weakness>;')
  'Threats' = @('<new threat>;')
  'Opportunities' = @('<new opportunity>;')
}
$pp = [Runtime.InteropServices.Marshal]::GetActiveObject('PowerPoint.Application')
$pr = $null; foreach ($p in $pp.Presentations) { if ($p.Name -eq 'MINE_UG_3MS_NOVEMBER_26 Draft.pptx') { $pr = $p } }
$s = $pr.Slides.FindBySlideID(444); $sh = $s.Shapes | Where-Object { $_.HasTable } | Select-Object -First 1; $t = $sh.Table
$RED = 255   # RGB(255,0,0)
function RowOf($text) { for ($r = 1; $r -le $t.Rows.Count; $r++) { if ($t.Cell($r, 1).Shape.TextFrame.TextRange.Text.Trim() -eq $text) { return $r } }; 0 }
$heads = @('Strengths', 'Weaknesses', 'Threats', 'Opportunities')
$n = 0
foreach ($h in $heads) { foreach ($line in $add[$h]) {
    if (($t.Rows | ForEach-Object { $_.Cells.Item(1).Shape.TextFrame.TextRange.Text }) -contains $line) { continue }
    $hr = RowOf $h; if (-not $hr) { "heading '$h' not found - skipped"; continue }
    # last text row of this section (stop at the next heading or a spacer row)
    $next = $t.Rows.Count + 1; foreach ($h2 in $heads) { $r2 = RowOf $h2; if ($r2 -gt $hr -and $r2 -lt $next) { $next = $r2 } }
    $last = $hr; for ($r = $hr + 1; $r -lt $next; $r++) { if ($t.Cell($r, 1).Shape.TextFrame.TextRange.Text.Trim()) { $last = $r } }
    $empty = 0; for ($r = $last + 1; $r -lt $next; $r++) { $tx = $t.Cell($r, 1).Shape.TextFrame.TextRange; if (-not $tx.Text.Trim() -and $tx.Font.Size -ge 10) { $empty = $r; break } }
    if ($empty) { $row = $empty } else { $row = $last + 1; if ($row -le $t.Rows.Count) { [void]$t.Rows.Add($row) } else { [void]$t.Rows.Add() } }
    $tr = $t.Cell($row, 1).Shape.TextFrame.TextRange; $tr.Text = $line
    $ref = $t.Cell($last, 1).Shape.TextFrame.TextRange; if ($last -eq $hr) { $ref = $t.Cell($hr + 1, 1).Shape.TextFrame.TextRange }
    $tr.Font.Size = 12; $tr.Font.Bold = 0; $tr.Font.Name = $ref.Font.Name; $tr.Font.Color.RGB = $RED; $n++ } }
# fit on the slide
$size = 12; while (($sh.Top + $sh.Height) -gt 532 -and $size -gt 9) { $size -= 0.5
  for ($r = 1; $r -le $t.Rows.Count; $r++) { $tr = $t.Cell($r, 1).Shape.TextFrame.TextRange; if ($tr.Text.Trim() -and $heads -notcontains $tr.Text.Trim() -and $tr.Text.Trim() -ne 'SWOT ANALYSIS') { $tr.Font.Size = $size } } }
"added $n red rows; body text {0} pt; table bottom {1:0} pt of 540; rows now {2}" -f $size, ($sh.Top + $sh.Height), $t.Rows.Count
for ($r = 1; $r -le $t.Rows.Count; $r++) { $tr = $t.Cell($r, 1).Shape.TextFrame.TextRange; if ($tr.Text.Trim()) { "{0}{1}" -f $(if ($tr.Font.Color.RGB -eq $RED) { '  [RED] ' } else { '        ' }), $tr.Text.Trim() } }
"deck saved flag: $($pr.Saved) (not saved)"
