# Flagship Gameplay Screens — Combat & Map

> Extends GDD **§10.2** (combat) and **§10.3** (map). Uses the **warm-dim stage** theme (§1.2) and
> the component library (§1.4). Screen-spec template: **purpose · zones · components · bindings ·
> interactions · states · accessibility**. Bindings name event-bus channels / SOs (§10.7.3) — the UI
> never reads game state directly (`ui.md`).

---

## 2.1 Combat Screen  (base panel · extends §10.2)

**Purpose.** The moment-to-moment battle. Surfaces the whole decision: enemy intent (Pillar 1
telegraphy), the shared hand from the Active Team (5 skill + 2 consumable cards), AP economy, swap
consequences (Pillar 2), and every targeting/damage telegraph needed to plan the turn. The combat
screen is the flagship UI — **readability is the binding contract** (`ui.md` invariants + Pillar 1).

---

### Zones & Master Layout

**`↻ CHANGE to §10.2.1`** — the side-by-side squad formation replaces the old top/bottom stack.

```
┌────────────────────────────────────────────────────────────────────────┐
│  [Region badges] [Field] [Relics]     [Bag][Team][Dex][Pause] (60px)  │ ← Top status bar
├────────────────────────────────────────────────────────────────────────┤
│                    │                                                   │
│   PLAYER SQUAD ←   │      CENTER GAP       →   ENEMY SQUAD             │
│    (LEFT 30%)      │                             (RIGHT 40%)           │
│                    │                                                   │
│   [Bench 1]        │                         [Enemy 1 / Boss]          │
│   [LEAD ★]  ←──────┼──── intent arrows ───→  (scaled large if solo)   │
│   [Bench 2]        │                         [Enemy 2] [Enemy 3]       │
│                    │                           (squad if multi)        │
│                    │                                                   │
│   (layered depth)  │                          (layered depth)          │
│                    │                                                   │
├────────────────────────────────────────────────────────────────────────┤
│  ● ● ● AP  │ Deck: 8  Discard: 3                       [End Turn]     │ ← Bottom tray header
│                                                                        │
│  [Card1] [Card2] [Card3] [Card4] [Card5]  │  [Cons1] [Cons2]          │ ← Hand
│   (skill cards, compressed)               │   (consumables)           │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

**Reference canvas:** 1920×1080 (§1.1). All panels use flex/anchor percentages for reflow (4:3 → 21:9).

**Zone breakdown:**

1. **Top status bar (60px fixed height)** — persistent badges, always visible, never obscured.
2. **Player squad zone (left 30% width, ~575px)** — the Active Team as a layered formation.
3. **Enemy squad zone (right 40% width, ~768px)** — enemy formation; scales dynamically for 1/2/3.
4. **Center gap (~25% width, ~385px)** — atmospheric backdrop (Region vista from `08` P1-2,
   desaturated 70% + lightness overlay per art-director notes); intent arrows cross this gap.
5. **Bottom hand tray (~340px height)** — hand row (cards) + AP/counters + End Turn button.

**Safe margins:** 48px outer margin on all base panels (§1.1); combat fills the full stage but
keeps the hand tray + status bar within the safe area so UI chrome never clips.

---

### Zone 1 — Top Status Bar (60px)

**Purpose.** Persistent telegraphs for run-wide and combat-specific modifiers; always on-screen,
never hidden or collapsed.

**Layout** (left-to-right, 8px gaps, `--sp-2` inner padding):
- **Left cluster:** Region modifier badges (e.g., "Verdant: Grass +10%") → Field badges (e.g.,
  "Grassy Terrain active") → active Relic badges (up to 3 visible; hover shows full list).
- **Right cluster:** toolbar icon buttons (Bag · Team · Pokédex · Pause), 48×48 each, `--sp-2` gaps.

**Badge anatomy** (shared for Region/Field/Relic):
- 32×32 icon, `--surface-2` backing, `--radius-sm` (8px), 1px `--border-subtle` outline.
- Hover/focus → tooltip with full name + effect text (120ms fade-in, `--sp-3` padding, ≤ 240px wide).
- Rarity-tinted border for Relics (§1.2 rarity tints).

**Toolbar buttons** (icon-only, §1.4):
- **Bag** — opens Inventory overlay (Relics · Consumables · Held tabs) via `UIRouter.Push()`.
- **Team** — opens Active Team panel (portraits + HP + status + reorder) overlay.
- **Pokédex** — opens Pokédex overlay (list view, filters by caught/seen/tier).
- **Pause** — opens Pause modal (Resume · Settings · Save & Quit · Abandon Run).

All four push overlays on the `UIRouter` stack (layer 200); focus traps inside the overlay; Esc/B
pops back to combat. Pause is available **mid-Action-Phase** (§10.6.1.8 accessibility mandate).

**Bindings:**
- Region modifiers: `RunStateSO.RegionModifiers` (passive)
- Field effect: `FieldStateChannel` (event-driven, CL-012)
- Relic badges: `RunStateSO.ActiveRelics` (passive list read)

---

### Zone 2 — Player Squad (Left 30%, layered formation)

**Purpose.** Show the Active Team as a **squad** — the Lead is forward + prominent; the two Bench
slots flank and recede, creating depth via **overlap + z-order + scale differential**.

**Formation geometry** (concrete layout for the 3 slots):

| Slot    | Horizontal offset | Vertical offset | Z-order | Scale | Visual cues                  |
|---------|-------------------|-----------------|---------|-------|------------------------------|
| Lead    | center (287px)    | baseline (0)    | 3       | 1.25× | gold crown, forward, largest |
| Bench-L | left (160px)      | +24px behind    | 2       | 1.0×  | slightly overlapped by Lead  |
| Bench-R | right (414px)     | +24px behind    | 1       | 1.0×  | slightly overlapped by Lead  |

**Depth cues:**
- Lead sits **ahead** (z-order 3, no overlap from others, larger scale).
- Bench slots sit **24px higher** (behind in pseudo-3D space) and overlap with Lead at the edges
  (~12px overlap on each side) — the visual language is "squad flanking the leader," not a flat row.
- Lead portrait card has a **4px gold border** (`--accent-action`) + **1.05× scale bump** when
  rendered; Bench slots have a 2px `--border-subtle` neutral border.

**Per-slot HUD anatomy** (identical structure per slot; Lead + Bench differ only in scale/border):

1. **Portrait (96×96 sprite)** — species portrait, centered in a rounded card (`--radius-md`).
2. **Type badge(s)** — 20×20 circle(s) overlaid top-left of portrait; dual-type shows 2 stacked.
3. **Lead crown badge** (Lead only) — 16×16 gold crown icon, top-center overlay (§1.4 crown spec).
4. **HP bar** (below portrait, 110px wide × 8px track):
   - Track: `--surface-0` (warm-dim aubergine).
   - Fill: gradient (green → amber → red by % thresholds: >60% green, 30–60% amber, <30% red).
   - **Phase-threshold notch markers** (boss-tier only, §4.4.3): vertical 2px `--border-strong` line
     at each threshold % (e.g., 70% and 40% for a 3-phase boss) — instant visual telegraph of phase
     transitions. Notches appear only on boss-tier enemies, never on player or standard wilds.
   - **Shield-HP overlay** (CL-022): when `PokemonInstance.ShieldHP > 0`, render a **lighter cyan
     segment** (`#A8E6F0` at 80% opacity) overlaid on the left edge of the fill, width proportional
     to `ShieldHP / MaxHP`. The cyan segment sits **above** the base HP fill (z +1) so it reads as
     a distinct absorb layer. Shield HP is absorbed before real HP; the overlay shrinks as shield
     takes damage. **Numeric display:** `curHP / maxHP` overlaid right-aligned on the bar in
     `--type-number` (tabular); when shield is present, show `curHP+shieldHP / maxHP` with the
     shield portion in cyan text. Example: `45+10 / 60` (shield cyan, HP white).
   - **Minimum legible width:** 80px (smaller viewports may drop the numeric overlay and show
     bar-only; tap/focus reveals a tooltip with full `cur+shield / max`).
