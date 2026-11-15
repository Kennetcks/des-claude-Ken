# Call a tool on the Kennet.DeswikClaude plugin running inside Deswik, through its local named pipe (one JSON line each way,
# one request per connection). Changes still raise the plugin's own approval dialog in Deswik (unless auto-approve is on).
# Several Deswik windows can serve the same pipe; with -docMatch, run_csharp code gets a guard that stops it unless the open
# document's file name matches, and the call is resent until it lands in that window.
param([Parameter(Mandatory)][string]$name, [string]$inputJson = '{}', [string]$inputFile, [string]$out, [int]$timeoutMs = 600000,
  [string]$docMatch, [int]$tries = 30)
$ErrorActionPreference = 'Stop'
if ($inputFile) { $inputJson = [IO.File]::ReadAllText($inputFile) }
if ($docMatch) {
  if ($name -ne 'run_csharp') { throw '-docMatch only works with run_csharp' }
  $o = $inputJson | ConvertFrom-Json
  $guard = 'if (!(App.Filename ?? "").Contains(' + (ConvertTo-Json $docMatch) + ')) { Out.AppendLine("WRONG_DOC " + App.Filename); return null; }' + "`n"
  $o.code = $guard + $o.code
  $inputJson = $o | ConvertTo-Json -Compress -Depth 5
}
$msg = '{"op":"call","name":' + (ConvertTo-Json $name) + ',"input":' + $inputJson + '}'
$msg = $msg -replace "`r?`n", ' '
for ($i = 1; $i -le [Math]::Max(1, $tries); $i++) {
  $c = New-Object IO.Pipes.NamedPipeClientStream('.', 'KennetDeswikClaude', [IO.Pipes.PipeDirection]::InOut); $c.Connect(5000)
  $w = New-Object IO.StreamWriter($c); $w.AutoFlush = $true; $r = New-Object IO.StreamReader($c)
  $w.WriteLine($msg); $task = $r.ReadLineAsync(); if (-not $task.Wait($timeoutMs)) { $c.Dispose(); throw 'timed out waiting for Deswik' }
  $resp = $task.Result; $c.Dispose()
  $o = $resp | ConvertFrom-Json
  $txt = if ($o.content) { ($o.content | ForEach-Object { $_.text }) -join "`n" } elseif ($null -ne $o.text) { $o.text } else { $resp }
  if (-not $docMatch -or $txt -notmatch '^WRONG_DOC') { break }
  if ($i -eq $tries) { throw "never reached a Deswik window with '$docMatch' open" }
}
if ($o.isError) { $txt = "ERROR: $txt" }
if ($out) { $txt | Set-Content $out -Encoding UTF8; "saved $($txt.Length) chars to $out" } else { $txt }
