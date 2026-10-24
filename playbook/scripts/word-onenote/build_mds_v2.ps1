# Mine Design Standard WIP -> WIP_v2: update the intersection-cable table and the stripping table from the site Ground Support
# Standard (GSS) and the design drafter reference layers, add a GSS regime-by-profile table, a cable bolting subsection with figures,
# the strip standards figure, and a version row. Original WIP file is not touched. Table values below are examples - use your GSS.
$ErrorActionPreference = 'Stop'
$folder = "<DOCS>"
$src = Join-Path $folder 'Mine Design Standard_WIP.docx'
$dst = Join-Path $folder 'Mine Design Standard_WIP_v2.docx'
$img = "$env:TEMP\dsw\gss_shots"
if (Test-Path -LiteralPath $dst) { "v2 already exists - stopping so nothing is overwritten"; return }
Copy-Item -LiteralPath $src -Destination $dst
$log = New-Object System.Collections.Generic.List[string]
$wd = New-Object -ComObject Word.Application; $wd.Visible = $false; $wd.DisplayAlerts = 0
function cellText($tb, $r, $c) { ($tb.Cell($r, $c).Range.Text -replace "[\r\a]", '').Trim() }
function findTable($d, $first) { foreach ($t in $d.Tables) { try { if ((cellText $t 1 1) -eq $first) { return $t } } catch {} }; return $null }
function setRow($tb, $r, $vals) { for ($c = 0; $c -lt $vals.Count; $c++) { $tb.Cell($r, $c + 1).Range.Text = [string]$vals[$c] } }
try {
  $d = $wd.Documents.Open($dst)
  $d.TrackRevisions = $true   # all edits show as tracked changes for review
  $bodyStyle = 'Body Text'; $capStyle = $null
  # ---------------- Table 9 Intersections
  $t9 = findTable $d 'Intersection Type'
  if (-not $t9) { throw 'Table 9 (Intersection Type) not found' }
  $rows9 = @(
    @('3-Way, 4.5 - 5.0 m drives (GSS CB-01)', '3m max fillets', '8', '9'),
    @('3-Way, 5.0 - 5.5 m drives (GSS CB-02)', '3m max fillets', '9', '10'),
    @('3-Way, 5.5 - 6.0 m drives (GSS CB-00)', '3m max fillets', '8', '11'),
    @('4-Way, 5.0 - 5.5 x 4.5 - 5.0 m drives (GSS CB-03) [CONFIRM]', '3m max fillets', '12', '11'),
    @('4-Way, 5.5 m drives (GSS CB-04) [CONFIRM]', '3m max fillets', '17', '12'),
    @('Vertical opening brow (GSS CB-05)', 'N/A', '3', 'N/A'))
  while ($t9.Rows.Count -lt $rows9.Count + 1) { [void]$t9.Rows.Add() }
  for ($i = 0; $i -lt $rows9.Count; $i++) { setRow $t9 ($i + 2) $rows9[$i] }
  $log.Add("Table 9: 6 rows from GSS CB-00 to CB-05 (large 4-way 15 -> 17 cables)")
  # ---------------- Table 11 Stripping (Level Design Drafter 0\REFERENCE\STRIP STANDARDS)
  $t11 = findTable $d 'Stripping Type'
  if (-not $t11) { throw 'Table 11 (Stripping Type) not found' }
  $map11 = @{}; for ($r = 2; $r -le $t11.Rows.Count; $r++) { $map11[(cellText $t11 $r 1)] = $r }
  function upd($name, $vals) { if ($map11.ContainsKey($name)) { $r = $map11[$name]; for ($c = 0; $c -lt $vals.Count; $c++) { if ($null -ne $vals[$c]) { $t11.Cell($r, $c + 2).Range.Text = [string]$vals[$c] } }; $log.Add("Table 11: $name updated") } else { $log.Add("Table 11: $name NOT FOUND") } }
  upd 'Truck Fillet' @('3.0', '3.0', $null, $null, $null)
  upd 'Small Fillet' @('1.5', '1.5', $null, $null, $null)
  upd 'Distribution Box' @('9.0', '1.5', '4.0', '4.0', '54')
  upd 'Electrical Box' @($null, $null, $null, $null, '24')
  upd 'Jumbo Box' @($null, $null, $null, $null, '12')
  if ($map11.ContainsKey('Pump Flitch')) { $r = $map11['Pump Flitch']; $t11.Cell($r, 1).Range.Text = 'Pump Strip'; setRow $t11 $r @('Pump Strip', '1.0', '1.0', '4.0', '4.0', '4'); $log.Add('Table 11: Pump Flitch -> Pump Strip 1.0 x 1.0 x 4.0') }
  foreach ($nr in @(@('J/Box & Fan Starter', '2.5', '2.0', '4.0', '4.0', '20'), @('Fan Starter x2', '6.5', '1.5', '4.0', '4.0', '39'), @('Laser Channel', '1.0', '0.5', '2.0', '2.0', '1'))) { [void]$t11.Rows.Add(); setRow $t11 $t11.Rows.Count $nr }
  $log.Add('Table 11: added J/Box & Fan Starter, Fan Starter x2, Laser Channel')
  # note under Table 11
  $rg = $t11.Range; $rg.Collapse(0); $rg.InsertParagraphAfter(); $p = $d.Range($rg.End, $rg.End)
  $p.InsertAfter('Stripping dimensions follow the strip standards in the Level Design Drafter (layer 0\REFERENCE\STRIP STANDARDS, shown in the strip standards figure below). Fillet lengths and widths are the fillet legs. Volumes for boxes and strips are length x width x height; fillets are not calculated [00].')
  $p.Style = $bodyStyle
  # ---------------- moving insertion point helpers (P = an empty paragraph that content is written into, in order)
  function newPointBefore($para) { $st = $para.Range.Start; $para.Range.InsertParagraphBefore(); $q = $d.Range($st, $st).Paragraphs.Item(1); return $q }
  function putText($P, $text, $style, $align) { $r = $P.Range; [void]$r.MoveEnd(1, -1); $r.Text = $text; $P.Style = $style; $P.Alignment = $align; $P.Range.InsertParagraphAfter(); return $P.Next(1) }
  function putPicture($P, $file, $widthPt) { $r = $P.Range; [void]$r.MoveEnd(1, -1); $P.Style = 'Normal'; $P.Alignment = 1
    $pic = $d.InlineShapes.AddPicture($file, $false, $true, $r); $ratio = $pic.Height / $pic.Width; $pic.Width = $widthPt; $pic.Height = $widthPt * $ratio
    $P.Range.InsertParagraphAfter(); $n = $P.Next(1); $n.Style = $bodyStyle; $n.Alignment = 0; return $n }
  function putCaption($P, $label, $title) { $P.Range.InsertCaption($label, ". $title", '', 0); return $P }   # caption goes above P (= after the previous item)
  # ---------------- strip figure + cable subsection before the "the mine Profiles" heading
  $wp = $null; foreach ($pa in $d.Paragraphs) { if ($pa.Range.Text.Trim() -eq 'the mine Profiles' -and ([string]$pa.Style.NameLocal) -like 'Heading*') { $wp = $pa; break } }
  if (-not $wp) { throw '"the mine Profiles" heading not found' }
  $h3 = [string]$wp.Style.NameLocal
  $P = newPointBefore $wp; $P.Style = $bodyStyle
  $P = putPicture $P "$img\ref_strip_standards_userview.png" 450
  $P = putCaption $P 'Figure' 'Strip standards in the Level Design Drafter (0\REFERENCE\STRIP STANDARDS).'
  $P = putText $P 'Cable Bolting at Intersections' $h3 0
  $P = putText $P 'Intersections and vertical openings are cable bolted to the site Ground Support Standard. Table 9 lists the nominal cables and maximum spans for each GSS cable sheet. Designs must keep intersection spans within these limits and fillets to the maximum in the standard. Requirements from the GSS are;' $bodyStyle 0
  foreach ($bt in @(
      '<cable bolt type, length and grouting from the GSS>,',
      '<installation sequence>,',
      '<maximum advance before secondary cables are installed>,',
      '<plating and tensioning requirement>,',
      'Cable positions are shown in the Design Master (0\DESIGN MASTER\CABLEBOLTS) with the GSS sheet label so the weekly process carries them into the mine plan.')) { $P = putText $P $bt $bodyStyle 0 }
  $P = putPicture $P "$img\dm_cablebolts.png" 450
  $P = putCaption $P 'Figure' 'Example of cable bolt and stripping layout in the Design Master (G6440 / G6410 area, Level Design Drafter).'
  foreach ($cb in @(@('gss_CB-01', 'GSS CB-01, 3-way intersection, 4.5 - 5.0 m drives, 8 cables, max span 9 m.'), @('gss_CB-02', 'GSS CB-02, 3-way intersection, 5.0 - 5.5 m drives, 9 cables, max span 10 m.'), @('gss_CB-00', 'GSS CB-00, 3-way intersection, 5.5 - 6.0 m drives, 8 cables, max span 11 m.'), @('gss_CB-03', 'GSS CB-03, intersection, 5.0 - 5.5 m x 4.5 - 5.0 m drives, 12 cables, max span 11 m.'), @('gss_CB-04', 'GSS CB-04, 4-way intersection, 5.5 m drives, 17 cables, max span 12 m.'), @('gss_CB-05', 'GSS CB-05, vertical opening brow, 3 cables.'), @('gss_cable_sequence_3way', 'GSS cable bolt sequence guidelines, 3-way intersections.'), @('gss_cable_sequence_4way', 'GSS cable bolt sequence guidelines, 4-way intersections.'))) {
    $P = putPicture $P "$img\$($cb[0]).png" 400; $P = putCaption $P 'Figure' $cb[1] }
  $P.Range.Delete() | Out-Null
  $log.Add('Added: strip standards figure, Cable Bolting at Intersections subsection (text + 9 figures)')
  # ---------------- GSS regime by profile table before "Design Properties"
  $dp = $null; foreach ($pa in $d.Paragraphs) { if ($pa.Range.Text.Trim() -eq 'Design Properties' -and ([string]$pa.Style.NameLocal) -like 'Heading*') { $dp = $pa; break } }
  if (-not $dp) { throw '"Design Properties" heading not found' }
  $P = newPointBefore $dp; $P.Style = $bodyStyle
  $P = putText $P 'Ground support for each profile follows the site GSS. The GSS attribute on the design string (drop-down in the design drafter file) records the regime. [CONFIRM the drop-down list against the GSS].' $bodyStyle 0
  $P = putCaption $P 'Table' 'Ground support regime by development profile (site GSS).'
  $rowsG = @(
    @('GSS regime', 'Headings', 'Mesh height', 'Bolts', 'Quantities (per GSS sheet)'),
    @('1.0', '6.0mW x 6.0mH', '2.5 m', '2.4 m friction bolts + 0.9 m (39 mm) friction bolts', '6 mesh sheets (4.5 x 2.4 m), 39 x 2.4 m, 13 x 0.9 m'),
    @('1.1 (portal, first 20 m)', '6.0mW x 6.0mH', '2.5 m, FRS to min 1.5 m', '50 mm fibrecrete, mesh, grouted friction bolts, 6 m cable bolts', '6 mesh sheets, 39 x 2.4 m, 13 x 0.9 m + cables'),
    @('2.0', '5.5mW x 5.7mH, 5.5mW x 5.5mH', '2.5 m', '2.4 m friction bolts + 0.9 m (39 mm) friction bolts', '5 mesh sheets, 33 x 2.4 m, 11 x 0.9 m'),
    @('3.0', '5.0mW x 5.0mH', '2.5 m', '2.4 m friction bolts + 0.9 m (39 mm) friction bolts', '4 mesh sheets, 27 x 2.4 m, 9 x 0.9 m'),
    @('3.1', '5.0mW x 5.0mH', 'to grade line', '2.4 m friction bolts + 0.9 m (39 mm) friction bolts', '5 mesh sheets, 33 x 2.4 m, 11 x 0.9 m'),
    @('4.0', '4.5mW x 4.5mH, 5.0mW x 4.5mH', '2.0 m', '2.1 m friction bolts + 0.9 m (39 mm) friction bolts', '4 mesh sheets, 27 x 2.1 m, 9 x 0.9 m'),
    @('4.0 (rhino hole)', '700 mm rhino hole', 'folded mesh', '0.9 m (47 mm) friction bolts', '1 mesh sheet (4.0 x 2.4 m) folded, 4 x 0.9 m'))
  $P.Style = 'Normal'
  $tg = $d.Tables.Add($P.Range, $rowsG.Count, 5)
  $t12 = findTable $d 'Development type'; if ($t12) { try { $tg.Style = $t12.Style } catch {} }
  for ($r = 0; $r -lt $rowsG.Count; $r++) { for ($c = 0; $c -lt 5; $c++) { $tg.Cell($r + 1, $c + 1).Range.Text = $rowsG[$r][$c] } }
  $tg.Rows.Item(1).HeadingFormat = $true; $tg.Rows.Item(1).Range.Font.Bold = $true
  $log.Add('Added: GSS regime by profile table + GSS attribute note')  # ---------------- version row
  $tv = findTable $d 'Version'
  if ($tv -and (cellText $tv 1 2) -like 'Description*') { [void]$tv.Rows.Add(); $n = $tv.Rows.Count; $cols = $tv.Columns.Count
    $vals = @('WIP v2', 'Tables updated to the site GSS and the design drafter reference layers; added GSS regime-by-profile table, cable bolting section and figures. Draft prepared with AI assistance - for review by the Geotechnical Engineer and Snr Mining Engineer.', '<author>', '<dd/mm/yyyy>', 'Draft')
    for ($c = 0; $c -lt [math]::Min($cols, $vals.Count); $c++) { $tv.Cell($n, $c + 1).Range.Text = $vals[$c] }; $log.Add("Version table: row added ($cols columns)") } else { $log.Add('Version table not found') }
  # ---------------- refresh numbering, TOC, tables of figures
  [void]$d.Fields.Update(); foreach ($t in $d.TablesOfContents) { $t.Update() }; foreach ($t in $d.TablesOfFigures) { $t.Update() }
  $log.Add("pages now " + $d.ComputeStatistics(2) + ", tracked revisions " + $d.Revisions.Count)
  $d.Save(); $d.Close(0); $log.Add("saved $dst")
} catch { $log.Add("ERROR: " + $_.Exception.Message); try { $d.Close(0) } catch {}; if (Test-Path -LiteralPath $dst) { Remove-Item -LiteralPath $dst; $log.Add('removed the incomplete copy') } }
finally { $wd.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($wd) }
$log
