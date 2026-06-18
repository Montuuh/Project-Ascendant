# UI Design System — The Standards

> The shared foundation every screen inherits. If a screen needs something not defined here, it is
> added here first, then used — never invented per-screen. Maps 1:1 onto a UI Toolkit `theme.uss`
> token sheet + a reusable component USS/UXML library. Extends GDD §10.1.3, §10.4, §10.7, §10.9.

## §1.0 Art direction — "Pokémon Center warmth" (Pillar 5)

> `↻ DECISION (user, this pass):` the UI leans **warm, bright, friendly and rounded** — the cosy
> Pokémon-Center / hand-held-menu feeling — *over* a cold tactical-dark deckbuilder look. This is a
> deliberate steer toward **Pillar 5 (cheerful core)** and is the most-visible change this spec makes
> to existing canon. It revises the **§10.1.3 palette** (currently cold navy) and the radius/tone
> language below. Routed to `art-director` + WCAG re-verification at ratification (CL-023).

**Principles**
- **Warm not cold.** Surfaces are warm cream / soft parchment in the front-end and a warm-neutral
  slate in combat — never the old blue-black. Accents are sunny and candy-bright, not neon.
- **Round not sharp.** Generous corner radii everywhere; panels feel like rounded cards/pills, like
  a Pokédex device. The lone exception is the *skill card* silhouette (see §1.1 corner language).
- **Bright & legible.** High saturation reserved for type identity, HP/affordance, and rewards.
  Backgrounds stay calm so sprites and type colors pop (the readability contract is unchanged).
- **Friendly motion.** Soft springy settles over snappy tactical cuts (still instant for *info*; see
  §1.6). Bouncy, never frantic.
- The regional-accent rule from §10.1.1 still holds: each Region tints the warm base; it never goes
  grim. Volcanic Highlands = "warm and intense," not "dark."

---

## §1.1 Layout grid & spacing

- **Reference canvas:** 1920×1080 logical (matches §10.1.2). UI Toolkit scales by % / `length`
  percentages so it reflows 4:3 → 21:9 (§10.8). Design at this reference; never hardcode pixel
  positions in logic — panels use flex/anchors.
- **Safe area:** 48px outer margin on all base panels (content never touches screen edge).
- **8-point spacing scale** (the *only* allowed gaps/padding values):

  | Token | px | Use |
  |-------|----|-----|
  | `--sp-0` | 0 | flush |
  | `--sp-1` | 4 | icon↔label, tight inline |
  | `--sp-2` | 8 | intra-component padding |
  | `--sp-3` | 12 | list-row inner padding |
  | `--sp-4` | 16 | default gap between components |
  | `--sp-5` | 24 | group separation |
  | `--sp-6` | 32 | section separation |
  | `--sp-8` | 48 | panel margin, zone separation |
  | `--sp-10` | 64 | hero spacing |

- **Radius (warm/rounded, §1.0):** `--radius-sm` 8px (chips/badges), `--radius-md` 14px
  (buttons/list-rows), `--radius-lg` 22px (cards/panels), `--radius-xl` 32px (modals, hero panels),
  `--radius-pill` 999px (toggles, AP pips, tabs). Everything rounder than a stock deckbuilder.
- **Corner language (preserved readability cue, §10.2.2.4):** the *skill card* keeps a **crisper,
  squarer silhouette** (`--radius-md`) while the *consumable card* is **pill-soft** (`--radius-xl`).
  Both are rounder than before, but the silhouette contrast that lets players tell them apart at a
  glance is retained — only now the difference is "soft vs softer," tuned by the art-director.
- **Z-layers (UIRouter):** `0 world/base` · `100 base-panel HUD` · `200 pushed overlay` ·
  `300 modal/confirm` · `400 toast` · `500 tooltip/damage-preview` · `600 transition curtain`.

## §1.2 Color tokens

> `↻ CHANGE to §10.1.3.1` — the **Core UI palette is re-warmed** (see §1.0). The **Type palette
> (§10.1.3.2) is kept verbatim** (those 15 hues are Pokémon-canonical and already cheerful). All new
> warm surface/text values below must be re-verified for **WCAG AA** against type chips and text at
> ratification — flagged for `art-director`.

