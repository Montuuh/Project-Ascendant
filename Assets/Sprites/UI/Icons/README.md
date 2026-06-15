# UI Icons — game-specific SVG set

Original warm/rounded vector glyphs authored for Project Ascendant (the **Claude-SVG** half of the
hybrid icon plan, `design/ui/08`). The generic nav/system/toolbar icons stay **Tabler (MIT)** and node
markers stay **game-icons.net (CC BY 3.0)** — those are *not* in this folder.

## Style spec
- **32×32 viewBox**, single visual weight, **2.2–3px rounded strokes**, rounded joins/caps.
- **Filled** for `Type/` + `Status/` (they carry colour identity). **Line** for `Intent/`, `Trait/`,
  `Modifier/`.
- **Shape carries meaning** so each reads without colour (colorblind requirement, §10.6.1.1).
- Reads from a 48px relic icon down to a 16px card badge.

## Families & files (44)
| Folder | Count | Colour | Notes |
|--------|------:|--------|-------|
| `Type/` | 15 | fixed type palette (§10.1.3.2) | filled silhouettes — normal/fire/water/electric/grass/ice/fighting/poison/ground/flying/psychic/bug/rock/ghost/dragon |
| `Status/` | 6 | fixed condition colour | filled **badge** + a unique **colorblind hatch/dot pattern** per condition + white glyph — burn/poison/paralysis/sleep/freeze/confusion |
| `Intent/` | 7 | **monochrome `#3A2E22`** → tint in USS | attack/cleave/backstrike/buff/stall/status/unknown |
| `Trait/` | 5 | **monochrome** → tint | category: offensive/defensive/utility · range: melee/ranged |
| `Modifier/` | 2 | **gold `#E0A81E`** (movement class) | step-forward / step-backward |
| `Rarity/` | 5 | fixed rarity colour | rounded-square frames; Legendary adds a star |
| `Medal/` | 4 | fixed tier colour | bronze/silver/gold/platinum |

## Naming
`icon-<family>-<name>.svg` (kebab-case), e.g. `icon-type-fire.svg`, `icon-status-burn.svg`,
`icon-intent-attack.svg`, `icon-mod-step-forward.svg`, `icon-cat-offensive.svg`,
`icon-range-melee.svg`, `icon-rarity-legendary.svg`, `icon-medal-gold.svg`.

## Tinting (UI Toolkit)
- **Monochrome** families (`Intent/`, `Trait/`) are authored in `#3A2E22` and meant to be **tinted**
  via USS (`-unity-background-image-tint-color`) to white on coloured chips or to the intent colour.
- **Fixed-colour** families (`Type/`, `Status/`, `Rarity/`, `Medal/`, `Modifier/`) ship their colour
  and are used as-is.
- Import as **Vector** (com.unity.vectorgraphics) → `VectorImage`, or rasterise to Sprite at 2× for
  pixel-snapping. Map portraits/sprites are a separate pipeline (PokéAPI, `design/ui/08`).

## Status
✅ 44 / 44 authored (iteration 1, style approved 2026-06-14). Refine individual paths during the first
Epic-13 build pass if any glyph reads weakly at 16px.
