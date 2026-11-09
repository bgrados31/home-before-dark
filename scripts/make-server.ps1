# Builds / updates the dedicated server of Home Before Dark from the newest dist\*.mrpack.
#  - installs NeoForge server (once) with the ATLauncher Java 21
#  - downloads every file whose env.server is not "unsupported" (client-only mods are skipped)
#  - removes client-only extras listed in $ClientOnly (mods marked "both" that only work on the client)
#  - copies overrides\ except client things (menus, resource packs, shaders, default client options)
#  - writes server.properties / user_jvm_args.txt / iniciar.bat only when missing (keeps the owner's edits)
# Usage: powershell -ExecutionPolicy Bypass -File scripts\make-server.ps1 [-Out <folder>] [-Zip]
param([string]$Out = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'servidor'), [switch]$Zip)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$root = Split-Path -Parent $PSScriptRoot
$u8 = New-Object System.Text.UTF8Encoding($false)
$toml = [IO.File]::ReadAllText((Join-Path $root 'pack\pack.toml'), $u8)
$ver = [regex]::Match($toml, '(?m)^version = "([^"]+)"').Groups[1].Value
$nf = [regex]::Match($toml, '(?m)^neoforge = "([^"]+)"').Groups[1].Value
$mr = Join-Path $root "dist\Home-Before-Dark-$ver.mrpack"
if (-not (Test-Path $mr)) { throw "Falta $mr (corre export.ps1 primero)" }
$java = "$env:APPDATA\ATLauncher\jre\bin\java.exe"
if (-not (Test-Path $java)) { $java = 'java' }
New-Item -ItemType Directory -Force $Out | Out-Null
Write-Host "Servidor Home Before Dark $ver (NeoForge $nf) -> $Out"

# mods that declare both sides but are client-only in practice (they crash or do nothing on a dedicated server)
$ClientOnly = @()

# ---- 1. NeoForge server
$nfDir = Join-Path $Out "libraries\net\neoforged\neoforge\$nf"
if (-not (Test-Path (Join-Path $nfDir 'win_args.txt'))) {
    $inst = Join-Path $Out "neoforge-$nf-installer.jar"
    $wc = New-Object Net.WebClient; $wc.Headers.Add('User-Agent', 'home-before-dark-server/1.0')
    $wc.DownloadFile("https://maven.neoforged.net/releases/net/neoforged/neoforge/$nf/neoforge-$nf-installer.jar", $inst); $wc.Dispose()
    Push-Location $Out
    & $java -jar $inst --installServer $Out | Out-Null
    Pop-Location
    if (-not (Test-Path (Join-Path $nfDir 'win_args.txt'))) { throw "No se pudo instalar NeoForge $nf" }
    Remove-Item $inst, (Join-Path $Out "neoforge-$nf-installer.jar.log") -ErrorAction SilentlyContinue
    Write-Host "  NeoForge $nf instalado"
}