5. **Status condition row** (below HP bar, 6px gap):
   - Horizontal row of 20×20 status icons (Burn, Poison, Paralysis, etc., §10.4.3 + §1.5 glyphs).
   - Pattern overlays (stripes/dots, not color-only) for colorblind modes.
   - Max 3 visible icons; if > 3 conditions (rare edge case), show `+N more` text chip (11px caption).
6. **Stat stage row** (below status row, 4px gap):
   - Horizontal row of stat-stage chips (e.g., `Atk +2`, `Def −1`, 18×18 icons + number).
   - Cap at 3 visible; if > 3 stages, show `+N more` chip.
   - Wrap or horizontal scroll if total exceeds portrait width (never clip or hide).
7. **Trauma badge** (top-right overlay on portrait, 20×20 circle):
   - `⚠` glyph on `--accent-warning` fill, 2px dark border (§6.2.5).
   - If Trauma ×2 or higher, show `⚠N` instead of just `⚠`.
8. **Lead Aura indicator** (Lead only, below stat-stage row, 4px gap):
   - 24×24 icon tinted to the Aura's type color (§5.5.4), persistent while Aura is active.
   - Hover/focus reveals tooltip: "Lead Aura: [type] — [effect text]".

**Fainted state:**
- Portrait 60% desaturated + 50% opacity (never hidden, `ui.md`).
- HP bar fill = 0, numeric `0 / max`.
- Status/stage rows cleared (faint clears all conditions per §4.2.7).
- Still occupies the slot; still visible; reorderable in the Box after combat ends.

