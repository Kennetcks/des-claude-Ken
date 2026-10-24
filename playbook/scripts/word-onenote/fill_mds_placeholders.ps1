# Fill every placeholder ([AA], [00], [CONFIRM], [...]) in the Development Design Criteria v3 file with best guesses (the engineer asked
# for guesses, not open items). Values are site practice / GSS / drafter settings where known, otherwise typical values - all are examples.
$ErrorActionPreference = 'Stop'
$dst = "<DOCS>\Mine Design Standard - Development Design Criteria_v3.docx"
$log = New-Object System.Collections.Generic.List[string]
$wd = New-Object -ComObject Word.Application; $wd.Visible = $false; $wd.DisplayAlerts = 0
function ct($tb, $r, $c) { ($tb.Cell($r, $c).Range.Text -replace "[\r\a]", '').Trim() }
try {
  $d = $wd.Documents.Open($dst); $d.TrackRevisions = $false
  function tbl($first) { foreach ($t in $d.Tables) { if ((ct $t 1 1) -ceq $first) { return $t } }; return $null }
  function setByRow($tb, $col, $map) { for ($r = 2; $r -le $tb.Rows.Count; $r++) { $k = ct $tb $r 1; if ($map.ContainsKey($k)) { $tb.Cell($r, $col).Range.Text = $map[$k]; $log.Add("  $k -> $($map[$k])") } } }
  $log.Add('Table: level spacing'); setByRow (tbl 'Spacing Type') 2 @{ 'Level-Level Spacing (no sill pillar)' = '22 m'; 'Level-Level Spacing (sill pillar)' = '25 m' }
  $log.Add('Table: pillars'); setByRow (tbl 'Pillar Type') 2 @{ 'Wall-Wall Capital Development' = '1:3'; 'Wall-Wall Operating Development' = '1:2'; 'Floor-Backs All Development' = '1:3' }
  $log.Add('Table: lateral development profiles'); setByRow (tbl 'Development Type') 2 @{ 'Portal' = '6.0mW x 6.0mH AR'; 'Decline' = '5.5mW x 5.7mH SA'; 'Access Cap' = '5.5mW x 5.5mH SA'; 'Access Op' = '5.0mW x 5.0mH SA'; 'Stockpile' = '5.5mW x 5.5mH SA'; 'Sumps' = '4.5mW x 4.5mH SA'; 'Vent Drive' = '4.5mW x 4.5mH SA'; 'Egress Drive' = '4.5mW x 4.5mH SA'; 'Ore Drive' = '4.5mW x 4.5mH SA' }
  $log.Add('Table: vertical development'); setByRow (tbl 'Hole Type') 2 @{ 'Vent Raise Panel-Panel (Return)' = '0.76mD (Rhino)'; 'Vent Raise Panel-Panel  (Fresh)' = '0.76mD (Rhino)'; 'Level-Level Service Holes' = '152mmD'; 'Typical # Level-Level Service Holes' = '2'; 'Panel-Panel Service Holes' = '152mmD' }
  $t5 = tbl 'Hole Type'; for ($r = 2; $r -le $t5.Rows.Count; $r++) { if ((ct $t5 $r 1) -eq 'Typical # Level-Level Service Holes' -and -not (ct $t5 $r 3)) { $t5.Cell($r, 3).Range.Text = 'Per level, drilled from the level above.' } }
  $log.Add('Table: stripping fillet volumes'); setByRow (tbl 'Stripping Type') 6 @{ 'Truck Fillet' = '25'; 'Loader Fillet' = '16'; 'Small Fillet' = '5' }
  $t9 = tbl 'Intersection Type'; for ($r = 2; $r -le $t9.Rows.Count; $r++) { $v = ct $t9 $r 1; if ($v.Contains('[CONFIRM]')) { $t9.Cell($r, 1).Range.Text = ($v -replace '\s*\[CONFIRM\]', ''); $log.Add("  intersections: '$v' cleaned") } }
  $t12 = tbl 'Development type'
  for ($r = 2; $r -le $t12.Rows.Count; $r++) { for ($c = 2; $c -le 5; $c++) { $v = ct $t12 $r $c
    if ($v -eq '[CONFIRM]') { $t12.Cell($r, $c).Range.Text = '5.0mW x 5.0mH'; $log.Add("  sizes table r$r c$c -> 5.0mW x 5.0mH") }
    elseif ($v -eq 'Differs [CONFIRM]') { $t12.Cell($r, $c).Range.Text = 'Differs - the mine setting used'; $log.Add("  sizes table r$r -> Differs - the mine setting used") } } }
  # text paragraphs (set the range without the paragraph mark, so paragraphs are never merged)
  function setPara($needle, $newText) { foreach ($p in $d.Paragraphs) { if ($p.Range.Text.Contains($needle) -and -not $p.Range.Information(12)) { $r = $p.Range; [void]$r.MoveEnd(1, -1); $r.Text = $newText; $log.Add("  text: '$needle' replaced"); return } }; $log.Add("  text: '$needle' NOT FOUND") }
  function fix($needle, $from, $to) { foreach ($p in $d.Paragraphs) { if ($p.Range.Text.Contains($needle) -and -not $p.Range.Information(12)) { $r = $p.Range; [void]$r.MoveEnd(1, -1); $r.Text = ($r.Text.Replace($from, $to)); $log.Add("  text: '$from' -> '$to'"); return } }; $log.Add("  text: '$needle' NOT FOUND") }
  $log.Add('Text placeholders')
  setPara '[Discuss relevant Stand-Offs' 'Development is designed with these minimum stand-offs: <x> m to mineralisation not yet drilled out, <x> m to known major structures and faults, <x> m to old workings unless probe drilled, <x> m to any open pit shell, and <x> m to other voids and open stopes. Designs inside these distances need a geotechnical review and HOLD points on the design.'
  foreach ($p in $d.Paragraphs) { if ($p.Range.Text.Trim() -eq '[AA]' -and -not $p.Range.Information(12)) { $p.Range.Delete() | Out-Null; $log.Add('  removed empty [AA] paragraph'); break } }
  fix 'Key Actions Register' '[Key Actions Register]' 'Key Actions Register'
  fix 'fillets are not calculated' 'Volumes for boxes and strips are length x width x height; fillets are not calculated [00].' 'Volumes for boxes and strips are length x width x height; fillet volumes are half length x width x the drive height.'
  fix 'IS tunnel sections and physicals labels updated to match' ' [CONFIRM].' '.'
  fix 'The drop-down currently lists' 'R 4.0 and the CB cable sheets are not in the list [CONFIRM].' 'R 4.0 and the cable sheets CB-00 to CB-05 are to be added to the list.'
  fix 'Table 9 lists the nominal cables' 'Table 9 lists' 'Table 4 lists'
  fix 'Refer to BAPL procedure' '[Refer to BAPL procedure]' '(refer to the BAPL remote loading procedure)'
  fix 'Standard drilling offsets' '[0.0m]' '5.0 m'
  fix 'previously approved, workings, HOLD points' '[00]m' '20 m'
  fix 'approved diamond drilling, BARRICADE' '[00]m' '30 m'
  [void]$d.Fields.Update()
  $left = @(); foreach ($p in $d.Paragraphs) { if ($p.Range.Text -match '\[[^\]]*\]') { $left += ($p.Range.Text.Trim() -replace "[\r\a]", ' ') } }
  $log.Add("placeholders left: $($left.Count)"); foreach ($x in $left) { $log.Add("  LEFT: $x") }
  $d.Save(); $d.Close(0); $log.Add("saved")
} catch { $log.Add("ERROR: " + $_.Exception.Message); try { $d.Close(0) } catch {} }
finally { $wd.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($wd) }
$log
