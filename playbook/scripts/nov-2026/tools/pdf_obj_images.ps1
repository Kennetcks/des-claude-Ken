# READ ONLY: save PDF image objects by object number (JPEG as-is, Flate RGB/Gray to PNG) and build a labelled contact sheet.
param([string]$pdf, [int[]]$ids, [string]$outDir = "$env:TEMP\dsw\pdfimg")
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
if (-not ('PixCopy' -as [type])) {
  Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System; using System.Drawing; using System.Drawing.Imaging; using System.Runtime.InteropServices;
public static class PixCopy {
  public static Bitmap ToBitmap(byte[] raw, int w, int h) {
    int ch = raw.Length / (w * h); var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
    var bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, bmp.PixelFormat); var row = new byte[bd.Stride];
    for (int y = 0; y < h; y++) { for (int x = 0; x < w; x++) { int q = (y * w + x) * ch;
        if (ch >= 3) { row[3*x] = raw[q+2]; row[3*x+1] = raw[q+1]; row[3*x+2] = raw[q]; } else { row[3*x] = row[3*x+1] = row[3*x+2] = raw[q]; } }
      Marshal.Copy(row, 0, IntPtr.Add(bd.Scan0, y * bd.Stride), bd.Stride); }
    bmp.UnlockBits(bd); return bmp; } }
'@ }
New-Item -ItemType Directory -Force $outDir | Out-Null
$b = [IO.File]::ReadAllBytes($pdf); $s = [Text.Encoding]::GetEncoding(28591).GetString($b)
$obj = @{}; foreach ($m in [regex]::Matches($s, '(?s)(\d+) 0 obj(.*?)endobj')) { $obj[[int]$m.Groups[1].Value] = $m.Groups[2] }
$tw = 330; $th = 230; $sheet = New-Object Drawing.Bitmap (3 * $tw), ([Math]::Ceiling($ids.Count / 3) * $th); $g = [Drawing.Graphics]::FromImage($sheet); $g.Clear([Drawing.Color]::White)
$f = New-Object Drawing.Font('Arial', [single]12, [Drawing.FontStyle]::Bold); $k = 0
foreach ($n in $ids) {
  $m = $obj[$n]; $v = $m.Value; $i = $v.IndexOf('stream'); $hd = $v.Substring(0, $i)
  $w = [int][regex]::Match($hd, '/Width (\d+)').Groups[1].Value; $h = [int][regex]::Match($hd, '/Height (\d+)').Groups[1].Value
  $len = [regex]::Match($hd, '/Length (\d+)( 0 R)?'); $L = [int]$len.Groups[1].Value; if ($len.Groups[2].Success) { $L = [int][regex]::Match($obj[$L].Value, '\d+').Value }
  $st = $m.Index + $i + 6; if ($s[$st] -eq "`r") { $st++ }; if ($s[$st] -eq "`n") { $st++ }
  $data = New-Object byte[] $L; [Array]::Copy($b, $st, $data, 0, $L)
  if ($hd -match 'DCTDecode') { [IO.File]::WriteAllBytes("$outDir\o$n.jpg", $data); $img = [Drawing.Image]::FromFile("$outDir\o$n.jpg") }
  else { $ms = [IO.MemoryStream]::new($data, 2, ($L - 2)); $ds = [IO.Compression.DeflateStream]::new($ms, [IO.Compression.CompressionMode]::Decompress); $o = [IO.MemoryStream]::new(); $ds.CopyTo($o)
    $img = [PixCopy]::ToBitmap($o.ToArray(), $w, $h); $img.Save("$outDir\o$n.png") }
  $cx = ($k % 3) * $tw; $cy = [Math]::Floor($k / 3) * $th; $sc = [Math]::Min(($tw - 10) / $img.Width, ($th - 25) / $img.Height)
  $g.DrawImage($img, $cx + 5, $cy + 22, [int]($img.Width * $sc), [int]($img.Height * $sc)); $g.DrawString("obj $n", $f, [Drawing.Brushes]::Red, $cx + 5, $cy + 2); $k++ }
$sheet.Save("$outDir\sheet.png"); "sheet: $outDir\sheet.png"
