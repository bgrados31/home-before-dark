# Builds the "Home Before Dark - Español" resource pack from:
#   1) the mods' own es_es + other Spanish variants (es_mx, es_ar...) for missing keys
#   2) hand translations in translations\manual\<namespace>.json
#   3) compositional translation of block/item names (translations\materials|shapes|words.json + vanilla es_es)
# Output: pack\resourcepacks\HomeBeforeDark-Espanol.zip  (+ translations\report.txt)
# Usage: powershell -ExecutionPolicy Bypass -File scripts\build-translations.ps1 [-Analyze]
param([string]$ModsDir = "$env:APPDATA\ATLauncher\instances\HomeBeforeDark\mods", [switch]$Analyze)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem, System.Web.Extensions
$root = Split-Path -Parent $PSScriptRoot
$tr = Join-Path $root 'translations'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$json = New-Object System.Web.Script.Serialization.JavaScriptSerializer; $json.MaxJsonLength = 100MB
if (-not ('HBDT.Composer' -as [type])) { Add-Type -TypeDefinition ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'translate\Compose.cs'))) -ReferencedAssemblies System.Core -Language CSharp }

function ReadJson($path) { $json.DeserializeObject([IO.File]::ReadAllText($path, $utf8)) }
function ReadEntry($e) { $sr = New-Object IO.StreamReader($e.Open(), $utf8); $t = $sr.ReadToEnd(); $sr.Close(); try { $json.DeserializeObject($t) } catch { $null } }

# ---- vanilla dictionaries
$at = "$env:APPDATA\ATLauncher"
$idx = ReadJson "$at\assets\indexes\17.json"
$h = $idx['objects']['minecraft/lang/es_es.json']['hash']
$vEs = ReadJson "$at\assets\objects\$($h.Substring(0,2))\$h"
$z = [IO.Compression.ZipFile]::OpenRead("$at\libraries\net\minecraft\client\1.21.1\client-1.21.1.jar")
$vEn = ReadEntry ($z.GetEntry('assets/minecraft/lang/en_us.json')); $z.Dispose()

$cp = New-Object HBDT.Composer
foreach ($k in $vEn.Keys) {
    if ($k -match '^(block|item)\.minecraft\.[a-z0-9_]+$' -and $vEs.ContainsKey($k)) {
        $en = $vEn[$k]; if (-not $cp.Vanilla.ContainsKey($en)) { $cp.Vanilla[$en] = $vEs[$k] }
    }
}
foreach ($pair in @(@('materials.json', $cp.Material), @('shapes.json', $cp.Shape), @('words.json', $cp.Word))) {
    $d = ReadJson (Join-Path $tr $pair[0]); foreach ($k in $d.Keys) { $pair[1][$k] = $d[$k] }
}
$cp.Index()

# ---- scan mods
$langs = @{}
Get-ChildItem -LiteralPath $ModsDir -Filter *.jar | ForEach-Object {
    $zz = [IO.Compression.ZipFile]::OpenRead($_.FullName)
    try {
        foreach ($e in $zz.Entries) {
            $m = [regex]::Match($e.FullName, '^assets/([^/]+)/lang/(en_us|es_es|es_mx|es_ar|es_cl|es_uy|es_ve|es_ec)\.json$')
            if (-not $m.Success) { continue }
            $n = $m.Groups[1].Value
            if (-not $langs.ContainsKey($n)) { $langs[$n] = @{} }
            $d = ReadEntry $e; if ($d) { $langs[$n][$m.Groups[2].Value] = $d }
        }
    } finally { $zz.Dispose() }
}