The game runs **two themes off one token set**: a **warm-light front-end** (menus, hub, shops,
management — the cosy Pokémon-Center feeling) and a **warm-dim combat/map stage** (so 96×96 sprites
and type colors pop). Components reference tokens, never raw hex, so the same UXML renders in either
theme by swapping the token sheet.

### Surfaces & elevation — Front-end (warm light)
| Token | Hex | Use |
|-------|-----|-----|
| `--surface-0` | #FBF4E6 | app background — warm cream |
| `--surface-1` | #FFFDF8 | panel / card — near-white warm |
| `--surface-2` | #F3E7D0 | raised / selected fill — soft parchment |
| `--surface-3` | #EAD9B8 | hovered raised |
| `--ink-primary` | #3A2E22 | headings / key data — warm espresso (not black) |
| `--ink-secondary`| #6B5A45 | body text |
| `--ink-muted` | #A8967C | disabled / watermark |
| `--border-subtle`| #E4D4B5 | dividers, card outlines |
| `--border-strong`| #C9A86A | focused/active outline (warm tan) |
| `--scrim` | rgba(58,46,34,0.55) | modal/pause dim (warm, not black) |

### Surfaces & elevation — Combat/Map stage (warm dim)
| Token | Hex | Use |
|-------|-----|-----|
| `--surface-0` | #2A2230 | stage backdrop — warm aubergine-charcoal (replaces cold navy) |
| `--surface-1` | #3A3040 | HUD panel / hand tray |
| `--surface-2` | #4A3E50 | raised card / tooltip |
| `--surface-3` | #5A4C62 | hovered raised |
| `--ink-primary` | #FFF6EC | headings / key data — warm white |
| `--ink-secondary`| #E0D2C2 | body text |
| `--ink-muted` | #9E8FA0 | disabled / watermark |
| `--border-subtle`| #4A3E50 | dividers, card outlines |
| `--border-strong`| #FFC15A | focused/active outline (warm gold) |
| `--scrim` | rgba(26,18,28,0.66) | modal/pause dim |

> Per-Region the stage `--surface-0/1` are tinted (Verdant = warm green-slate, Coastal = warm
> teal-slate, Volcanic = warm ember-slate) — still warm, never grim (§10.1.1).

### Interaction states (apply to any interactive element)
> `--ink-*` are the warm replacements for the old `--text-*` tokens; `--text-primary/secondary/muted`
> become aliases of `--ink-primary/secondary/muted` so existing §10.x references still resolve.

| State | Treatment |
|-------|-----------|
| default | base surface + `--ink-secondary` |
| hover | surface +1 step, `--ink-primary`, soft 120ms ease |
| pressed | surface −1 step, 0.97 scale (springy settle) |
| focused (kbd/pad) | 3px `--accent-action` rounded outline, offset 2px (always visible — accessibility) |
| disabled | 50% opacity, no pointer, `--ink-muted` (never hidden — `ui.md`) |
| selected | `--accent-action` 4px inset edge + surface +1 |
| danger | `--accent-negative` fill/outline |

### Rarity tints (relics, Legendary picks, loot) — new, derived to harmonize with type palette
| Rarity | Border | Glow |
|--------|--------|------|
| Common | #8A95B0 (`--text-muted`) | none |
| Uncommon | #5CE181 (`--accent-positive`) | soft |
| Rare | #6890F0 (type-water blue) | medium |
| Epic | #A040A0 (type-poison purple) | medium-strong |
| Legendary | #FFD451 (`--accent-action` gold) | strong + animated shimmer (reduced-motion: static) |

### Semantic accents (warmed; `--accent-*` from §10.1.3.1 kept — already bright & cheerful)
`--accent-action` #FFCB3D sunny gold (playable / AP / focus) · `--accent-positive` #54D98A fresh
green (heal / buff) · `--accent-negative` #FF6B6B coral red (damage / faint) · `--accent-warning`
#FF9F45 warm amber (status / Trauma).

