# READ ONLY: dump every non-empty cell (values, plus formulas when -formulas) and cell comments of an .xlsx without opening Excel.
param([Parameter(Mandatory)][string]$path, [switch]$formulas)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$z = [IO.Compression.ZipFile]::OpenRead($path)
function ReadZ($n) { $e = $z.GetEntry($n); if (-not $e) { return $null }; $sr = New-Object IO.StreamReader($e.Open()); $t = $sr.ReadToEnd(); $sr.Close(); $t }
$ss = @(); $t = ReadZ 'xl/sharedStrings.xml'; if ($t) { foreach ($si in ([xml]$t).sst.si) { $ss += $si.InnerText } }
$wb = [xml](ReadZ 'xl/workbook.xml'); $rels = [xml](ReadZ 'xl/_rels/workbook.xml.rels')
$rns = 'http://schemas.openxmlformats.org/officeDocument/2006/relationships'
foreach ($s in $wb.workbook.sheets.sheet) {
  $tgt = ($rels.Relationships.Relationship | Where-Object { $_.Id -eq $s.GetAttribute('id', $rns) }).Target -replace '^/?(xl/)?', ''
  "===== SHEET: $($s.name)"
  $sx = [xml](ReadZ "xl/$tgt")
  foreach ($row in $sx.worksheet.sheetData.row) {
    $vals = @(); foreach ($c in $row.c) { $v = $c.v; if ($c.t -eq 's') { $v = $ss[[int]$v] } elseif ($c.t -eq 'inlineStr') { $v = $c.is.InnerText }
      $f = if ($formulas -and $c.f) { " {=" + $c.f.InnerText + "}" } else { '' }
      if ("$v$f" -ne '') { $vals += "$($c.r)=$v$f" } }
    if ($vals) { $vals -join ' | ' } }
  $srels = ReadZ ("xl/worksheets/_rels/" + [IO.Path]::GetFileName($tgt) + '.rels')
  if ($srels) { foreach ($rr in ([xml]$srels).Relationships.Relationship | Where-Object { $_.Type -match 'comments' }) {
      $cx = [xml](ReadZ ('xl/' + ($rr.Target -replace '^\.\./', '' -replace '^/?xl/', '')))
      foreach ($cm in $cx.comments.commentList.comment) { "  [comment $($cm.ref)] $($cm.InnerText)" } } }
}
$z.Dispose()
