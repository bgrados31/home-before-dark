# Writes the FancyMenu / Drippy layouts and global options for Home Before Dark.
#  - Main menu: Minecraft's title screen is replaced by our own FancyMenu screen ("hogar"): seasonal cycle
#    background + ambience, logo top-left, lantern menu column (custom pixel font), small links at the bottom.
#    The menu pieces are rendered here (Gen.cs RunMenu) because they include the version number.
#  - Loading screen (Drippy): one layout per season, picked by the real-time month.
# Formats verified against FancyMenu 1.21.1 / Drippy v3 sources.
# Usage: powershell -ExecutionPolicy Bypass -File scripts\make-layouts.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$cfg = Join-Path $root 'pack\config'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$toml = [IO.File]::ReadAllText((Join-Path $root 'pack\pack.toml'), $utf8)
$version = [regex]::Match($toml, '(?m)^version = "([^"]+)"').Groups[1].Value
$A = '[source:local]/config/fancymenu/assets'
$assets = Join-Path $cfg 'fancymenu\assets'
$custDir = Join-Path $cfg 'fancymenu\customization'
New-Item -ItemType Directory -Force $custDir | Out-Null
Get-ChildItem $custDir -Filter *.txt | Remove-Item

Add-Type -AssemblyName System.Drawing
if (-not ('HBD.Program' -as [type])) {
    Add-Type -TypeDefinition ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'art\Gen.cs'))) -ReferencedAssemblies System.Drawing -Language CSharp
}
[HBD.Program]::RunMenu($assets, "v$version") | Out-Null
function Size($rel) { $img = [System.Drawing.Image]::FromFile((Join-Path $assets $rel)); $s = @($img.Width, $img.Height); $img.Dispose(); return ,$s }

function Meta($identifier, $reqLines) {
    return @"
layout-meta {
  identifier = $identifier
  render_custom_elements_behind_vanilla = false
  last_edited_time = 1762700000000
  is_enabled = true
  randommode = false
  randomgroup = 1
  randomonlyfirsttime = false
  layout_index = 0
  widget_identifier_revision = 2$reqLines
}

customization {
  action = backgroundoptions
  keepaspectratio = true
}
"@
}

function Background($file, $parallax) {
    return @"
menu_background {
  instance_identifier = fondo
  background_type = image
  image_path = $A/escenas/$file
  slide = false
  repeat_texture = false
  parallax = $parallax
  parallax_intensity_x = 0.006
  parallax_intensity_y = 0.006
  invert_parallax = false
  restart_animated_on_menu_load = true
}
"@
}

function Btn($id, $label, $x, $y, $w, $h) {
    $lab = if ($label) { "`n  label = $label" } else { '' }
    return @"
vanilla_button {
  element_type = vanilla_button
  instance_identifier = $id
  anchor_point = mid-left
  x = $x
  y = $y
  width = $w
  height = $h
  stay_on_screen = true$lab
  hoversound = $A/sonidos/hover.wav
  label_base_color = #F3E4C4
  label_hover_color = #FFD27A
}
"@
}
function Hidden($id) { return "vanilla_button {`n  element_type = vanilla_button`n  instance_identifier = $id`n  is_hidden = true`n}" }
function Join-Layout($parts) { "type = fancymenu_layout`n`n" + (($parts | ForEach-Object { $_.Replace("`r`n", "`n").Trim() }) -join "`n`n") + "`n" }

