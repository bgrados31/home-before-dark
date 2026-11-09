# AGENTS.md — guía para agentes de IA

Proyecto: **Home Before Dark**, modpack cooperativo de Minecraft (rollo medieval RPG, aventura, misiones y construcción)
para Bryan, su novia y ~10 amigos. Todo el juego va **en español**. Lee esto entero antes de tocar nada.

## Lo esencial

| | |
|---|---|
| Minecraft / loader | 1.21.1 · NeoForge 21.1.265 · Java 21 |
| Gestor del pack | [packwiz](https://packwiz.infra.link/) (`..\tools\packwiz.exe`), pack en `pack\` |
| Launcher de los jugadores | ATLauncher (importan el `.mrpack` de `dist\`) |
| Instancia de prueba | `%APPDATA%\ATLauncher\instances\HomeBeforeDark` (nombre visible "Home Before Dark"; antes `CrnicasdeEscarcha`) |
| Servidor | `C:\Users\Bryan\Desktop\minecraft\servidor` (lo genera `scripts\make-server.ps1`) |
| PC de Bryan | Windows 11, i5-13500H, RTX 4050 6 GB, 24 GB RAM; la instancia usa 8 GB |
| Shell | Windows PowerShell 5.1 (ver trampas abajo) |

## Reglas de trabajo con Bryan

1. **Bryan no corre scripts** (PowerShell le muestra errores en rojo). El agente ejecuta todo: scripts, exportar, sincronizar,
   abrir el juego y probar. Bryan solo acepta los popups de Windows.
2. **Probar siempre en el juego** después de cambiar mods: que llegue al menú (`logs\latest.log` → "Game took … seconds")
   y, si se tocó generación de mundo/estructuras, crear un mundo nuevo. Los crash están en `crash-reports\`.
3. **No enviar teclas ni clics al juego si Bryan lo está usando** (si escribe en el chat o la terminal está al frente, parar).
   Para cerrar el juego sin perder nada: `CloseMainWindow()` sobre el proceso `javaw` con título "Home Before Dark" (guarda el mundo).
4. **No publicar páginas/artifacts en claude.ai**: listas y reportes van en el chat o en archivos locales (`lista-de-mods.html`).
5. **EULA de Minecraft:** nunca aceptarlo por Bryan. `iniciar.bat` del servidor se lo pregunta a él.
6. Gustos de Bryan: menús originales (nada de "estilo Minecraft" genérico), **sin parallax** (nada que se mueva con el cursor),
   le encantan los sonidos de ambiente y el ciclo de estaciones del menú. Tiempo de carga importa (PCs medias).
7. Responder en español, claro y sin jerga.

## Estructura

| Ruta | Qué es |
|---|---|
| `pack\pack.toml`, `pack\index.toml` | Definición packwiz (versión del pack en `pack.toml`) |
| `pack\mods\*.pw.toml`, `pack\resourcepacks\*.pw.toml` | Un archivo por mod/resource pack (descarga + hash + lado) |
| `pack\config\` | Configs que van en el pack (overrides). Incluye menús FancyMenu, Drippy, defaultoptions, FTB Quests |
| `pack\resourcepacks\HomeBeforeDark-Espanol.zip` | Resource pack de traducciones (lo genera `build-translations.ps1`) |
| `mods.txt` | Lista legible y comentada de los mods, por secciones/versiones. Mantenerla al día |
| `quests\*.json` | Fuente del libro de misiones (ver `quests\LEEME.md` y `MISIONES.md`) |
| `translations\` | Diccionarios y traducciones manuales/pendientes |
| `scripts\` | Todo el tooling (abajo) |
| `dist\` | Salidas: `Home-Before-Dark-X.Y.Z.mrpack` (y el zip del servidor con `-Zip`) |
| `PLAN.md`, `MISIONES.md`, `README.md`, `lista-de-mods.html` | Plan general, plan de misiones, guía de uso, lista de mods por categoría |

## Scripts (ejecutar con `powershell -ExecutionPolicy Bypass -File scripts\<script>.ps1`)

| Script | Para qué |
|---|---|
| `export.ps1` | `packwiz refresh` + exporta `dist\Home-Before-Dark-<ver>.mrpack`. Los jars de CurseForge (FTB, Twilight Forest) se referencian por la CDN oficial (no se redistribuyen) |
| `check-deps.ps1 -ModsDir <instancia>\mods` | Dependencias faltantes (incluye jar-in-jar) y jars que NeoForge no puede cargar. **Correr siempre después de agregar mods** |
| `sync-instance.ps1` | Actualiza la instancia de ATLauncher con el último `.mrpack` (descarga/aparta jars, copia configs, activa resource packs). **Juego cerrado** |
| `make-server.ps1 [-Zip]` | Crea/actualiza el servidor dedicado: NeoForge server, mods sin los de solo-cliente, configs, `server.properties`, `user_jvm_args.txt` (8 GB, G1GC), `iniciar.bat` |
| `make-quests.ps1` | Genera el libro de FTB Quests desde `quests\*.json` (IDs estables por clave) |
| `make-layouts.ps1` | Genera los layouts de FancyMenu/Drippy, opciones globales y las imágenes del menú con la versión (llama a `art\Gen.cs`) |
| `make-art.ps1` + `art\Gen.cs` | Arte procedural (escenas APNG del valle por estación, logo, botones) y audio (música y ambientes) |
| `build-translations.ps1 [-Analyze]` | Resource pack de traducciones: es_es de los mods + otras variantes de español + manuales + compuestas |
| `lang-scan.ps1`, `add-all.ps1` | Reporte de cobertura de español; agregar todo `mods.txt` con packwiz |

### Flujo para una versión nueva
1. Cambios (packwiz add/remove, configs en `pack\config`, `quests\`, arte/layouts/traducciones con sus scripts).
2. Subir `version` en `pack\pack.toml` → `make-layouts.ps1` (pone la versión en el menú) → `make-quests.ps1` si hubo misiones.
3. `export.ps1` → (cerrar el juego) `sync-instance.ps1` → `check-deps.ps1` → abrir el juego y probar.
4. `make-server.ps1` para actualizar el servidor.
5. Actualizar `mods.txt`, `README.md`/`PLAN.md` y `lista-de-mods.html` si cambió la lista.

Agregar un mod: `cd pack; ..\..\tools\packwiz.exe -y modrinth add <slug>` (o `curseforge add --addon-id <id>`). Fijar versión:
`modrinth add --project-id <id> --version-id <id>` y luego `packwiz pin <slug>`.

Abrir el juego: `Start-Process "$env:APPDATA\ATLauncher\ATLauncher.exe" -ArgumentList '--launch','"Home Before Dark"'`.
Carga típica: 85–120 s (la primera vez tras cambiar mods, más).

## Trampas conocidas (no repetir)

**Mods**
- Versiones "neoforge" multi-loader de Modrinth a veces requieren Fabric API → `check-deps.ps1` lo detecta. Quitamos LambDynamicLights por eso.
- Mods/datapacks multiversión (Structory, Low On Fire) publican builds "26.x" que NeoForge 1.21.1 rechaza ("Missing ModLoader") o con pack_format nuevo → fijar la build 1.21.x.
- Dependencias no declaradas en Modrinth: `runiclib`, `irons-lib`, `bundle-api` (Archers), `kambrik` (Bountiful).
- Distant Horizons + Chunky en cliente crashea al crear mundo → Chunky es `side = "server"`.
- Every Compat fue **quitado**: registraba 14.500 bloques copia (+40 s de carga); su config de entradas solo oculta, no des-registra.
- Enhanced Celestials fijado a 6.0.2.5: la 6.0.2.6 abre un aviso en inglés ("EC2 is here") en cada entrada al mundo.
- Ice and Fire CE es beta; dragones sin romper bloques (`config\iceandfire\iaf-common.json`). Sparse Structures en 1.35 (aldeas 1.0).
- Inventory Profiles Next: perfiles, flechas de auto-refill y sus botones (que tapaban la barra de FTB) desactivados en `config\inventoryprofilesnext\inventoryprofiles.json`.

**FancyMenu / Drippy (menú y pantallas de carga)**
- La pantalla de título se reemplaza por una pantalla propia `hogar` (`config\fancymenu\custom_gui_screens.txt` + `customization\menu_hogar.txt`).
- Imágenes animadas **deben** terminar en `.apng` (un `.png` animado se ve estático). Texturas sin filtrado (pixel art nítido).
- Anclas sin `sticky_anchor`: la esquina superior-izquierda del elemento = origen + offset (no restan el tamaño).
- `global_menu_background_texture` tapa el juego en el menú de pausa → NO usarlo. El fondo de los menús fuera del mundo es un
  layout universal (`identifier = %fancymenu:universal_layout%`, lista negra `hogar;title_screen;drippy_loading_overlay;` — terminar
  con `;`), con requisito `fancymenu_loading_requirement_is_world_loaded` en modo `if_not`. Las pantallas vanilla deben estar en `customizablemenus.txt`.
- `text_v2` cerca del borde inferior se recorta → el texto pequeño del menú se dibuja en PNG (fuente pixel propia en `Gen.cs`).
- Formato de `options.txt` de FancyMenu/Drippy: `##[seccion]` y `B:clave = 'valor';`.

**FTB Quests 2101.1.38**
- Textos en `lang\<idioma>.snbt` (UN archivo por idioma; la estructura por carpetas es de versiones más nuevas y se ignora).
- Claves: `chapter.<id>.title`, `chapter.<id>.chapter_subtitle`, `chapter_group.<id>.title`, `quest.<id>.title`, `quest.<id>.quest_desc` (lista), `task.<id>.title`, `file.0000000000000001.title`.
- No hay FTB Filter System: las tareas de ítem no aceptan tags → pedir ítems sin variantes o usar logros/estructuras.

**Arranque / JVM**
- ATLauncher trae `-XX:+UseG1GC` en `%APPDATA%\ATLauncher\configs\ATLauncher.json` (`javaParameters`) y Distant Horizons
  avisa en el chat ("G1 Garbage collector detected... FPS stuttering"). Medido en este PC: G1 = 101 s al menú,
  **ZGC generacional = 180 s** (mata los tirones pero casi duplica la carga), **Shenandoah = 114 s** (concurrente y sin pausas
  largas) → dejar Shenandoah: `-XX:+UseShenandoahGC -XX:+PerfDisableSharedMem`. El JVM del juego es el 21 de
  `runtimes\minecraft\java-runtime-delta\windows-x64\...` (el `ATLauncher\jre` es 17 y no acepta ZGenerational).
  Probar flags sin abrir el juego: `<ruta>\bin\java.exe <flags> -version`.
- El aviso de Distant Horizons se desactiva en su config, pero la causa real era el GC: no desactivar el aviso sin cambiar el GC.
- Medir la carga por fases en `logs\latest.log`: `ModLauncher ... starting` → `Launching target` → `Reloading ResourceManager`
  → `Sound engine started` → `ModernFix: Game took N seconds`. Con 268 mods: ~45 s de mods + ~22 s de recursos.

**PowerShell 5.1**
- Scripts con texto no-ASCII necesitan BOM (o escribir los acentos desde datos JSON/UTF-8).
- Variables y claves de hashtable no distinguen mayúsculas: `$zip` choca con un parámetro `-Zip`; `"$p:algo"` se lee como unidad `p:` → usar `"${p}:algo"`.
- Rutas con `[ ]` → `-LiteralPath`. `Add-Type` compila C# 5 (sin interpolación `$""`, sin `=>` en miembros).
- Capturas de la ventana: `SetProcessDPIAware` + `GetWindowRect`/`ClientToScreen` + `CopyFromScreen`.

## Servidor

- `make-server.ps1` arma `..\servidor\`: NeoForge server + 194 mods (omite los 57 de solo cliente), configs y misiones.
- `server.properties`, `user_jvm_args.txt` e `iniciar.bat` se crean solo si faltan (se respetan los cambios de Bryan).
- `iniciar.bat` usa el Java 21 de ATLauncher, pide aceptar el EULA la primera vez y reinicia el servidor si se cae.
- Pendiente: primera prueba de arranque (tras aceptar el EULA), whitelist/ops, pre-generar el mundo con Chunky, abrir puerto 25565 o usar un túnel.

## Teclas del pack (conflictos ya resueltos en `pack\config\defaultoptions\options.txt`)

**F6** shaders (Iris) · **O** elegir shader · **K** árbol de habilidades · **R** rodar (Combat Roll) · **Z** zoom (Just Zoom) ·
**M** mapa (FTB Chunks) · **J** órdenes a reclutas · **X** rueda de hechizos de Iron's · **V** lanzar hechizo de Iron's ·
**B** mochila · **G** accesorios (Curios) · **F8** ajustes de chat de voz · **F10** silenciar micrófono · **` (izq. del 1)** FTB Ultimine.
Se desasignaron: recarga de Iris, deshacer/rehacer de Effortless Building (chocaban con Z/Y), zoom de Twilight Forest,
mapa de Recruits, opciones de Just Zoom, intercambiar hombro, historial de Corpse, etc. Los solapes que quedan son
condicionales (solo con una varita en la mano, montando un dragón, dentro de una pantalla…). Si se agrega un mod con
teclas, revisar duplicados: script en la sección de trampas (agrupar `key_*` de `options.txt` por valor).

## Estado actual (v0.4.1, 9-nov-2026) — handoff para el siguiente agente

Hecho y sincronizado en la instancia de Bryan (llega al menú en ~150 s la primera vez):
- 35 mods RPG/estructuras/dimensiones (v0.3.0), animaciones/ambiente + 7 texture packs (v0.3.1), y en v0.3.2: AE2 (+GuideME),
  Storage Drawers, Tom's Simple Storage, Traveler's Backpack, River Redux, YUNG's Better Caves, Regions Unexplored,
  Enhanced Celestials 2 (core + lunas + soporte de shaders; reemplazó a EC1), packs FA Player Extension, Better Cats x FA,
  Refined Buckets, Fast Better Grass (+compat RU) y Yuushya 16x (incluido pero desactivado).
- Misiones: 6 capítulos / 68 misiones (`quests\01..06`), con fondo por capítulo.
- Traducciones reconstruidas (90 namespaces); el pack de español también lleva `recursos\` (fondos de misiones).

**Sin probar todavía (hacerlo primero):**
1. Crear un **mundo nuevo** con v0.3.2 (cambió la generación: River Redux + Better Caves + Regions Unexplored). Bryan
   interrumpió la prueba. Mirar `crash-reports\` y que el spawn no tarde demasiado.
2. Abrir el libro de misiones y revisar los capítulos 3–6: que todos los íconos/ítems existan (ningún "?" morado),
   que se lean los textos y que los fondos se vean bien. Probar que las lunas de EC2 se vean con shaders (F6).
3. Servidor: está en v0.3.1 → correr `make-server.ps1` para pasarlo a 0.3.2. Primer arranque pendiente de que Bryan acepte el EULA.

**v0.3.3 (hecho, sin verificar en pantalla):**
- **Fondo completo del libro de misiones** vía tema de FTB Quests: `make-quests.ps1` genera `recursos\assets\ftbquests\ftb_quests_theme.txt`
  (`[*] background: ...` + un bloque `[<ID de capítulo>]` por capítulo) y los PNG opacos (`Gen.cs` → `RunQuestFullBackground`). El tema de un
  resource pack se mezcla con el del mod (nombre del archivo: `ftb_quests_theme.txt`, con guion bajo). Ya no se usan `images:` en los capítulos.
  Verificar en el juego que cada capítulo muestre su fondo; si el selector por ID no funcionara, al menos aplica `[*]`.
- **Shader por defecto:** Complementary Reimagined r5.9.3 + Euphoria Patches 1.10.5 (la carpeta la crea el mod Euphoria Patcher al arrancar;
  `config\iris.properties` apunta a ella y `enableShaders=true`). Preset en `pack\shaderpacks\<nombre del pack>.txt`: perfil Alto + auroras
  (luna llena o nieve), nebulosa nocturna, nubes y agua Reimagined (con olas), charcos con lluvia, follaje interactivo, luz de colores en
  pantalla (MCBL=1), parpadeo suave de antorchas; `SEASONS=0` porque las estaciones las maneja Serene Seasons. Falta ver FPS en PCs medias.
- Packs nuevos: JICKLUS (base rústica, abajo de todo), Fresh Compats, Vocal Villagers, Fresh Waystones, Fresh Flower Pots, Simple Grass Flowers
  (+ compat con Fast Better Grass). Dos archivos venían con `§` en el nombre: se renombraron en su `.pw.toml` (`filename`).
- `sync-instance.ps1` ahora reordena los resource packs según `defaultoptions` cuando llegan packs nuevos (conserva los extra del jugador).

**v0.3.4:**
- **Consejos de las pantallas de carga** = sistema "Pro Tips" de The Aether (también usado por Deep Aether). No se puede apagar, pero sus textos son
  claves de idioma (`aether.pro_tips.line.aether.*` ×57, `...deep_aether.*` ×5, título `gui.aether.pro_tip`): `translations\manual\aether.json`
  las reemplaza con 62 consejos del pack. `build-translations.ps1` ahora deja que lo manual **reemplace** también el español propio de los mods.
  Para agregar más consejos: sumar archivos `assets/aether/tips/<nombre>.json` (formato de Deep Aether: `tip` + `title`) en `recursos\`.
- Navidad/invierno: Macaw's Holidays, Winterly, Snow! Real Magic (+Kiwi), Snow Under Trees, Wintery Atmosphere; pack Christmas Mobs incluido
  pero desactivado (activarlo en diciembre). Ya estaban: Snowy Spirit, Festive Delight, Fairy Lights, Giant Christmas Tree, Create: Wrapped.
- Cubeta de ajolote animada: **no es posible** en 1.21.1 NeoForge (Refined Buckets necesita Modefite vía Sinytra Connector y Hold My Items).
- Arranque con los mods de invierno: OK (103 s, sin errores). Sin probar en juego: Snow! Real Magic junto a Serene Seasons.
- **Distant Horizons + shaders:** no hay alternativa mejor en NeoForge 1.21.1 (Voxy solo existe para Fabric y versiones nuevas). DH 3.3.3,
  Iris 1.8.14 y Sodium 0.8.13 son las últimas. Bryan reporta "errores con shaders" (sin captura). Cambios en `config\DistantHorizons.toml`:
  `vanillaFadeMode = "NONE"` (el shader ya mezcla con `DH_BLENDING`; doble fundido = terreno transparente/fantasma en el borde),
  `enableCaveCulling = false` (huecos en montañas lejanas de Tectonic), `lodChunkRenderDistanceRadius = 96` (antes 128).
  Si siguen los problemas, pedir captura; probar `transparency = "FAKE"`, `enableDhFog = false`, o quitar BadOptimizations.
- Traducción: algunos textos vienen de es_ar (voseo, p. ej. "Mantené" en Inventory Profiles Next); revisar prioridad es_mx/es_es.

**v0.4.0 (sincronizada y probada hasta el menú; falta probar dentro de un mundo):**
- 12 mods nuevos (268 en total) buscando llenar huecos: cocina/bebida (`farmers-respite-unofficial-port`, `brewin-and-chewin`,
  `cooking-for-blockheads`), pesca (`aquaculture`), monturas/transporte (`immersive-aircraft`), armadura cosmética
  (`cosmetic-armor-reworked-forked`), QoL (`easy-shulker-boxes`, `ping-wheel` —Mouse5—, `better-compatibility-checker`),
  optimización (`noisium`, `alternate-current`) y backups (`simple-backups`, config en `config\simplebackups`).
- **Quitados tras probar (no repetir sin versión nueva):**
  - `numismatic-overhaul-neoforged` 4.0.0 (+ `villagerapi`): al crear mundo, `FATAL Mixin apply ... VillagerPackCodecsMixin`
    (`parseTrade` no existe en Villager API 1.26.9.6) mata el hilo del servidor → el mundo se queda "cargando" para siempre.
  - `small-ships` 2.0.2: su `Unloading` handler llama `getFullPath()` sobre un config ya nulo → excepción al detener el servidor.
    Reintentar con 2.0.1 si se quiere.
- `check-deps.ps1` corregido: lee jars anidados en `META-INF/jars` (Easy Shulker Boxes trae `iteminteractions` ahí) y evalúa el
  rango de versión de las incompatibilidades (ya no avisa de Farmer's Delight, Sodium ni T&T).
- Probado: sin dependencias faltantes (306 modIds); menú en **168,7 s** el primer arranque tras los cambios y **101,7 s** el segundo.
  Sin teclas duplicadas en `options.txt`. **Crear mundo y entrar: OK** (sin FATAL tras quitar Numismatic; probar contenido nuevo).
- Ice and Fire: su menú propio (láminas de dragones + splash) se apaga con `customMainMenu = false` en `pack\config\iceandfire\iaf-client.json`
  (antes no existía en el pack y el mod lo generaba con `true`). Mismo archivo copiado a la instancia; requiere reiniciar el juego.
- `saves\Mundo nuevo (3)` quedó a medias del intento fallido; se puede borrar.
- Pendiente de documentar: `lista-de-mods.html` no incluye v0.4.0.

**v0.4.1 (9-nov-2026): rendimiento, tooltips con color, baldes animados:**
- **GC**: ATLauncher pasó de G1 a Shenandoah (`javaParameters` en `configs\ATLauncher.json`; backup `ATLauncher.json.bak-hbd`).
  ZGC generacional se probó y se descartó por el arranque (180 s). Ver la trampa "Arranque / JVM".
- **FPS**: `moreculling` 1.0.11 (+ config `config\moreculling.toml` con `useOnModdedBlocksByDefault = true`).
- **Tooltips con color**: `legendary-tooltips` 1.5.5 + `iceberg` 1.3.2 + `prism-lib` 1.0.11. La config por defecto ya colorea
  borde por rareza (`borders_match_rarity = true`, `level0_entries = ["!epic", "!rare"]`), centra el título y muestra modelos 3D
  en el tooltip (`render_item_model = "EQUIPMENT"`). Si se quiere color en más ítems: agregar selectores en
  `legendarytooltips.toml` → `[client.definitions]` (`level1_entries = [...]` y sus colores en `[client.colors]`).
- **Baldes 3D animados**: `bucket-buddies` (resource pack) reemplaza a `refined-buckets`: el ajolote/pez asoma del balde y
  parpadea, agua y lava animadas. Funciona sin mods (pack_format 15 con `supported_formats`). Ojo: `sync-instance.ps1` solo
  aparta jars viejos de `mods\`; los resource packs quitados hay que borrarlos a mano de `resourcepacks\` y de `options.txt`.
- Arranques medidos (mismo contenido): 120,9 s el primero tras el cambio, 114,7 s el segundo con Shenandoah; sin dependencias
  faltantes (306 modIds). No se midió FPS promedio dentro de un mundo (falta entrar con shaders + DH).

**Pedidos de Bryan pendientes:**
- Verificar en pantalla: fondos del libro de misiones por capítulo, shader dentro de un mundo (que compile, y los FPS), JICKLUS con los mods.
- Misiones "más completas": seguir `MISIONES.md` (capítulos 5–12 de la historia: mazmorras, Twilight, Aether, profundidades,
  dragones, Eternal Starlight, jefes, el Fin; oficios: cocina, construcción, magia, fauna; temporada: Navidad, lunas).
  Ideas: recompensas en cajas de botín (reward tables), misiones de equipo, tareas `kill` de jefes, imágenes/íconos propios.
- `lista-de-mods.html` no tiene aún lo de v0.3.2 (almacenamiento, mundo, packs nuevos).

**Mejorable / configurable:**
- Traducciones: ~24.000 textos en inglés (Simply Swords, Yuushya, Eternal Starlight, Epic Knights `magistuarmory`, menús de
  Villager Recruits, Traveler's Backpack…). Ver `translations\pending` y `report.txt`; priorizar UI y nombres visibles.
- Rendimiento: 240+ mods; vigilar RAM (8 GB) y tiempos. Distant Horizons: ajustar calidad/distancia para PCs medias.
  Voxy (más rápido que DH) **no existe** para NeoForge 1.21.1 (solo Fabric y versiones nuevas).
- Yuushya 16x: decidir con Bryan si se activa (cambia el look de todo el juego). Continuity no sirve en NeoForge sin Sinytra Connector.
- Actions & Stuff: no se puede (necesita mods de Fabric). Alternativas ya puestas: FA Player Extension, Refined Buckets, Not Enough Animations.
- Perros: Refined Dogs reemplaza el modelo del lobo y perdería las animaciones de Fresh Animations → no se agregó.
- Ofrecidos y no agregados: MineColonies (pesado para un servidor de 10), Biomes O' Plenty (se solapa con Terralith/RU).
- Sparse Structures (1.35) y Ice and Fire (sin romper bloques) quizá necesiten ajuste tras jugar.
- Enhanced Celestials 2 funciona (probado por Bryan: luna de sangre, azul y superluna con `/ec setLunarEvent enhancedcelestials2defaultlunarevents:<evento>`),
  pero los avisos del chat salen en inglés ("The Blood Moon rises..."): traducir sus claves en el pack de español.
- Auroras: hoy aparecen hacia el norte (polo en Z≈-10000, `config\auroras\overworld.json`: `equatorZ` 10000, `blocks90` 20000) y no todas las
  noches (`auroras-startup.toml` → `auroraVisibilityOctaves` 1; -1 = todas las noches). Bryan podría querer verlas sobre su base: preguntar.
  Para probar: `/tp @s ~ 200 -8000` (en creativo) + `/time set night` + `/weather clear`. Con `/spreadplayers` usar un rango grande (500), con 1 falla si cae en agua.
- Publicación en Modrinth: no es posible tal cual (FTB y Twilight Forest solo están en CurseForge).
