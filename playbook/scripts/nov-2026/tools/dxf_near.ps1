# READ ONLY: for each DXF, report the extents and the points within $r m (plan) of X,Y, grouped by layer with Z range.
param([double]$x = 8168.1, [double]$y = 63131.8, [double]$r = 150)
$ErrorActionPreference = 'Stop'
$dir = '<DESKTOP>\02 Design & Technical\CAD'
foreach ($f in Get-ChildItem -LiteralPath $dir -Filter *.dxf) {
  $L = [IO.File]::ReadAllLines($f.FullName); $layer = ''; $px = $null; $py = $null
  $minx = 1e12; $maxx = -1e12; $miny = 1e12; $maxy = -1e12; $minz = 1e12; $maxz = -1e12; $near = @{}
  for ($i = 0; $i -lt $L.Length - 1; $i += 2) {
    $c = $L[$i].Trim(); $v = $L[$i + 1].Trim()
    if ($c -eq '8') { $layer = $v }
    elseif ($c -eq '10') { $px = [double]$v }
    elseif ($c -eq '20') { $py = [double]$v }
    elseif ($c -eq '30' -and $px -ne $null) { $pz = [double]$v
      if ($px -lt $minx) { $minx = $px }; if ($px -gt $maxx) { $maxx = $px }; if ($py -lt $miny) { $miny = $py }; if ($py -gt $maxy) { $maxy = $py }; if ($pz -lt $minz) { $minz = $pz }; if ($pz -gt $maxz) { $maxz = $pz }
      $d = [Math]::Sqrt(($px - $x) * ($px - $x) + ($py - $y) * ($py - $y))
      if ($d -le $r) { if (-not $near[$layer]) { $near[$layer] = @(0, 1e12, -1e12, 1e12, 0) }; $n = $near[$layer]; $n[0]++; if ($pz -lt $n[1]) { $n[1] = $pz }; if ($pz -gt $n[2]) { $n[2] = $pz }; if ($d -lt $n[3]) { $n[3] = $d; $n[4] = $pz } }
      $px = $null } }
  '{0}: X {1:F0}-{2:F0} Y {3:F0}-{4:F0} Z {5:F1}-{6:F1}' -f $f.Name, $minx, $maxx, $miny, $maxy, $minz, $maxz
  foreach ($k in $near.Keys) { $n = $near[$k]; '   [{0}] {1} pts, Z {2:F1}-{3:F1}, closest {4:F1} m at Z {5:F1}' -f $k, $n[0], $n[1], $n[2], $n[3], $n[4] }
}