### Friendly secondary accents (new — reinforce the Pokémon-Center identity)
`--brand-red` #E84C4C (Poké-Ball red — primary brand mark, New Run / confirm-positive moments) ·
`--brand-blue` #4FA3E3 (soft sky — informational highlights, links, water-UI) · `--brand-cream`
#FFF1D6 (card paper / reward shine). Use sparingly as identity punctuation, not as fills.

## §1.3 Typography

- **Two families:** `--font-display` (pixel-style display face for headings, Pokémon names, numbers
  on cards) and `--font-body` (clean humanist sans for body/longform — readability at small sizes &
  CJK roadmap §10.10). Both must ship en-US glyph coverage; body face must have a CJK fallback hook.
- **Type scale** (base 16px; all sizes are tokens, multiplied by the accessibility text-size factor
  80/100/125/150% from §10.6.1 — **every text element binds the factor, designed to not clip at 150%**):

  | Token | px@100% | Weight | Use |
  |-------|---------|--------|-----|
  | `--type-display` | 40 | display bold | screen titles |
  | `--type-h1` | 30 | display bold | section headers, Pokémon name (combat) |
  | `--type-h2` | 24 | display semibold | card titles, kiosk headers |
  | `--type-h3` | 20 | display semibold | sub-headers, button labels |
  | `--type-body` | 17 | body regular | descriptions, effect text |
  | `--type-small` | 15 | body regular | secondary/meta |
  | `--type-caption` | 13 | body medium | badges, footnotes, counters |
  | `--type-number` | 22 | display bold tabular | HP/AP/damage (tabular figures, no jitter) |

- **Numbers use tabular figures** everywhere they change live (HP, AP, damage preview, counters).
- **Line length:** body text wraps at ≤ 60ch. **Never** rely on overflow clipping for meaning.

## §1.4 Component library

Each component = one UXML template + one USS block, BEM-named (`.pa-button`, `.pa-button--primary`,
`.pa-button__icon`) per §10.7.2. Components are **state-driven via classes**, fed by event-bus data
(§10.7.3) — they never read game state directly (`ui.md`).

### Buttons
| Variant | Use | Look |
|---------|-----|------|
| `--primary` | confirm / advance | `--accent-action` fill, dark text |
| `--secondary` | neutral / back | `--surface-2` fill, light text |
| `--ghost` | tertiary / inline | transparent, 1px `--border-subtle` |
| `--danger` | destructive (abandon run) | `--accent-negative` outline→fill on hover |
| `--icon` | toolbar (Bag/Dex/Team/Pause) | 48×48 square, icon-only, tooltip on hover/focus |

States from §1.2. Min hit target **44×44** (§10.8). Disabled stays visible.

### Panel / Card chrome
- **Panel:** `--surface-1`, `--radius-lg`, 1px `--border-subtle`, `--sp-6` inner padding, optional
  title bar (`--type-h2` + close `--icon` button top-right).
- **Card (generic):** `--surface-2`, `--radius-md`, hover → `--surface-3` + 2px rise.
- **Skill card / Consumable card / Relic card / Pokémon card:** see component specs in §1.4.1–§1.4.4.

### List row
- Height 56px, `--surface-1` (alt rows +2% lightness), `--sp-3` padding, hover/selected per §1.2.
- Slots: `[leading icon/portrait] [title + subtitle] [trailing value/status]`. Used by Pokédex,
  achievements, inventory lists, box list, shop list.

### Tabs / segmented control
- For Inventory (Relics·Consumables·Held), Settings (Display·Audio·Controls), Pokédex filters.
- Underline-style selected tab in `--accent-action`; keyboard left/right cycles; focus ring visible.

### Badge / chip
- `--radius-sm`, `--type-caption`. Variants: type-color, status-condition, rarity, count (e.g.
  `×3`), Trauma (`⚠ ×2`, `--accent-warning`), tier (Familiar/Veteran/Master).
- **Lead crown badge:** small gold crown icon overlaid top-center of the Lead portrait (battlefield +
  map + team screen). 16×16px, `--accent-action` gold with a 1px dark outline for contrast. Not
  interactive; purely decorative status indicator.

