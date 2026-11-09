# Plan de misiones — Home Before Dark

Libro de misiones de **FTB Quests** en español. Las misiones guían, no bloquean: todo el contenido se puede usar sin completarlas (`progression_mode: flexible`). Cada capítulo presenta los mods de una etapa y da recompensas útiles pero no rompen el equilibrio.

**Cómo se construye:** cada capítulo es un JSON en `quests\` → `scripts\make-quests.ps1` genera los archivos de FTB Quests en `pack\config\ftbquests\quests\`. Formato y reglas en `quests\LEEME.md`.

**Estado:** ✅ hecho · 🟡 en progreso · ⏳ pendiente

**Fondos:** cada capítulo puede llevar `"background": { "season": "Autumn", "frame": 30 }` (el valle del menú en esa estación, dibujado por `Gen.cs`). Es el **fondo completo** del libro: `make-quests.ps1` escribe el tema de FTB Quests (`recursos\assets\ftbquests\ftb_quests_theme.txt`, un selector por ID de capítulo + `[*]` por defecto) dentro del pack de español.

## Grupo 1 — Historia principal (el camino del grupo)

| # | Capítulo | Clave | Estado | Contenido |
|---|---|---|---|---|
| 1 | Hogar antes del anochecer | `hogar` | ✅ 15 misiones | Bienvenida, equipo (FTB Teams), reclamar terreno (FTB Chunks), mesa, mochila, herramientas, horno, refugio, antorchas, dormir en cama, siembra (Serene Seasons), olla (Farmer's Delight), hierro, waystones, primera armadura. |
| 2 | El pueblo | `pueblo` | ✅ 7 misiones | Encontrar aldea, comerciar (Trade Cycling), tablón de recompensas (Bountiful), guardias (Guard Villagers), reclutas (Villager Recruits), Lootr, héroe de la aldea (invasión). |
| 3 | Acero y escudo | `armeria` | ✅ 11 misiones | Armas y armaduras medievales: Epic Knights (armaduras de caballero, escudos con heráldica), Simply Swords, Fantasy Armor, combate con Better Combat y Combat Roll, yunque cómodo (Easy Anvils), encantamientos (Easy Magic). |
| 4 | Elige tu camino | `clases` | ✅ 12 misiones | Clases RPG Series: mago, paladín/sacerdote, pícaro/guerrero, arquero; árbol de habilidades (Pufferfish's Skills + Skill Tree); joyas (Jewelry). Una misión por clase, todas opcionales; el grupo se reparte roles. |
| 5 | Mazmorras y ruinas | `mazmorras` | ⏳ | Estructuras: mazmorras YUNG's, Dungeons and Taverns (mapas de taberna), When Dungeons Arise, Medieval Buildings, Towns and Towers, Structory. Tareas de tipo `structure`. |
| 6 | El Bosque del Crepúsculo | `twilight` | ⏳ | Portal y cadena de jefes de Twilight Forest: Naga → Lich → Minotauro → Hidra → Ur-Ghast → Reina de las Nieves. Tareas `dimension` + `kill`. |
| 7 | Más allá de las nubes | `aether` | ⏳ | The Aether + Deep Aether: portal de piedra luminosa, mazmorras de bronce/plata/oro, monturas voladoras. |
| 8 | Bajo la tierra | `profundidades` | ⏳ | Nether (Incendium), The Undergarden, Deep Dark + Deeper and Darker (Otherside, Warden). |
| 9 | Dragones | `dragones` | ⏳ | Ice and Fire: guaridas, huevos, criar un dragón, forja de dragón, otras criaturas (grifos, cíclopes). |
| 10 | La luz de las estrellas | `estrellas` | ⏳ | Eternal Starlight: ruinas del portal, el Guardián, jefes de la dimensión. |
| 11 | Jefes legendarios | `jefes` | ⏳ | L_Ender's Cataclysm y Mowzie's Mobs: cada jefe con su arena; recompensas grandes; pensado para hacer en grupo. |
| 12 | El Fin | `fin` | ⏳ | Fortaleza (YUNG's), dragón del End, Nullscape, ciudades del End, élitros. Cierre de la historia. |

## Grupo 2 — Oficios y guías (opcionales, en paralelo)

| Capítulo | Clave | Estado | Contenido |
|---|---|---|---|
| La cocina | `cocina` | ⏳ | Farmer's Delight y sus extensiones, cultivos por estación (Serene Seasons), comidas con efectos. |
| Almacén y máquinas | `almacen` | ✅ 12 misiones | Storage Drawers, Tom's Simple Storage, Create (con Ponder), AE2 con su guía. Falta: trenes (Steam 'n' Rails), electricidad (Crafts & Additions), autocrafteo de AE2. |
| Construir el pueblo | `construccion` | ⏳ | Yuushya Townscape, Macaw's, Supplementaries, Handcrafted, Chipped, FramedBlocks, varitas y Effortless Building. Misiones de "construye y marca" (checkmark). |
| Magia | `magia` | ⏳ | Iron's Spells (mesa de inscripción, pergaminos, escuelas) y Ars Nouveau (libro de hechizos, glifos, rituales). |
| Campamento y exploración | `campamento` | ✅ 11 misiones | Traveler's Backpack, sacos y hamacas, fogata, brújulas, biomas (River Redux, Regions Unexplored, cerezos), foto con shaders, Hora de aventuras. |
| Criaturas | `fauna` | ⏳ | Alex's Mobs, Naturalist, Friends & Foes: domesticar, montar, encontrar criaturas raras. |

## Grupo 3 — Eventos de temporada

| Capítulo | Clave | Estado | Contenido |
|---|---|---|---|
| Navidad en el pueblo | `navidad` | ⏳ | Snowy Spirit (casas de jengibre, trineos, regalos), Festive Delight, Fairy Lights, árboles de Navidad gigantes, Create: Wrapped. |
| Lunas | `lunas` | ⏳ | Enhanced Celestials: sobrevivir una luna de sangre, aprovechar la luna de cosecha. |

## Reglas de diseño

- Texto en español, corto y con tono cercano; colores con códigos `&` (`&6` dorado para nombres de mods/títulos, `&e` amarillo para avisos, `&a` verde para comandos).
- Una idea por misión. Preferir tareas que se cumplen solas (`item`, `advancement`, `structure`, `biome`, `dimension`, `kill`) y usar `checkmark` solo cuando no hay forma de detectarlo.
- Para ítems de madera/colores, pedir algo que no dependa de la variante (mesa de trabajo, cofre, fogata) o usar un logro de Minecraft.
- Recompensas: XP siempre; objetos útiles para la etapa siguiente; nada que salte progresión (no dar diamantes en el capítulo 1).
- Cada misión importante del grupo (jefes, invasiones) con forma `pentagon` o `diamond` y tamaño 1.5–2.
