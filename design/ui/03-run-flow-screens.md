# Run-Flow Screens — Front-end, New-Run Setup, Node, Results

> Warm-light theme (§1.2) for front-end/setup; warm-dim stage for in-run results. Template:
> **purpose · zones · components · bindings · interactions · states · accessibility.** New strings
> are named by loc-key namespace, never literal English (§1.8).

---

## 3.1 Boot / Splash  (transient)

**Purpose.** Studio/fan-disclaimer card → load `_Persistent` services + `MetaProgressionSO` → Main
Menu. **Components.** logo lockup, `loading curtain`. **Bindings.** save-load readiness from
`SaveSystem`. **Interactions.** any-key skip after logo. **States.** loading / save-corrupt (→ toast
+ safe Main Menu). **Accessibility.** fan-disclaimer text honors text-size; no flashing logo
(photosensitivity §10.6.4).

---

## 3.2 Main Menu  (base panel · FrontEnd scene)

**Purpose.** Front door. Routes to Continue / New Run / Trainer Hub / Pokédex / Settings / Quit.
**Zones.** Title lockup (Poké-Ball-red brand mark, `--brand-red`) · vertical button stack (centered/
left) · build-version + save-status footer · warm illustrated backdrop (Region-1 vista, parallax —
off in reduced-motion). **Components.** `button --primary` (New Run), `button --secondary` (rest),
`Continue` shows a **run-summary chip** (Region, team portraits, depth) when a save exists.
**Bindings.** `SaveSystem.HasResumableRun`, `RunStateDTO` summary, `MetaProgressionSO` (Trainer Lv on
a small card). **Interactions.** Continue (disabled-but-visible when no save) → resumes (§9.8); New
Run → Difficulty Select; others push overlays/panels. **States.** no-save (Continue disabled, not
hidden) · save present (Continue primary, New Run warns "abandons current run?" via confirm modal).
**Accessibility.** full kbd/pad vertical nav, focus ring; backdrop is decorative (`aria-hidden`).
**Loc:** `ui.menu.*`.

---

## 3.3 New-Run setup (linear, reversible until first node) — `↻ DECISION OD-2` order

Difficulty → Starter → Starting Relic → Region Modifier. A shared **stepper chrome** (progress dots
top, `◀ Back` / `Continue ▶` bottom) wraps all four; Back is allowed within setup; the final
**Region-Modifier Confirm** locks the run (seed → map gen) behind a confirm modal.

### 3.3a Difficulty Select
**Purpose.** Choose difficulty (`DifficultyModifierSO`). **Components.** 2–4 `option cards` (one
recommended, 2px brand outline), each: name, one-line effect, modifiers list. **Bindings.**
`DifficultyCatalog`, unlock state from `MetaProgressionSO`. **Interactions.** pick → Continue.
**States.** locked tiers (shown, lock glyph + "unlock at Trainer Lv N"). **Accessibility.** effect
text not color-only. **Loc:** `ui.newrun.difficulty.*`.

### 3.3b Starter Select  (resolves OD-3 — single pick, §2.1)
**Purpose.** Pick one Starter from the unlocked pool (Bulbasaur/Charmander/Squirtle + meta unlocks).
**Zones.** Starter roster (portrait tiles) · detail panel (type, base stats radial, the 2 starting
moves per CL-006, evolution line preview, flavor). **Components.** `Pokémon card tile`, `type badge`,
`stat radial`, `move chip`. **Bindings.** `StarterCatalog` (unlocked subset via `MetaProgressionSO`),
`PokemonSpeciesSO`, `LearnsetSO`. **Interactions.** select tile → detail updates → Continue.
**States.** locked Starters (silhouette + unlock hint, not hidden) · selected (brand edge).
**Accessibility.** roster is a focusable grid; stat radial has a text-equivalent table.
**Loc:** `species.*`, `ui.newrun.starter.*`.

