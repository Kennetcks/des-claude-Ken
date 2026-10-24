# Add red "UPDATE 22/10:" lines under matching items on the open OneNote handover page (22/10/2026). Existing text is not changed.
# A backup of the page XML is written to %TEMP%\dsw before the update.
$ErrorActionPreference = 'Stop'
$on = New-Object -ComObject OneNote.Application
$out = ''; $on.GetHierarchy('', 4, [ref]$out); [xml]$h = $out; $nsh = New-Object Xml.XmlNamespaceManager($h.NameTable); $nsh.AddNamespace('one', $h.DocumentElement.NamespaceURI)
$pg = $h.SelectNodes('//one:Page', $nsh) | Where-Object { $_.name -eq '22/10/2026' } | Sort-Object lastModifiedTime -Descending | Select-Object -First 1
if (-not $pg) { throw 'page 22/10/2026 not found' }
$xmlText = ''; $on.GetPageContent($pg.ID, [ref]$xmlText, 0)
[IO.File]::WriteAllText("$env:TEMP\dsw\onenote_handover_22-10_backup.xml", $xmlText, [Text.Encoding]::UTF8)
[xml]$p = $xmlText; $ns = New-Object Xml.XmlNamespaceManager($p.NameTable); $one = $p.DocumentElement.NamespaceURI; $ns.AddNamespace('one', $one)
$updates = [ordered]@{   # text found on the page -> update line added under it in red. Placeholders below.
  '<text of an existing handover item>' = '<what was done, with dates>'
  '<another item>' = '<update>'
}
$extraPO = @('<extra procurement line>', '<extra procurement line>')
function NewOE([string]$text) { $oe = $p.CreateElement('one', 'OE', $one); $t = $p.CreateElement('one', 'T', $one)
  [void]$t.AppendChild($p.CreateCDataSection("<span style='color:#FF0000'>UPDATE 22/10: " + [Security.SecurityElement]::Escape($text) + "</span>")); [void]$oe.AppendChild($t); $oe }
$log = New-Object System.Collections.Generic.List[string]
foreach ($k in $updates.Keys) {
  $t = $p.SelectNodes('//one:T', $ns) | Where-Object { ($_.InnerText -replace '<[^>]+>', '') -like "*$k*" } | Select-Object -First 1
  if (-not $t) { $log.Add("NOT FOUND: '$k' - skipped"); continue }
  $oe = $t.ParentNode; if (($oe.NextSibling.InnerText) -like '*UPDATE 22/10*') { $log.Add("already updated: '$k'"); continue }
  [void]$oe.ParentNode.InsertAfter((NewOE $updates[$k]), $oe); $log.Add("added under: '$k'") }
$last = $p.SelectNodes('//one:T', $ns) | Where-Object { ($_.InnerText -replace '<[^>]+>', '') -like '*<last item on the page>*' } | Select-Object -First 1
if ($last) { $anchor = $last.ParentNode; while ($anchor.NextSibling -and $anchor.NextSibling.InnerText -like '*UPDATE 22/10*') { $anchor = $anchor.NextSibling }
  foreach ($x in $extraPO) { if ($p.InnerText -notlike "*$($x.Substring(0, 30))*") { $n = NewOE $x; [void]$anchor.ParentNode.InsertAfter($n, $anchor); $anchor = $n; $log.Add("added PO line: $($x.Substring(0, 40))...") } } }
$on.UpdatePageContent($p.OuterXml)
$log; "backup: $env:TEMP\dsw\onenote_handover_22-10_backup.xml"