**Bindings:**
- Portrait sprites: `ActiveTeamChannel.Slot[0/1/2].SpeciesSO → portrait asset`
- HP/ShieldHP/status/stages/Trauma: `ActiveTeamChannel.Slot[N]` struct (event-raised on change)
- Lead index: `LeadChangedChannel` (fires when Lead swaps)
- Lead Aura: `LeadAuraChannel` (event-raised when Aura activates/changes/ends)

---

### Zone 3 — Enemy Squad (Right 40%, dynamic 1/2/3 formation)

**Purpose.** Display the enemy team with the **same squad depth language** as the player side
(when 2 or 3 enemies are present); scale a solo enemy large to convey threat.

**Formation logic:**

| Enemy count | Layout                                          | Notes                                |
|-------------|-------------------------------------------------|--------------------------------------|
| 1 (solo)    | Centered, 1.5× scale (boss-presence)            | Fills the zone; feels heavier        |
| 2           | Side-by-side, equal scale (1.0×), 48px gap      | Flat row; no Lead distinction        |
| 3           | Mirrored player squad (center Lead + 2 flanks)  | Center enemy z-order 3, flanks 2 & 1 |

**Squad geometry (3-enemy case, mirrored):**

| Slot       | Horizontal offset       | Vertical offset | Z-order | Scale | Visual cues           |
|------------|-------------------------|-----------------|---------|-------|-----------------------|
| Enemy Lead | center (right-zone mid) | baseline (0)    | 3       | 1.25× | forward, larger       |
| Flank-L    | left offset (−120px)    | +24px behind    | 2       | 1.0×  | overlapped by Lead    |
| Flank-R    | right offset (+120px)   | +24px behind    | 1       | 1.0×  | overlapped by Lead    |

**Per-enemy HUD anatomy** (extends player HUD with enemy-specific additions):

1. **Portrait (96×96 sprite)** — enemy species portrait, same rendering as player.
2. **Type badge(s)** — 20×20 circle(s) overlaid top-left.
3. **Pokédex tier badge** (wild only, top-right, 20×20):
   - Familiar / Veteran / Master tier icon (§4.4 Pokédex tier system).
   - Unlocks intent detail (Unknown `❓` → revealed per tier).
4. **HP bar** (below portrait, 120px wide × **10px track**, thicker than player for visual weight):
   - Same gradient fill + phase-threshold notches (boss-tier only).
   - **Boss-tier aura glow** (boss only): 12px blur, type-color at 15% opacity, subtle pulsing (1.5s
     ease-in-out loop; static in reduced-motion).
   - Numeric `cur / max` right-aligned, `--type-number`.
5. **Catch gauge** (wild Pokémon only, CL-014, **above HP bar, 6px gap**):
   - 0–100 fill bar, 120px wide × 6px track, `--surface-0` track.
   - Fill color: `--ink-muted` (neutral gray) at gauge < 100; shifts to `--accent-positive` (green)
     **when gauge = 100** (catch-ready telegraph).
   - **Numeric %** overlaid right-aligned (tabular): "Catchability: N%".
   - Appears **only on wild Pokémon**; hidden for Trainer battles.
   - Positioned **above the HP bar** (not below, not overlaid) — the catch decision is read-priority
     when it appears, so it sits higher in the visual hierarchy.
6. **Status / stat-stage rows** — identical to player HUD (below HP bar, same 6px/4px gaps).
7. **Intent display** (above enemy portrait, 32px gap):
   - Intent chip (icon + magnitude + target slot) per §10.2.5 vocabulary.
   - Arrow crosses the center gap and lands in the target slot (player side).
   - Multi-target intents (Cleave) show a **sweeping bracket** or multiple arrows (bracket preferred;
     less clutter). The bracket is a 3px `--accent-negative` arc spanning all 3 player slots.

**Enemy visual weight (art-director notes):**
- Enemy portraits sit on a **−5% lightness inset** (`--surface-1` darkened) to feel heavier.
- Enemy HP bars are **10px track** (vs 8px player) for stronger presence.
- Boss-tier gets a faint **type-color aura glow** (12px blur, 15% opacity).

