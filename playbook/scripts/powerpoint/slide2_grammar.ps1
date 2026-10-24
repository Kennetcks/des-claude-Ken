# Slide 2 comments: light grammar / spelling fixes in the engineer's own wording. Only the text after each label's colon is replaced,
# matched by label, so the label formatting stays. Writes into the open deck; never saves.
$ErrorActionPreference = 'Stop'
$new = [ordered]@{
  'Development'    = ' <metres by month and for the quarter, in the engineer''s own words, grammar fixed>;'
  'Fleet'          = ' <fleet changes by month>;'
  'Service holes'  = ' <status>;'
}
$pp = [Runtime.InteropServices.Marshal]::GetActiveObject('PowerPoint.Application')
$pr = $null; foreach ($p in $pp.Presentations) { if ($p.Name -eq 'MINE_UG_3MS_NOVEMBER_26 Draft.pptx') { $pr = $p } }
$s = $pr.Slides.FindBySlideID(434)
$tr = ($s.Shapes | Where-Object { $_.Name -eq 'TextBox 2' }).TextFrame.TextRange
for ($k = 1; $k -le $tr.Paragraphs().Count; $k++) { $p = $tr.Paragraphs($k); $txt = $p.Text.TrimEnd("`r"); $col = $txt.IndexOf(':'); if ($col -lt 1) { continue }
  $label = $txt.Substring(0, $col); if (-not $new.Contains($label)) { continue }
  $tail = $txt.Substring($col + 1); if ($tail -eq $new[$label]) { continue }
  $r = $p.Characters($col + 2, $tail.Length); $r.Text = $new[$label]; "fixed: $label" }
"--- slide 2 now:"; for ($k = 1; $k -le $tr.Paragraphs().Count; $k++) { "  " + $tr.Paragraphs($k).Text.TrimEnd("`r") }
"deck saved flag: $($pr.Saved) (not saved)"
