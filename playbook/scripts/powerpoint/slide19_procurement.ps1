# Slide 19 "Procurement and Orders": plain table of pending items from the procurement schedule
# ('Purchase Order' tab). Delivered items left out. Rows below are placeholders. Writes into the open deck; never saves.
$ErrorActionPreference = 'Stop'
$rows = @(
  @('Item', 'Qty', 'CER', 'PO', 'Status'),
  @('<Item>', '<Qty>', 'Approved', '<PO number>', 'Waiting delivery (<lead time>)'),
  @('<Item>', '<Qty>', 'Approved', 'PR raised', 'Waiting PO'),
  @('<Item>', '<Qty>', 'Not required', '-', 'Planning')
)
$pp = [Runtime.InteropServices.Marshal]::GetActiveObject('PowerPoint.Application')
$pr = $null; foreach ($p in $pp.Presentations) { if ($p.Name -eq 'MINE_UG_3MS_NOVEMBER_26 Draft.pptx') { $pr = $p } }
$s = $pr.Slides.FindBySlideID(457)
if ($s.Shapes | Where-Object { $_.Name -eq 'Procurement table' }) { "slide already has the procurement table - nothing added"; return }
$shp = $s.Shapes.AddTable($rows.Count, 5, 80, 105, 840, 380); $shp.Name = 'Procurement table'; $t = $shp.Table
$w = @(330, 70, 85, 125, 230); for ($c = 1; $c -le 5; $c++) { $t.Columns.Item($c).Width = $w[$c - 1] }
for ($r = 1; $r -le $rows.Count; $r++) { for ($c = 1; $c -le 5; $c++) { $tf = $t.Cell($r, $c).Shape.TextFrame; $tf.TextRange.Text = $rows[$r - 1][$c - 1]
    $tf.TextRange.Font.Size = 11; $tf.TextRange.Font.Bold = [int]($r -eq 1); $tf.MarginTop = 2; $tf.MarginBottom = 2 } }
"table added on slide $($s.SlideIndex): $($rows.Count - 1) items; bottom at {0:0} pt of 540" -f ($shp.Top + $shp.Height)
"deck saved flag: $($pr.Saved) (not saved)"