# ---- 2. mods and other files from the index
$arch = [IO.Compression.ZipFile]::OpenRead($mr)
try {
    $sr = New-Object IO.StreamReader($arch.GetEntry('modrinth.index.json').Open()); $index = $sr.ReadToEnd() | ConvertFrom-Json; $sr.Close()
    $sha1 = [Security.Cryptography.SHA1]::Create()
    function Get-Sha1($p) { -join ($sha1.ComputeHash([IO.File]::ReadAllBytes($p)) | ForEach-Object { $_.ToString('x2') }) }
    $wanted = @{}; $dl = 0; $skipped = 0
    foreach ($f in $index.files) {
        if ($f.env.server -eq 'unsupported' -or -not $f.path.StartsWith('mods/')) { $skipped++; continue }
        $name = Split-Path $f.path -Leaf
        if ($ClientOnly | Where-Object { $name -like $_ }) { $skipped++; continue }
        $target = Join-Path $Out ($f.path -replace '/', '\')
        $wanted[$target.ToLower()] = $true
        if ((Test-Path -LiteralPath $target) -and ((Get-Sha1 $target) -eq $f.hashes.sha1)) { continue }
        [void][IO.Directory]::CreateDirectory((Split-Path $target))
        $wc = New-Object Net.WebClient; $wc.Headers.Add('User-Agent', 'home-before-dark-server/1.0'); $wc.DownloadFile($f.downloads[0], $target); $wc.Dispose()
        if ((Get-Sha1 $target) -ne $f.hashes.sha1) { throw "Hash incorrecto: $($f.path)" }
        $dl++
    }
    $moved = 0; $bak = Join-Path $Out '_quitados'
    if (Test-Path (Join-Path $Out 'mods')) {
        Get-ChildItem (Join-Path $Out 'mods') -Filter *.jar | Where-Object { -not $wanted.ContainsKey($_.FullName.ToLower()) } | ForEach-Object {
            New-Item -ItemType Directory -Force $bak | Out-Null; Move-Item -LiteralPath $_.FullName -Destination (Join-Path $bak $_.Name) -Force; $moved++
        }
    }

    # ---- 3. overrides (configs, quests) without the client-only parts
    $skipDirs = 'resourcepacks/', 'shaderpacks/', 'config/fancymenu/', 'config/drippyloadingscreen/', 'config/defaultoptions/', 'config/iris.properties', 'config/emi.css'
    $ov = 0
    foreach ($e in $arch.Entries) {
        $n = $e.FullName
        if ($n.EndsWith('/')) { continue }
        if ($n.StartsWith('overrides/')) { $rel = $n.Substring(10) } elseif ($n.StartsWith('server-overrides/')) { $rel = $n.Substring(17) } else { continue }
        if ($skipDirs | Where-Object { $rel.StartsWith($_) }) { continue }
        $target = Join-Path $Out ($rel -replace '/', '\')
        [void][IO.Directory]::CreateDirectory((Split-Path $target))
        [IO.Compression.ZipFileExtensions]::ExtractToFile($e, $target, $true); $ov++
    }
} finally { $arch.Dispose() }

# ---- 4. server files (only when missing, so the owner's changes survive updates)
function Write-IfMissing($name, $text) { $p = Join-Path $Out $name; if (-not (Test-Path $p)) { [IO.File]::WriteAllText($p, $text.Replace("`r`n", "`n"), $u8); Write-Host "  creado $name" } }
Write-IfMissing 'server.properties' @"
motd=§6Home Before Dark §7- servidor de amigos
max-players=12
difficulty=normal
view-distance=8
simulation-distance=6
spawn-protection=0
allow-flight=true
enable-command-block=true
online-mode=true
pvp=true
white-list=false
max-tick-time=-1
sync-chunk-writes=false
network-compression-threshold=256
entity-broadcast-range-percentage=100
"@
# 8 GB with G1GC tuned for big modpacks (Aikar-style flags). The NeoForge installer leaves a comments-only file: replace that one.
$jvm = Join-Path $Out 'user_jvm_args.txt'
if ((Test-Path $jvm) -and -not (Get-Content $jvm | Where-Object { $_.Trim() -and -not $_.TrimStart().StartsWith('#') })) { Remove-Item $jvm }
Write-IfMissing 'user_jvm_args.txt' "-Xms4G`n-Xmx8G`n-XX:+UseG1GC`n-XX:+ParallelRefProcEnabled`n-XX:MaxGCPauseMillis=200`n-XX:+UnlockExperimentalVMOptions`n-XX:+DisableExplicitGC`n-XX:+AlwaysPreTouch`n-XX:G1NewSizePercent=30`n-XX:G1MaxNewSizePercent=40`n-XX:G1HeapRegionSize=8M`n-XX:G1ReservePercent=20`n-XX:G1HeapWastePercent=5`n-XX:G1MixedGCCountTarget=4`n-XX:InitiatingHeapOccupancyPercent=15`n-XX:G1MixedGCLiveThresholdPercent=90`n-XX:G1RSetUpdatingPauseTimePercent=5`n-XX:SurvivorRatio=32`n-XX:+PerfDisableSharedMem`n-XX:MaxTenuringThreshold=1`n"
$bat = @"
@echo off
title Home Before Dark - Servidor
cd /d "%~dp0"
set JAVA=java
if exist "%APPDATA%\ATLauncher\jre\bin\java.exe" set JAVA="%APPDATA%\ATLauncher\jre\bin\java.exe"
findstr /c:"eula=true" eula.txt >nul 2>&1
if errorlevel 1 (
  echo Para abrir un servidor de Minecraft hay que aceptar el EULA de Mojang:
  echo https://aka.ms/MinecraftEULA
  choice /c SN /m "Aceptas el EULA"
  if errorlevel 2 exit /b
  echo eula=true> eula.txt
)
:inicio
%JAVA% @user_jvm_args.txt @libraries/net/neoforged/neoforge/$nf/win_args.txt nogui
echo.
echo El servidor se cerro. Se reinicia en 10 segundos (cierra esta ventana para no reiniciar).
timeout /t 10
goto inicio
"@
$batPath = Join-Path $Out 'iniciar.bat'
[IO.File]::WriteAllText($batPath, $bat.Replace("`r`n", "`n").Replace("`n", "`r`n"), [Text.Encoding]::ASCII)
Write-Host "Listo: $dl descargados, $moved apartados, $ov configs copiadas, $skipped archivos solo-cliente omitidos."

if ($Zip) {
    $z = Join-Path $root "dist\Home-Before-Dark-Servidor-$ver.zip"
    if (Test-Path $z) { Remove-Item $z }
    $tmp = Join-Path $env:TEMP "hbd-server-zip"; if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
    New-Item -ItemType Directory $tmp | Out-Null
    foreach ($d in 'mods', 'config', 'libraries', 'defaultconfigs') { if (Test-Path (Join-Path $Out $d)) { Copy-Item (Join-Path $Out $d) (Join-Path $tmp $d) -Recurse } }
    foreach ($f in 'server.properties', 'user_jvm_args.txt', 'iniciar.bat', 'run.bat', 'run.sh') { if (Test-Path (Join-Path $Out $f)) { Copy-Item (Join-Path $Out $f) $tmp } }
    [IO.Compression.ZipFile]::CreateFromDirectory($tmp, $z); Remove-Item $tmp -Recurse -Force
    Write-Host "Zip: $z ($([math]::Round((Get-Item $z).Length / 1MB)) MB)"
}