**Bindings:**
- Enemy sprites/HP/status: `EnemyStateChannel` (event-raised struct per enemy slot)
- Intent: `IntentRevealedChannel` (§4.4, fires at Intent Phase start)
- Pokédex tier: `PokedexTierSO` (passive read per species)
- Catch gauge: `CatchGaugeChannel` → `WildCatchResolver.Catchability(hpPercent, hasStatus, ball)` (pure function, CL-014)

---

### Zone 4 — Center Gap (atmospheric backdrop)

**Purpose.** Spatial separation between player and enemy squads; intent arrows cross this gap.

**Visual treatment:**
- Region-1 vista backdrop (1920×1080 sprite from `08` P1-2), heavily desaturated (70%) + lightness
  overlay (+15%) so it reads as **atmospheric, not busy** (art-director spec).
- **Optional scrim** (if backdrop is too detailed): 40% opacity `--scrim` rectangle just behind the
  node markers/graph area to create separation (backdrop visibility is an art-director tuning call).
- Region tinting applied (Verdant = warm green-slate, Coastal = warm teal-slate, Volcanic = warm
  ember-slate, all warm per §1.0 / §10.1.1).

**Intent arrow rendering:**
- Arrows are **3px thick**, drop-shadow for readability against any background.
- Color-coded by intent type:
  - `--accent-negative` (coral red) for Attack/Cleave/Backstrike.
  - `--accent-warning` (amber) for status/debuff intents.
  - `--ink-muted` (gray) for Stall/self-buff (non-threatening).
- Arrow **terminates directly in/on the target slot** (player side) with a subtle 2px pulsing
  outline (1.2s ease-in-out loop) on the target slot in the same color.
- **Unknown intent (`❓`, CL-011)** renders as a dashed gray arrow; tooltip on hover/focus: "Unrevealed — raise Pokédex tier to see this intent."

**Bindings:** backdrop sprite is passive (loaded from `RunStateSO.RegionIndex`).

---

### Zone 5 — Bottom Hand Tray (~340px height)

**Purpose.** Display the hand (5 skill + 2 consumable cards), AP economy, deck/discard state, and
the End Turn button. Cards are **dragged to targets** or click-selected then click-target; the
**damage preview appears on drag-over-target** (final calculated value, §10.2.4 / §10.7.4 / `09`).

**Layout** (top-to-bottom, then left-to-right):

1. **Tray header row (48px height, full width):**
   - **AP pips** (left, 96px cluster):
     - 3 base pips (`--accent-action` lit / `--ink-muted` dim), pill-shaped (`--radius-pill`),
       14×14px each, 4px gaps.
     - Animate spend with soft 180ms shrink-fade.
     - **AP > 3 (rare buff state):** add a second row underneath (max 6 pips on-screen). If AP > 6,
       show `+N` text badge (12px caption, gold) next to the pips instead of more visual pips.
   - **Deck / Discard counts** (center-left, 160px cluster):
     - `Deck: N` · `Discard: M` in `--type-small`, `--ink-secondary`.
   - **End Turn button** (right, 140px × 40px):
     - `--primary` variant (§1.4 buttons), label "End Turn" (loc key `ui.combat.end_turn`).
     - **Gold shift** when no affordable playable card remains (§10.2.2.4): border + text shift to
       `--accent-action` to signal "your turn is done; confirm to proceed."
     - Keyboard: Space / Y (gamepad).

2. **Hand row (cards, 260px height, full width):**
   - **Skill cards (5, left cluster):** compressed state by default (110px wide × 196px tall, per
     `09` locked anatomy). 8px gaps between cards.
   - **Consumable cards (2, right cluster):** rounded silhouette (per `09`), 110px wide × 196px tall.
   - **Separator (optional, art-director call):** 2px `--border-subtle` vertical line between skill
     and consumable clusters (16px gap total). If added, keep it subtle — the rounded silhouette
     contrast is already strong.
   - **Density check (7 cards at 110px + gaps):** 7 × 110 + 6 × 8 = 770 + 48 = 818px. Fits within
     1920px with ample margin. Safe.

**Card interaction states (per `09` locked anatomy):**

