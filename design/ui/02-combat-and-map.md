# Flagship Gameplay Screens — Combat & Map

> Extends GDD **§10.2** (combat) and **§10.3** (map). Uses the **warm-dim stage** theme (§1.2) and
> the component library (§1.4). Screen-spec template: **purpose · zones · components · bindings ·
> interactions · states · accessibility**. Bindings name event-bus channels / SOs (§10.7.3) — the UI
> never reads game state directly (`ui.md`).

---

## 2.1 Combat Screen  (base panel · extends §10.2)

**Purpose.** The moment-to-moment battle. Surfaces the whole decision: enemy intent (Pillar 1
telegraphy), the shared 12-card hand from the Active Team, AP economy, and every Swap consequence
(Pillar 2). Layout, zones, card anatomy, hover preview, intent vocabulary already canon in
§10.2.1–§10.2.5 — this entry **binds** them to data and adds the post-CL states.

**Zones** (`↻ CHANGE to §10.2.1` — side-by-side replaces top/bottom stack): Top status bar (60px) ·
**Enemy formation (RIGHT 40% width, full combat-stage height)** · **Player Active Team formation (LEFT
30% width, full combat-stage height)** · Bottom hand tray (~340px, full width). The side-by-side
layout allows intent arrows to cross the center gap and land in the target slot — a clearer spatial
telegraph than the old vertical stack. A lone boss scales to fill its side.

**Components used.** `HP bar` (boss phase notches + shield-HP overlay) · `AP pips` · `skill card` ·
`consumable card` · `intent chip` · `status badge` · `Trauma badge` · `type badge` · `catch gauge` ·
`damage-preview tooltip` · `icon button` (toolbar) · `toast`.

**Data bindings.**
| UI element | Channel / source |
|-----------|------------------|
| Enemy sprite/intent/HP/status/tier | `EnemyStateChannel`, `IntentRevealedChannel` (§4.4), `PokedexTierSO` |
| Active Team portraits / Lead / HP / status / stages / Trauma / Aura | `ActiveTeamChannel`, `LeadChangedChannel` (§3), `TraumaChannel` (§6.2.5), `LeadAuraChannel` (§5.5.4) |
| Hand (skill + consumable cards) | `HandChannel` (§ Deck), card SOs |
| AP pips / deck / discard counts | `APChannel`, `DeckCountChannel` |
| Damage preview | `DamageCalculator.Preview(card,target) → DamageBreakdown` (§10.7.4, pure) |
| Catch gauge (wild) | `CatchGaugeChannel` → `WildCatchResolver.Catchability` (CL-014) |
| Region mods / relics / field badges (top bar) | `RunStateSO`, `FieldStateChannel` (CL-012) |

**Interactions.** Hover/focus card → docked or popover damage preview (final value always, §10.2.4).
Select target → tap enemy/ally slot (multi-target intents show sweep). Play card → drag-to-target or
click-card-then-target; AP pip animates spend. Manual Swap → drag a bench portrait onto Lead, or
select + Swap action; **swap cost ladder telegraphed before commit** (Pillar 2). End Turn → button;
goes gold when no affordable playable card remains (§10.2.2.4). Catch (wild) → Catch action lights
when gauge = 100 (CL-014).

**States.** *Targeting* (valid slots highlight `--accent-action` dashed, invalid dimmed) · *Unaffordable
card* (60% desaturated, NOT hidden) · *Frozen/asleep Lead* (faint-precedence cues per §3.3.5) ·
*Boss Phase 2/3* (HP crosses a notch → signature-phase banner + `combat_signature_phase_layer` audio) ·
*Resume-into-combat* (CL-022: rebuild from `RunStateDTO`, no animation replay) · *Empty hand* / *no
AP* (End Turn pulses).

**Accessibility.** Damage-preview always-on dock toggle (§10.6.1.4) · status/type icons carry
pattern+glyph not color-only (§1.5) · reduced-motion: instant HP fills, no screen-shake, intent &
preview still instant (info never gated on motion, §1.6) · full keyboard/pad: card row = number keys
1–7 / d-pad, target = arrows, End Turn = Space/Y · pause available mid-Action-Phase (§10.6.1.8).

> `↻ flag` Confirm the **shield-HP overlay** visual (CL-022) and **catch-gauge telegraph** position
> with `systems-designer`/`game-designer` at ratification — both are post-§10.2 additions.

---

## 2.2 Map View  (base panel · extends §10.3)

**Purpose.** Between-combat navigation + the **only** place to change the Active Team/Box loadout
(§2.3, locked on node entry). Shows the branching Region graph, current position, the team, the Box,
and utility access. Layout already canon in §10.3.

