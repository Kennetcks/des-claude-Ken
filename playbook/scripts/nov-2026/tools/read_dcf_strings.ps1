# READ ONLY: list every string in an extracted Deswik .dcf (VectorDraw XML) with layer, key attributes and decoded vertices.
param([string]$dir = "$env:TEMP\dsw\raddcf", [string]$outJson = "$env:TEMP\dsw\rad_strings.json")
$ErrorActionPreference = 'Stop'
$all = @()
foreach ($f in Get-ChildItem $dir) {
  $t = [IO.File]::ReadAllText($f.FullName)
  # layer handle -> name (layer table lives in the main file)
  $layers = @{}
  foreach ($m in [regex]::Matches($t, '<O F="_Item" T="vdLayer"><P F="Handle" T="vdHandle" V="(\d+)" />.*?<P F="Name" V="([^"]*)"')) { $layers[$m.Groups[1].Value] = $m.Groups[2].Value }
  $parts = [regex]::Split($t, '(?=<O F="_Item" T="(?:dwPolyline|vdPolyline|vdLine)">)')
  foreach ($p in $parts) {
    $ty = [regex]::Match($p, '^<O F="_Item" T="(\w+)">').Groups[1].Value; if (-not $ty) { continue }
    $h = [regex]::Match($p, 'F="Handle" T="vdHandle" V="(\d+)"').Groups[1].Value
    $lay = [regex]::Match($p, '<H F="Layer" V="(\d+)"').Groups[1].Value
    $attr = @{}; foreach ($m in [regex]::Matches($p, '<P F="Name" V="([^"]*)" /><P F="PropValue"(?: T="\w+")? V="([^"]*)"')) { $attr[$m.Groups[1].Value] = $m.Groups[2].Value }
    $pts = @()
    $s = [regex]::Match($p, 'F="VertexList" T="Vertexes"><P F="streamed" V="([^"]+)"').Groups[1].Value
    if ($s) { $b = [Convert]::FromBase64String($s); $n = [BitConverter]::ToInt32($b, 0); $stride = [int](($b.Length - 4) / [Math]::Max($n, 1))
      for ($i = 0; $i -lt $n; $i++) { $o = 4 + $i * $stride; $pts += , @([BitConverter]::ToDouble($b, $o), [BitConverter]::ToDouble($b, $o + 8), [BitConverter]::ToDouble($b, $o + 16)) } }
    elseif ($ty -eq 'vdLine') { foreach ($k in 'StartPoint', 'EndPoint') { $m = [regex]::Match($p, "F=`"$k`"[^>]*V=`"([^`"]+)`""); if ($m.Success) { $pts += , @($m.Groups[1].Value -split '[ ,]+' | ForEach-Object { [double]$_ }) } } }
    $all += [pscustomobject]@{ File = $f.Name.Replace('RAD Decline draft.dcf', ''); Type = $ty; Handle = $h; Layer = $(if ($layers[$lay]) { $layers[$lay] } else { $lay }); ACT_TYPE = $attr['ACT_TYPE']; LEVEL = $attr['LEVEL']; Description = $attr['Description']; PROFILE = $attr['PROFILE']; Status = $attr['Status']; N = $pts.Count; Pts = $pts }
  }
}
$all | ConvertTo-Json -Depth 6 | Set-Content $outJson -Encoding UTF8
foreach ($a in $all) {
  $len = 0; for ($i = 1; $i -lt $a.N; $i++) { $dx = $a.Pts[$i][0] - $a.Pts[$i - 1][0]; $dy = $a.Pts[$i][1] - $a.Pts[$i - 1][1]; $dz = $a.Pts[$i][2] - $a.Pts[$i - 1][2]; $len += [Math]::Sqrt($dx * $dx + $dy * $dy + $dz * $dz) }
  $s0 = if ($a.N) { '{0:F1} {1:F1} {2:F1}' -f $a.Pts[0][0], $a.Pts[0][1], $a.Pts[0][2] } else { '' }
  $s1 = if ($a.N) { '{0:F1} {1:F1} {2:F1}' -f $a.Pts[$a.N - 1][0], $a.Pts[$a.N - 1][1], $a.Pts[$a.N - 1][2] } else { '' }
  '{0} h{1} [{2}] {3} L{4} "{5}" {6} {7} n={8} len={9:F1} | start {10} | end {11}' -f $a.Type, $a.Handle, $a.Layer, $a.ACT_TYPE, $a.LEVEL, $a.Description, $a.PROFILE, $a.Status, $a.N, $len, $s0, $s1
}
