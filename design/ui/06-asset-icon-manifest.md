# UI Asset & Icon Production Manifest

> The complete art hand-off for Epic 13. Every icon, sprite frame, panel chrome, and UI texture the
> UI Toolkit screens need. Resolutions follow §10.1.2; pipeline & naming follow the Assets/Sprites/
> conventions and `data-assets.md` (PascalCase `.asset`, kebab-case source). Owner: **art-director**
> (briefs) → human/image-gen production. Style: warm/rounded (§1.0), single-weight line icons on a
> 32×32 pixel grid unless noted; **color is never the only channel** — every meaning-bearing icon has
> a shape/glyph (§1.5, accessibility §10.6.1.1).

> Counts are the production checklist totals. Items marked `(exists §10.x)` are already specified in
> canon and listed here only so the manifest is complete; items marked `NEW` are introduced by this
> spec.

---

## Art-director production notes (2026-06-14)

### Style lock confirmation
The warm/rounded art direction (§1.0) is **locked** for Epic 13. All icon families, chrome, and UI
textures below follow the warm Pokémon-Center aesthetic: generous radii, warm cream/aubergine palette,
friendly line-weight (2px stroke, rounded caps), single optical size. **Do not** introduce sharp/angular
icons or cold blues — the warmth is load-bearing for Pillar 5.

### Icon family consistency checks
Every icon set below must pass these gates before marking "done":
1. **Optical size parity** — all icons in a family (e.g. all 15 type glyphs) should feel the same size
   when placed side-by-side, even if their bounding boxes differ slightly. A wide icon (Fighting 拳)
   and a tall icon (Dragon 竜) should have the same *visual mass*.
2. **2px grid alignment** — all strokes and key vertices snap to a 2px grid at the base artboard size
   (32×32 for most icons). This ensures crisp rendering at 1× and clean scaling at 2×/3× (common on
   high-DPI displays). SVG output should preserve this.
3. **Colorblind-safe pattern overlay** — every icon that conveys meaning via color (type, status, rarity)
   ships a pattern variant (stripes, dots, crosshatch) so it reads in grayscale or under colorblind
   modes. The pattern sits at 30% opacity over the fill in the accessible mode; it's hidden in default
   mode. This is a **non-negotiable accessibility requirement** (§10.6.1.1).
4. **Warm color harmony** — even "cold" types (Ice, Water) use *warm* tints of blue/cyan (add 8–10° hue
   shift toward yellow in HSL space). The type palette (§10.1.3.2) is canonical, but rendering it on
   the warm stage means every chip gets a warm backing or warm-tinted glow. Test all 15 type chips on
   both the warm-light and warm-dim backgrounds before locking.

### Chrome texture production (9-slices)
All panel/card/button chrome is authored as **9-slice SVG** → rasterized at 3× (for high-DPI) → imported
as Sprite (9-slice mode) in Unity. The 9-slice grid is:
- **Corners:** fixed size (e.g. 28×28px for `--radius-lg` panels).
- **Edges:** tile-repeat (for arbitrary width/height).
- **Center:** scale or tile (typically scale for solid fills, tile for textures).

The warm "paper texture" referenced in the card chrome (optional) should be **very subtle** — a 3–5%
noise/grain overlay, never a busy pattern. If it competes with the card art or effect text, cut it.

### Rarity frame shimmer (Legendary only)
The Legendary rarity frame has an "animated shimmer" mentioned in §1.2 and the manifest. Specification:
- A subtle traveling-light effect along the gold border (think a soft specular highlight sliding around
  the frame perimeter, 2.5s loop, ease-in-out).
- Implemented as a CSS `@keyframes` animation in UI Toolkit (a white-to-transparent radial gradient mask
  traveling via `background-position`).
- Reduced-motion mode shows a **static** version: the gold border gets a soft radial gradient (lighter
  gold at top-left, deeper gold at bottom-right) instead of the moving shimmer. Still special, zero motion.
- Do not add sparkle particles or flashing — the shimmer is a *glow*, not a strobe.

---

## A. Theme & chrome (UI Toolkit textures / 9-slices)

| Asset | Spec | Notes |
|-------|------|-------|
| `theme-frontend.uss` token sheet | warm-light tokens (§1.2) | not art, but the paired deliverable |
| `theme-stage.uss` token sheet | warm-dim tokens (§1.2) | Region-tint variants ×3 |
| Panel 9-slice | `--radius-lg`, warm | front-end + stage variants |
| Modal 9-slice | `--radius-xl` | with `--scrim` |
| Card 9-slice (skill) | `--radius-md`, squarer silhouette | type-band region |
| Card 9-slice (consumable) | `--radius-xl`, pill-soft | distinct silhouette |
| Card 9-slice (relic) | rarity-border variants ×5 | Common→Legendary (§1.2) |
| Button 9-slices | primary/secondary/ghost/danger × states | hover/pressed/disabled/focus |
| Tab / segmented / toggle / slider chrome | pill (`--radius-pill`) | §1.4 |
| Tooltip / popover 9-slice | `--radius-md` | layer 500 |
| HP-bar / XP-bar / catch-gauge track + fill | rounded; phase-notch + shield-overlay parts | §1.4, CL-014/CL-022 |
| AP pip (lit / dim) | pill | §10.4.2 |
| Focus ring | 3px rounded, `--accent-action` | accessibility |