**Zones.** Top bar (Region name + ₽ / ⭐ XP / 🎒) · Left column (Active Team + reorderable Box) ·
Right (branching map graph, layered, current node highlighted, future node-type icons §10.4.1) ·
Bottom utility bar (Inventory · Pokédex · Settings · Save & Quit).

**Components.** `Pokémon card/portrait tile` · `drag-and-drop list` (Box) · `node marker` (per §10.4.1
icons, biome-tinted wild) · `HP bar` (mini) · `Trauma badge` · `icon button` · `confirm modal`.

**Data bindings.**
| UI element | Channel / source |
|-----------|------------------|
| Region name / biome tint / layer | `RunStateSO`, `MapGenerationConfigSO` |
| Map graph nodes + edges + current pos | `MapGraphChannel`, `NodeVisitedChannel` |
| Active Team + Box (order, HP, Trauma, level) | `BoxChannel`, `ActiveTeamChannel`, `TraumaChannel` |
| Currency / XP / inventory counts | `RunEconomyChannel`, `MetaProgressionSO` |

**Interactions.** Click a **reachable** node → Node Preview popover (§3.x). Drag Box↔Active to
reorder/swap; **Confirm** commits the loadout (Pillar 2 weight; §2.3) — unreachable while a node is
entered. Lead is the front Active slot, gold-crowned. Bottom bar opens overlays (Inventory/Pokédex/
Settings) via UIRouter push. Save & Quit → confirm modal → autosave (§9.8) → Main Menu.

**States.** *Reachable vs locked nodes* (locked = dimmed, not hidden) · *Current node* (pulse ring) ·
*Trauma'd Box member* (⚠ badge, can still be fielded) · *Loadout dirty* (Confirm button highlights) ·
*Gym layer* (👑 layer visually emphasized as the Region climax) · *Resume* (CL-022: map re-derived by
MapRNG replay — must render identically).

**Accessibility.** Box drag has keyboard equivalent (focus + `[ ]` to move, §1.4 drag spec) ·
node markers carry shape+label not color-only · reduced-motion: instant pan, no pulse · every node
marker is focusable and announces "type + reachable/locked" · loadout Confirm reversible until node
entry (matches §2.3 + the reversibility rule in `00`).

---

## Art-director visual-hierarchy notes (Map View)

### Node-graph spatial clarity
The branching map graph must read as a **navigable tree**, not a tangle. Visual requirements:
- **Edges** (connections between nodes) = 2px `--border-subtle` lines, subtle and recessive.
- **Nodes** = 40×40px circles (or 44×44 if hit-target compliance demands it; add 2px transparent padding).
- **Current position** = 54×54px (1.35× scale), pulsing gold ring (2px `--accent-action`, 1.5s ease-in-out
  loop; static in reduced-motion).
- **Reachable nodes** = full-color icon + white border (2px).
- **Locked/future nodes** = 50% desaturated icon + dashed border (1.5px `--border-subtle`).
- **Visited nodes** = checkmark overlay (12×12px white ✓ on a 60% opacity dark circle, bottom-right corner).
- **Gym/Boss node (layer climax)** = 1.2× scale + gold aura glow (8px `--accent-action` at 20% opacity).

If the graph has > 15 nodes visible at once (deep layer), consider a subtle **depth-fade**: nodes further
from the current layer are 70% opacity, so the local decision (next 2–3 nodes) pops forward.

### Active Team + Box layout (left column)
The left column shows the Active Team (3 slots) + the Box (scrollable list). Vertical space is tight.
**Hierarchy:**
- **Active Team** occupies the top ~180px (3 × 56px portrait cards + 12px gaps).
- The **Lead** slot is visually distinct: +4px gold border, 1.05× scale, and the crown badge (§1.4).
- **Box list** fills the remaining vertical space (~600px at 1080p). Each Box row is 48px (portrait 40×40
  + 4px padding). Scrollable via scrollbar (8px wide, `--surface-3` track, `--accent-action` thumb).
- **Trauma badge** overlays the top-right of any portrait (Active or Box). It's a 20×20px circle with
  `⚠` glyph, `--accent-warning` fill, 2px dark border. If a Pokémon has Trauma ×2, show `⚠2` instead.
- **Fainted** Pokémon in the Box are 60% desaturated + 50% opacity, but NOT hidden (they're still
  drag-reorderable so you can prep your post-Center lineup).

