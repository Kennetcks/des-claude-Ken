# READ ONLY: stream a gunzipped Deswik .vdcl/.dcf XML and write out the full XML of every entity on the given layer handles
# (one entity per line) so they can be studied or used as templates.
param([string]$xml = "$env:TEMP\dsw\ldd\ldd.xml", [string[]]$layers = @('63433786'), [string]$out = "$env:TEMP\dsw\ldd\strip_standards.xmlents")
$ErrorActionPreference = 'Stop'
$skip = 'vdXProperty', 'vdXProperties', 'vdLayer', 'vdColor'
$pat = '<H F="Layer" V="(' + ($layers -join '|') + ')" />'
$set = New-Object Xml.XmlReaderSettings; $set.IgnoreWhitespace = $true
$r = [Xml.XmlReader]::Create($xml, $set); $w = New-Object IO.StreamWriter($out, $false, [Text.Encoding]::UTF8); $n = 0; $seen = @{}
$r.Read() | Out-Null
while (-not $r.EOF) {
  if ($r.NodeType -eq 'Element' -and $r.Name -eq 'O' -and $r.GetAttribute('F') -eq '_Item' -and $r.GetAttribute('T') -notin $skip -and $r.Depth -le 6) {
    $t = $r.GetAttribute('T'); $d = $r.Depth; $x = $r.ReadOuterXml()   # moves past the element
    $seen["$t@$d"] = 1 + [int]$seen["$t@$d"]
    # the entity's own Layer handle sits after its XProperties block; strip nested XProperties before testing
    $own = [regex]::Replace($x, '<O F="XProperties" T="vdXProperties">.*?</O></O>(?=<)', '', 'Singleline')
    if ($own -match $pat) { $w.WriteLine($x); $n++ }
    continue }
  $r.Read() | Out-Null }
$r.Close(); $w.Close()
"entities written: $n -> $out"
$seen.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 15 | ForEach-Object { '{0,-30} {1}' -f $_.Key, $_.Value }
