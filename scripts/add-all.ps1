# Adds every entry of mods.txt to the packwiz pack in .\pack (deps resolved by packwiz).
# Usage: powershell -ExecutionPolicy Bypass -File scripts\add-all.ps1
$root = Split-Path -Parent $PSScriptRoot
$pw = Join-Path (Split-Path -Parent $root) 'tools\packwiz.exe'
$utf8 = New-Object System.Text.UTF8Encoding($false)
Push-Location (Join-Path $root 'pack')
$failed = @()
foreach ($line in [IO.File]::ReadAllLines((Join-Path $root 'mods.txt'), $utf8)) {
    $l = ($line -replace '#.*$', '').Trim()
    if ($l -eq '') { continue }
    $id = ($l -split '\s+')[0]
    if ($id.StartsWith('cf:')) { $out = & $pw -y curseforge add --addon-id $id.Substring(3) 2>&1 }
    else { $out = & $pw -y modrinth add $id 2>&1 }
    if ($LASTEXITCODE -ne 0) { $failed += $id; Write-Host "FAIL $id`n$($out -join "`n")" } else { Write-Host "ok   $id" }
}
Pop-Location
Write-Host "`nFallidos: $($failed.Count) $($failed -join ', ')"
