# Asset Generation & Acquisition Tracker

> Single source of truth for **what art to make/get, in what order, and how**. Priority-ordered so you
> build toward a convincing **hero screenshot (Combat)** first, then the loop, then meta/polish.
> Method legend: **AI** = generate (Gemini/Imagen) · **FREE** = ripped/CC free asset · **SVG** = Claude
> hand-authors vector · **USS** = no asset, pure UI Toolkit styling. Update the Status column as you go.

> ✅ **Art style LOCKED: Modern Illustrated** (bright cel-shaded anime creature-RPG). Sprites = PokéAPI
> **`official-artwork`** high-res renders (illustrated lane). AI backgrounds use the modern-illustrated
> prompts in `07`. All rows below assume this style.

> ### 🔒 Locked decisions (master designer, 2026-06-14)
> - **Sprite lane = Illustrated** — all battle (96×96) / map (32×32) portraits from PokéAPI
>   `official-artwork`, downscaled + cropped. (Pixel lane rejected.)
> - **Icon production = Hybrid** — **Tabler (MIT)** for ALL nav/system/toolbar/action icons (one
>   family); **Claude-authored SVG** for the game-specific glyphs (type ×15, status ×6, intent ×7,
>   rarity ×5, medals ×4); **game-icons.net (CC BY 3.0)** for node icons, recoloured warm in USS. One
>   art-direction pass over the imported set; all icons pass the 4 consistency gates (§06 production notes).
> - **Damage preview = hover / drag-over-target only**, no persistent dock (drops §10.6.1.4 always-on
>   toggle — flagged for accessibility review at ratification).
> - **Move card = +5 iteration-2 refinements adopted** (see `09 §2`).
> - **Action still open for the master designer:** generate + approve the **Main-Menu vista style key**
>   in the Gemini app, then batch the other 7 backgrounds off it (`07` workflow). This is the last P0.

---

## Priority tiers

- **P0 — Hero screenshot (Combat must look real).** Everything visible in one great combat shot.
- **P1 — Core loop (Map → node → reward).** What a 2-minute play-through touches.
- **P2 — Meta & front-end.** Menu, hub, shops, settings, achievements.
- **P3 — Polish / out-of-VS.** R2/R3, animation flourish, nice-to-have.

---

## P0 — Hero screenshot (do first)

| ID | Asset | Category | Method | Count | Status | Notes |
|----|-------|----------|--------|-------|--------|-------|
| P0-1 | Combat backdrop — Region 1 | Background | AI | 1 | ☐ | `07 #1`; generate after style key |
| P0-2 | VS Pokémon battle sprites (starters + first recruits + Gym ace) | Sprite | FREE | ~8–12 | ☐ | PokéAPI / Showdown (see Sprites §) |
| P0-3 | Type glyphs (the ~8 types in VS roster) | Icon | SVG | ~8–15 | ☐ | Claude authors; warm/rounded |
| P0-4 | Status-condition icons (Burn/Poison/Para/Sleep/Freeze/Confuse) | Icon | SVG | 6 | ☐ | + colorblind patterns |
| P0-5 | Intent icons (Attack/Cleave/Backstrike/Buff/Stall/Status/Unknown) | Icon | SVG | 7 | ☐ | Claude authors |
| P0-6 | Skill-card + consumable-card chrome | Chrome | USS+1tex | — | ☐ | mostly USS; optional paper texture |
| P0-7 | HP bar / AP pips / catch gauge | Chrome | USS | — | ☐ | pure USS (radius+color) |
| P0-8 | Move card art (VS move set) | Sprite | AI/FREE | ~12–20 | ☐ | 144×96; AI small illustrations |
| P0-9 | Fonts (display + body) | Font | FREE | 2 | ☐ | Google Fonts (see Fonts §) |
| P0-10 | Toolbar icons (Bag/Team/Dex/Settings/Pause/Map) | Icon | FREE | 6 | ☐ | Tabler/Kenney, recolor in USS |

## P1 — Core loop

| ID | Asset | Category | Method | Count | Status | Notes |
|----|-------|----------|--------|-------|--------|-------|
| P1-1 | Map backdrop — Region 1 | Background | AI | 1 | ☐ | `07 #2` |
| P1-2 | Node-type icons (Wild/Trainer/Elite/Center/Shop/Dojo/Mystery/Gym/City) | Icon | FREE+SVG | 9 | ☐ | game-icons.net base, tint in USS |
| P1-3 | Pokémon Center backdrop | Background | AI | 1 | ☐ | `07 #3` — warmest scene |
| P1-4 | Map portraits (small 32×32) | Sprite | FREE | =roster | ☐ | downscale from P0-2 |
| P1-5 | Rarity frames (Common→Legendary) | Icon/Chrome | SVG | 5 | ☐ | Claude authors; Legendary shimmer |
| P1-6 | Relic / consumable / held-item icons | Icon | AI/FREE | ~30 | ☐ | AI small icons or game-icons |
| P1-7 | Reward / XP-bar / Trainer-Level bar chrome | Chrome | USS | — | ☐ | USS |
| P1-8 | Action-verb icons (Use/Equip/Swap/Evolve/Teach/Catch…) | Icon | FREE | ~10 | ☐ | Tabler/game-icons |
| P1-9 | Gym arena backdrop — Region 1 (boss stage) | Background | AI | 1 | ☐ | `07 #8`; the Region climax |

