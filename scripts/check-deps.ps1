# Reads META-INF/neoforge.mods.toml of every jar in a mods folder and lists required
# dependencies that no installed jar provides (including nested jar-in-jar mods).
# Usage: powershell -ExecutionPolicy Bypass -File scripts\check-deps.ps1 -ModsDir <instance>\mods
param([Parameter(Mandatory)][string]$ModsDir)
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

$provided = @{ minecraft = 1; neoforge = 1; java = 1; forge = 1 }
$providedVer = @{}
$deps = @()
$incompat = @()

function Read-Toml($stream) { $sr = New-Object IO.StreamReader($stream); $t = $sr.ReadToEnd(); $sr.Close(); $t }

# Compara versiones por segmentos (0.8.13 <= 0.8.13+mc1.21.1, 1.3.4 > 1.3.0, 21.1.36 > 21.1.21)
function Compare-Version($a, $b) {
    $pa = @($a -split '[.\-+]'); $pb = @($b -split '[.\-+]')
    $n = [Math]::Max($pa.Count, $pb.Count)
    for ($i = 0; $i -lt $n; $i++) {
        $x = if ($i -lt $pa.Count) { $pa[$i] } else { '0' }
        $y = if ($i -lt $pb.Count) { $pb[$i] } else { '0' }
        $nx = 0; $ny = 0
        if ([int]::TryParse($x, [ref]$nx) -and [int]::TryParse($y, [ref]$ny)) {
            if ($nx -ne $ny) { return [Math]::Sign($nx - $ny) }
        } else {
            $c = [string]::Compare($x, $y, $true)
            if ($c -ne 0) { return [Math]::Sign($c) }
        }
    }
    return 0
}

# Rangos Maven de mods.toml: "*", "[1.0,)", "(,2.0]", "[1.0,2.0)", "[1.0]"
function Test-Range($ver, $range) {
    if (-not $range -or $range -eq '*' -or -not $ver) { return $true }
    $r = $range.Trim()
    if ($r -match '^([\[\(])\s*([^,\]\)]*)\s*,\s*([^\]\)]*)\s*([\]\)])$') {
        $lo = $matches[2]; $hi = $matches[3]
        if ($lo -ne '') { $c = Compare-Version $ver $lo; if ($c -lt 0 -or ($c -eq 0 -and $matches[1] -eq '(')) { return $false } }
        if ($hi -ne '') { $c = Compare-Version $ver $hi; if ($c -gt 0 -or ($c -eq 0 -and $matches[4] -eq ')')) { return $false } }
        return $true
    }
    if ($r -match '^[\[\(]([^\]\)]+)[\]\)]$') { return ((Compare-Version $ver $matches[1]) -eq 0) }
    return $true
}

function Scan-Zip($zip, $label) {
    foreach ($e in $zip.Entries) {
        if ($e.FullName -eq 'META-INF/neoforge.mods.toml' -or $e.FullName -eq 'META-INF/mods.toml') {
            $t = Read-Toml $e.Open()
            foreach ($mb in [regex]::Matches($t, '(?s)\[\[mods\]\](.*?)(?=\[\[|\z)')) {
                $m = [regex]::Match($mb.Groups[1].Value, 'modId\s*=\s*"([^"]+)"')
                if ($m.Success) { $script:provided[$m.Groups[1].Value] = 1 }
                $mv = [regex]::Match($mb.Groups[1].Value, '(?m)^\s*version\s*=\s*"([^"]+)"')
                if ($m.Success -and $mv.Success -and $mv.Groups[1].Value -notmatch '\$\{') {
                    $script:providedVer[$m.Groups[1].Value] = $mv.Groups[1].Value
                }
            }
            # dependency blocks
            foreach ($blk in [regex]::Matches($t, '(?s)\[\[dependencies\.[^\]]+\]\](.*?)(?=\[\[|\z)')) {
                $b = $blk.Groups[1].Value
                $id = [regex]::Match($b, 'modId\s*=\s*"([^"]+)"').Groups[1].Value
                $type = [regex]::Match($b, '(?m)^\s*type\s*=\s*"([^"]+)"').Groups[1].Value
                $mand = [regex]::Match($b, '(?m)^\s*mandatory\s*=\s*(\w+)').Groups[1].Value
                $side = [regex]::Match($b, '(?m)^\s*side\s*=\s*"([^"]+)"').Groups[1].Value
                $range = [regex]::Match($b, 'versionRange\s*=\s*"([^"]+)"').Groups[1].Value
                $req = ($type -eq 'required') -or ($type -eq '' -and $mand -eq 'true')
                if ($req -and $id) { $script:deps += [pscustomobject]@{ from = $label; id = $id; range = $range; side = $side } }
                if ($type -eq 'incompatible' -and $id) { $script:incompat += [pscustomobject]@{ from = $label; id = $id; range = $range } }
            }
        }
        # jar-in-jar: NeoForge usa META-INF/jarjar, pero algunos mods guardan el jar en META-INF/jars
        # (la ruta real la declara META-INF/jarjar/metadata.json), asi que se miran ambas
        if ($e.FullName -match '^META-INF/(jarjar|jars)/.+\.jar$') {
            $ms = New-Object IO.MemoryStream; $s = $e.Open(); $s.CopyTo($ms); $s.Close(); $ms.Position = 0
            $inner = New-Object IO.Compression.ZipArchive($ms)
            Scan-Zip $inner "$label > $($e.Name)"
            $inner.Dispose()
        }
    }
}

$invalid = @()
Get-ChildItem $ModsDir -Filter *.jar | ForEach-Object {
    $z = [IO.Compression.ZipFile]::OpenRead($_.FullName)
    try {
        Scan-Zip $z $_.Name
        # NeoForge refuses jars whose manifest has no modLoader (e.g. builds for newer Minecraft versions)
        $mt = $z.GetEntry('META-INF/neoforge.mods.toml'); $mf = $z.GetEntry('META-INF/MANIFEST.MF')
        if ($mt) { if ((Read-Toml $mt.Open()) -notmatch '(?m)^\s*modLoader\s*=') { $invalid += $_.Name } }
        elseif (-not $mf -or (Read-Toml $mf.Open()) -notmatch 'FMLModType') { $invalid += $_.Name }
    } finally { $z.Dispose() }
}
if ($invalid) { Write-Host "JARS INVALIDOS para esta version de NeoForge (fijar otra version):"; $invalid | ForEach-Object { Write-Host "  $_" } }

$clash = $incompat | Where-Object { $provided.ContainsKey($_.id) -and (Test-Range $providedVer[$_.id] $_.range) }
if ($clash) { Write-Host "INCOMPATIBILIDADES (revisar rango de version):"; $clash | Sort-Object id -Unique | Format-Table id, range, from -AutoSize | Out-String -Width 250 | Write-Host }
$missing = $deps | Where-Object { -not $provided.ContainsKey($_.id) }
if (-not $missing) { Write-Host "Sin dependencias faltantes ($($provided.Count) modIds instalados)." }
else { $missing | Sort-Object id -Unique | Format-Table id, range, side, from -AutoSize | Out-String -Width 250 | Write-Host }
