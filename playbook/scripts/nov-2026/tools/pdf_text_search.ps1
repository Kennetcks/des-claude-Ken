# READ ONLY: inflate every FlateDecode stream in a PDF, pull the text-showing operators out, and print hits for a regex with context.
param([string]$pdf = 'C:\Program Files\Ventsim DESIGN 6\VentsimManual.pdf', [string]$pattern = '(?i)import', [int]$ctx = 300, [int]$max = 40)
$ErrorActionPreference = 'Stop'
$b = [IO.File]::ReadAllBytes($pdf); $s = [Text.Encoding]::GetEncoding(28591).GetString($b)
$hits = 0; $pos = 0; $n = 0
while (($i = $s.IndexOf('stream', $pos)) -ge 0 -and $hits -lt $max) {
  $st = $i + 6; if ($s[$st] -eq "`r") { $st++ }; if ($s[$st] -eq "`n") { $st++ }
  $e = $s.IndexOf('endstream', $st); if ($e -lt 0) { break }; $pos = $e + 9; $n++
  try {
    $ms = New-Object IO.MemoryStream($b, ($st + 2), ($e - $st - 2))   # skip the 2-byte zlib header
    $ds = New-Object IO.Compression.DeflateStream($ms, [IO.Compression.CompressionMode]::Decompress); $sr = New-Object IO.StreamReader($ds, [Text.Encoding]::GetEncoding(28591)); $txt = $sr.ReadToEnd()
  } catch { continue }
  if ($txt -notmatch 'TJ|Tj') { continue }
  $plain = (([regex]::Matches($txt, '\((?:\\.|[^\\)])*\)') | ForEach-Object { $_.Value.Substring(1, $_.Value.Length - 2) -replace '\\(.)', '$1' }) -join '')
  foreach ($m in [regex]::Matches($plain, $pattern)) { $hits++; "--- stream ${n}: " + $plain.Substring([Math]::Max(0, $m.Index - $ctx), [Math]::Min($plain.Length - [Math]::Max(0, $m.Index - $ctx), 2 * $ctx)); if ($hits -ge $max) { break } }
}
"streams scanned: $n, hits: $hits"