### Progress / bars
- **HP bar:** track `--surface-0`, fill gradient (green→amber→red by % thresholds), **phase-threshold
  notch markers** for boss-tier (§4.4.3 / `ui.md`), shield-HP overlay segment (lighter cyan) when
  present (CL-022). Numeric `cur / max` in `--type-number` overlaid right-aligned. **Minimum legible
  width: 80px** (smaller portraits may show bar-only, no text overlay — tap/focus reveals full detail).
- **XP / Trainer-Level bar:** segmented to show milestone ticks (CL-019 token milestones). Each segment
  is a discrete pill; completed segments glow gold briefly before settling to full-lit state.
- **Catch gauge (CL-014):** 0–100 fill, telegraph color shifts to `--accent-positive` at threshold;
  shown in Combat over a wild target. Positioned above the wild's HP bar, ~6px gap, same width.
- **AP pips:** 3 base pips (`--accent-action` lit / `--ink-muted` dim), pill-shaped (`--radius-pill`),
  animate spend with a soft 180ms shrink-fade. When AP > 3 (rare buff state), add pips in a second row
  underneath, max 6 total on-screen (overflow shows `+N` text badge instead of more pips).

### Tooltip & damage-preview (layer 500)
- Tooltip: `--surface-2`, `--radius-md`, `--sp-3` padding, ≤ 320px wide, 120ms fade, 6px rise.
- **Damage preview** = a specialized tooltip bound to `DamageCalculator.Preview(...)` →
  `DamageBreakdown` (§10.7.4). Always shows final value + breakdown + crit% + rider + redundancy
  warning (§10.2.4). Has an **always-on docked mode** (accessibility §10.6.1.4).

### Modal & confirmation
- Centered `--surface-1` panel over `--scrim`. Title + body + `[secondary Cancel] [primary Confirm]`.
- Destructive confirms use `--danger` primary. Esc = Cancel; Enter = Confirm (focus on Cancel by
  default for destructive actions).

### Toast / notification (layer 400)
- Bottom-center, `--surface-2`, auto-dismiss 3s, stacks max 3. For "Relic acquired", "Achievement
  unlocked 🏆", "Autosaved". Subtitle-style; never blocks input.

### Drag-and-drop affordance
- Used by Team/Loadout reorder (§10.3) and any reorderable list. Grab handle (⠿) on the leading
  edge; lifted item gets `--surface-3` + shadow + 1.03 scale; valid drop slots highlight
  `--accent-action` dashed outline; invalid = no highlight. Keyboard equiv: focus + `[ ]` to move.

### Empty / loading / error states
- Every list/grid defines an **empty state** (icon + one line, e.g. "Box is empty"). Loading uses a
  skeleton shimmer (reduced-motion: static "Loading…"). Errors are non-fatal toasts.

## §1.4.1–§1.4.4 Content card anatomies (cross-referenced by screen specs)
- **Skill card** — 🔒 locked anatomy in **`09-component-move-card.md`** (full type-colour theming,
  compressed↔expanded states, owner avatar, AP dots wrapping past 3, category/range/modifier trait
  strip, status-rider corner badge, two distinct unplayable states). Supersedes the old §10.2.3 note.
- **Consumable card** — same frame, rounded chrome, no power footer, "use" verb; 48×48 icon framed.
- **Relic card** — 64×64 icon, rarity-tinted border (§1.2), name, effect text, source tag; used in
  Inventory grid + Starting-Relic / Legendary pick.
- **Pokémon card / portrait tile** — portrait (96×96 battle / 32×32 map), type badge(s), level,
  HP bar, status row, Trauma badge, Lead crown when Lead. Used in Team, Box, Combat, Pokédex.

## §1.5 Iconography system (extends §10.4)

- **Inherit** §10.4 node icons, resource/state icons, status-condition icons verbatim.
- **Rule:** every icon that encodes meaning by color also carries a **shape/pattern** so it reads
  under colorblind modes (§10.6.1.1) — status icons get pattern overlays (stripes/dots), type chips
  get a glyph. Color is never the sole channel.