---

## B. Iconography

### B1. Type icons — 15 (exists §10.1.3.2 colors) + NEW glyph/pattern overlays
15 Gen-I types. Each needs: a **colored chip** (canon hue) **and** a distinct **glyph + colorblind
pattern overlay** (stripes/dots) so type reads without color. 15 × (chip + glyph + 3 CB-pattern
variants).

### B2. Status-condition icons — 6 (exists §10.4.3) + NEW patterns
Burn, Poison, Paralysis, Sleep, Freeze, Confusion. Each: icon + color tint + pattern overlay.

### B3. Node-type icons — 9 (exists §10.4.1)
Wild (biome-tinted ×N biomes), Trainer, Elite, Center, Shop, Tutor/Dojo, Mystery, Gym, City. Wild
needs per-biome tints.

### B4. Resource / state icons — 6 (exists §10.4.2)
AP ●, ₽, ⭐ Trainer XP, 🪙 Token, ⚠ Trauma, 🏆 Mastery tier. Re-style to warm/rounded line set.

### B5. Toolbar icons — NEW (6)
Bag/Inventory, Team, Pokédex, Settings, Pause, Map. 48×48 button glyphs.

### B6. Action-verb icons — NEW (~10)
Use, Equip, Unequip, Swap, Evolve, Teach, Catch, Release, Heal, Buy.

### B7. Navigation / system icons — NEW (~8)
Back ◀, Confirm ✓, Close ✕, drag-handle ⠿, search, filter, lock (for locked content), info.

### B8. Rarity frames — NEW (5)  &  Tier badges — NEW (3)
Rarity borders Common/Uncommon/Rare/Epic/Legendary (Legendary has animated shimmer + static
fallback). Pokédex tier badges Familiar/Veteran/Master.

### B9. Intent icons — 7 (exists §10.2.5)
Attack, Cleave, Backstrike, Buff, Stall, Status, Unknown ❓. Re-style to warm set; Unknown stays
interactable.

### B10. Medal-tier icons — NEW (4)
Bronze / Silver / Gold / Platinum (achievements, CL-020).

### B11. Brand marks — NEW
Poké-Ball-red logo lockup (Main Menu/Boot), small Trainer-Card emblem, reward "shine" sprite.

---

## C. Pokémon & combat sprites (exists §10.1.2 — listed for completeness)

| Asset | Res | Frames (§10.9.1) |
|-------|-----|------------------|
| Battle portrait (player + enemy) | 96×96 | idle 2 · attack 4 · damaged 1+2 · faint 3 |
| Evolution animation | 96×96 | 8-frame special |
| Map portrait (small) | 32×32 | static |
| Move card art | 144×96 | static, framed |
| Relic icon | 64×64 | rarity-tinted border |
| Held Item icon | 32×32 | equipped-overlay state |
| Consumable icon | 48×48 | in-hand 64×96 framed |

**VS sprite scope (per §10.11 + VS roster):** Starters + their evolution lines + the Region-1
recruit/wild lines + Gym aces (Rock/Water/Bug/Normal per `design/map-redesign-gyms.md`). Exact species
list = the VS Pokédex; pull from `content-designer` roster, not invented here.

---

## D. Backgrounds & ambience

| Asset | Res | Notes |
|-------|-----|-------|
| Combat backdrop — Region 1 | 1920×1080 | parallax-ready, warm (§10.1.2); R2/R3 out of VS |
| Map backdrop — Region 1 | 1920×1080 | single-layer pannable, warm |
| Pokémon Center interior | 1920×1080 | the warmest scene (Pillar 5 tone) |
| Shop / Dojo interiors | 1920×1080 | warm kiosk framing |
| Main Menu vista | 1920×1080 | Region-1 vista, gentle parallax |
| Trainer Hub kiosk room | 1920×1080 | warm 2D kiosk (§6.4, not 3D) |

---

## E. Audio (exists §10.5 — cross-listed so Epic 14 inherits one list)
Combat stems ×6 (§10.5.2), SFX bible (§10.5.3), Region-1 theme + 1 boss track, Center jingle. Full
detail stays in §10.5; not re-specced here.

---

## F. Production totals (UI art, excluding species sprites & audio)

- Chrome/9-slices & bars: ~22 assets (§A)
- Icons: 15 type (+CB variants) · 6 status · 9 node · 6 resource · 6 toolbar · 10 verb · 8 nav · 5
  rarity · 3 tier · 7 intent · 4 medal · brand set ≈ **90+ icon deliverables**
- Backgrounds: 7 (§D)

> **Pipeline rules.** Source art kebab-case (`icon-type-fire.png`); imported sprite `.asset`
> PascalCase; all under `Assets/Sprites/UI/…` by family (`Sprites/UI/Icons/Type/`, `/Status/`,
> `/Node/`, `/Chrome/`, `/Rarity/`). URP 2D import settings per art-director's pipeline doc. Every
> color-coded asset ships its colorblind/pattern variant in the same import batch (§10.6.1.1).
