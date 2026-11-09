# Prints random compositional translations and a breakdown of untranslated keys by category.
param([int]$Count = 40)
Add-Type -AssemblyName System.Web.Extensions
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$tr = Join-Path $root 'translations'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$j = New-Object System.Web.Script.Serialization.JavaScriptSerializer; $j.MaxJsonLength = 100MB
if (-not ('HBDT.Composer' -as [type])) { Add-Type -TypeDefinition ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Compose.cs'))) -ReferencedAssemblies System.Core }
$at = "$env:APPDATA\ATLauncher"
$idx = $j.DeserializeObject([IO.File]::ReadAllText("$at\assets\indexes\17.json")); $h = $idx['objects']['minecraft/lang/es_es.json']['hash']
$vEs = $j.DeserializeObject([IO.File]::ReadAllText("$at\assets\objects\$($h.Substring(0,2))\$h", $utf8))
Add-Type -AssemblyName System.IO.Compression.FileSystem
$z = [IO.Compression.ZipFile]::OpenRead("$at\libraries\net\minecraft\client\1.21.1\client-1.21.1.jar"); $sr = New-Object IO.StreamReader($z.GetEntry('assets/minecraft/lang/en_us.json').Open()); $vEn = $j.DeserializeObject($sr.ReadToEnd()); $sr.Close(); $z.Dispose()
$cp = New-Object HBDT.Composer
foreach ($k in $vEn.Keys) { if ($k -match '^(block|item)\.minecraft\.[a-z0-9_]+$' -and $vEs.ContainsKey($k) -and -not $cp.Vanilla.ContainsKey($vEn[$k])) { $cp.Vanilla[$vEn[$k]] = $vEs[$k] } }
foreach ($p in @(@('materials.json', $cp.Material), @('shapes.json', $cp.Shape), @('words.json', $cp.Word))) { $d = $j.DeserializeObject([IO.File]::ReadAllText((Join-Path $tr $p[0]), $utf8)); foreach ($k in $d.Keys) { $p[1][$k] = $d[$k] } }
$cp.Index()
$tests = @('Cornered Light Gray Wool', 'Birch Planks Crate Slab', 'Tiled Light Blue Stained Glass Pane', 'Oak Top Roof', 'Riveted Locometal', 'Angry Raw Copper Block', 'Small Gabbro Brick Wall', 'Polished Cut Dolomite Wall', 'Striped Block of Emerald Stairs', 'Blank Purpur Block Carving', 'Black Single Pane Locometal Window', 'Maroon Small Deepslate Tiles', 'Wavy Warped Planks Stairs', 'Crimson Lower Base Roof')
foreach ($s in $tests) { "{0,-40} -> {1}" -f $s, $cp.Translate($s, $false) }
