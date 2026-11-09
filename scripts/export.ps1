# Exports .\pack to dist\<name>-<version>.mrpack with packwiz, then replaces the
# CurseForge jars that packwiz bundles into overrides/ with download links to the
# official CurseForge CDN, so no third-party jar is redistributed inside the file.
# Usage: powershell -ExecutionPolicy Bypass -File scripts\export.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$root = Split-Path -Parent $PSScriptRoot
$packDir = Join-Path $root 'pack'
$pw = Join-Path (Split-Path -Parent $root) 'tools\packwiz.exe'
$utf8 = New-Object System.Text.UTF8Encoding($false)

$toml = [IO.File]::ReadAllText((Join-Path $packDir 'pack.toml'), $utf8)
$version = [regex]::Match($toml, '(?m)^version = "([^"]+)"').Groups[1].Value
$dist = Join-Path $root 'dist'
if (-not (Test-Path $dist)) { New-Item -ItemType Directory $dist | Out-Null }
$out = Join-Path $dist "Home-Before-Dark-$version.mrpack"
if (Test-Path $out) { Remove-Item $out }

Push-Location $packDir
& $pw refresh | Out-Null
& $pw modrinth export --restrictDomains=false -o $out | Out-Null
if ($LASTEXITCODE -ne 0) { Pop-Location; throw 'packwiz export failed' }
Pop-Location

# filename -> CurseForge file id, from the .pw.toml metadata
$cf = @{}
Get-ChildItem $packDir -Recurse -Filter *.pw.toml | ForEach-Object {
    $t = [IO.File]::ReadAllText($_.FullName, $utf8)
    if ($t -match 'mode = "metadata:curseforge"') {
        $fn = [regex]::Match($t, '(?m)^filename = "([^"]+)"').Groups[1].Value
        $fid = [regex]::Match($t, '(?m)^file-id = (\d+)').Groups[1].Value
        $side = [regex]::Match($t, '(?m)^side = "([^"]+)"').Groups[1].Value
        $dir = $_.Directory.Name
        $cf["overrides/$dir/$fn"] = @{ id = [int64]$fid; name = $fn; dir = $dir; side = $side }
    }
}

$zip = [IO.Compression.ZipFile]::Open($out, 'Update')
try {
    $idxEntry = $zip.GetEntry('modrinth.index.json')
    $sr = New-Object IO.StreamReader($idxEntry.Open(), $utf8); $index = $sr.ReadToEnd() | ConvertFrom-Json; $sr.Close()
    $files = New-Object System.Collections.ArrayList
    foreach ($f in $index.files) { [void]$files.Add($f) }

    foreach ($e in @($zip.Entries)) {
        if (-not $cf.ContainsKey($e.FullName)) { continue }
        $info = $cf[$e.FullName]
        $ms = New-Object IO.MemoryStream; $s = $e.Open(); $s.CopyTo($ms); $s.Close()
        $bytes = $ms.ToArray()
        $sha1 = -join ([Security.Cryptography.SHA1]::Create().ComputeHash($bytes) | ForEach-Object { $_.ToString('x2') })
        $sha512 = -join ([Security.Cryptography.SHA512]::Create().ComputeHash($bytes) | ForEach-Object { $_.ToString('x2') })
        $url = "https://mediafilez.forgecdn.net/files/$([math]::Floor($info.id / 1000))/$($info.id % 1000)/$([Uri]::EscapeDataString($info.name))"
        $env = [ordered]@{ client = 'required'; server = 'required' }
        if ($info.side -eq 'client') { $env.server = 'unsupported' }
        if ($info.side -eq 'server') { $env.client = 'unsupported' }
        [void]$files.Add([ordered]@{
            path = "$($info.dir)/$($info.name)"
            hashes = [ordered]@{ sha1 = $sha1; sha512 = $sha512 }
            env = $env
            downloads = @($url)
            fileSize = $bytes.Length
        })
        $e.Delete()
        Write-Host "CDN  $($info.name)"
    }

    $index.files = $files.ToArray()
    $idxEntry.Delete()
    $ne = $zip.CreateEntry('modrinth.index.json')
    $sw = New-Object IO.StreamWriter($ne.Open(), $utf8)
    $sw.Write((ConvertTo-Json -InputObject $index -Depth 10)); $sw.Close()
} finally { $zip.Dispose() }

Write-Host "Creado: $out ($($files.Count) archivos, $([math]::Round((Get-Item $out).Length / 1KB)) KB)"
