# READ ONLY: save the images drawn on PDF pages whose text contains "Page <n>" (footer), JPEG as .jpg, Flate RGB/Gray as .png.
param([string]$pdf, [int[]]$pages, [string]$outDir = "$env:TEMP\dsw\pdfimg")
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $outDir | Out-Null
$b = [IO.File]::ReadAllBytes($pdf); $s = [Text.Encoding]::GetEncoding(28591).GetString($b)
$obj = @{}
foreach ($m in [regex]::Matches($s, '(?s)(\d+) 0 obj(.*?)endobj')) { $obj[[int]$m.Groups[1].Value] = $m.Groups[2] }
function Dict($n) { if (-not $obj.ContainsKey($n)) { return '' }; $t = $obj[$n].Value; $i = $t.IndexOf('stream'); if ($i -ge 0) { $t.Substring(0, $i) } else { $t } }
function Raw($n) {
  $m = $obj[$n]; $hd = Dict $n
  $len = [regex]::Match($hd, '/Length (\d+)( 0 R)?'); $L = [int]$len.Groups[1].Value; if ($len.Groups[2].Success) { $L = [int][regex]::Match($obj[$L].Value, '\d+').Value }
  $st = $m.Index + $m.Value.IndexOf('stream') + 6; if ($s[$st] -eq "`r") { $st++ }; if ($s[$st] -eq "`n") { $st++ }
  $data = New-Object byte[] $L; [Array]::Copy($b, $st, $data, 0, $L); , $data }
function Inflate($data) { $ms = New-Object IO.MemoryStream($data, 2, $data.Length - 2); $ds = New-Object IO.Compression.DeflateStream($ms, 'Decompress'); $o = New-Object IO.MemoryStream; $ds.CopyTo($o); , $o.ToArray() }
$want = @{}; foreach ($p in $pages) { $want["Page $p "] = $p }
foreach ($n in $obj.Keys) {
  $d = Dict $n; if ($d -notmatch '/Type\s*/Page[^s]') { continue }
  $txt = ''
  foreach ($c in [regex]::Matches([regex]::Match($d, '/Contents\s*(\[[^\]]*\]|\d+ 0 R)').Groups[1].Value, '(\d+) 0 R')) {
    try { $raw = Inflate (Raw ([int]$c.Groups[1].Value)); $t = [Text.Encoding]::GetEncoding(28591).GetString($raw); $txt += (([regex]::Matches($t, '\((?:\\.|[^\\)])*\)') | ForEach-Object { $_.Value.Substring(1, $_.Value.Length - 2) }) -join '') } catch {} }
  $pg = $null; foreach ($k in $want.Keys) { if ($txt.Contains($k)) { $pg = $want[$k] } }; if (-not $pg) { continue }
  $rd = $d; $res = [regex]::Match($d, '/Resources\s*(\d+) 0 R'); if ($res.Success) { $rd = Dict ([int]$res.Groups[1].Value) }
  $xo = [regex]::Match($rd, '/XObject\s*(?:<<(.*?)>>|(\d+) 0 R)')
  $xd = if ($xo.Groups[2].Success) { Dict ([int]$xo.Groups[2].Value) } else { $xo.Groups[1].Value }
  foreach ($r in [regex]::Matches($xd, '/(\w+)\s+(\d+) 0 R')) {
    $in = [int]$r.Groups[2].Value; $hd = Dict $in; if ($hd -notmatch '/Subtype\s*/Image') { continue }
    $w = [int][regex]::Match($hd, '/Width (\d+)').Groups[1].Value; $h = [int][regex]::Match($hd, '/Height (\d+)').Groups[1].Value
    if ($w -lt 100 -or $h -lt 60) { continue }
    $data = Raw $in; $name = "p{0}_{1}_{2}x{3}" -f $pg, $in, $w, $h
    if ($hd -match 'DCTDecode') { [IO.File]::WriteAllBytes("$outDir\$name.jpg", $data); "$name.jpg" }
    elseif ($hd -match 'FlateDecode') {
      $raw = Inflate $data; $ch = [int][Math]::Floor($raw.Length / ($w * $h)); if ($ch -lt 1) { continue }
      $bmp = New-Object Drawing.Bitmap $w, $h, ([Drawing.Imaging.PixelFormat]::Format24bppRgb)
      $rect = New-Object Drawing.Rectangle 0, 0, $w, $h; $bd = $bmp.LockBits($rect, 'WriteOnly', $bmp.PixelFormat)
      $row = New-Object byte[] ($bd.Stride)
      for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) { $i = ($y * $w + $x) * $ch; if ($ch -ge 3) { $row[3 * $x] = $raw[$i + 2]; $row[3 * $x + 1] = $raw[$i + 1]; $row[3 * $x + 2] = $raw[$i] } else { $row[3 * $x] = $row[3 * $x + 1] = $row[3 * $x + 2] = $raw[$i] } }
        [Runtime.InteropServices.Marshal]::Copy($row, 0, [IntPtr]::Add($bd.Scan0, $y * $bd.Stride), $bd.Stride) }
      $bmp.UnlockBits($bd); $bmp.Save("$outDir\$name.png"); "$name.png ($ch ch)" }
  }
}
