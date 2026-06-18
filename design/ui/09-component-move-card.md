# Component Spec — Move (Skill) Card  🔒 LOCKED (iteration 1)

> Authoritative anatomy for the skill card, agreed via mockup iteration (move_card_v3 + modifier/rider).
> Supersedes the generic card note in §10.2.3 and `01-design-system §1.4.1` — those should point here.
> Style: warm/rounded (§1.0), full type-colour theming. Fonts: **Baloo 2** (display) / **Nunito**
> (body). All taxonomy cited to canon; every face element maps to a `MoveSO` field. Status: locked for
> the first iteration; the three flagged micro-decisions at the end stay open.

---

## 1. Concept

The card is **flooded with its move's type colour** (instant hand-reading of type spread). The art
window and the effect text sit in **calm cream insets** so colour identifies while text stays legible.
The card has **two display states** so the battlefield stays visible:

- **Compressed** — the resting state in hand. Small footprint; shows only what you need to *decide*.
- **Expanded** — on **hover / keyboard-focus / select / drag**. Unfolds the art window + effect text.

> Transition: expand **grows in place within the hand row** — the card enlarges from its own slot and
> the neighbouring cards slide aside to make room. It does **not** pop above the tray. Reduced-motion:
> instant resize, no slide. (`01 §1.6`.)

---

## 2. Anatomy (final layout)

Corners belong to **overhanging badges**; the **top bar carries the name + owner**; the **footer is
AP-left / power-right**. No internal trait strip.

| Zone | Content | Placement / token |
|------|---------|-------------------|
| **Mechanics badges** | **category + range + modifier** icons | **overhang the top-LEFT edge**, half above the card; ~20–22px circles; modifier gold-tinted |
| **Status-rider badge** | condition glyph(s), condition-coloured | **overhang the top-RIGHT edge**; 2px dark ring |
| **Top bar** | **owner** avatar + **move name** | deeper type shade; name `Baloo 2`, owner avatar = 32×32 species portrait |
| **Window** (expanded only) | **owner portrait** ‖ **move art** | cream `--surface` inset, `--radius-md` |
| **Effect plate** (expanded only) | localised effect text, modifier/rider terms colour-coded inline | cream inset, `Nunito` ~12px |
| **Footer** | **AP cost dots — bottom-LEFT** · **`PWR n` — bottom-RIGHT** | `Baloo 2`; power = base only, **no STAB/maths here** |

**Compressed** = type-colour body · the two overhanging badge clusters · top bar (owner avatar + name)
· footer (AP bottom-left, power bottom-right). It drops only the window + effect plate. Everything
needed to *decide* is present; the full read appears on expand.

> **left = how it works (mechanics), right = what it inflicts (status)** — the corner split is the
> card's core mental model.

**Iteration-2 refinements — ADOPTED (master designer, 2026-06-14, all 5):**
1. **Owner-tinted inset** — the owner-portrait cream inset is tinted faintly toward the owner's primary
   type (Water-owner = faint blue cream, Fire-owner = faint orange cream). 15 tint variants in USS;
   instant "who contributed this card" read.
2. **Range icon always visible** — the **range** glyph (melee ✊ / ranged ◎) floats into the **top bar**
   beside the owner avatar, so it shows even compressed. Category + modifier badges stay in the
   top-left cluster (expanded-only); range is the tactically urgent one.
3. **Status-rider badge bumped** to **26–28px** (was 24) with a subtle pulsing glow (reduced-motion =
   static stronger border) — a rider out-weighs a range read, so it pops more.
4. **Not-enough-AP reinforced** — amber ⚠ next to the AP dots **and** an amber outer border (on top of
   the desaturate), so the deficit reads at a glance.
5. **Out-of-position text hierarchy** — "**Melee**" is the 18px bold heading; "needs Lead slot" the
   11px semibold subtitle (the actionable line, weighted under the verb).

---

## 3. Icon taxonomy (all canon — bind to data, don't invent)

