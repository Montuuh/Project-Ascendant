# Trainer sprites — VS

Trainer-class sprites for **Trainer** and **Elite Trainer** encounter nodes (the actual fought
trainer's art, distinct from the generic node-marker icon `icon-node-trainer.svg`).

## Folders
- `Trainers/` — regular Gen-1 trainer classes (Hiker, Bug Catcher, Bird Keeper, …)
- `Trainers/Elite/` — elite trainer sprites (tougher variants / boss-tier trainers)

## Naming — two sprites per class
Each trainer class has two pixel-art registers:
- **Battle / VS sprite** (front-facing, **64×64**) → `trainer-<class>.png`
- **Overworld sprite** (3/4 top-down map view, **32×32**) → `trainer-<class>-overworld.png`

Example: `trainer-black-belt.png` + `trainer-black-belt-overworld.png`.

Classes (kebab-case — keep two-word names hyphenated for consistency):
`hiker` · `bug-catcher` · `bird-keeper` · `lass` · `youngster` · `fisherman` · `sailor` · `beauty` ·
`super-nerd` · `rocker` · `gambler` · `juggler` · `black-belt` · `cooltrainer` …

Elite: `Elite/trainer-elite-<name>.png` (+ `-overworld` variant if used).

## Recommended size & import
- **Source from FireRed/LeafGreen-era trainer sprites** (cleaner than RBY) — The Spriters Resource,
  Pokémon GBA sections. Native is small pixel art (~64–80 px).
- **Pad each to a consistent 96×96 transparent square, bottom-aligned**, so every class frames
  uniformly in the encounter UI regardless of the original silhouette.
- Unity import: **Sprite (2D)**, **Filter = Point (no filter)**, **Compression = None**, PPU to taste.
  Display at ~80–120 px (combat) or ~44 px (node preview thumb).

## Note
These are pixel-art; keep them in their own register. (Pokémon battle portraits in `../Portraits/`
are illustrated official-artwork — different register, Bilinear filter. Don't mix the two looks on
the same screen if you can avoid it.)
