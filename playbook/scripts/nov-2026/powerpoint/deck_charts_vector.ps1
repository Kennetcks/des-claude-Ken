# Slides 8-11 (IDs 453-456) of the OPEN November deck: replace the chart picture with a vector (enhanced metafile) copy of the
# matching chart on 'YTD Graphs vs Budget' in the OPEN MC+MD tracking sheet, so it stays sharp at any size. Same box, aspect kept.
# Falls back to a 3x-size PNG export if the clipboard paste fails. Param: slide IDs to do. Never saves.
param([string[]]$ids = @('453', '454', '455', '456'))
$ErrorActionPreference = 'Stop'
$pp = [Runtime.InteropServices.Marshal]::GetActiveObject('PowerPoint.Application')
$pr = $null; foreach ($p in $pp.Presentations) { if ($p.Name -eq 'MINE_UG_3MS_NOVEMBER_26 Draft.pptx') { $pr = $p } }
$xl = [Runtime.InteropServices.Marshal]::GetActiveObject('Excel.Application')
$tw = $xl.Workbooks | Where-Object { $_.Name -eq 'MINE UG_Monthly Tracking Sheet Budget FY27 - MC+MD.xlsx' }
if (-not $pr -or -not $tw) { throw 'deck and tracking sheet must be open' }
$names = @{ '453' = 'Total Mined Ounces'; '454' = 'Development Metres'; '455' = 'Decline Advance'; '456' = 'Total Movement' }
$ws = $tw.Worksheets.Item('YTD Graphs vs Budget')
foreach ($id in $ids) {
  $title = $names[$id]
  $co = $null; foreach ($c in $ws.ChartObjects()) { if ($c.Chart.HasTitle -and $c.Chart.ChartTitle.Text -eq $title) { $co = $c } }
  if (-not $co) { "chart '$title' not found - slide kept"; continue }
  $s = $pr.Slides.FindBySlideID([int]$id)
  $old = $s.Shapes | Where-Object { $_.Type -eq 13 } | Sort-Object { $_.Width * $_.Height } -Descending | Select-Object -First 1
  $L = $old.Left; $T = $old.Top; $W = $old.Width; $H = $old.Height
  $new = $null; $how = ''
  for ($try = 1; $try -le 6 -and -not $new; $try++) {
    try { [void]$co.Chart.CopyPicture(2, -4147); Start-Sleep -Milliseconds (400 * $try); $new = $s.Shapes.PasteSpecial(2).Item(1); $how = "vector (EMF), try $try" } catch { Start-Sleep -Milliseconds 300 } }
  if (-not $new) {
    # fallback: export at 3x size, then put the chart back
    $w0 = $co.Width; $h0 = $co.Height; $png = "$env:TEMP\dsw\ytd_charts_2910\hi_$($title -replace '[^A-Za-z]', '_').png"
    try { $co.Width = $w0 * 3; $co.Height = $h0 * 3; [void]$co.Chart.Export($png, 'PNG') } finally { $co.Width = $w0; $co.Height = $h0 }
    $new = $s.Shapes.AddPicture($png, 0, -1, $L, $T); $how = 'PNG at 3x size' }
  $new.LockAspectRatio = -1; $sc = [math]::Min($W / $new.Width, $H / $new.Height); $new.Width = $new.Width * $sc
  $new.Left = $L + ($W - $new.Width) / 2; $new.Top = $T + ($H - $new.Height) / 2; $new.Name = "YTD chart - $title"
  $old.Delete()
  "slide {0}: '{1}' replaced as {2}, {3:0}x{4:0} pt" -f $s.SlideIndex, $title, $how, $new.Width, $new.Height }
"deck saved flag: $($pr.Saved) (not saved)"