# ---------------- title screen (one layout, all year)
# Buttons keep Minecraft's own (centred) positions, which adapt to every window size; we only restyle them.
# (Mods / language / accessibility can't be repositioned reliably, so the design follows the vanilla column.)
function Restyle($id, $label) {
    $lab = if ($label) { "`n  label = $label" } else { '' }
    return "vanilla_button {`n  element_type = vanilla_button`n  instance_identifier = $id`n  anchor_point = vanilla$lab`n  hoversound = $A/sonidos/hover.wav`n  label_base_color = #F3E4C4`n  label_hover_color = #FFD27A`n}"
}
# Single/Multiplayer anchored to the Options button and moved down one row, to fill the hidden Realms slot
function Shifted($id, $label, $dy) {
    return "vanilla_button {`n  element_type = vanilla_button`n  instance_identifier = $id`n  anchor_point = element`n  anchor_point_element = mc_titlescreen_options_button`n  x = 0`n  y = $dy`n  width = 200`n  height = 20`n  stay_on_screen = true`n  label = $label`n  hoversound = $A/sonidos/hover.wav`n  label_base_color = #F3E4C4`n  label_hover_color = #FFD27A`n}"
}
function Img($id, $rel, $anchor, $x, $y, $w, $h) {
    return "element {`n  element_type = image`n  instance_identifier = $id`n  anchor_point = $anchor`n  x = $x`n  y = $y`n  width = $w`n  height = $h`n  stay_on_screen = false`n  source = $A/$rel`n}"
}
# custom button whose look is two images (normal / hover) from ui\menu; one action
function MenuButton($id, $anchor, $x, $y, $action, $value) {
    $s = Size "ui\menu\$id.png"
    return @"
element {
  element_type = custom_button
  instance_identifier = boton-$id
  anchor_point = $anchor
  x = $x
  y = $y
  width = $($s[0])
  height = $($s[1])
  stay_on_screen = false
  backgroundnormal = $A/ui/menu/$id.png
  backgroundhovered = $A/ui/menu/${id}_hover.png
  hoversound = $A/sonidos/hover.wav
  navigatable = true
  button_element_executable_block_identifier = bloque-$id
  [executable_block:bloque-$id][type:generic] = [executables:accion-$id;]
  [executable_action_instance:accion-$id][action_type:$action] = $value
}
"@
}
$ambience = @"
element {
  element_type = audio_v2
  instance_identifier = ambiente
  anchor_point = top-left
  x = 0
  y = 0
  width = 20
  height = 20
  audio_instance_0 = $A/sonidos/ambiente_ciclo.wav
  audio_instance_weight_0 = 1.0
  play_mode = normal
  looping = true
  sound_source = ambient
  volume = 0.55
}
"@

# ---------------- main menu: our own screen "hogar" replaces Minecraft's title screen
$lema = Size 'ui\menu\lema.png'; $info = Size 'ui\menu\info.png'; $idioma = Size 'ui\menu\idioma.png'
$hogar = @(
    (Meta 'hogar' ''),
    (Background 'ciclo.apng' 'false'),
    (Img 'sombra-izq' 'ui/menu/sombra_izq.png' 'top-left' 0 0 230 2000),
    (Img 'sombra-abajo' 'ui/menu/sombra_abajo.png' 'bottom-left' 0 -36 4000 36),
    (Img 'logo' 'ui/logo.png' 'top-left' 19 14 103 52),
    (Img 'lema' 'ui/menu/lema.png' 'top-left' 22 68 $lema[0] $lema[1]),
    (MenuButton 'solo' 'mid-left' 3 -36 'opengui' 'select_world_screen'),
    (MenuButton 'amigos' 'mid-left' 3 -17 'opengui' 'join_multiplayer_screen'),
    (MenuButton 'mods' 'mid-left' 3 2 'opengui' 'net.neoforged.neoforge.client.gui.ModListScreen'),
    (MenuButton 'opciones' 'mid-left' 3 21 'opengui' 'options_screen'),
    (MenuButton 'salir' 'mid-left' 3 46 'quitgame' ''),
    (MenuButton 'idioma' 'bottom-left' 22 -13 'opengui' 'language_select_screen'),
    (MenuButton 'accesibilidad' 'bottom-left' (22 + $idioma[0] + 8) -13 'opengui' 'accessibility_options_screen'),
    (Img 'info' 'ui/menu/info.png' 'bottom-right' (-$info[0] - 3) -20 $info[0] $info[1]),
    $ambience
)
[IO.File]::WriteAllText((Join-Path $custDir 'menu_hogar.txt'), (Join-Layout $hogar), $utf8)
$gui = "type = custom_gui_screens`n`noverridden_screens {`n  title_screen = hogar`n}`n`ncustom_gui {`n  identifier = hogar`n  title = `n  allow_esc = false`n  transparent_world_background = true`n  transparent_world_background_overlay = false`n  pause_game = false`n  popup_mode = false`n  popup_mode_background_overlay = false`n}`n"
[IO.File]::WriteAllText((Join-Path $cfg 'fancymenu\custom_gui_screens.txt'), $gui, $utf8)

