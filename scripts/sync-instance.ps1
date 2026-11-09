# Syncs an ATLauncher instance with the newest dist\*.mrpack (client side):
#  - downloads missing/changed files (sha1 verified)
#  - moves jars in mods\ that are no longer in the pack to <instance>\_quitados_por_sync\
#  - copies overrides\ (configs, layouts, assets)
# Usage: powershell -ExecutionPolicy Bypass -File scripts\sync-instance.ps1 [-Instance <path>]
param([string]$Instance = "$env:APPDATA\ATLauncher\instances\CrnicasdeEscarcha")
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path $Instance)) { throw "No existe la instancia: $Instance" }
$pack = Get-ChildItem (Join-Path $root 'dist') -Filter *.mrpack | Sort-Object LastWriteTime | Select-Object -Last 1
Write-Host "Pack: $($pack.Name) -> $Instance"

$zip = [IO.Compression.ZipFile]::OpenRead($pack.FullName)
try {
    $sr = New-Object IO.StreamReader($zip.GetEntry('modrinth.index.json').Open()); $index = $sr.ReadToEnd() | ConvertFrom-Json; $sr.Close()
    $sha1 = [Security.Cryptography.SHA1]::Create()
    function Get-Sha1($p) { -join ($sha1.ComputeHash([IO.File]::ReadAllBytes($p)) | ForEach-Object { $_.ToString('x2') }) }

    $wanted = @{}
    $dl = 0
    foreach ($f in $index.files) {
        if ($f.env.client -eq 'unsupported') { continue }
        $target = Join-Path $Instance ($f.path -replace '/', '\')
        $wanted[$target.ToLower()] = $true
        if ((Test-Path -LiteralPath $target) -and ((Get-Sha1 $target) -eq $f.hashes.sha1)) { continue }
        [void][IO.Directory]::CreateDirectory((Split-Path $target))
        $wc = New-Object Net.WebClient; $wc.Headers.Add('User-Agent', 'modpack-sync/1.0'); $wc.DownloadFile($f.downloads[0], $target); $wc.Dispose()
        if ((Get-Sha1 $target) -ne $f.hashes.sha1) { throw "Hash incorrecto: $($f.path)" }
        Write-Host "  descargado $($f.path)"; $dl++
    }

    foreach ($e in $zip.Entries) { if ($e.FullName.StartsWith('overrides/mods/')) { $wanted[(Join-Path $Instance ($e.FullName.Substring(10) -replace '/', '\')).ToLower()] = $true } }
    $moved = 0
    $bak = Join-Path $Instance '_quitados_por_sync'
    Get-ChildItem (Join-Path $Instance 'mods') -Filter *.jar | ForEach-Object {
        if (-not $wanted.ContainsKey($_.FullName.ToLower())) {
            New-Item -ItemType Directory -Force $bak | Out-Null
            Move-Item -LiteralPath $_.FullName -Destination (Join-Path $bak $_.Name) -Force
            Write-Host "  apartado  mods/$($_.Name)"; $moved++
        }
    }

    # layouts are fully owned by the pack: remove stale ones so old layouts don't stack with new ones
    $layoutDir = Join-Path $Instance 'config\fancymenu\customization'
    $packLayouts = @{}
    foreach ($e in $zip.Entries) { if ($e.FullName -like 'overrides/config/fancymenu/customization/*.txt') { $packLayouts[$e.Name.ToLower()] = $true } }
    if ((Test-Path -LiteralPath $layoutDir) -and $packLayouts.Count -gt 0) {
        Get-ChildItem -LiteralPath $layoutDir -Filter *.txt | Where-Object { -not $packLayouts.ContainsKey($_.Name.ToLower()) } | ForEach-Object {
            Remove-Item -LiteralPath $_.FullName; Write-Host "  layout viejo eliminado: $($_.Name)"
        }
    }

    $ov = 0
    foreach ($e in $zip.Entries) {
        if (-not $e.FullName.StartsWith('overrides/') -or $e.FullName.EndsWith('/')) { continue }
        $target = Join-Path $Instance ($e.FullName.Substring(10) -replace '/', '\')
        [void][IO.Directory]::CreateDirectory((Split-Path $target))
        [IO.Compression.ZipFileExtensions]::ExtractToFile($e, $target, $true); $ov++
    }
} finally { $zip.Dispose() }

# make sure every resource pack enabled in the pack's default options is also enabled in an already-existing
# options.txt (defaultoptions only applies on a first launch). Missing ones go just below the Spanish pack,
# which always stays on top; the player's own order and extra packs are kept.
$opt = Join-Path $Instance 'options.txt'
$defOpt = Join-Path $Instance 'config\defaultoptions\options.txt'
if ((Test-Path -LiteralPath $opt) -and (Test-Path -LiteralPath $defOpt)) {
    $u8 = New-Object System.Text.UTF8Encoding($false)
    $want = @([regex]::Matches(([IO.File]::ReadAllText($defOpt, $u8) | Select-String -Pattern '(?m)^resourcePacks:.*$').Matches[0].Value, '"([^"]+)"') | ForEach-Object { $_.Groups[1].Value })
    $lines = [IO.File]::ReadAllLines($opt, $u8)
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -notlike 'resourcePacks:*') { continue }
        $have = New-Object System.Collections.Generic.List[string]
        foreach ($m in [regex]::Matches($lines[$i], '"([^"]+)"')) { $have.Add($m.Groups[1].Value) }
        $missing = @($want | Where-Object { $_ -like 'file/*' -and -not $have.Contains($_) })
        if ($missing.Count -eq 0) { continue }
        # new packs arrived: use the pack's order, keeping the player's own extra packs just below the Spanish pack
        $es = 'file/HomeBeforeDark-Espanol.zip'
        $extra = @($have | Where-Object { $want -notcontains $_ })
        $final = @($want | Where-Object { $_ -ne $es }) + $extra + @($es)
        $missing | ForEach-Object { Write-Host "  activado: $_" }
        $lines[$i] = 'resourcePacks:[' + (($final | ForEach-Object { '"' + $_ + '"' }) -join ',') + ']'
    }
    [IO.File]::WriteAllLines($opt, $lines, $u8)
}
Write-Host "Listo: $dl descargados, $moved apartados, $ov archivos de configuracion copiados."
