# Replace the pictures on slides 8-11 (slide IDs 453-456) of the OPEN November deck with image copies of the matching charts
# on 'YTD Graphs vs Budget' in the COPY tracking sheet. Same position; fitted inside the old picture's box. Deck not saved.
$ErrorActionPreference = 'Stop'
$src = "<SHARE>\Reporting\Monthly Tracking Sheet\FY27\MINE UG_Monthly Tracking Sheet Budget FY27 - COPY.xlsx"
$plan = [ordered]@{ '453' = 'Total Mined Ounces'; '454' = 'Development Metres'; '455' = 'Decline Advance'; '456' = 'Total Movement' }
$tmp = "$env:TEMP\dsw\ytd_charts"; New-Item -ItemType Directory -Force $tmp | Out-Null
$pp = [Runtime.InteropServices.Marshal]::GetActiveObject('PowerPoint.Application')
$pr = $null; foreach ($p in $pp.Presentations) { if ($p.Name -eq 'MINE_UG_3MS_NOVEMBER_26 Draft.pptx') { $pr = $p } }
if (-not $pr) { throw 'November draft deck is not open' }
$xs = New-Object -ComObject Excel.Application; $xs.Visible = $false; $xs.DisplayAlerts = $false; $xs.AskToUpdateLinks = $false
try {
  $wb = $xs.Workbooks.Open($src, 0, $true); $ws = $wb.Worksheets.Item('YTD Graphs vs Budget')
  foreach ($id in $plan.Keys) {
    $co = $null; foreach ($c in $ws.ChartObjects()) { if ($c.Chart.HasTitle -and $c.Chart.ChartTitle.Text -eq $plan[$id]) { $co = $c } }
    if (-not $co) { "chart '$($plan[$id])' not found - slide skipped"; continue }
    $s = $pr.Slides.FindBySlideID([int]$id)
    $old = $s.Shapes | Where-Object { $_.Type -eq 13 } | Sort-Object { $_.Width * $_.Height } -Descending | Select-Object -First 1
    $L = $old.Left; $T = $old.Top; $W = $old.Width; $H = $old.Height
    $png = Join-Path $tmp ("{0}.png" -f ($plan[$id] -replace '[^A-Za-z]', '_'))
    $how = 'copy-paste'
    try { $co.Chart.CopyPicture(1, -4147) | Out-Null; Start-Sleep -Milliseconds 300; $new = $s.Shapes.PasteSpecial(6).Item(1) }   # xlScreen, xlPicture -> ppPastePNG
    catch { $how = 'export'; [void]$co.Chart.Export($png, 'PNG'); $new = $s.Shapes.AddPicture($png, 0, -1, $L, $T) }
    $new.LockAspectRatio = -1; $sc = [math]::Min($W / $new.Width, $H / $new.Height); $new.Width = $new.Width * $sc
    $new.Left = $L + ($W - $new.Width) / 2; $new.Top = $T + ($H - $new.Height) / 2; $new.Name = "YTD chart - $($plan[$id])"
    $old.Delete()
    "slide {0}: '{1}' pasted ({2}), {3:0}x{4:0} pt at {5:0},{6:0}" -f $s.SlideIndex, $plan[$id], $how, $new.Width, $new.Height, $new.Left, $new.Top }
  $wb.Close($false)
} finally { $xs.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($xs) }
"deck saved flag: $($pr.Saved) (not saved)"