### Box drag-and-drop visual feedback
When dragging a Box portrait onto an Active slot (or reordering within the Box):
- **Lifted card** = 1.08× scale, 6px drop-shadow, `--surface-3` backing.
- **Valid drop target** = 3px dashed `--accent-action` outline, 120ms pulse.
- **Invalid drop target** = no highlight (stays neutral).
- **Snap-back animation** (if dropped on invalid area) = 250ms ease-out spring back to origin.

Keyboard reorder (`[ ]` keys): show the focused card with a 3px `--accent-action` solid ring + a small
floating "⬆⬇ reorder" hint (12px caption text, appears on focus, fades on blur).

### Map-graph backdrop visibility
The map backdrop (Region-1 vista, `08` P1-2) sits behind the graph. It should be **atmospheric, not
busy** — a soft-focus or heavily desaturated scene (70% desaturation, +15% lightness overlay). The node
markers and edges must read clearly against it. If the backdrop is too detailed, add a 40% opacity
`--scrim` rectangle behind the graph canvas (just the graph area, not the whole screen) to create separation.

---

## Cross-screen notes

- Combat and Map are the two **base panels** of the `Run` scene; the UIRouter `Replace()`s between
  them with a 300ms warm cross-fade (instant in reduced-motion).
- Both host the same bottom/utility affordances styled per theme; the **toolbar icon set** (Bag,
  Team, Dex, Settings, Pause, Map) is shared and lives in the asset manifest (`06`).
- Region tinting of `--surface-0/1` (§1.2) is applied at the stage level so both screens inherit the
  Region's warmth automatically.

---

## Art-director visual-hierarchy notes (Combat Screen)

### Intent arrow clarity (P0 — blocks telegraphed-tactics pillar)
The side-by-side layout puts enemy intents on the RIGHT and player slots on the LEFT. **Intent arrows
must cross the center gap and terminate directly in/on the target slot.** Visual requirements:
- Arrow color = `--accent-negative` (damage intents) or `--accent-warning` (status/debuff intents) or
  `--ink-muted` (stall/self-buff).
- Arrow thickness 3px, with a subtle drop-shadow so it reads against any background.
- The target slot gets a thin pulsing outline in the same color (2px, 1.2s ease-in-out loop).
- Multi-target intents (Cleave) show a sweeping bracket or multiple arrows; the bracket is preferred
  (less visual clutter).
- Unknown intent (`❓`) shows a dashed gray arrow with a tooltip on hover/focus: "Unrevealed — raise
  Pokédex tier to see this intent."

### Enemy vs player visual weight
The enemy formation is the **threat** — it should feel visually heavier/larger than the player team even
when sprite sizes are identical. Techniques:
- Enemy portraits sit on a subtly darker `--surface` inset (−5% lightness from base).
- Enemy HP bars are thicker (8px track vs 6px for player).
- Boss-tier enemies get a faint aura glow (their type color at 15% opacity, 12px blur).
- Player portraits feel lighter/closer via a +3% lightness lift on their background cards.

### Status/stage icon row legibility
Status icons (Burn, Poison, etc.) and stat-stage icons (Atk +1, Def −2) sit below HP bars. When a
Pokémon has both, the rows can collide. **Stacking rule:**
- Status conditions = top row (always visible, never hidden).
- Stat stages = second row, capped at 3 visible icons; if > 3 stages, show `+2 more` text chip.
- If total row height exceeds the portrait's vertical space, stat stages wrap or scroll horizontally
  (never clip or hide status conditions).

### Hand tray card density
At 7 cards (5 skills + 2 consumables, worst case), the hand tray must fit without horizontal scroll at
1920px width. Layout math:
- 7 cards × 110px compressed width = 770px.
- Gaps: 6 × 8px = 48px.
- Total: ~820px. Safe margin.
- If consumables and skills are visually separated by a subtle divider (2px `--border-subtle` vertical
  line), add 16px. Still safe.
- **Do not** add a consumable-section background tint — the rounded cream silhouette is enough contrast.
  A tint will break the warm stage backdrop harmony.

### Reduced-motion combat readability check
When animations are disabled:
- HP bar fills = instant jump to new value (no fill animation).
- Intent arrows = always visible, no fly-in (they appear when intents are revealed, per the combat phase).
- Damage numbers = appear instantly above the target, hold 1.2s, fade out over 300ms (this is the only
  allowed motion in reduced-mode for damage — the persistence is needed for readability).
- Status-condition application = icon appears instantly in the status row, with a 2-frame flash (white
  border pulse, 80ms × 2) — this is PEAT-safe and conveys "new condition" without motion.
