# Slides 8-11 (IDs 453-456) of the OPEN November deck: replace the chart picture with a PNG export of the matching chart on
# 'YTD Graphs vs Budget' in the OPEN MC+MD tracking sheet (same box, aspect kept), then update the slide 1 notes. Never saves.
$ErrorActionPreference = 'Stop'
$pp = [Runtime.InteropServices.Marshal]::GetActiveObject('PowerPoint.Application')
$pr = $null; foreach ($p in $pp.Presentations) { if ($p.Name -eq 'MINE_UG_3MS_NOVEMBER_26 Draft.pptx') { $pr = $p } }
$xl = [Runtime.InteropServices.Marshal]::GetActiveObject('Excel.Application')
$tw = $xl.Workbooks | Where-Object { $_.Name -eq 'MINE UG_Monthly Tracking Sheet Budget FY27 - MC+MD.xlsx' }
if (-not $pr -or -not $tw) { throw 'deck and tracking sheet must be open' }
$tmp = "$env:TEMP\dsw\ytd_charts_2910"; New-Item -ItemType Directory -Force $tmp | Out-Null
$plan = [ordered]@{ '453' = 'Total Mined Ounces'; '454' = 'Development Metres'; '455' = 'Decline Advance'; '456' = 'Total Movement' }
$ws = $tw.Worksheets.Item('YTD Graphs vs Budget'); $xl.Calculate()
foreach ($id in $plan.Keys) {
  $co = $null; foreach ($c in $ws.ChartObjects()) { if ($c.Chart.HasTitle -and $c.Chart.ChartTitle.Text -eq $plan[$id]) { $co = $c } }
  if (-not $co) { "chart '$($plan[$id])' not found - slide kept"; continue }
  $s = $pr.Slides.FindBySlideID([int]$id)
  $old = $s.Shapes | Where-Object { $_.Type -eq 13 } | Sort-Object { $_.Width * $_.Height } -Descending | Select-Object -First 1
  if (-not $old) { "slide $($s.SlideIndex): no picture to replace - skipped"; continue }
  $L = $old.Left; $T = $old.Top; $W = $old.Width; $H = $old.Height
  $png = Join-Path $tmp ("{0}.png" -f ($plan[$id] -replace '[^A-Za-z]', '_'))
  if (Test-Path $png) { Remove-Item $png }
  [void]$co.Chart.Export($png, 'PNG')
  if (-not (Test-Path $png)) { "slide $($s.SlideIndex): export failed - kept old picture"; continue }
  $new = $s.Shapes.AddPicture($png, 0, -1, $L, $T)
  $new.LockAspectRatio = -1; $sc = [math]::Min($W / $new.Width, $H / $new.Height); $new.Width = $new.Width * $sc
  $new.Left = $L + ($W - $new.Width) / 2; $new.Top = $T + ($H - $new.Height) / 2; $new.Name = "YTD chart - $($plan[$id])"
  $old.Delete()
  "slide {0}: '{1}' replaced, {2:0}x{3:0} pt" -f $s.SlideIndex, $plan[$id], $new.Width, $new.Height }
$pr.Slides.Item(1).NotesPage.Shapes.Placeholders.Item(2).TextFrame.TextRange.Text = 'DRAFT - data slides updated 29/10/2026 from the refreshed November physicals and MC+MD tracking sheet (MINE_UG_FY27 Desktop\jumbo copy, re-levelled 28-29/10/2026, not yet saved) with AI assistance. Comment numbers updated in your wording; check slide 2 jumbo / MC decline dates, pictures, level and geology slides before approval.'
'slide 1: notes updated'; "deck saved flag: $($pr.Saved) (not saved)"