# ---------------- fallback: Minecraft's own title screen, restyled (only shows if the override above is removed)
$title = @(
    (Meta 'title_screen' ''),
    (Background 'ciclo.apng' 'false'),
    @"
element {
  element_type = image
  instance_identifier = logo
  anchor_point = top-centered
  sticky_anchor = true
  x = 0
  y = 22
  width = 103
  height = 52
  stay_on_screen = true
  source = $A/ui/logo.png
}
"@,
    $ambience,
    (Shifted 'mc_titlescreen_singleplayer_button' 'Un jugador' -82),
    (Shifted 'mc_titlescreen_multiplayer_button' 'Multijugador' -58),
    (Restyle 'mc_titlescreen_options_button' 'Opciones'),
    (Restyle 'mc_titlescreen_quit_button' 'Salir'),
    (Hidden 'mc_titlescreen_realms_button'),
    (Hidden 'minecraft_logo_widget'),
    (Hidden 'minecraft_splash_widget'),
    (Hidden 'minecraft_branding_widget'),
    (Hidden 'minecraft_realms_notification_icons_widget')
)
[IO.File]::WriteAllText((Join-Path $custDir 'menu_principal.txt'), (Join-Layout $title), $utf8)

# ---------------- loading screen (Drippy), one per season by real-time month
$seasons = @(
    @{ id = 'invierno'; months = '11,12,1,2'; logo = 'logo_nieve.png' },
    @{ id = 'primavera'; months = '3,4,5'; logo = 'logo.png' },
    @{ id = 'verano'; months = '6,7,8'; logo = 'logo.png' },
    @{ id = 'otono'; months = '9,10'; logo = 'logo.png' }
)
foreach ($s in $seasons) {
    $rid = "mes-carga-$($s.id)"
    $req = "`n  [loading_requirement_container_meta:req-carga-$($s.id)] = [groups:][instances:$rid;]`n  [loading_requirement:fancymenu_visibility_requirement_is_realtime_month][requirement_mode:if][req_id:$rid] = $($s.months)"
    $parts = @(
        (Meta 'drippy_loading_overlay' $req),
        (Background "$($s.id).apng" 'false'),
        @"
element {
  element_type = image
  instance_identifier = logo-carga
  anchor_point = mid-centered
  sticky_anchor = true
  x = 0
  y = -40
  width = 206
  height = 104
  stay_on_screen = true
  source = $A/ui/$($s.logo)
}
"@,
        (Hidden 'mojang_logo')
    )
    [IO.File]::WriteAllText((Join-Path $custDir "carga_$($s.id).txt"), (Join-Layout $parts), $utf8)
}

# ---------------- illustrated background for every menu OUTSIDE a world (universal layout).
# Inside a world (pause menu, inventories, options) the layout is skipped, so the game stays visible behind them.
$uni = @"
layout-meta {
  identifier = %fancymenu:universal_layout%
  render_custom_elements_behind_vanilla = false
  last_edited_time = 1762700000000
  is_enabled = true
  randommode = false
  randomgroup = 1
  randomonlyfirsttime = false
  layout_index = 0
  universal_layout_blacklist = hogar;title_screen;drippy_loading_overlay;
  widget_identifier_revision = 2
  [loading_requirement_container_meta:req-fuera-del-mundo] = [groups:][instances:sin-mundo;]
  [loading_requirement:fancymenu_loading_requirement_is_world_loaded][requirement_mode:if_not][req_id:sin-mundo] =
}

customization {
  action = backgroundoptions
  keepaspectratio = true
}
"@
[IO.File]::WriteAllText((Join-Path $custDir 'fondo_menus.txt'), (Join-Layout @($uni, (Background 'fondo_menus.png' 'false'))), $utf8)

# ---------------- which screens FancyMenu may customize (class names). The vanilla menus are listed so the
# universal background above reaches them.
$screens = @(
    'net.minecraft.client.gui.screens.TitleScreen', 'de.keksuccino.drippyloadingscreen.customization.DrippyOverlayScreen',
    'net.minecraft.client.gui.screens.worldselection.SelectWorldScreen', 'net.minecraft.client.gui.screens.worldselection.CreateWorldScreen',
    'net.minecraft.client.gui.screens.worldselection.EditWorldScreen', 'net.minecraft.client.gui.screens.worldselection.EditGameRulesScreen',
    'net.minecraft.client.gui.screens.worldselection.ExperimentsScreen', 'net.minecraft.client.gui.screens.multiplayer.JoinMultiplayerScreen',
    'net.minecraft.client.gui.screens.EditServerScreen', 'net.minecraft.client.gui.screens.DirectJoinServerScreen',
    'net.minecraft.client.gui.screens.ConnectScreen', 'net.minecraft.client.gui.screens.DisconnectedScreen',
    'net.minecraft.client.gui.screens.options.OptionsScreen', 'net.minecraft.client.gui.screens.options.VideoSettingsScreen',
    'net.minecraft.client.gui.screens.options.SoundOptionsScreen', 'net.minecraft.client.gui.screens.options.controls.ControlsScreen',
    'net.minecraft.client.gui.screens.options.controls.KeyBindsScreen', 'net.minecraft.client.gui.screens.options.MouseSettingsScreen',
    'net.minecraft.client.gui.screens.options.LanguageSelectScreen', 'net.minecraft.client.gui.screens.options.AccessibilityOptionsScreen',
    'net.minecraft.client.gui.screens.options.ChatOptionsScreen', 'net.minecraft.client.gui.screens.options.SkinCustomizationScreen',
    'net.minecraft.client.gui.screens.options.OnlineOptionsScreen', 'net.minecraft.client.gui.screens.options.FontOptionsScreen',
    'net.minecraft.client.gui.screens.packs.PackSelectionScreen', 'net.minecraft.client.gui.screens.CreditsAndAttributionScreen',
    'net.minecraft.client.gui.screens.telemetry.TelemetryInfoScreen', 'net.neoforged.neoforge.client.gui.ModListScreen'
)
$cm = "type = customizablemenus`n`n" + (($screens | ForEach-Object { "$_ {`n}`n" }) -join "`n")
[IO.File]::WriteAllText((Join-Path $cfg 'fancymenu\customizablemenus.txt'), $cm, $utf8)