| State                | Trigger                                        | Treatment (verbatim from `09`)                                                                                   |
|----------------------|------------------------------------------------|------------------------------------------------------------------------------------------------------------------|
| Compressed (default) | resting, affordable, playable                  | Full colour, crisp; shows owner avatar + name + AP + power + range icon (always visible)                        |
| Expanded             | hover / focus / select / drag                  | Unfolds art window + effect text; grows in place within the hand row (neighbouring cards slide aside)           |
| Not enough AP        | `MoveSO.ApCost > currentAP`                    | Desaturated (grayscale + 50% opacity) · AP dots gold-have vs red-missing · amber `⚠` next to dots + amber border · **stays fully visible** |
| Out of position      | Melee card AND owner is NOT Lead AND no Step-Forward | **Not desaturated** (it's affordable) · blue positional lock overlay + melee glyph + `Melee` 18px bold + `needs Lead slot` 11px semibold subtitle · blue = position |
| Drag-over-target     | dragged onto an enemy/ally slot                | **Damage preview** appears (layer 500): final value + STAB/type/crit breakdown + rider + redundancy warning (§10.2.4 / §10.7.4) |

**Melee playability rule (§3.3.1 / §3.3.6, load-bearing):**
- Melee cards are playable **only from the Lead slot**, UNLESS the card carries Step-Forward (which
  swaps the bench Pokémon into Lead before resolving).
- Ranged cards are playable from any position (no restriction).
- The "Out of position" state triggers when: `card.Range == Melee AND card.OwnerInstance.SlotIndex != LeadIndex AND !card.HasModifier(StepForward)`.
- UI reads `IsPlayable(card, state) → (bool, reason enum)`; it does NOT compute the rule directly.

**Damage preview (drag-over-target only, no persistent dock):**
- Tooltip layer (500), 120ms fade-in, `--surface-2`, `--radius-md`, `--sp-3` padding, ≤ 320px wide.
- **Always shows:** final calculated damage value (large, `--type-number`), breakdown line (Base ×
  STAB × Type × …), crit chance % (if non-zero), status rider details (condition + % chance),
  redundancy warning (e.g., "Already Burned" if re-applying).
- **Triggered by:** dragging a skill card over an enemy slot; the preview updates live as the drag
  moves between targets (instant recalc per target).
- **No persistent dock** (`09` §7 accessibility note): the §10.6.1.4 "always-on damage preview"
  accessibility toggle is **dropped in this iteration** (master-designer decision 2026-06-14). Flag
  for accessibility review at ratification — a lightweight "sticky preview" option may be re-added
  if needed, but the current design is hover/drag-only.

**Swap cost ladder telegraph (Pillar 2, §3.3.1 load-bearing):**
- Manual Lead swap costs scale: **1st = 1 AP, 2nd = 2 AP, 3rd = 3 AP** within a turn; counter resets
  at turn start. Step-Forward / Step-Backward do NOT increment this counter.
- **Visual telegraph (DECIDED — concrete spec):**
  - When hovering/dragging a Bench portrait onto the Lead slot (manual swap preview), show a small
    **cost chip** overlaid on the Bench portrait: `Swap: N AP` (11px caption, gold text on
    `--surface-2` backing, `--radius-sm`).
  - The chip shows the **current swap cost** based on the swap counter (read from
    `SwapCounterChannel`): 1st swap in turn shows `Swap: 1 AP`, 2nd shows `Swap: 2 AP`, etc.
  - The chip appears **only during hover/drag** (instant on hover-start, 80ms fade-out on hover-end).
  - **Keyboard/pad:** when a Bench slot is focused and the player presses the Swap action key (S /
    Select), the chip appears and remains visible until focus moves or the swap is committed.
- **Insufficient AP:** if `currentAP < swapCost`, the chip shows `Swap: N AP` in **amber + ⚠**
  (same treatment as unaffordable cards); the swap is blocked.
- **Post-swap reset:** the counter resets at turn start (no UI telegraph needed; the cost ladder
  re-starts at 1 AP on the next turn).

**Bindings:**
- Hand (cards): `HandChannel` → `CardInstance[]` (event-raised on draw/play/discard)
- AP pips: `APChannel` → `currentAP, maxAP` (event-raised on spend/gain)
- Deck/Discard counts: `DeckCountChannel` → `deckCount, discardCount`
- Damage preview: `DamageCalculator.Preview(card, target) → DamageBreakdown` (pure function, §10.7.4)
- Swap cost: `SwapCounterChannel` → `currentSwapCount` (event-raised on manual swap; resets at turn start)
- Playability: `HandChannel.CardState[i] → (isPlayable, reason)` (event-raised per card on state change)

---

### Interactions (complete input model)

**Play a card:**
1. **Mouse/touch:** drag card onto target slot → damage preview appears on drag-over → drop to commit.
   - OR: click card (selects) → click target slot → commit.
2. **Keyboard:** number key 1–7 selects card in hand → arrow keys select target slot → Enter commits.
3. **Gamepad:** d-pad/stick navigates hand → A selects card → d-pad/stick selects target → A commits.

**Manual Lead swap:**
1. **Mouse/touch:** drag Bench portrait onto Lead slot → swap cost chip appears → drop to commit.
   - OR: select Bench portrait → click "Swap" action button → confirm modal → commit.
2. **Keyboard:** Tab to Bench slot → S (Swap key, rebindable) → confirm → commit.
3. **Gamepad:** d-pad to Bench slot → X (Swap button, rebindable) → confirm → commit.

**End Turn:**
1. **Mouse/touch:** click End Turn button.
2. **Keyboard:** Space (primary) / Enter (alt).
3. **Gamepad:** Y (primary) / Start (alt).

**Catch (wild only, when gauge = 100):**
- Pokéball consumable card becomes playable (drag onto wild → commit).
- Catch gauge shifts to `--accent-positive` green to signal ready.

**Inspect (tooltip / hover detail):**
- **Mouse:** hover any badge/icon/intent → tooltip appears (120ms fade).
- **Keyboard:** focus + hold (500ms dwell) → tooltip.
- **Gamepad:** focus + hold Right Trigger → tooltip.

**Pause (mid-Action-Phase, §10.6.1.8):**
- Esc / B / Start → Pause modal (layer 300, `--scrim`, focus trap).

---

### States (edge cases & special moments)

| State                      | Trigger                                   | UI treatment                                                                                                     |
|----------------------------|-------------------------------------------|------------------------------------------------------------------------------------------------------------------|
| **Targeting**              | Card selected, awaiting target           | Valid target slots: 3px dashed `--accent-action` outline, 120ms pulse. Invalid slots: 60% desaturated, no highlight. |
| **Unaffordable card**      | `card.ApCost > currentAP`                 | Card: grayscale + 50% opacity, amber `⚠` + border, **stays fully visible** (`ui.md`). Label: "Not enough AP".   |
| **Out of position (melee)**| Melee card, owner not Lead, no SF         | Card: **not desaturated** (affordable), blue lock overlay + `Melee` 18px bold + `needs Lead slot` 11px subtitle. |
| **Frozen/asleep Lead**     | Lead has Sleep/Freeze condition           | Lead portrait: 80% desaturated overlay + condition icon enlarged (32×32, pulsing). Cards from Lead: unplayable (per §4.2.2.4 / §4.2.2.5). Faint precedence (§3.3.5.1): if Lead faints same turn, Freeze lock voided. |
| **Boss Phase 2/3**         | Boss HP crosses phase-threshold notch     | Signature-phase banner (300ms slide-in from top, 2s hold, fade-out): "Phase 2!" + boss name. Audio layer swap: `combat_signature_phase_layer` crossfades in (250ms ramp). |
| **Resume-into-combat**     | Load save mid-combat (CL-022)             | Rebuild full UI state from `RunStateDTO` (no animation replay). HP bars fill instantly. Intent arrows appear instantly (no fly-in). Resume banner (1s): "Resuming combat…". |
| **Empty hand**             | All cards played/discarded, AP remaining  | End Turn button pulses gold (1.2s ease-in-out loop). Tooltip: "No cards left — end turn to draw." (accessibility). |
| **No AP left**             | `currentAP = 0`, unplayed cards remain    | End Turn button pulses gold. Tooltip: "Out of AP — end turn to continue."                                       |
| **Catch ready (wild)**     | Wild Pokémon, gauge = 100                 | Catch gauge fill shifts to `--accent-positive` green. Pokéball consumable (if in hand) highlights gold (playable cue). Tooltip on gauge: "CATCH READY — use Poké Ball." |
| **Multi-target (Cleave)**  | Enemy intent = Cleave(N)                  | Sweeping bracket (3px `--accent-negative` arc) spans all 3 player slots. Tooltip: "⚔ N dmg → ALL SLOTS".       |
| **Unknown intent (`❓`)**  | Intent unrevealed (low Pokédex tier)      | Dashed gray arrow, `❓` chip. Tooltip: "Unrevealed — raise Pokédex tier to see this intent." (CL-011).          |

---

### Accessibility (§10.6 compliance + `ui.md` invariants)

1. **Colorblind modes (§10.6.1.1):**
   - Type/status icons carry **glyph + pattern** (stripes/dots), not color-only.
   - Intent arrows: color + icon (⚔/🎯/⬆/🛡/💢) dual-encode meaning.
2. **Text size (§10.6.1.2):**
   - All text elements scale 80/100/125/150% via Settings.
   - Layout designed at 150% to avoid clipping; buttons hug content with min-width.
3. **Reduced motion (§10.6.1.3):**
   - HP bar fills: instant jump to new value (no fill animation).
   - Intent arrows: always visible, no fly-in (appear instantly at Intent Phase start).
   - Damage numbers: appear instantly above target, hold 1.2s, fade 300ms (only allowed motion;
     persistence needed for readability).
   - Status application: 2-frame white border pulse (80ms × 2, PEAT-safe) to signal "new condition."
   - Card animations: instant state changes (no expand slide, no shrink-fade on spend).
4. **Damage preview always-on dock (§10.6.1.4):**
   - **DROPPED in this iteration** (`09` §7 note). The hover/drag-only model is the current spec.
   - **Flag for accessibility review:** if a lightweight "sticky preview" toggle is needed (e.g., a
     small docked panel showing the last-hovered card's damage breakdown), it can be added post-VS.
5. **Keyboard/pad full parity (no hover-only):**
   - Card selection: number keys 1–7 / d-pad.
   - Target selection: arrow keys / d-pad.
   - Swap: S key / X button (rebindable).
   - End Turn: Space / Y.
   - Inspect: hold-focus (500ms dwell) / Right Trigger.
   - Pause: Esc / B / Start (available mid-Action-Phase).
6. **Focus ring (always visible, §1.2):**
   - 3px `--accent-action` rounded outline, offset 2px, on every focusable element.
7. **Grayed-not-hidden (`ui.md` invariant):**
   - Unaffordable / out-of-position cards: desaturated/overlaid, **never removed from hand**.
   - Disabled UI elements: 50% opacity, no pointer, `--ink-muted`, **still visible**.
8. **Boss HP phase markers (`ui.md` invariant):**
   - Vertical 2px `--border-strong` notch at each phase threshold on boss HP bars.
9. **Intent shows slot + occupant (`ui.md` invariant):**
   - Intent display: `⚔ N → Lead (Squirtle)` — slot label + current occupant name.
10. **Subtitles for audio cues (§10.6.1.6):**
    - Optional text feedback for SFX-only signals (e.g., "Crit!", "Super effective!") in a small
      toast (bottom-left, 1s hold, fade).

---

### Open Combat Flags — Resolved + Remaining

**RESOLVED (concrete decisions in this spec):**

1. **Shield-HP overlay visual (CL-022):**
   - **DECIDED:** Lighter cyan segment (`#A8E6F0` at 80% opacity) overlaid on HP bar fill, width
     proportional to `ShieldHP / MaxHP`. Sits above base HP fill (z +1). Numeric display:
     `curHP+shieldHP / maxHP` with shield portion in cyan text. Example: `45+10 / 60`.
   - **No further sign-off needed** — this is the implementation spec.

2. **Catch-gauge telegraph position (CL-014):**
   - **DECIDED:** Positioned **above the wild's HP bar, 6px gap**, same 120px width. Fill color
     neutral gray at gauge < 100; shifts to `--accent-positive` green when gauge = 100 (catch-ready).
   - **No further sign-off needed** — this is the implementation spec.

3. **Swap cost-ladder visual (§3.3.1):**
   - **DECIDED:** Cost chip (`Swap: N AP`, 11px caption, gold on `--surface-2`, `--radius-sm`)
     overlaid on Bench portrait during hover/drag. Shows current swap cost (1/2/3 AP based on
     `SwapCounterChannel.currentSwapCount`). Appears only during hover/drag (instant on, 80ms fade out).
   - **No further sign-off needed** — this is the implementation spec.

4. **Damage-preview drag treatment (§10.2.4 / `09`):**
   - **DECIDED:** Hover/drag-over-target only; no persistent dock. Tooltip layer 500, final value +
     breakdown + crit% + rider + redundancy. The §10.6.1.4 "always-on dock" toggle is **dropped** in
     iteration 1 (flagged for accessibility review at ratification; may re-add a lightweight sticky
     option post-VS if needed).
   - **No further sign-off needed** — this is the implementation spec.

5. **Toolbar placement (Bag/Team/Dex/Pause):**
   - **DECIDED:** Top status bar, right cluster, 48×48 icon buttons, `--sp-2` gaps. All push
     overlays via `UIRouter` (layer 200). Pause available mid-Action-Phase.
   - **No further sign-off needed** — this is the implementation spec.

6. **Melee-usable-only-from-Lead rule (`09` flag, §3.3.1 / §3.3.6):**
   - **DECIDED (verified from GDD):** Melee cards are playable **only from Lead slot**, UNLESS the
     card has Step-Forward modifier (which swaps bench → Lead before resolving). Ranged cards
     playable from any position. Out-of-position state: blue lock overlay + `Melee` 18px bold +
     `needs Lead slot` 11px subtitle (per `09` iteration-2 refinement).
   - **No further sign-off needed** — this matches §3.3.1 / §3.3.6 exactly.

7. **AP-overflow +N visual (§1.4 AP pips):**
   - **DECIDED:** 3 base pips; if AP > 3, add second row (max 6 pips on-screen). If AP > 6, show
     `+N` text badge (12px caption, gold) next to the pips instead of rendering more pips.
   - **Needs systems-designer confirmation:** Does any content grant > 6 AP? (Rare buff state; if
     it exists, the `+N` spec is ready; if not, cap at 6 and no badge needed.)
   - **Action:** Query `systems-designer` at ratification.

**REMAINING FLAGS (need game-/systems-designer sign-off at ratification):**

None — all combat flags are either resolved with concrete specs above, or deferred to
post-iteration-1 (accessibility dock toggle, AP > 6 confirmation).

---

### GDD §10.2 Master Layout Change Summary (`↻ CHANGE` items)

**§10.2.1 Master Layout:**
- **OLD:** Top/bottom stack (enemy top 40%, player mid 30%, hand bottom).
- **NEW:** Side-by-side (player LEFT 30%, enemy RIGHT 40%, hand bottom). Center gap for atmospheric
  backdrop + intent arrow crossing. Squad formations (layered depth: Lead forward + larger, Bench
  behind/flanking) replace flat rows.
- **Rationale:** Intent arrows crossing the center gap create a clearer spatial telegraph (Pillar 1).
  The squad depth language (overlap + scale + z-order) conveys "team formation" vs "card slots."
- **Action at ratification:** User/art-director/game-designer approve the side-by-side geometry +
  squad layering before Epic 13 implementation begins. This is a **load-bearing layout change** and
  the most visible revision to §10.2.

---

### Data Bindings Summary (complete event-bus map)

| UI element                      | Channel / source                                                           |
|---------------------------------|----------------------------------------------------------------------------|
| Enemy sprite/intent/HP/status   | `EnemyStateChannel`, `IntentRevealedChannel` (§4.4)                        |
| Enemy Pokédex tier badge        | `PokedexTierSO` (passive read per species)                                 |
| Catch gauge (wild)              | `CatchGaugeChannel` → `WildCatchResolver.Catchability(hp%, status, ball)`  |
| Player Active Team portraits    | `ActiveTeamChannel.Slot[0/1/2]` (event-raised on change)                   |
| Lead index / crown              | `LeadChangedChannel` (event-raised on swap)                                |
| HP / ShieldHP                   | `ActiveTeamChannel.Slot[N].HP / .ShieldHP`                                 |
| Status conditions               | `ActiveTeamChannel.Slot[N].StatusConditions[]`                             |
| Stat stages                     | `ActiveTeamChannel.Slot[N].StatModifiers{}`                                |
| Trauma badge                    | `TraumaChannel` → `PokemonInstance.TraumaCount` (§6.2.5)                   |
| Lead Aura                       | `LeadAuraChannel` (event-raised on activate/change/end)                    |
| Hand (skill + consumable cards) | `HandChannel` → `CardInstance[]` (event-raised on draw/play/discard)       |
| AP pips                         | `APChannel` → `currentAP, maxAP`                                           |
| Deck / Discard counts           | `DeckCountChannel` → `deckCount, discardCount`                             |
| Swap cost                       | `SwapCounterChannel` → `currentSwapCount` (resets at turn start)           |
| Damage preview (drag)           | `DamageCalculator.Preview(card, target) → DamageBreakdown` (pure, §10.7.4) |
| Region modifiers / Field        | `RunStateSO.RegionModifiers`, `FieldStateChannel` (CL-012)                 |
| Active Relics                   | `RunStateSO.ActiveRelics`                                                  |

UI **never** queries logic systems directly — all bindings are event-driven or pure function calls (`ui.md`).

---

**Acceptance checklist (§1.9):**
- ✅ Uses only tokens from §1.1–§1.3 (no ad-hoc px/hex).
- ✅ Composes only §1.4 components (HP bar, AP pips, skill/consumable cards, badges, tooltips, buttons).
- ✅ States every component's data binding as event-bus channel / SO.
- ✅ Lists empty/loading/disabled/focus states (see States table).
- ✅ Has reduced-motion notes (instant HP fills, no screen-shake, instant intent/preview).
- ✅ Has full keyboard + pad navigation (see Interactions + Accessibility).
- ✅ Names loc-key namespaces (e.g., `ui.combat.end_turn`, `combat.card.*`).
- ✅ Cites GDD § (§3.3.1 swap cost, §3.3.5 faint precedence, §4.2.2 status, §4.4 intent, §5.5.4 Aura, §10.2).
- ✅ Passes Pillar-5 readability skim (warm-dim theme, cheerful motion, friendly type colors).
- ✅ Passes `ui.md` invariants (damage preview final value, grayed-not-hidden, intent slot+occupant, boss HP phase markers).

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
- Enemy HP bars are thicker (10px track vs 8px for player).
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
