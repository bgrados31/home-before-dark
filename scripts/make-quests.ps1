# Generates the FTB Quests book (FTB Quests 2101, MC 1.21.1) from the JSON chapters in quests\.
#   quests\_grupos.json      -> chapter groups  [{ "key": "...", "title": "..." }]
#   quests\NN-<key>.json     -> one chapter each (see quests\LEEME.md for the format)
# Output: pack\config\ftbquests\quests\ (data.snbt, chapter_groups.snbt, chapters\*.snbt, lang\es_es + en_us)
# IDs are derived from the keys (MD5), so regenerating keeps every player's progress. Never rename a key
# of a quest that is already in use: change its title instead.
# Usage: powershell -ExecutionPolicy Bypass -File scripts\make-quests.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root 'quests'
$dst = Join-Path $root 'pack\config\ftbquests\quests'
$u8 = New-Object System.Text.UTF8Encoding($false)
$md5 = [Security.Cryptography.MD5]::Create()

function Id([string]$s) {
    $b = $md5.ComputeHash([Text.Encoding]::UTF8.GetBytes("hbd:" + $s))
    $b[0] = $b[0] -band 0x7F; if ($b[0] -eq 0) { $b[0] = 0x10 }
    return (-join ($b[0..7] | ForEach-Object { $_.ToString('X2') }))
}
function Q([string]$s) { return '"' + $s.Replace('\', '\\').Replace('"', '\"') + '"' }
function D($n) { return ([double]$n).ToString('0.0###', [Globalization.CultureInfo]::InvariantCulture) + 'd' }
function ReadJson($p) { return ([IO.File]::ReadAllText($p, $u8) | ConvertFrom-Json) }
function Has($o, $name) { return ($null -ne $o.PSObject.Properties[$name]) }

if (Test-Path $dst) { Remove-Item $dst -Recurse -Force }
foreach ($d in 'chapters', 'lang') { New-Item -ItemType Directory -Force (Join-Path $dst $d) | Out-Null }
$allLang = New-Object System.Collections.Generic.List[string]   # FTB Quests 2101.1.38 reads ONE file per locale: lang\<locale>.snbt
$script:themeLines = @(); $script:themeDefault = $null; $script:bgCount = 0

# ---- book settings
[IO.File]::WriteAllText((Join-Path $dst 'data.snbt'), @"
{
	default_autoclaim_rewards: "disabled"
	default_consume_items: false
	default_quest_disable_jei: false
	default_quest_shape: "circle"
	default_reward_team: false
	detection_delay: 20
	disable_gui: false
	drop_loot_crates: false
	emergency_items_cooldown: 300
	grid_scale: 0.5d
	icon: {
		id: "minecraft:campfire"
	}
	lock_message: ""
	loot_crate_no_drop: {
		boss: 0
		monster: 600
		passive: 4000
	}
	pause_game: false
	progression_mode: "flexible"
	show_lock_icons: true
	version: 13
}
"@.Replace("`r`n", "`n"), $u8)

# ---- groups
$groups = ReadJson (Join-Path $src '_grupos.json')
$gid = @{}
$gl = @(); $glang = @()
foreach ($g in $groups) { $gid[$g.key] = Id "group:$($g.key)"; $gl += "`t`t{ id: $(Q $gid[$g.key]) }"; $glang += "`tchapter_group.$($gid[$g.key]).title: $(Q $g.title)" }
[IO.File]::WriteAllText((Join-Path $dst 'chapter_groups.snbt'), "{`n`tchapter_groups: [`n" + ($gl -join "`n") + "`n`t]`n}`n", $u8)

$chLang = @(); $nQuests = 0; $nChapters = 0
foreach ($f in Get-ChildItem $src -Filter '*.json' | Where-Object { $_.Name -ne '_grupos.json' } | Sort-Object Name) {
    $c = ReadJson $f.FullName
    $cid = Id "chapter:$($c.key)"
    $chLang += "`tchapter.$cid.title: $(Q $c.title)"
    if (Has $c 'subtitle') { $chLang += "`tchapter.$cid.chapter_subtitle: [$(Q $c.subtitle)]" }
    $qids = @{}; foreach ($q in $c.quests) { $qids[$q.key] = Id "quest:$($c.key):$($q.key)" }
    $ql = @(); $lang = @()
    foreach ($q in $c.quests) {
        $qid = $qids[$q.key]; $p = "quest:$($c.key):$($q.key)"
        $lines = @()
        if ((Has $q 'deps') -and $q.deps.Count) {
            $deps = foreach ($d in $q.deps) { if ($d.Contains(':')) { Id "quest:$d" } elseif ($qids.ContainsKey($d)) { $qids[$d] } else { throw "Dependencia desconocida '$d' en $($c.key)/$($q.key)" } }
            $lines += "dependencies: [" + (($deps | ForEach-Object { Q $_ }) -join ', ') + "]"
        }
        if (Has $q 'hide') { $lines += "hide_until_deps_visible: true" }
        if (Has $q 'icon') { $lines += "icon: { id: $(Q $q.icon) }" }
        $lines += "id: $(Q $qid)"
        if ((Has $q 'optional') -and $q.optional) { $lines += "optional: true" }
        # rewards
        $rw = @(); $i = 0
        foreach ($r in @($q.rewards)) {
            if (-not $r) { continue }
            $rid = Id "${p}:reward:$i"; $i++
            switch ($r.type) {
                'xp'        { $rw += "{ id: $(Q $rid), type: `"xp`", xp: $([int]$r.xp) }" }
                'xp_levels' { $rw += "{ id: $(Q $rid), type: `"xp_levels`", xp_levels: $([int]$r.levels) }" }
                'item'      { $cnt = if (Has $r 'count') { [int]$r.count } else { 1 }; $rw += "{ " + $(if ($cnt -gt 1) { "count: $cnt, " } else { '' }) + "id: $(Q $rid), item: { count: 1, id: $(Q $r.item) }, type: `"item`" }" }
                default     { throw "Recompensa desconocida '$($r.type)' en ${p}" }
            }
        }
        if ($rw.Count) { $lines += "rewards: [" + ($rw -join ', ') + "]" }
        if (Has $q 'shape') { $lines += "shape: $(Q $q.shape)" }
        if (Has $q 'size') { $lines += "size: $(D $q.size)" }
        # tasks
        $tk = @(); $i = 0
        foreach ($t in @($q.tasks)) {
            $tid = Id "${p}:task:$i"; $i++
            switch ($t.type) {
                'checkmark'   { $tk += "{ id: $(Q $tid), type: `"checkmark`" }" }
                'item'        { $cnt = if (Has $t 'count') { [int64]$t.count } else { 1 }; $tk += "{ " + $(if ($cnt -gt 1) { "count: ${cnt}L, " } else { '' }) + "id: $(Q $tid), item: { count: 1, id: $(Q $t.item) }, type: `"item`" }" }
                'kill'        { $cnt = if (Has $t 'count') { [int64]$t.count } else { 1 }; $tk += "{ entity: $(Q $t.entity), id: $(Q $tid), type: `"kill`", value: ${cnt}L }" }
                'advancement' { $tk += "{ advancement: $(Q $t.advancement), criterion: `"`", id: $(Q $tid), type: `"advancement`" }" }
                'structure'   { $tk += "{ id: $(Q $tid), structure: $(Q $t.structure), type: `"structure`" }" }
                'biome'       { $tk += "{ biome: $(Q $t.biome), id: $(Q $tid), type: `"biome`" }" }
                'dimension'   { $tk += "{ dimension: $(Q $t.dimension), id: $(Q $tid), type: `"dimension`" }" }
                default       { throw "Tarea desconocida '$($t.type)' en ${p}" }
            }
            if (Has $t 'title') { $lang += "`ttask.$tid.title: $(Q $t.title)" }
        }
        $lines += "tasks: [" + ($tk -join ', ') + "]"
        $lines += "x: $(D $q.x)"; $lines += "y: $(D $q.y)"
        $ql += "`t`t{`n" + (($lines | ForEach-Object { "`t`t`t$_" }) -join "`n") + "`n`t`t}"
        $lang += "`tquest.$qid.title: $(Q $q.title)"
        if (Has $q 'desc') { $lang += "`tquest.$qid.quest_desc: [" + ((@($q.desc) | ForEach-Object { Q $_ }) -join ', ') + "]" }
        $nQuests++
    }
    $group = if (Has $c 'group') { $gid[$c.group] } else { '' }
    # optional full-screen background of the quest book for this chapter: the valley in a season (Gen.cs),
    # applied through the FTB Quests theme (recursos\assets\ftbquests\ftb_quests_theme.txt, selector = chapter ID)
    $images = ''
    if (Has $c 'background') {
        if (-not ('HBD.Program' -as [type])) { Add-Type -TypeDefinition ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'art\Gen.cs'))) -ReferencedAssemblies System.Drawing -Language CSharp }
        $png = Join-Path $root "recursos\assets\homebeforedark\textures\misiones\$($c.key).png"
        [void][HBD.Program]::RunQuestFullBackground($png, $c.background.season, [int]$c.background.frame)
        $tex = "homebeforedark:textures/misiones/$($c.key).png"
        if (-not $script:themeDefault) { $script:themeDefault = $tex }
        $script:themeLines += "[$cid]`nbackground: $tex`n"
        $script:bgCount++
    }
    $chapter = @"
{
	default_hide_dependency_lines: false
	default_quest_shape: ""
	filename: $(Q $c.key)
	group: $(Q $group)
	icon: { id: $(Q $c.icon) }
	id: $(Q $cid)
$images	order_index: $([int]$c.order)
	progression_mode: "flexible"
	quest_links: [ ]
	quests: [
$($ql -join "`n")
	]
}
"@
    [IO.File]::WriteAllText((Join-Path $dst "chapters\$($c.key).snbt"), $chapter.Replace("`r`n", "`n") + "`n", $u8)
    foreach ($x in $lang) { $allLang.Add($x) }
    $nChapters++
}
# same Spanish text for es_es and en_us, so nobody ever sees raw keys
$langText = "{`n`tfile.0000000000000001.title: `"Home Before Dark`"`n" + (($glang + $chLang + $allLang) -join "`n") + "`n}`n"
foreach ($loc in 'es_es', 'en_us') { [IO.File]::WriteAllText((Join-Path $dst "lang\$loc.snbt"), $langText, $u8) }

# FTB Quests theme: merged on top of the mod's default theme; [*] is the fallback for chapters without a picture
$res = Join-Path $root 'recursos'
$themeFile = Join-Path $res 'assets\ftbquests\ftb_quests_theme.txt'
if ($script:themeDefault) {
    New-Item -ItemType Directory -Force (Split-Path $themeFile) | Out-Null
    $theme = "// Home Before Dark - fondo completo del libro de misiones (lo genera scripts\make-quests.ps1)`n[*]`nbackground: $($script:themeDefault)`n`n" + ($script:themeLines -join "`n")
    [IO.File]::WriteAllText($themeFile, $theme, $u8)
}

# refresh recursos\ inside the Spanish resource pack (build-translations.ps1 also includes them when it rebuilds the zip)
$rp = Join-Path $root 'pack\resourcepacks\HomeBeforeDark-Espanol.zip'
if ((Test-Path $res) -and (Test-Path $rp)) {
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::Open($rp, 'Update')
    try {
        @($zip.Entries | Where-Object { $_.FullName.StartsWith('assets/homebeforedark/') -or $_.FullName.StartsWith('assets/ftbquests/') }) | ForEach-Object { $_.Delete() }
        Get-ChildItem $res -Recurse -File | ForEach-Object { [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $_.FullName.Substring($res.Length + 1).Replace('\', '/')) }
    } finally { $zip.Dispose() }
}
Write-Host "Misiones: $nChapters capitulos, $nQuests misiones, $([int]$script:bgCount) fondos -> $dst"