| Group | Values | Source | Mockup icon (final = SVG, `06`) |
|-------|--------|--------|------------------------------|
| **Category** | Offensive · Defensive · Utility | §5.3.6 kit composition | ⚔ sword / 🛡 shield / ✦ sparkles |
| **Range** | Melee · Ranged | §5.3.6, move lines | ✊ grab / ◎ target-arrow |
| **Modifier** | Step-Forward · Step-Backward · none | §5.3.4 Vanguard | ▲/▼ chevrons, **gold tint** = movement class |
| **Status rider** | Burn, Poison, Paralysis, Sleep, Freeze, Confusion (+chance) | §4 status, §5.3.x riders | condition glyph on the **top-right overhang badge**, condition-coloured |

Two distinct visual languages, deliberately: **gold = modifier** (a guaranteed positional effect, in
the **top-left** mechanics cluster) vs **condition-colour = status rider** (a chance to inflict, the
**top-right** badge). They never read the same.

---

## 4. AP cost — dots

- AP lives at the **footer bottom-LEFT** (power is bottom-RIGHT) — identical on compressed and expanded
  so it never shifts when a card grows.
- Render AP as **gold dots**; **wrap to a new row after 3** (4 → 3+1, 5 → 3+2, 6 → 3+3). (`01 §1.4`.)
- In the **not-enough-AP** state, show **available** dots filled gold and **missing** dots as red
  dashed outlines, so the deficit is legible.

---

## 5. States (the load-bearing part)

| State | Trigger | Treatment |
|-------|---------|-----------|
| **Default (compressed)** | resting, affordable, playable | full colour, crisp |
| **Expanded** | hover / focus / select / drag | lifts above tray, unfolds window + effect |
| **Not enough AP** | `MoveSO.ApCost > currentAP` | card **desaturated** (grayscale+50% opacity) · AP dots show gold-have vs red-missing · amber `Not enough AP` label · **stays fully visible** (`ui.md`) |
| **Out of position** | move is **Melee** and owner is **not in the Lead slot** | card **not desaturated** (it's affordable) · **blue positional lock** overlay + melee glyph + `Needs Lead slot` · blue = position |
| **Drag-over-target** | dragged onto an enemy | the **damage preview** appears (final value + STAB/type/crit breakdown) — this is the *only* place the maths shows (`§10.2.4`, `10.7.4`) |

> The two "can't play" reasons are colour-separated on purpose: **amber = AP**, **blue = position**.
> Verify the exact melee-position rule (melee usable only from Lead?) against §3 / §4 targeting before
> implementation — UI reads `IsPlayable(move, state)` + a reason enum, it does not compute the rule.

---

## 6. Data bindings (`MoveSO` / event channels)

| Element | Field |
|---------|-------|
| type colour + band | `MoveSO.Type` → type palette (§10.1.3.2) |
| name / effect text | loc keys `move.<id>.name` / `.effect` (§10.10) |
| AP dots | `MoveSO.ApCost` |
| category / range / modifier icons | `MoveSO.Category` / `.Range` / `.Modifier` |
| status-rider badge | `MoveSO.StatusRider` (condition + chance) |
| power | `MoveSO.Power` |
| owner avatar + name | owning `PokemonInstance` → species portrait (32×32) + `species.<id>.name` |
| playable / reason | `HandChannel` card-state + `IsPlayable()` reason enum (AP / position / none) |
| damage preview (drag) | `DamageCalculator.Preview(card,target) → DamageBreakdown` (pure, §10.7.4) |

UI never computes affordability/legality — it renders the state the combat system publishes (`ui.md`).

---

## 7. Accessibility

- Type/category/status read by **glyph + shape**, not colour alone (colorblind, §10.6.1.1); status
  badges carry the §1.5 pattern overlay.
- Every card is keyboard/pad focusable; focus = expanded + gold ring (`01 §1.2`).
- Unplayable cards are **never hidden**; the reason is text, not just a tint.
- Damage preview is **hover / drag-over-target only — no persistent dock** (master-designer decision
  2026-06-14). `↻ NOTE:` this drops the GDD §10.6.1.4 "always-on damage preview" accessibility toggle;
  surface at ratification so the accessibility reviewer can weigh re-adding a lightweight option.
- Number text uses tabular figures (`01 §1.3`).

---

## 8. Asset hand-off (feeds `06` / `08`)

New/confirmed icons this card needs: **category ×3** (offensive/defensive/utility), **range ×2**
(melee/ranged), **modifier ×2** (step-fwd/back), **status riders ×6** (the §10.4.3 set) as art-corner
badges, **AP dot**. All warm/rounded SVG (`08` P0-3/4/5). Owner avatars come from the 32×32 sprite
pipeline (`08`).

---

## 9. Open micro-decisions (flagged — not blocking iteration 1)

1. **Multiple status riders** on one move → stack badges up the art-window edge (max 2–3) vs one
   "multi-status" badge that expands on hover. *Default: stack, cap 3.*
2. **Show the rider chance %** on/near the badge, or keep the badge clean and let the effect line +
   hover carry the % . *Default: clean badge, % in effect text + hover.*
3. **Power as `PWR n`** text vs an icon+number like the other stats. *Default: keep `PWR n`.*

Resolve these during the combat-screen assembly pass or first playtest; they don't change the layout.

---

## Art-director review notes (2026-06-14)

### Refinements — ✅ ALL 5 ADOPTED (master designer, 2026-06-14) — folded into §2 above; detail kept here

1. **Owner portrait inset tint** — the owner portrait sits in a cream `--surface` inset that currently
   matches the effect plate. Consider tinting this inset subtly toward the *owner's primary type* (e.g.
   a Squirtle-owned move gets a faint blue-tinted cream, a Charmander-owned move a faint orange cream).
   This gives instant "who contributed this card" read at a glance without needing to parse the avatar.
   Trade-off: adds 15 tint variants (one per type) to the component USS. Benefit: hand-read speed.

