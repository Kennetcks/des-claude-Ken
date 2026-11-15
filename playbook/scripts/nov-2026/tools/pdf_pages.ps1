# Render PDF pages to PNG with the Windows built-in PDF API (no extra tools), then tile them into contact sheets.
param([string]$pdf, [string]$outDir, [int[]]$pages, [int]$perSheet = 6, [string]$prefix = 'sheet')
Add-Type -AssemblyName System.Runtime.WindowsRuntime, System.Drawing
$null = [Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime]
$null = [Windows.Data.Pdf.PdfDocument, Windows.Data.Pdf, ContentType = WindowsRuntime]
$null = [Windows.Storage.Streams.InMemoryRandomAccessStream, Windows.Storage.Streams, ContentType = WindowsRuntime]
$asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' }
function Await($op, [Type]$t) { $task = $asTask.MakeGenericMethod($t).Invoke($null, @($op)); $task.Wait() | Out-Null; $task.Result }
function AwaitAction($op) { $m = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncAction' }; $m.Invoke($null, @($op)).Wait() | Out-Null }
New-Item -ItemType Directory -Force $outDir | Out-Null
$file = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($pdf)) ([Windows.Storage.StorageFile])
$doc = Await ([Windows.Data.Pdf.PdfDocument]::LoadFromFileAsync($file)) ([Windows.Data.Pdf.PdfDocument])
"pages in pdf: $($doc.PageCount)"
$imgs = @()
foreach ($n in $pages) {
  if ($n -gt $doc.PageCount) { continue }
  $page = $doc.GetPage($n - 1); $stream = New-Object Windows.Storage.Streams.InMemoryRandomAccessStream
  $opt = New-Object Windows.Data.Pdf.PdfPageRenderOptions; $opt.DestinationWidth = 700
  AwaitAction ($page.RenderToStreamAsync($stream, $opt))
  $net = [System.IO.WindowsRuntimeStreamExtensions]::AsStreamForRead($stream); $net.Position = 0
  $imgs += , @($n, [System.Drawing.Image]::FromStream($net)); $page.Dispose()
}
for ($s = 0; $s -lt $imgs.Count; $s += $perSheet) {
  $chunk = $imgs[$s..([Math]::Min($s + $perSheet, $imgs.Count) - 1)]; $cols = 3; $rows = [Math]::Ceiling($chunk.Count / $cols)
  $W = 700; $H = [int]($chunk[0][1].Height * $W / $chunk[0][1].Width)
  $bmp = New-Object System.Drawing.Bitmap ($W * $cols), ($H * $rows); $g = [System.Drawing.Graphics]::FromImage($bmp); $g.Clear([System.Drawing.Color]::DarkGray)
  for ($i = 0; $i -lt $chunk.Count; $i++) { $x = ($i % $cols) * $W; $y = [Math]::Floor($i / $cols) * $H; $g.DrawImage($chunk[$i][1], $x + 3, $y + 3, $W - 6, $H - 6); $g.DrawString("p$($chunk[$i][0])", (New-Object System.Drawing.Font('Arial', 18, [System.Drawing.FontStyle]::Bold)), [System.Drawing.Brushes]::Red, $x + 8, $y + 8) }
  $f = Join-Path $outDir ("{0}_{1:D2}.png" -f $prefix, ($s / $perSheet + 1)); $bmp.Save($f); $g.Dispose(); $bmp.Dispose(); $f
}