## P2 — Meta & front-end

| ID | Asset | Category | Method | Count | Status | Notes |
|----|-------|----------|--------|-------|--------|-------|
| P2-1 | Main-Menu vista | Background | AI | 1 | ☐ | `07 #6` — **make this the STYLE KEY, generate first** |
| P2-2 | Trainer Hub kiosk room | Background | AI | 1 | ☐ | `07 #7` |
| P2-3 | Shop interior | Background | AI | 1 | ☐ | `07 #4` |
| P2-4 | Dojo interior | Background | AI | 1 | ☐ | `07 #5` |
| P2-5 | Medal-tier icons (Bronze/Silver/Gold/Platinum) | Icon | SVG | 4 | ☐ | Claude authors |
| P2-6 | Nav/system icons (Back/Confirm/Close/drag/search/filter/lock/info) | Icon | FREE | 8 | ☐ | Tabler (MIT) |
| P2-7 | Brand mark / logo lockup | Icon | AI/SVG | 1 | ☐ | Poké-Ball-red brand |
| P2-8 | Evolution 8-frame animation | Sprite | FREE/AI | per-evo | ☐ | hard; can defer to P3 |
| P2-9 | Settings / tabs / toggle / slider chrome | Chrome | USS | — | ☐ | USS |

## P3 — Polish / out-of-VS

| ID | Asset | Category | Method | Count | Status | Notes |
|----|-------|----------|--------|-------|--------|-------|
| P3-1 | R2 / R3 combat + map backdrops | Background | AI | 4 | ☐ | out of VS (§10.11) |
| P3-2 | Boss / Gym intro art | Background | AI | ~4 | ☐ | optional drama |
| P3-3 | Sprite attack/idle/faint frames | Sprite | FREE | many | ☐ | Showdown animated sprites if used |
| P3-4 | Particle / VFX sheets | VFX | AI/FREE | — | ☐ | post-VS |

---

## Free asset sources (concrete)

> **Rule of thumb — literal vs original.** For anything that must look like *real* Pokémon (the
> creatures themselves, and game-accurate routes/towns), **download the real free assets** below —
> AI generation cannot reproduce them faithfully or consistently and is the wrong tool. Reserve AI
> (Nano Banana) for **original atmospheric backgrounds** (`07`) and image-editing. Don't try to make
> Nano Banana output literal named Pokémon or specific franchise cities; use the asset rips.

### Environments — routes / cities / tilesets (literal franchise look)
- **The Spriters Resource** (`spriters-resource.com`) — ripped tilemaps + town/route tilesets per game;
  assemble routes/cities from tiles like the originals do.
- **Pokémon Essentials / RPG Maker XP fan tileset packs** (PokéCommunity) — the route/city tilesets fan
  games use. → `Assets/Sprites/Environments/`.
- Use these for accurate Map/route/town art; use AI backgrounds (`07`) only for the stylised
  full-scene stages (combat backdrop, gym arena, menu vista, interiors).

### Pokémon sprites (gray-zone, fine for a portfolio)
- **PokéAPI** (`pokeapi.co`) — per-species sprite URLs, **two flavors**:
  - `sprites/.../front_default` etc. → **pixel sprites** (Gen 3–5 style) → use for the **pixel lane**.
  - `other/official-artwork/front_default` → **high-res Sugimori-style renders** → use for the
    **illustrated lane** (battle portraits, Pokédex).
- **Pokémon Showdown** (`play.pokemonshowdown.com/sprites/`) — clean pixel + animated (gen5ani) sprites,
  easy bulk download by name.
- **Smogon / Sprite resource sites** — full sheets if you want consistency.
- Pick **one** flavor to match the chosen art style (mixing pixel sprites with illustrated backgrounds
  clashes). Drop into `Assets/Sprites/Pokemon/` (kebab-case: `squirtle-front.png`).

### UI buttons / chrome / icons (clean licenses)
- **Kenney.nl** — **CC0** (zero obligation). "UI Pack", "Game Icons", "Board Game Icons", pixel UI
  packs. Best single source for buttons/panels if you don't go pure-USS.
- **game-icons.net** — CC BY 3.0, 4,000+ game line icons (verbs, items, status).
- **Tabler (MIT)** / **Lucide (ISC)** — crisp nav/system glyphs.

### Fonts (Google Fonts, OFL — free, embeddable)
- **Body (warm + rounded):** Nunito, Baloo 2, Quicksand, or Fredoka.
- **Display / pixel:** Silkscreen or Pixelify Sans (pixel lane); or a bold rounded like Baloo for the
  illustrated lane.
- Drop into `Assets/Fonts/`, build UI Toolkit FontAssets.

---

## Generation budget plan
- **AI backgrounds (8 VS scenes + R2/R3/boss in P3):** Gemini app, free with Pro. Generate **P2-1
  (Main-Menu vista) first as the style key**, then style-match the other 7 off it (`07`).
- **Move-art + small item icons (P0-8, P1-6):** if you batch these, the **$10 API credit** is the place
  (~250 Imagen images) — but try the free app first.
- **Everything else:** FREE packs / SVG (Claude) / USS — $0.

---

## Status key
☐ not started · ◐ in progress · ⏳ generated, needs import · ✅ in project. Keep this file updated as
the running checklist; it pairs with `06` (the spec of *what each asset is*).