# ---------------- FancyMenu global options (theme for every menu, music, window, no welcome guide)
$U = "$A/ui"
$opts = @"
##[general]
B:play_vanilla_menu_music = 'false';

##[customization]
B:show_customization_overlay = 'false';

##[tutorial]
B:show_welcome_screen = 'false';

##[window]
B:show_custom_window_icon = 'true';
S:custom_window_icon_16 = '/config/fancymenu/assets/ui/icono_16.png';
S:custom_window_icon_32 = '/config/fancymenu/assets/ui/icono_32.png';
S:custom_window_title = 'Home Before Dark';

##[global_customizations]
S:global_button_background_normal = '$U/boton.png';
S:global_button_background_hover = '$U/boton_hover.png';
S:global_button_background_inactive = '$U/boton_inactivo.png';
B:global_button_background_nine_slice = 'true';
I:global_button_background_nine_slice_border_top = '3';
I:global_button_background_nine_slice_border_right = '3';
I:global_button_background_nine_slice_border_bottom = '3';
I:global_button_background_nine_slice_border_left = '3';
S:global_button_label_base_color = '#F3E4C4';
S:global_button_label_hover_color = '#FFD27A';
S:global_slider_background = '$U/slider_fondo.png';
B:global_slider_background_nine_slice = 'true';
I:global_slider_background_nine_slice_border_top = '3';
I:global_slider_background_nine_slice_border_right = '3';
I:global_slider_background_nine_slice_border_bottom = '3';
I:global_slider_background_nine_slice_border_left = '3';
S:global_slider_label_base_color = '#F3E4C4';
S:global_slider_label_hover_color = '#FFD27A';
S:global_slider_handle_normal = '$U/slider_mango.png';
S:global_slider_handle_hover = '$U/slider_mango_hover.png';
S:global_menu_music_tracks = '$A/musica/hogar.wav%!source_end!%$A/musica/sendero.wav';
S:global_button_click_sound = '$A/sonidos/click.wav';
F:global_button_click_sound_volume = '0.6';
"@
[IO.File]::WriteAllText((Join-Path $cfg 'fancymenu\options.txt'), $opts.Replace("`r`n", "`n") + "`n", $utf8)

# ---------------- Drippy early loading window (before the game window is ready)
$dr = @"
##[general]
B:fade_out_loading_screen = 'true';
B:wait_for_textures_in_loading = 'true';

##[early_loading]
S:early_loading_window_title = 'Home Before Dark';
S:early_loading_background_texture_path = '/config/fancymenu/assets/escenas/carga_inicial.png';
B:early_loading_background_preserve_aspect_ratio = 'true';
S:early_loading_logo_texture_path = '/config/fancymenu/assets/ui/logo_x3.png';
I:early_loading_logo_width = '309';
I:early_loading_logo_height = '156';
I:early_loading_logo_position_offset_x = '0';
I:early_loading_logo_position_offset_y = '-50';
I:early_loading_bar_position_offset_y = '70';
B:early_loading_hide_logger = 'true';
"@
New-Item -ItemType Directory -Force (Join-Path $cfg 'drippyloadingscreen') | Out-Null
[IO.File]::WriteAllText((Join-Path $cfg 'drippyloadingscreen\options.txt'), $dr.Replace("`r`n", "`n") + "`n", $utf8)
Get-ChildItem $custDir | ForEach-Object { "$($_.Name) $($_.Length) B" }
