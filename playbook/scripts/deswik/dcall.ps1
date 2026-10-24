# Call a Kennet.DeswikClaude tool over the pipe, with a reply timeout, retrying when the call lands in the other Deswik window.
param([string]$name, [string]$inputJson = '{}', [string]$mustContain = '', [int]$tries = 10, [int]$waitMs = 120000)
for ($k = 1; $k -le $tries; $k++) {
  $c = New-Object System.IO.Pipes.NamedPipeClientStream('.', 'KennetDeswikClaude', [System.IO.Pipes.PipeDirection]::InOut, [System.IO.Pipes.PipeOptions]::Asynchronous)
  try { $c.Connect(5000) } catch { Start-Sleep 1; continue }
  $w = New-Object System.IO.StreamWriter($c); $w.AutoFlush = $true; $r = New-Object System.IO.StreamReader($c)
  $w.WriteLine((@{ op = 'call'; name = $name; input = ($inputJson | ConvertFrom-Json) } | ConvertTo-Json -Depth 20 -Compress))
  $t = $r.ReadLineAsync(); $ok = $t.Wait($waitMs); $c.Dispose()
  if (-not $ok) { "NO REPLY within $($waitMs / 1000) s (Deswik busy or waiting for approval)"; return }
  $resp = $t.Result | ConvertFrom-Json; $text = [string]$resp.text
  if ($text -match 'WRONG WINDOW' -or ($mustContain -and $text -notmatch [regex]::Escape($mustContain))) { continue }
  if ($resp.isError) { 'ERROR:' }; $text; return }
"gave up after $tries tries (calls kept landing in the other window)"
