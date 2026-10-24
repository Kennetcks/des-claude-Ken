# November 3MS draft deck: copy of the October Approved deck with the data slides updated from the November physicals.
# Updated: slide 1 month + engineer date cleared, slide 2 development line, slide 5 12-month table, slide 6 fleet table
# (schedule-driven rows from the workbook, typed rows kept by calendar month from the October deck), slide 15 SWOT numbers.
# Pictures, level slides, geology slides and other comments are left for the engineer. October deck is not touched.
$ErrorActionPreference = 'Stop'
$docs = "<SHARE>\3MS\05 Nov\Documentation"
$src = Join-Path $docs 'MINE_UG_3MS_OCTOBER_26 Approved.pptx'
$dst = Join-Path $docs 'MINE_UG_3MS_NOVEMBER_26 Draft.pptx'
if (Test-Path -LiteralPath $dst) { "November draft already exists - stopping so nothing is overwritten"; return }
$r12 = Get-Content "$env:TEMP\dsw\nov_12m.json" -Raw | ConvertFrom-Json
$pe = Get-Content "$env:TEMP\dsw\nov_pe.json" -Raw | ConvertFrom-Json
function norm($s) { (("$s").ToLower() -replace 'haualge', 'haulage' -replace 'verital', 'vertical' -replace '[^a-z0-9&]', '') }
$months = @('Nov-26','Dec-26','Jan-27','Feb-27','Mar-27','Apr-27','May-27','Jun-27','Jul-27','Aug-27','Sep-27','Oct-27')
$lat = ($r12 | Where-Object { $_.L -eq 'Total Lateral Dev (m)' }).V
$q = [int]$lat[0].Replace(',', '') + [int]$lat[1].Replace(',', '') + [int]$lat[2].Replace(',', '')
Copy-Item -LiteralPath $src -Destination $dst
$log = New-Object System.Collections.Generic.List[string]
$pp = New-Object -ComObject PowerPoint.Application
try {
  $pr = $pp.Presentations.Open($dst, 0, 0, 0)
  function setText($tr, $old, $new) { $f = $tr.Find($old); if ($f) { [void]$tr.Replace($old, $new); return $true }; return $false }
  # slide 1
  $s1 = $pr.Slides.Item(1)
  foreach ($sh in $s1.Shapes) { if ($sh.HasTextFrame -and $sh.TextFrame.HasText -and $sh.TextFrame.TextRange.Text -eq 'October 2026') { $sh.TextFrame.TextRange.Text = 'November 2026'; $log.Add('slide 1: month -> November 2026') }
    if ($sh.HasTable) { $c = $sh.Table.Cell(2, 4).Shape.TextFrame.TextRange; if ($c.Text -eq '01/10/2026') { $c.Text = ''; $log.Add('slide 1: engineer sign-off date cleared (to be signed)') } } }
  $s1.NotesPage.Shapes.Placeholders.Item(2).TextFrame.TextRange.Text = 'DRAFT - data slides updated from the November 3MS (A+B, MINE_UG_FY27 levelled 17/10/2026) with AI assistance. Pictures, level slides, geology slides and comments still to be reviewed and updated before approval.'
  # slide 2 development line
  $s2 = $pr.Slides.Item(2)
  foreach ($sh in $s2.Shapes) { if ($sh.HasTextFrame -and $sh.TextFrame.HasText) { $tr = $sh.TextFrame.TextRange
    if (setText $tr 'Development: <old line from last month>' ("Development: {0} m in Nov-26, {1} m in Dec-26 and {2} m in Jan-27, {3} m for the quarter;" -f $lat[0], $lat[1], $lat[2], $q)) { $log.Add('slide 2: development line updated') } } }
  # slide 5: 12 month table
  $t = ($pr.Slides.Item(5).Shapes | Where-Object { $_.HasTable } | Select-Object -First 1).Table
  for ($c = 0; $c -lt 12; $c++) { $t.Cell(1, 3 + $c).Shape.TextFrame.TextRange.Text = $months[$c] }
  $hit = 0; $miss = @()
  for ($r = 2; $r -le $t.Rows.Count; $r++) { $lab = $t.Cell($r, 1).Shape.TextFrame.TextRange.Text; if (-not $lab.Trim()) { continue }
    $src12 = $r12 | Where-Object { (norm $_.L) -eq (norm $lab) } | Select-Object -First 1
    if (-not $src12) { $miss += $lab; continue }
    $hasVals = @($src12.V | Where-Object { $_ }).Count -gt 0
    for ($c = 0; $c -lt 12; $c++) { $t.Cell($r, 3 + $c).Shape.TextFrame.TextRange.Text = $(if ($hasVals) { [string]$src12.V[$c] } else { '' }) }
    $hit++ }
  $log.Add("slide 5: 12-month table - $hit rows filled, months Nov-26..Oct-27" + $(if ($miss.Count) { "; NOT matched: " + ($miss -join ', ') } else { '' }))
  # slide 6: fleet table
  $t6 = ($pr.Slides.Item(6).Shapes | Where-Object { $_.HasTable } | Select-Object -First 1).Table
  $oct = @{}; for ($r = 2; $r -le $t6.Rows.Count; $r++) { $k = $t6.Cell($r, 1).Shape.TextFrame.TextRange.Text.Trim(); $oct[$k] = (3..14 | ForEach-Object { $t6.Cell($r, $_).Shape.TextFrame.TextRange.Text }) }
  for ($c = 0; $c -lt 12; $c++) { $t6.Cell(1, 3 + $c).Shape.TextFrame.TextRange.Text = $months[$c] }
  $driven = @('Jumbo', 'Loader', 'Truck')
  for ($r = 2; $r -le $t6.Rows.Count; $r++) { $k = $t6.Cell($r, 1).Shape.TextFrame.TextRange.Text.Trim()
    if ($driven -contains $k) { $w = $pe | Where-Object { $_.L -eq $k } | Select-Object -First 1; for ($c = 0; $c -lt 12; $c++) { $v = [string]$w.V[$c]; if ($v -eq '0') { $v = '-' }; $t6.Cell($r, 3 + $c).Shape.TextFrame.TextRange.Text = $v } }
    else { $o = $oct[$k]; for ($c = 0; $c -lt 12; $c++) { $t6.Cell($r, 3 + $c).Shape.TextFrame.TextRange.Text = $(if ($c -lt 11) { $o[$c + 1] } else { $o[11] }) } } }
  $log.Add('slide 6: fleet table - Jumbo, Loader, Truck from the November workbook; other rows kept from the October plan by calendar month (Oct-27 = Sep-27)')
  # slide 15 SWOT
  $s15 = $pr.Slides.Item(15); $tt = ($s15.Shapes | Where-Object { $_.HasTable } | Select-Object -First 1).Table
  for ($r = 1; $r -le $tt.Rows.Count; $r++) { $tr = $tt.Cell($r, 1).Shape.TextFrame.TextRange
    if (setText $tr '<old SWOT development line>' ("{0} m of lateral development planned for the quarter ({1} / {2} / {3} m)" -f $q, $lat[0], $lat[1], $lat[2])) { $log.Add('slide 15: SWOT development numbers updated') } }
  $pr.Save(); $pr.Close(); $log.Add("saved $dst")
} catch { $log.Add("ERROR: " + $_.Exception.Message); try { $pr.Close() } catch {}; if (Test-Path -LiteralPath $dst) { Remove-Item -LiteralPath $dst; $log.Add('removed the incomplete copy') } }
finally { $pp.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($pp) }
$log