2. **Compressed state legibility check** — the compressed card shows owner avatar + name + AP + power,
   but the mechanics badges (category/range/modifier) are ONLY visible in expanded state (they're part
   of the art-window zone which is hidden when compressed). This may slow the "is this melee or ranged?"
   decision. **Recommendation:** float the **range icon** (melee ✊ / ranged ◎) into the top bar next
   to the owner avatar so it's always visible, even compressed. The category/modifier badges can stay
   in the mechanics cluster (expanded-only) since they're less tactically urgent.

3. **Status-rider badge scale** — at 24px the rider badge is visually equal to the 20px mechanics badges,
   but it encodes MORE decision weight (a status is rarer and more impactful than knowing the move is
   ranged). Consider bumping the rider badge to 26–28px so it pops more, or give it a subtle pulsing
   glow (reduced-motion = static stronger border). The mockup shows it well, but in a 7-card hand with
   rapid scanning, a bit more emphasis helps.

4. **Not-enough-AP state clarity** — the grayscale+opacity treatment works, but the red-dashed AP dots
   are small and easy to miss when scanning quickly. Add a small **amber warning icon** (⚠) next to the
   AP dots in this state, or tint the card's outer border amber (not just desaturate). Keep the
   "Not enough AP" text label, but reinforce it visually on the card itself.

5. **Out-of-position overlay text hierarchy** — the blue lock overlay is clear, but "needs Lead slot"
   could be larger and bolder. The text is the actionable part; the lock icon is decorative. Swap the
   visual weights: make "Melee" the larger heading (16px → 18px bold), and "needs Lead slot" the
   subtitle (stays 11px but bumps to semibold).

### Strengths to preserve

- The **flooded type-colour** + **cream insets** contrast is excellent — legibility is high, the cards
  feel tactile and distinct.
- **Corner badge overhang** is a strong visual signature; keep it exactly as-is.
- **Compressed ↔ expanded** growing in place is the right interaction model — no jarring pop-above.
- The silhouette contrast (skill card `--radius-md` vs consumable `--radius-xl`) reads instantly even
  in peripheral vision — do not soften this further.

### Flags for game-designer / ui-programmer

- Confirm **melee usability rule**: the mockup shows "needs Lead slot" for melee moves. Is this the
  final rule, or can Bench Pokémon use melee moves that target their own row? (Affects the overlay text.)
- Confirm **AP overflow visual** (the design system now caps at 6 pips + `+N` text) — does any content
  grant > 6 AP? If so, the `+N` badge needs a spec (probably a small gold chip next to the pips).
