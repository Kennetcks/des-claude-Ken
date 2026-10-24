# Development Design Criteria only: copy of WIP_v2 with all revisions accepted, everything outside the section removed,
# Figures 1-6 replaced with views of the design (Level Design Drafter Design Master / MINE_UG_FY27). WIP and v2 untouched.
$ErrorActionPreference = 'Stop'
$folder = "<DOCS>"
$src = Join-Path $folder 'Mine Design Standard_WIP_v2.docx'
$dst = Join-Path $folder 'Mine Design Standard - Development Design Criteria_v3.docx'
$img = "$env:TEMP\dsw\mds_figs"
if (Test-Path -LiteralPath $dst) { "v3 already exists - stopping so nothing is overwritten"; return }
Copy-Item -LiteralPath $src -Destination $dst
$log = New-Object System.Collections.Generic.List[string]
$wd = New-Object -ComObject Word.Application; $wd.Visible = $false; $wd.DisplayAlerts = 0
try {
  $d = $wd.Documents.Open($dst)
  $d.TrackRevisions = $false; $d.Revisions.AcceptAll(); $log.Add('accepted all revisions; tracking off')
  # ---- keep only Heading 1 "Development Design Criteria" up to (not incl.) Heading 1 "Stope Design Criteria"
  $h1s = $null; $h1e = $null
  foreach ($p in $d.Paragraphs) { $t = $p.Range.Text.Trim(); $st = [string]$p.Style.NameLocal
    if ($st -eq 'Heading 1' -and $t -eq 'Development Design Criteria') { $h1s = $p.Range.Start }
    if ($st -eq 'Heading 1' -and $t -eq 'Stope Design Criteria' -and $h1s) { $h1e = $p.Range.Start; break } }
  if (-not $h1s -or -not $h1e) { throw 'section headings not found' }
  $d.Range($h1e, $d.Content.End).Delete() | Out-Null
  $d.Range(0, $h1s).Delete() | Out-Null
  $log.Add("kept the Development Design Criteria section only; pages " + $d.ComputeStatistics(2))
  # ---- replace pictures: find the caption, the picture is the previous paragraph
  function capPara($needle) { foreach ($p in $d.Paragraphs) { if (([string]$p.Style.NameLocal) -like 'Caption*' -and $p.Range.Text -like "*$needle*") { return $p } }; return $null }
  function replacePic($needle, $files, $newTitle) {
    $cp = capPara $needle; if (-not $cp) { $log.Add("caption '$needle' NOT FOUND"); return }
    $pp = $cp.Previous(1); if ($pp.Range.InlineShapes.Count -lt 1) { $log.Add("no picture before '$needle'"); return }
    $old = $pp.Range.InlineShapes.Item(1); $w = $old.Width; $rg = $old.Range; $old.Delete()
    $each = [double][math]::Max([double]$w, 420.0)
    for ($i = $files.Count - 1; $i -ge 0; $i--) { $r2 = $pp.Range; [void]$r2.MoveEnd(1, -1); $r2.Collapse(1)
      $pic = $d.InlineShapes.AddPicture($files[$i], $false, $true, $r2); $ratio = $pic.Height / $pic.Width; $pic.Width = [single][math]::Min($each, 460.0); $pic.Height = [single]($pic.Width * $ratio) }
    $rf = $cp.Range; $f = $rf.Find; $f.ClearFormatting(); $f.Replacement.ClearFormatting()
    [void]$f.Execute($needle, $false, $false, $false, $false, $false, $true, 0, $false, $newTitle, 1)
    $log.Add("replaced picture: $needle") }
  replacePic 'Example of back-stripping requirements.' @("$img\fig1_back_strip_dsp.png") 'Back stripping at a decline stockpile: truck loading strip (Design Master).'
  replacePic 'Example of wall-stripping requirements.' @("$img\fig2_wall_strip_dec.png") 'Wall stripping on the decline: fan strip and J/Box strip (Design Master).'
  replacePic 'Example of electrical and pump box stripping requirements.' @("$img\fig3_elec_pump.png") 'Electrical and pump stripping on the decline and at a sump (Design Master).'
  replacePic 'Example of Decline Design.' @("$img\fig4_decline.png") 'Decline design with the stockpile and level take-offs (Design Master).'
  replacePic 'Example of Accessway Design.' @("$img\fig5_access.png") 'Accessway design: MLA, ISP, ACC and SMP off the decline (Design Master).'
  replacePic 'Example of Ladderway Arrangement for Secondary Egress.' @("$img\fig6_egress_3d.png") 'Secondary egress: escapeway drives and rises between levels.'
  [void]$d.Fields.Update()
  $d.Save(); $log.Add("pages now " + $d.ComputeStatistics(2)); $d.Close(0); $log.Add("saved $dst")
} catch { $log.Add("ERROR: " + $_.Exception.Message); try { $d.Close(0) } catch {}; if (Test-Path -LiteralPath $dst) { Remove-Item -LiteralPath $dst; $log.Add('removed the incomplete copy') } }
finally { $wd.Quit(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($wd) }
$log