### 3.3c Starting Relic Select
**Purpose.** Pick the opening relic (replaces the temp `StartingRelicPanelUI`). **Components.**
`relic card` grid with **rarity-tinted borders** (§1.2), detail on focus. **Bindings.**
`StartingRelicPool`, `RelicSO`. **Interactions.** 1-of-N pick → Continue. **States.** hover/focus
shows full effect + source; selected = brand edge. **Accessibility.** rarity encoded by border +
label, not color-only. **Loc:** `relic.*`.

### 3.3d Region Modifier Select  (CL-016; per-Region re-pick)
**Purpose.** Choose the Region's single active modifier from the 16-pool (CL-016); re-chosen each
Region. **The Confirm here LOCKS the run/seed** → map generates. **Components.** `modifier card`
grid (1 active), confirm modal. **Bindings.** `RegionModifierResolver`, `RegionModifierCatalog`,
seeds `RunStateSO`. **Interactions.** pick → **Confirm (modal: "Lock in {Modifier}? The map is set
for this Region.")** → Map View. **States.** Naturalist's Lens (CL-018) shows its biome-steer preview.
**Accessibility.** confirm modal Esc=cancel; modifier effects are full text. **Loc:** `regionmod.*`.

---

## 3.4 Node Preview  (anchored popover · over Map)

**Purpose.** Confirm entering a node; preview what's inside before locking the team (Pillar 2).
**Zones.** Popover anchored to the node marker: type icon + name, short preview (Wild: biome +
possible species silhouettes + Pokédex tier; Trainer/Elite/Gym: archetype hint + reward; Center/Shop/
Dojo/Mystery: one-line). **Components.** `popover`, `type/node badge`, `reward chip`. **Bindings.**
`NodeSO`, `EncounterPreviewChannel`, reward tables. **Interactions.** `Enter` (routes per node type,
§00 flow) / `Cancel`. **States.** team-not-ready warning (e.g. all-fainted Box) · first-visit vs
known. **Accessibility.** popover traps focus, Esc=Cancel, Enter=Enter. **Loc:** `node.*`.

---

## 3.5 Post-Combat Reward  (full-screen result · forward-only)

**Purpose.** Resolve victory: XP to team + Box (CL-010 100%/75%), loot, catch result, Trainer-XP/
Battle-Pass progress (CL-019). **Zones.** header ("Victory!" / "Caught {species}!") · XP panel (per-
Pokémon bars filling, level-up & evolution-eligible flags) · loot row (₽, relics, consumables, TMs) ·
Trainer-Level track delta (milestone ticks, CL-019) · `Continue`. **Components.** `XP bar`, `Trainer-
Level bar` (segmented), `relic/consumable/loot card`, `toast` (achievement unlocked, CL-020).
**Bindings.** `CombatResultChannel`, `XPAwarder`, `RunEconomyChannel`, `BattlePassChannel`,
`AchievementService`. **Interactions.** staggered reveal (skippable, §10.6.1.5) → Continue → Evolution
(if eligible) or Map. **States.** catch-success branch (adds caught Pokémon to Box) · level-up (badge)
· evolution-eligible (routes to 3.6) · Battle-Pass milestone (token toast). **Accessibility.** Skip-
animations honored; bars fill instant in reduced-motion; everything has text labels. **Loc:**
`ui.reward.*`.

> **Art-director note:** The XP-bar fill animation is the *reward* moment — it should feel satisfying,
> not rushed. 400ms ease-out per bar (staggered 80ms between team members), with a soft gold particle
> burst (3–5 particles, 16px sprites, `--accent-action`, 300ms rise+fade) when a bar completes. In
> reduced-motion mode: instant fill, 2-frame flash (white border pulse, 100ms × 2), no particles. The
> Battle-Pass milestone tick should "ding" (visually: a 1.15× scale pulse + gold glow, 200ms) and play
> the achievement-unlock toast if a Token reward is reached.

---

## 3.6 Evolution Screen  (full-screen result · §10.9.1 evo anim)

**Purpose.** Play the evolution moment and apply the CL-007 evolution payload (free archetype move,
stat shift). **Zones.** centered evolving Pokémon (8-frame anim §10.9.1) · before→after name/type/
stat diff · new-move/archetype callout (CL-007). **Components.** `Pokémon portrait`, `stat-diff
rows`, `move chip`, `Confirm`. **Bindings.** `EvolutionChannel`, `PokemonInstance`, `LearnsetSO`.
**Interactions.** play anim → reveal diff → (if a move choice is offered) pick → Confirm. **States.**
cancel-evolution option if the design allows holding (verify §5.3); reduced-motion = instant
before/after, no flash (photosensitivity). **Accessibility.** the screen-flash on complete (§10.5.3)
is capped/PEAT-safe and disabled in reduced-motion. **Loc:** `ui.evolution.*`, `species.*`.

> **Art-director note:** The evolution animation (§10.9.1, 8-frame sequence) is the **one place** the
> game goes full celebratory. The before→after stat diff and new-move callout should feel like **level-up
> rewards in a JRPG**: big numbers, warm gold accents, a subtle rising-sparkle background (opt-out in
> reduced-motion). The screen-flash at the end is a white full-screen overlay at 60% opacity, held for
> 80ms, then 180ms fade-out — PEAT-verified safe, but **completely disabled** in reduced-motion mode
> (just cuts to the "after" portrait, no flash). Do not let the celebration undercut Pillar 5 warmth —
> the flash should feel like a *camera flash*, not a lightning strike.

---

## 3.7 Legendary Pick  (full-screen step · CL-021, Gym victory)

**Purpose.** 1-of-3 Legendary relic choice on Gym victory / Summit / Black-Market (CL-021), max
2/run. Replaces temp `LegendaryPickSelectUI`. **Zones.** three `relic card`s with the **Legendary
gold shimmer** rarity treatment (§1.2; static in reduced-motion) · "max 2 per run" counter · Confirm.
**Bindings.** `LegendaryRelicCatalog`, `RunStateSO` (held count, seeded per-Region). **Interactions.**
focus → full effect; pick one → confirm. **States.** at-cap (screen explains; offers skip/alt reward
per CL-021) · already-held dimmed. **Accessibility.** shimmer is decorative; rarity also via label.
**Loc:** `relic.legendary.*`.

---

## 3.8 Victory / Run-Cleared Summary  (full-screen result)

**Purpose.** Celebrate clearing the Region/Gym climax (VS end-state); roll meta rewards. **Zones.**
banner · run stats (depth, turns, catches, badges) · Trainer-XP/Token earned · unlocked achievements
· `Continue → Trainer Hub`. **Components.** `metric tiles`, `achievement toast list`, `button`.
**Bindings.** `RunResultChannel`, `MetaProgressionSO`, `AchievementService`. **Interactions.**
Continue → Hub. **Accessibility.** all stats text; celebratory motion off in reduced-motion. **Loc:**
`ui.victory.*`.

---

## 3.9 Defeat / Run-Over Summary  (full-screen result)

**Purpose.** Party-wipe end (§run loss). Honest, warm-not-punishing tone (Pillar 5). **Zones.**
"Run over" · how far you got · meta rewards still earned (Trainer-XP/Tokens from the run, CL-019) ·
a single encouraging line · `Continue → Trainer Hub`. **Components.** same as 3.8 minus celebration.
**Bindings.** `RunResultChannel`, `MetaProgressionSO`. **Interactions.** Continue → Hub.
**Accessibility.** no harsh red flash; warm amber framing. **Loc:** `ui.defeat.*`.

---

## Shared run-flow rules

- All **setup** screens (3.3a–d) share one stepper chrome + Back/Continue; all **result** screens
  (3.5–3.9) share one forward-only result chrome (header · body · Continue) so the player learns one
  pattern.
- Result screens never trap the player: Continue is always reachable by keyboard/pad.
- `↻ flag` 3.6 "cancel evolution" and 3.7 "at-cap behavior" need a `game-designer` confirm against
  §5.3 / CL-021 before ratification.
