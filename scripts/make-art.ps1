# Builds all menu art + audio for Home Before Dark into pack\config\fancymenu\assets\
# Usage: powershell -ExecutionPolicy Bypass -File scripts\make-art.ps1 [-SkipAudio] [-SkipArt]
param([switch]$SkipAudio, [switch]$SkipArt, [int]$Frames = 72, [int]$Fps = 12)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'pack\config\fancymenu\assets'
$src = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'art\Gen.cs'))
if (-not ('HBD.Program' -as [type])) {
    Add-Type -TypeDefinition $src -ReferencedAssemblies System.Drawing -Language CSharp
}
$sw = [Diagnostics.Stopwatch]::StartNew()
if (-not $SkipArt) { Write-Host ([HBD.Program]::Run($out, $Frames, $Fps)) }
if (-not $SkipAudio) { Write-Host ([HBD.Program]::RunAudio($out)) }
# remove intermediate outputs the layouts do not use (unused)
foreach ($x in "escenas\neutral.apng","sonidos\ambiente_invierno.wav","sonidos\ambiente_primavera.wav","sonidos\ambiente_verano.wav","sonidos\ambiente_otono.wav") { $p = Join-Path $out $x; if (Test-Path $p) { Remove-Item $p } }
Get-ChildItem (Join-Path $out "escenas") -Filter "*preview*" | Remove-Item
Write-Host "Tiempo: $([math]::Round($sw.Elapsed.TotalSeconds))s"
