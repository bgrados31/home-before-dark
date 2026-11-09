# Plan del Modpack — **Home Before Dark**

> Modpack cooperativo de aventura y misiones para jugar en pareja/amigos.
> "Vuelve a casa antes de que oscurezca": explorar de día, construir un hogar juntos.
> El menú recorre las 4 estaciones y la pantalla de carga sigue la estación real (invierno nov–feb, primavera, verano, otoño),
> así que se siente navideño en diciembre pero dura todo el año.

## Estado (v0.3.1)
- ✅ v0.3.1 — más vida: auroras, lunas (Enhanced Celestials), partículas (Subtle Effects, Particle Effects), animaciones (Not Enough Animations, 3D Skin Layers, Wavey Capes, Smooth Swapping, Visual Workbench, ItemPhysic Lite), color de bioma (Ambient Environment) y 7 texture packs (Fresh Animations Extensions, Better Leaves, Fancy Crops, Better Lanterns, Low On Fire, Visual Armor Trims, Enhanced Boss Bars). Blur+ en menús.
- 🟡 Misiones: generador listo; capítulos 1 (Hogar antes del anochecer) y 2 (El pueblo) hechos — plan completo en `MISIONES.md`.
- 🟡 Servidor: `scripts\make-server.ps1` + carpeta `..\servidor` (194 mods); falta primera prueba de arranque (EULA).
- ✅ Pack cliente con 225 mods, exportable a `.mrpack` (ATLauncher). Carga ~2,5 min (laptop i5 + RTX 4050).
- ✅ v0.3.0 — rollo RPG medieval: clases RPG Series + árbol de habilidades (Pufferfish's Skills), Epic Knights, Simply Swords, Ice and Fire, Villager Recruits, Guard Villagers, Bountiful, Lootr; estructuras (When Dungeons Arise, Repurposed Structures, Medieval Buildings, Structory); dimensiones (Eternal Starlight, Undergarden, Deep Aether); Yuushya Townscape; Navidad (Snowy Spirit, Festive Delight, Fairy Lights, árboles gigantes). Every Compat quitado (14.500 bloques copia, +40 s de carga).
- ✅ Menú principal propio (reemplaza la pantalla de título de Minecraft): ciclo de estaciones animado con sonido, logo, columna de opciones con fuente pixel propia y faroles que se encienden.
- ✅ Pantallas de carga por estación, botones de madera en todo el juego, música y ambientes generados.
- ✅ Mods de rendimiento de carga/FPS + calidad de vida (zoom, F3, luces dinámicas, etc.).
- ✅ Configs ajustadas (ModernFix, Distant Horizons, Serene Seasons, Waystones, FTB Chunks).
- 🟡 Traducción: ~22.000 textos nuevos en español; quedan ~11.500 (sobre todo menús de configuración y nombres únicos).
- ⏳ Árbol de misiones (FTB Quests) en español.
- ⏳ Pack de servidor.

---

## 1. Versión elegida

| Opción | Pros | Contras |
|---|---|---|
| **NeoForge 1.21.1** ✅ | El ecosistema más grande y estable hoy: Create 6 oficial, FTB Quests, Ars Nouveau, Iron's Spells, Twilight Forest, Aether, Cataclysm, Distant Horizons, Sodium/Iris… Es la base de ATM10 y muchos packs grandes. | No es la versión más reciente de Minecraft. |
| NeoForge 26.x (26.1.2 / 26.3) | Lo más nuevo de Mojang. | Create oficial aún "en progreso" para 26.1; muchos mods de aventura todavía no portados. Pack mucho más pobre. |

| Forge 1.20.1 | Muchísimos mods clásicos (Ice and Fire, addons viejos de Create). | Ya es versión "vieja": muchos mods se dejaron de actualizar ahí (p. ej. Handcrafted). Rinde peor. La mayoría de los mods grandes ya están en 1.21.1 (Alex's Mobs tiene port, Mowzie's es oficial). |
| Fabric 1.21.1 | Ligero, ideal para packs "vanilla+". | Create en Fabric es un port que va atrás; muchos mods grandes de aventura/magia son NeoForge primero. Su ventaja de rendimiento ya no existe: Sodium/Iris corren en NeoForge. |

**Decisión: Minecraft 1.21.1 + NeoForge 21.1.x (Java 21).**
"Lo mejor" hoy en contenido = 1.21.1. Si en el futuro el ecosistema 26.x madura, se puede hacer una "temporada 2" del pack.

### Decisiones del grupo (actualizado)
- **PCs medias** → apuntar a ~200 mods, 6–8 GB de RAM para el cliente, shaders medios (Complementary en perfil medio / BSL), Distant Horizons con distancia moderada.
- **Servidor**: lo hospeda Bryan en su PC → Java 21, 8–10 GB de RAM para el servidor, pre-generar el mundo con **Chunky**, conexión con **playit.gg** (sin abrir puertos) o abriendo el puerto 25565.
- **Launcher**: ATLauncher (importa archivos `.mrpack` de Modrinth, incluso con NeoForge).
- **Distribución**: `.mrpack` → primero compartido como archivo; después opcional publicarlo en Modrinth para que las actualizaciones lleguen solas.
  - Por eso se prefieren mods **alojados en Modrinth**; los que solo estén en CurseForge se incluyen solo si su licencia lo permite.
- **Idioma**: todo en español (ver sección 7).

---

## 2. Pilares del pack

1. **Misiones (FTB Quests)** con árbol de capítulos, progreso compartido por equipo y recompensas.
2. **Aventura**: estructuras, mazmorras, jefes y dimensiones nuevas.
3. **Mundo hermoso**: terreno y biomas renovados pensados para verse increíbles con shaders + Distant Horizons.
4. **Progresión**: tecnología (Create) y magia (Ars Nouveau / Iron's Spells) como caminos alternativos.
5. **Vida en el mundo**: cocina, granja, decoración, estaciones del año.
6. **Rendimiento**: que corra bien aun con shaders.

---

## 3. Lista de mods (borrador)

Leyenda: ⚠️ = verificar que exista versión NeoForge 1.21.1 al momento de armar el pack.

### 3.1 Misiones, guías y equipos
- **FTB Quests**, FTB Library, FTB Teams — árbol de misiones con progreso compartido.
- **FTB Chunks** — mapa/minimapa + protección de terreno por equipo.
- **FTB Ultimine** — minar vetas (opcional, muy querido).
- **Patchouli** — libros-guía de los mods.
- **EMI** (+ JEI como compat) — ver recetas y usos.
- **Jade** — info del bloque/mob que miras.
- **Ponder** (viene con Create) — guías animadas.

### 3.2 Generación de mundo (lo que se verá con shaders)
- **Tectonic** — montañas, valles y océanos espectaculares.
- **Terralith** — +95 biomas con estilo vanilla.
- **Incendium** — Nether rediseñado.
- **Nullscape** — End rediseñado.
- **Lithostitched** — librería de compatibilidad de worldgen.
- **Serene Seasons** — estaciones del año (invierno = nieve real en diciembre 🎄 sin hacerlo navideño).

### 3.3 Estructuras y exploración
- **Suite YUNG's** (Better Dungeons, Mineshafts, Strongholds, Desert/Jungle Temples, Ocean Monuments, Witch Huts, Nether Fortress, End Island, Bridges, Extras).
- **ChoiceTheorem's Overhauled Village (CTOV)** — aldeas por bioma.
- **Towns and Towers**.
- **Dungeons and Taverns**.
- **Explorify**.
- **Integrated Dungeons and Structures** ⚠️.
- **Waystones** — viaje rápido entre puntos.
- **Nature's Compass / Explorer's Compass** — encontrar biomas/estructuras (útil para misiones).

### 3.4 Dimensiones y jefes
- **Twilight Forest** — dimensión con progresión de jefes (ideal para capítulo de misiones).
- **The Aether**.
- **Deeper and Darker** — dimensión bajo el Ancient City.
- **L_Ender's Cataclysm** — jefes épicos.
- **Mowzie's Mobs** ⚠️, **Friends & Foes**, **Naturalist** — mobs y fauna.
- **Artifacts** — reliquias en mazmorras.

### 3.5 Combate y magia
- **Better Combat** + **Combat Roll** ⚠️ — combate tipo RPG.
- **Iron's Spells 'n Spellbooks** — magia de hechizos con jefes.
- **Ars Nouveau** — magia creativa/automatización.
- **Curios API** / **Accessories** — ranuras de accesorios.

### 3.6 Tecnología
- **Create 6** + **Ponder**.
- **Create: Steam 'n' Rails** ⚠️ — trenes entre bases.
- **Create Crafts & Additions** ⚠️, **Create Enchantment Industry** ⚠️.
- **Sophisticated Storage** + **Sophisticated Backpacks**.
- **Applied Energistics 2** (opcional, almacenamiento endgame).

### 3.7 Comida y granja
- **Farmer's Delight** + Nether's Delight / End's Delight ⚠️ + Create Slice & Dice ⚠️.
- **Comforts** — sacos de dormir/hamacas para viajes.

### 3.7b Construcción y decoración (grande, a petición del grupo)
**Bloques y variantes**
- **Chipped** — miles de variantes de bloques vanilla.
- **Rechiseled** (+ Rechiseled: Create) ⚠️ — variantes con texturas conectadas.
- **Excessive Building** — bloques extra con estilo vanilla (versión 2026).
- **Framed Blocks** — bloques que copian la textura de otro (formas libres).
- **Create: Copycats+** ⚠️ y **Create Deco** ⚠️ — detalles industriales.
- **Fusion** — texturas conectadas (vidrio, piedra) en NeoForge.

**Muebles y detalles**
- **Macaw's** completo: Furniture, Doors, Windows, Fences & Walls, Roofs, Bridges, Trapdoors, Lights & Lamps, Paths & Pavings, Stairs.
- **Handcrafted** — muebles detallados (activo en 1.21.1).
- **Another Furniture** — muebles estilo vanilla.
- **Decor4All** — muebles para cada tipo de madera (2026).
- **Supplementaries** + **Amendments** — decenas de detalles (jarras, letreros, faroles, mejoras a bloques vanilla).
- **Urban Decor** (opcional, estilo moderno/urbano).
- **Every Compat (Wood Good)** — imprescindible: hace que TODOS los mods de decoración tengan versiones con las maderas de Terralith/BOP/etc.

**Herramientas de construcción**
- **Construction Wand** ⚠️ — colocar filas de bloques de golpe.
- **Effortless Building** ⚠️ o **Building Gadgets 2** — espejos, líneas, paredes.
- **Litematica** (cliente) ⚠️ — hologramas de planos (si hay versión NeoForge).
- **Immersive Paintings** ⚠️ — cuadros con imágenes propias (¡fotos de ustedes en la base!).

### 3.8 Co-op y calidad de vida
- **Simple Voice Chat** — chat de voz por proximidad 💬.
- **Corpse** — tu inventario queda en una tumba al morir.
- **AppleSkin**, **Mouse Tweaks**, **Inventory Profiles Next**, **Controlling**, **Clumps**, **Searchables**.
- **Traveler's Backpack** (opcional).

### 3.9 Rendimiento y visuales
- **Sodium** + **Iris** (NeoForge) — rendimiento + shaders.
- **Distant Horizons** — distancia de renderizado enorme, compatible con shaders (la clave del "se ve hermoso").
- **Lithium**, **ModernFix**, **FerriteCore**, **ImmediatelyFast**, **Entity Culling**.
- **Entity Model Features + Entity Texture Features** + resource pack **Fresh Animations**.
- **Sound Physics Remastered**, **Ambient Sounds** ⚠️, **Falling Leaves**, **Particle Rain** ⚠️.
- Shaders recomendados: **Complementary Reimagined/Unbound** (+ Euphoria Patches), **BSL**, **Bliss**.

Tamaño estimado: **~180–220 mods**. RAM recomendada: 8 GB asignados al cliente (10 GB con Distant Horizons + shaders).

---

## 4. Árbol de misiones (FTB Quests)

Progreso **compartido por equipo** (FTB Teams): ustedes como un equipo, o cada pareja en el suyo.

```
                         ┌─► 4. Cocina y Granja
0. Bienvenida ─► 1. Primeros Pasos ─► 2. Exploración ─┼─► 5. Camino de la Tecnología (Create) ─┐
                                                      ├─► 6. Camino de la Magia (Ars / Iron's) ─┤
                                                      └─► 3. Mazmorras ─► 7. Jefes ──────────────┤
                                                                                                 ▼
                                    8. Dimensiones (Twilight → Aether → Deeper&Darker → End) ─► 9. Leyenda (endgame)

  Capítulos laterales:  ❄ Temporada de Invierno · 🏠 Hogar y Construcción · 🐾 Bestiario · 🎖 Logros del equipo
```

| # | Capítulo | Ejemplos de misiones | Recompensas |
|---|---|---|---|
| 0 | Bienvenida | Abrir el libro de misiones, crear equipo, activar chat de voz | Mochila, comida |
| 1 | Primeros Pasos | Herramientas, primera casa, cama/saco de dormir, Waystone | Bolsas de botín |
| 2 | Exploración | Visitar 10 biomas de Terralith, encontrar una aldea CTOV, una taberna | Brújulas, mapas |
| 3 | Mazmorras | Completar YUNG's Dungeons, Ancient City, fortaleza | Artifacts, equipo |
| 4 | Cocina y Granja | Cocina de Farmer's Delight, 20 recetas, granja de animales | Comidas con buffs |
| 5 | Tecnología | Create: engranajes → molino → tren entre bases | Bloques de Create |
| 6 | Magia | Primer libro de Ars, primer hechizo de Iron's, primer jefe mago | Pergaminos |
| 7 | Jefes | Wither, Ender Dragon, jefes de Cataclysm | Trofeos, loot épico |
| 8 | Dimensiones | Progresión de Twilight Forest (Naga → Lich → … ), Aether | Equipo único |
| 9 | Leyenda | Derrotar todos los jefes, base completa, Netherite total | Título/trofeo final |
| ❄ | Temporada de Invierno | Sobrevivir el primer invierno, construir una cabaña, pesca en hielo, decorar la base, regalos entre jugadores | Decoración, ropa abrigada |
| 🏠 | Hogar | Muebles, base del equipo, jardín | Bloques decorativos |
| 🐾 | Bestiario | Encontrar/derrotar mobs nuevos | Experiencia, botín |

Se acompaña con **tablas de botín** (comunes, raras, épicas) para que las recompensas sean emocionantes.

---

## 5. Cómo se va a construir

1. **Herramienta**: [packwiz](https://packwiz.infra.link/) — el pack vive como archivos de texto en esta carpeta, versionado con git. Exporta a:
   - `.mrpack` (Modrinth App / Prism Launcher) y `.zip` de CurseForge → tus amigos solo importan un archivo.
   - Pack de servidor.
2. **Fases**
   - **Fase 1 – Base**: NeoForge + rendimiento + shaders + worldgen. Probar que carga y se ve bien.
   - **Fase 2 – Contenido**: aventura, magia, tecnología, comida, decoración. Probar compatibilidad.
   - **Fase 3 – Misiones**: escribir los capítulos de FTB Quests (archivos SNBT en `config/ftbquests/quests/`) + tablas de botín.
   - **Fase 4 – Pulido**: configs (estaciones, dificultad, menú principal, iconos), pruebas en servidor, exportar.
3. **Servidor**: recomendado un servidor dedicado (alquilado o en una PC) para que el mundo exista aunque el anfitrión no esté.

---

## 6. Configuraciones por mod (Fase 4)
Archivos en `config/`, `defaultconfigs/` (se copian a cada mundo nuevo) y `options.txt`:
- **Idioma por defecto español**, resource packs activados en orden (traducciones arriba, Fresh Animations).
- **Serene Seasons**: estaciones de 8 días reales de juego, invierno con nieve; comando para fijar invierno en diciembre.
- **FTB Chunks**: 500 chunks reclamables por equipo, sin explosiones en zonas reclamadas.
- **Corpse**: tumba al morir; **Waystones**: costo de XP bajo.
- **Distant Horizons**: preset "medio" para PCs medias; en el servidor, generación lenta en segundo plano.
- **Sodium/Iris**: valores seguros por defecto + shader Complementary preinstalado (desactivado; se activa con una tecla).
- **Mobs**: ajustar spawns para que no se llene de mobs (Alex's/Mowzie's/Cataclysm).
- **Pantalla de inicio**: menú personalizado con el nombre del pack (FancyMenu ⚠️ o Drippy Loading Screen).

## 7. Todo en español
1. **Misiones**: las escribo directamente en español (textos, descripciones, recompensas).
2. **Mods**: casi todos los mods grandes ya traen `es_es`/`es_mx`. Para los que no:
   - Revisar packs existentes ("Traducciones de Mods", "Universal Spanish Translations") — solo si cubren 1.21.1.
   - **Crear nuestro propio resource pack de traducciones**: extraigo el texto en inglés de cada mod que no tenga español y lo traduzco. Queda incluido en el pack y activado.
3. **Guías Patchouli**: se traducen dentro del mismo resource pack.

## 8. Pendiente
- Confirmar instalar las herramientas (packwiz + Java 21).
- Nombre final del pack 😄
- ¿Cuántos jugadores? (afecta la RAM del servidor y la dificultad de las misiones)
