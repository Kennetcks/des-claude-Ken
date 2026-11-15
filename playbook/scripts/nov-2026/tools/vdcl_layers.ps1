# READ ONLY: stream a gunzipped Deswik .vdcl/.dcf XML (too big to load) and list the layers (handle, name, every simple property),
# the ActiveLayer handle and how many entities sit on each layer. Writes layers.tsv beside the xml.
param([string]$xml = "$env:TEMP\dsw\ldd\ldd.xml", [string]$match = '')
$ErrorActionPreference = 'Stop'
$set = New-Object Xml.XmlReaderSettings; $set.IgnoreWhitespace = $true
$r = [Xml.XmlReader]::Create($xml, $set)
$layers = New-Object Collections.Generic.List[object]; $count = @{}; $active = @(); $cur = $null; $curDepth = -1
$entDepth = -1; $entType = ''
while ($r.Read()) {
  if ($r.NodeType -ne 'Element') { if ($r.NodeType -eq 'EndElement' -and $cur -and $r.Depth -eq $curDepth) { $layers.Add([pscustomobject]$cur); $cur = $null }; continue }
  $f = $r.GetAttribute('F'); $t = $r.GetAttribute('T')
  if ($r.Name -eq 'O' -and $t -eq 'vdLayer') { $cur = [ordered]@{}; $curDepth = $r.Depth; if ($r.IsEmptyElement) { $cur = $null }; continue }
  if ($cur -and $r.Name -eq 'P' -and $r.Depth -eq $curDepth + 1) { $v = $r.GetAttribute('V'); if ($null -ne $v) { $cur[$f] = $v } ; continue }
  if ($cur -and $r.Name -eq 'H' -and $r.Depth -eq $curDepth + 1) { $cur['H:' + $f] = $r.GetAttribute('V'); continue }
  if ($f -match '^Active' -and $r.Name -in 'H', 'P') { $active += "$f=$($r.GetAttribute('V')) (depth $($r.Depth))" }
  if ($r.Name -eq 'O' -and $f -eq '_Item' -and $t -ne 'vdLayer') { $entType = $t; $entDepth = $r.Depth }
  if ($r.Name -eq 'H' -and $f -eq 'Layer' -and $r.Depth -eq $entDepth + 1) { $k = $r.GetAttribute('V'); $count[$k] = 1 + [int]$count[$k] }
}
$r.Close()
"active: " + ($active | Select-Object -First 20) -join '; '
$props = $layers | ForEach-Object { $_.PSObject.Properties.Name } | Sort-Object -Unique
"layer props: " + ($props -join ', ')
$rows = foreach ($l in $layers) { [pscustomobject]@{ Handle = $l.Handle; Name = $l.Name; N = [int]$count[$l.Handle]; Raw = (($l.PSObject.Properties | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join '|') } }
$rows | Export-Csv ([IO.Path]::ChangeExtension($xml, '.layers.tsv')) -Delimiter "`t" -NoTypeInformation -Encoding UTF8
"layers: $($rows.Count)"
$rows | Where-Object { -not $match -or $_.Name -match $match } | ForEach-Object { '{0,8} {1,6}  {2}' -f $_.Handle, $_.N, $_.Name }
