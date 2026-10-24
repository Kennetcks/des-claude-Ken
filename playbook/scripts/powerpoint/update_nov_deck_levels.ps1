# Right-side "Level comments" on slides 10-14 (and the slide 9 caption) of the OPEN November draft deck,
# from the re-levelled MINE_UG_FY27 task export of 21/10/2026 (SCHEDULED = 1). Never saves.
$ErrorActionPreference = 'Stop'
$pp = [Runtime.InteropServices.Marshal]::GetActiveObject('PowerPoint.Application')
$pr = $null; foreach ($p in $pp.Presentations) { if ($p.Name -eq 'MINE_UG_3MS_NOVEMBER_26 Draft.pptx') { $pr = $p } }
if (-not $pr) { throw 'November draft deck is not open' }
$c = @{   # slide index -> comment lines (monthly metres only, next three months). Placeholders below.
  10 = @('<LEVEL> <HEADING>: <m> in <Mon-yy>, finishes <dd/mm/yy>;', '<LEVEL> development complete by <date>;', 'First stopes: <stope names> in <Mon-yy>;')
  11 = @('<LEVEL> <HEADING>: <m> in <Mon-yy>;')
  12 = @('<LEVEL> <HEADING>: <m>, <dd/mm/yy> to <dd/mm/yy>;')
  13 = @('<LEVEL> <HEADING>: <m>, <dd/mm/yy> to <dd/mm/yy>;')
  14 = @('Decline: <m> per month for the quarter;', 'Face position: <level> at month end;')
}
$log = New-Object System.Collections.Generic.List[string]
foreach ($i in 10..14) { $s = $pr.Slides.Item($i)
  $box = $s.Shapes | Where-Object { $_.Name -eq 'Level comments' } | Select-Object -First 1
  if (-not $box) { $log.Add("slide ${i}: no 'Level comments' box - skipped"); continue }
  $tr = $box.TextFrame.TextRange; $p1 = $tr.Paragraphs(1)
  if ($p1.Text.Trim() -ne 'Comments') { $log.Add("slide ${i}: first line is '$($p1.Text.Trim())', not 'Comments' - skipped"); continue }
  $rest = $tr.Characters($p1.Length + 1, $tr.Length - $p1.Length)
  $rest.Text = ($c[$i] -join "`r")
  $log.Add("slide ${i}: $($c[$i].Count) comment lines") }
# slide 9 caption
$cap = $pr.Slides.Item(9).Shapes | Where-Object { $_.HasTextFrame -and $_.TextFrame.TextRange.Text -like '*Development between*' } | Select-Object -First 1
if ($cap) { $cap.TextFrame.TextRange.Text = '<total> m development <Mon-yy> to <Mon-yy>: <areas>'; $log.Add('slide 9: caption updated') }
$log.Add("deck saved flag: $($pr.Saved) (not saved)"); $log