$manualDir = Join-Path $tr 'manual'
# voseo -> tú (whole words, keeps a leading capital)
$voseo = @{}; $vj = ReadJson (Join-Path $tr 'voseo.json'); foreach ($vk in $vj.Keys) { if (-not $vk.StartsWith('_')) { $voseo[$vk] = $vj[$vk] } }
function FixVoseo([string]$s) {
    if (-not $s) { return $s }
    return [regex]::Replace($s, '(?<![\p{L}])[\p{L}]+(?![\p{L}])', {
        param($m) $w = $m.Value; $lw = $w.ToLowerInvariant()
        if (-not $script:voseo.ContainsKey($lw)) { return $w }
        $r = $script:voseo[$lw]; if ([char]::IsUpper($w[0])) { $r = $r.Substring(0, 1).ToUpperInvariant() + $r.Substring(1) }; return $r })
}
$out = @{}; $stats = @(); $leftover = @{}
foreach ($n in ($langs.Keys | Sort-Object)) {
    $L = $langs[$n]; if (-not $L.ContainsKey('en_us')) { continue }
    $en = $L['en_us']; $es = if ($L.ContainsKey('es_es')) { $L['es_es'] } else { @{} }
    $man = @{}; $mp = Join-Path $manualDir "$n.json"; if (Test-Path $mp) { $man = ReadJson $mp }
    $add = New-Object 'System.Collections.Generic.SortedDictionary[string,string]'
    $cAlt = 0; $cMan = 0; $cComp = 0; $cLeft = 0
    # hand-written entries always win: they fill gaps AND replace the mod's own Spanish (e.g. our loading-screen tips)
    foreach ($k in $man.Keys) { $add[$k] = $man[$k]; $cMan++ }
    foreach ($k in $en.Keys) {
        if ($add.ContainsKey($k) -or $es.ContainsKey($k)) { continue }
        $v = $en[$k]; if (-not ($v -is [string]) -or $v.Trim() -eq '') { continue }
        # Spanish variants that use "tú" first; es_ar/es_uy (voseo) last, converted to "tú" (translations\voseo.json)
        $alt = $null; foreach ($a in 'es_mx', 'es_cl', 'es_ve', 'es_ec', 'es_ar', 'es_uy') { if ($L.ContainsKey($a) -and $L[$a].ContainsKey($k)) { $alt = $L[$a][$k]; if ($a -eq 'es_ar' -or $a -eq 'es_uy') { $alt = FixVoseo $alt }; break } }
        if ($alt) { $add[$k] = $alt; $cAlt++; continue }
        if ($k -match '^(block|item)\.') {
            $c = $cp.Translate($v, $true)
            if ($c) { $add[$k] = $c; $cComp++; continue }
        }
        $cLeft++; if (-not $leftover.ContainsKey($n)) { $leftover[$n] = New-Object 'System.Collections.Generic.SortedDictionary[string,string]' }; $leftover[$n][$k] = $v
    }
    if ($add.Count -gt 0) { $out[$n] = $add }
    $stats += [pscustomobject]@{ ns = $n; en = $en.Count; es = $es.Count; alt = $cAlt; manual = $cMan; comp = $cComp; left = $cLeft }
}

$rep = New-Object System.Text.StringBuilder
[void]$rep.AppendLine(("Recuperadas de es_mx/es_ar: {0}  Manuales: {1}  Compuestas: {2}  Sin traducir: {3}" -f ($stats | Measure-Object alt -Sum).Sum, ($stats | Measure-Object manual -Sum).Sum, ($stats | Measure-Object comp -Sum).Sum, ($stats | Measure-Object left -Sum).Sum))
[void]$rep.AppendLine(($stats | Where-Object { $_.alt + $_.manual + $_.comp + $_.left -gt 0 } | Sort-Object left -Descending | Format-Table -AutoSize | Out-String -Width 200))
[IO.File]::WriteAllText((Join-Path $tr 'report.txt'), $rep.ToString(), $utf8)
Write-Host $rep.ToString()

$pend = Join-Path $tr 'pending'; if (Test-Path $pend) { Remove-Item $pend -Recurse -Force }; New-Item -ItemType Directory $pend | Out-Null
foreach ($n in $leftover.Keys) { [IO.File]::WriteAllText((Join-Path $pend "$n.json"), $json.Serialize($leftover[$n]), $utf8) }

if ($Analyze) {
    Write-Host "`nPalabras desconocidas mas frecuentes:"
    $cp.Unknown.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 400 | ForEach-Object { "{0}={1}" -f $_.Key, $_.Value } | Out-String -Width 4000 | Set-Content (Join-Path $tr 'unknown-words.txt') -Encoding UTF8
    Write-Host "($($cp.Unknown.Count) palabras) -> translations\unknown-words.txt"
    return
}

# ---- write resource pack zip
$rpDir = Join-Path $root 'pack\resourcepacks'; New-Item -ItemType Directory -Force $rpDir | Out-Null
$zipPath = Join-Path $rpDir 'HomeBeforeDark-Espanol.zip'; if (Test-Path $zipPath) { Remove-Item $zipPath }
$zip = [IO.Compression.ZipFile]::Open($zipPath, 'Create')
try {
    $e = $zip.CreateEntry('pack.mcmeta'); $w = New-Object IO.StreamWriter($e.Open(), $utf8)
    $w.Write('{"pack":{"pack_format":34,"supported_formats":[34,99],"description":"Home Before Dark - Traducciones al español"}}'); $w.Close()
    foreach ($n in $out.Keys) {
        $e = $zip.CreateEntry("assets/$n/lang/es_es.json"); $w = New-Object IO.StreamWriter($e.Open(), $utf8)
        $w.Write($json.Serialize($out[$n])); $w.Close()
    }
    # extra pack assets (quest book backgrounds...) kept in <repo>\recursos\assets\...
    $res = Join-Path $root 'recursos'
    if (Test-Path $res) { Get-ChildItem $res -Recurse -File | ForEach-Object { [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $_.FullName.Substring($res.Length + 1).Replace('\', '/')) } }
} finally { $zip.Dispose() }
Write-Host "Creado $zipPath ($([math]::Round((Get-Item $zipPath).Length/1KB)) KB, $($out.Count) namespaces)"
