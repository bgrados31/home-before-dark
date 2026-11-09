# Capítulos de misiones (fuente)

Cada archivo `NN-<clave>.json` es un capítulo de FTB Quests. `scripts\make-quests.ps1` los convierte en
`pack\config\ftbquests\quests\` (no editar esos archivos a mano: se regeneran).

## Reglas importantes

- **Nunca cambies la `key` de un capítulo o misión que ya se jugó**: el ID interno sale de la clave
  (MD5 de `quest:<capítulo>:<misión>`). Cambiarla borra el progreso de esa misión. Para renombrar, cambia `title`.
- Los textos van en español y se copian igual a `lang\es_es.snbt` y `lang\en_us.snbt`
  (FTB Quests 2101.1.38 usa **un archivo por idioma**, no carpetas).
- Verifica que cada ID de ítem exista (se puede buscar en los `lang/en_us.json` de los jars, `item.<mod>.<id>`).

## Formato

```json
{
  "key": "hogar",                 // clave estable del capítulo (nombre del archivo .snbt)
  "group": "historia",            // clave de _grupos.json
  "order": 0,                     // orden dentro del libro
  "icon": "minecraft:campfire",
  "title": "&6Hogar antes del anochecer",
  "subtitle": "texto corto opcional",
  "quests": [
    {
      "key": "mesa",              // clave estable de la misión
      "x": 2.5, "y": 0,           // posición en el mapa del capítulo
      "deps": ["bienvenida"],     // claves del mismo capítulo, u "otro_capitulo:clave"
      "icon": "minecraft:crafting_table",   // opcional (por defecto, el ítem de la tarea)
      "shape": "gear",            // opcional: circle, square, rsquare, diamond, pentagon, hexagon, octagon, heart, gear
      "size": 1.5,                // opcional
      "optional": true,           // opcional
      "title": "Una mesa de trabajo",
      "desc": ["línea 1", "", "línea 3 con &6color&r"],
      "tasks": [
        { "type": "item", "item": "minecraft:crafting_table", "count": 1 },
        { "type": "checkmark", "title": "Texto del botón" },
        { "type": "kill", "entity": "minecraft:zombie", "count": 5 },
        { "type": "advancement", "advancement": "minecraft:story/smelt_iron" },
        { "type": "structure", "structure": "#minecraft:village" },
        { "type": "biome", "biome": "minecraft:cherry_grove" },
        { "type": "dimension", "dimension": "twilightforest:twilight_forest" }
      ],
      "rewards": [
        { "type": "xp", "xp": 20 },
        { "type": "xp_levels", "levels": 5 },
        { "type": "item", "item": "minecraft:bread", "count": 8 }
      ]
    }
  ]
}
```

Después de editar: `make-quests.ps1` → `export.ps1` → `sync-instance.ps1` (con el juego cerrado) → probar en un mundo
(el libro se abre desde el botón del libro arriba a la izquierda del inventario).
