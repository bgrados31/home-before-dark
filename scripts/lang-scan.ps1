# Scans every jar in a mods folder and reports Spanish (es_es) translation coverage per namespace.
# Writes lang-report.csv next to the modpack root and, with -Extract, dumps the missing en_us strings to
# translations\pending\<namespace>.json (only keys that es_es lacks).
# Usage: powershell -ExecutionPolicy Bypass -File scripts\lang-scan.ps1 -ModsDir <instance>\mods [-Extract]
param([Parameter(Mandatory)][string]$ModsDir, [switch]$Extract)
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Web.Extensions
$root = Split-Path -Parent $PSScriptRoot
$json = New-Object System.Web.Script.Serialization.JavaScriptSerializer
$json.MaxJsonLength = 100MB
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Read-Lang($entry) {
    $sr = New-Object IO.StreamReader($entry.Open(), $utf8); $t = $sr.ReadToEnd(); $sr.Close()
    try { return $json.DeserializeObject($t) } catch { return $null }
}

$ns = @{}   # namespace -> @{ jar; en; es; mx; other }
Get-ChildItem -LiteralPath $ModsDir -Filter *.jar | ForEach-Object {
    $jar = $_.Name
    $z = [IO.Compression.ZipFile]::OpenRead($_.FullName)
    try {
        foreach ($e in $z.Entries) {
            $m = [regex]::Match($e.FullName, '^assets/([^/]+)/lang/([a-z_]+)\.json$')
            if (-not $m.Success) { continue }
            $n = $m.Groups[1].Value; $lang = $m.Groups[2].Value
            if ($lang -notin @('en_us', 'es_es', 'es_mx', 'es_ar', 'es_cl', 'es_uy', 'es_ve', 'es_ec')) { continue }
            if (-not $ns.ContainsKey($n)) { $ns[$n] = @{ jar = $jar; langs = @{} } }
            $d = Read-Lang $e
            if ($d) { $ns[$n].langs[$lang] = $d }
        }
    } finally { $z.Dispose() }
}

$rows = @()
$pendingDir = Join-Path $root 'translations\pending'
if ($Extract) { New-Item -ItemType Directory -Force $pendingDir | Out-Null }
foreach ($n in ($ns.Keys | Sort-Object)) {
    $L = $ns[$n].langs
    if (-not $L.ContainsKey('en_us')) { continue }
    $en = $L['en_us']
    $es = if ($L.ContainsKey('es_es')) { $L['es_es'] } else { @{} }
    $alt = $null
    foreach ($a in 'es_mx', 'es_ar', 'es_cl', 'es_uy', 'es_ve', 'es_ec') { if ($L.ContainsKey($a)) { $alt = $a; break } }
    $missing = [ordered]@{}
    $fromAlt = 0
    foreach ($k in $en.Keys) {
        if ($es.ContainsKey($k)) { continue }
        if ($alt -and $L[$alt].ContainsKey($k)) { $fromAlt++; continue }
        $v = $en[$k]
        if ($v -is [string] -and $v.Trim() -ne '') { $missing[$k] = $v }
    }
    $rows += [pscustomobject]@{ namespace = $n; jar = $ns[$n].jar; en = $en.Count; es_es = $es.Count; alt = $alt; from_alt = $fromAlt; missing = $missing.Count }
    if ($Extract -and $missing.Count -gt 0) {
        [IO.File]::WriteAllText((Join-Path $pendingDir "$n.json"), $json.Serialize($missing), $utf8)
    }
}
$rows | Export-Csv (Join-Path $root 'lang-report.csv') -NoTypeInformation -Encoding UTF8
$tot = ($rows | Measure-Object missing -Sum).Sum
Write-Host ("Namespaces: {0}  con es_es completo: {1}  claves faltantes: {2}  recuperables de otra variante es_*: {3}" -f $rows.Count, ($rows | Where-Object { $_.missing -eq 0 -and $_.from_alt -eq 0 }).Count, $tot, ($rows | Measure-Object from_alt -Sum).Sum)
$rows | Where-Object { $_.missing -gt 0 } | Sort-Object missing -Descending | Select-Object namespace, en, es_es, alt, from_alt, missing | Format-Table -AutoSize | Out-String -Width 200 | Write-Host