- **Style:** single-weight, 2px pixel-grid-aligned line icons at 32×32 base; type/status icons may be
  filled. Consistent optical size; no mixed photoreal + line.
- **New icon families this spec introduces** (full list in `06-asset-icon-manifest.md`): toolbar
  icons (Bag, Team, Dex, Settings, Pause, Map), action verbs (Use, Equip, Swap, Evolve, Teach,
  Catch, Release), economy (₽, ⭐ XP, 🪙 Token, relic-slot), navigation (Back ◀, Confirm ✓, Close ✕,
  drag ⠿), rarity frames ×5, tier badges ×3.

## §1.6 Motion language (extends §10.9)

- **Easing:** `--ease-standard` cubic-bezier(0.2,0,0,1) for enters; `--ease-exit` (0.4,0,1,1).
- **Durations:** micro 80ms (hover/press) · short 120–220ms (overlays/popovers) · medium 300ms
  (panel swap) · result 250ms+40ms stagger. (Transition table in `00 §Transition language`.)
- **Choreography principle (Pillar 1 telegraphy):** anything the player must *read to decide*
  (intent, damage preview, catch gauge) appears **instantly / persistently**, never gated behind an
  animation. Flourish animates; information does not.
- **Reduced-motion (§10.6.1.3):** every animation has a defined reduced-motion fallback (mostly
  instant or fade-only). Bars fill instantly; no parallax/shake/particle. This is a per-component
  contract, listed in each component, not a global toggle bolted on after.

## §1.7 Input & navigation model

- **Three input classes, parity required (§10.8 "no hover-only"):** mouse/touch, keyboard, gamepad.
  Every hover reveal has a focus/tap equivalent; every click has a key/pad equivalent.
- **Global bindings (rebindable, §10.6.1.7, two profiles):**

  | Action | Mouse | Keyboard | Pad |
  |--------|-------|----------|-----|
  | Navigate | hover/click | arrows / Tab | d-pad / stick |
  | Confirm | left-click | Enter / Space | A |
  | Back / Cancel | right-click / Close btn | Esc / Backspace | B |
  | Pause | ⏸ btn | Esc (in base) | Start |
  | Inspect/tooltip | hover | hold-focus | trigger |
  | End Turn (combat) | button | Space | Y |
  | Toggle damage-preview dock | button | Tab | RB |

- **Focus model:** UIRouter tracks a focus owner per panel; pushing an overlay traps focus inside it
  and restores prior focus on pop. A visible focus ring (§1.2) is mandatory in all states.
- **The `UIRouter` contract** (the one piece of "UI plumbing" Epic 13 builds first): `Push(panel)`,
  `Pop()`, `Replace(base)`, `ShowModal(...)→Task<bool>`, `Toast(...)`, focus trap/restore, layer
  assignment, transition playback (honoring reduced-motion). All screens are authored against it.

## §1.8 Localization & text rules (restate §10.10 as binding constraints)

- No hardcoded display strings — every label is a loc key (`ui.button.end_turn`, `combat.card.*`).
  Screen specs name the **key namespace**, not literal English, for any new string.
- Layout must tolerate **+40% string length** (DE/FR) and **CJK line-break** without clipping;
  buttons hug content with min-width, never fixed-width text.
- Numbers/dates/currency go through the loc formatter (₽ placement may vary by locale).

## §1.9 What "fits the standards" means (acceptance checklist for every screen)

A screen spec is *done* only if it: ① uses only tokens from §1.1–§1.3 (no ad-hoc px/hex); ② composes
only §1.4 components (or adds a new one here first); ③ states every component's data binding as an
event-bus channel / SO, never a direct system call; ④ lists empty/loading/disabled/focus states;
⑤ has a reduced-motion note; ⑥ has full keyboard + pad navigation; ⑦ names loc-key namespaces;
⑧ cites the GDD § it surfaces; ⑨ passes a Pillar-5 readability skim and the `ui.md` invariants
(damage preview final value, grayed-not-hidden, intent shows slot+occupant, boss HP phase markers).
