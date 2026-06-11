# Project Ascendant — GDD Change Log (Design → Engineering Hand-off)

> **Purpose.** When a design decision **changes the GDD**, it is recorded here so
> the **engineering team can adapt the code**. This is the bridge between the
> design polish pass (tracked in [`open-questions.md`](open-questions.md)) and
> implementation.
>
> **Owner:** `gdd-steward` appends entries as questions resolve. **One entry per
> approved change.** Engineering reads this to know what to modify.
>
> **Relationship to canon:** the *authoritative* spec still lives in Notion → the
> local `docs/gdd/` snapshot. This log is the **delta record** — "what changed,
> where, and what code it touches" — not a replacement for the GDD.

## How an entry is created

1. A question in `open-questions.md` reaches ✅ Decided.
2. Append an entry below using the template.
3. Later (user-gated, batched): edit the canonical Notion §, re-export the
   snapshot, and tick the entry's **GDD updated** box.
4. Engineering picks up the entry, adapts the code, ticks **Code adapted**.

## Entry template

```
### CL-NNN — <short title>   (resolves Qx)
- Date: YYYY-MM-DD
- Topic / §: <e.g. Topic 4 §4.3.5>
- Change: <old → new, the essence of the decision>
- Rationale: <one line>
- Code impact: <files/systems engineering must touch, or "none — doc only">
- Status: [ ] GDD updated   [ ] Code adapted
```

## Status board (at a glance)

| ID | Resolves | Title | Topic/§ | GDD | Code |
|----|----------|-------|---------|-----|------|
| CL-001 | Q6 | Bestiary → Pokédex rename | T1/4/5/6/7/8/9/10 | ✅ | ✅ |
| CL-002 | Q5 | Stat-stage ladder → linear 0.4–1.6 | T4 §4.2.6 | ✅ | ✅ |
| CL-003 | Q4 | Wild catch = Victory + full XP | T3 §3.1, T7 §7.3.4 | ✅ | ✅* |
| CL-004 | Q11 | Defer League/Champion (scope) | T2 §2.1.6, T4 §4.6/§4.7 | ✅ | n/a |
| CL-005 | Q3 | Skill-card hand size 4 → 5 | T3 §3.2.2/§3.7 | n/a¹ | ✅ |
| CL-006 | Q13 | Move-acquisition: level-gated learnset, start 2 | T5 §5.12.1 | ✅² | ✅³ |
| CL-007 | Q15 | Evolution: free archetype/stage + lighter payload | T5 §5.12.2/§5.6 | ✅² | ✅⁴ |
| CL-008 | Q14 | Abilities kept, decoupled to an earned learner | T5 §5.12.3 | ✅² | ✅⁵ |
| CL-009 | Q16 | Move Tutor → paid "Dojo" node (moves + abilities) | T7 §7.14; T5 §5.12.4 | ✅² | ✅⁵ |
| CL-010 | Q12 | XP: Active 100% / Box 75% baseline | T5 §5.12.5; T8 §8.3.3 | ✅² | ✅⁷ |
| CL-011 | Q7 | Unknown intents: Elite/Gym baseline + Dense Fog extension | T4 §4.3.5 | ✅⁶ | ✅⁶ |
| CL-012 | Q8 | Field effects: tiered neutral Battlefield + enemy-owned Home Field | T4 §4.3.8/§4.3.8.4-6/§4.4.4.3/§4.8.2 | ✅ | ✅¹¹ |
| CL-013 | Q9 | Gym phases: remove mid-evo, power premium + per-type signature Phase 2 | T4 §4.3.7/§4.4.4.3/§4.4.4.4 | ✅ | ✅¹² |
| CL-014 | Q22 | Catch: deterministic Catchability Gauge (30%/50% thresholds, no RNG) | T7 §7.3.4.1/§7.3.4.2/§7.3.4.3 | ✅ | ✅⁹ |
| CL-015 | Q1 | City → Choice Plaza (StS hub) + risky optional City Gym (4th Badge) | T2 §2.1.4/§2.7; T7 §7.8/§7.8.4; T4 §4.5.3 | ✅ | ☐ |
| CL-016 | Q2 | Region Modifiers → per-Region (1 active, re-chosen) + 16-modifier pool | T2 §2.1.1/§2.1.4.1; T7 §7.8.3 | ✅ | ✅¹³ |
| CL-017 | Q17 | Trauma cap → two-zone curve, soft cap −75% at 10 stacks | T6 §6.2.1/§6.8.2/§6.13; T2 §2.6 | ✅ | ✅⁸ |
| CL-018 | Q21 | Biome↔Region binding confirmed + Naturalist's Lens (opt-in biome-steer modifier) | T7 §7.3.1/§7.8.3.1 | ✅ | ✅¹⁴ |
| CL-019 | Q18 | Trainer XP → Hybrid Battle Pass (per-level track + Token choice lane) | T6 §6.3.4/§6.3.5/§6.4.2/§6.5.2/§6.6.1 | ✅ | ☐ (post-VS) |

⁴ CL-007 #A–#D fully complete (0f40520). Wild lines Caterpie/Geodude/Pidgey now have 3 archetypes
per stage (parity with starters). 12 new branch SOs, 6 renames, 1 new move (signal_beam).
1050/1050 EditMode green (2026-06-09).

⁶ CL-011 code complete (2026-06-10, 1070/1070 green). 6 new IntentHidingTests. EliteTrainerController +
GymLeaderController set HideBaselineIntents=true. CombatController.RebuildEnemyIntents hides first
intent per unwitnessed enemy; ExecuteEnemyIntent marks fired enemies as Witnessed. Dense Fog: run
layer sets HideBaselineIntents=true on Wild/Trainer setups via DifficultyModifiers.HidesIntents().
GDD §4.3.5 written to Notion and snapshot re-exported (2026-06-10).

⁵ CL-008 + CL-009 code complete (2026-06-10, 1064/1064 green). New: NodeType.Dojo, DojoNodeController
(TeachMove/TeachAbility/OfferMoves/OfferAbilities), EconomyConfigSO Dojo pricing,
MapGenerationConfigSO.DojoWeight, PokemonSpeciesSO.AvailableAbilities, factory no-op ability.
PokemonCenterNodeController tutor service removed; NodePanelUI Dojo stub added.
GDD enriched (2026-06-10): §5.12.3 AvailableAbilities pool + PrimaryAbility legacy note; §7.14 full-pool
offer/no-cap/placeholder pricing; Move Tutor removed from §7.6.1/§7.8.1/§7.12; §7.2.2/§7.13 updated.

² Synced via the §5.12 progression-redesign override block + §7.14 Dojo + §8.3.3 Exp Share row
(2026-06-08). Old sections (§5.2.1/§5.3.x/§5.5/§5.10) are superseded-where-conflicting by §5.12;
full prose integration into those sections is a later steward pass.

¹ GDD already specified 5 — only code was wrong. · *Catch already routes to Victory; **full XP rides
the standard Victory→OnCombatEnded path automatically once the XP system (Epic 10) is built** — no
catch-specific code needed. · **All code changes verified: 1029/1029 EditMode tests green (2026-06-05).**

---

# Entries

### CL-001 — Bestiary → Pokédex rename   (resolves Q6)
- Date: 2026-06-05
- Topic / §: Topic 4 §4.3.9, Topic 6 §6.9, Topic 1 glossary, Topic 10 UI
- Change: The cross-run knowledge system is named **Pokédex** everywhere (UI already is).
- Rationale: Consistency; the backend lagged the UI rename.
- Code impact: Rename backend symbols `BestiaryProgressSO` → `PokedexProgressSO`,
  `BestiaryShinyUnlock` → `PokedexShinyUnlock`, `BestiaryMasteryUnlock` →
  `PokedexMasteryUnlock`, and the `SaveSystem` refs/field keys. **Watch save-schema
  compatibility** — a serialized field/key rename needs a migration or alias so existing
  saves still load (§9.8.3). **RESOLVED:** the on-disk save filename `bestiary.dat` was kept
  (JSON is field-based, not class-name-based), so no migration was needed.
- Done: 18 files (sed `Bestiary`→`Pokedex`, uppercase only); 4 file pairs renamed (3 SO/unlock +
  1 test, `.meta` GUIDs preserved). Lowercase `bestiary`/`bestiary.dat` left untouched → saves load.
- Status: [✅] GDD updated (all 10 topics)   [✅] Code adapted — verified 1029/1029 tests green

### CL-002 — Stat-stage ladder → linear 0.4–1.6   (resolves Q5)
- Date: 2026-06-05
- Topic / §: Topic 4 §4.2.6
- Change: Canon ladder is the **implemented linear 13-entry ±6 array**
  `0.4,0.5,0.6,0.7,0.8,0.9,1.0,1.1,1.2,1.3,1.4,1.5,1.6` (±0.1/stage), replacing the old
  multiplicative ×0.25…×3.0 list.
- Rationale: Gentler, bounded, non-degenerate stat swings; matches shipped `BattleConfig.asset`.
- Code impact: **None** — code/asset already ship this (`BattleConfigSO.StatStageMultipliers`,
  13 entries). This entry is GDD-catch-up only.
- Status: [✅] GDD updated (§4.2.6 rewritten to the linear ladder)   [✅] Code adapted (pre-existing)

### CL-003 — Wild catch = Victory + full XP   (resolves Q4)
- Date: 2026-06-05
- Topic / §: Topic 3 §3.1 (outcomes), Topic 7 §7.3.4 (catch flow)
- Change: A successful catch resolves the wild combat as a **Victory** and awards the Active
  Team **full combat XP** (same as a kill).
- Rationale: Recruitment should never cost XP relative to finishing the fight.
- Code impact: Ensure the catch path fires the Victory outcome + the standard XP/reward award
  (verify it doesn't currently treat catch as a no-XP early-exit). Touches the combat-end /
  XP-award flow and catch resolution. Confirm reward-table parity (§7.12 wild row).
- Found: `CombatController.DispatchCatch` already routes a successful catch to the Victory path
  (clears EnemyTeam → IsAllFainted Victory → CombatEnd → OnCombatEnded). XP award itself is an
  Epic-10 stub for ALL combats, so "full XP on catch" rides the standard Victory path for free
  once XP exists — no catch-specific code needed.
- Status: [✅] GDD updated (§3.1 + §7.3.4)   [✅*] Code — Victory path done; XP auto-follows (Epic 10)

### CL-005 — Skill-card hand size 4 → 5   (resolves Q3)
- Date: 2026-06-05
- Topic / §: Topic 3 §3.2.2 (Draw Phase), §3.7 (Action Economy)
- Change: Default skill-card hand = **5** (was 4 in code). Consumable hand stays 2. Hand is
  fixed regardless of deck size; relics (Reactor Core, Sage's Tome) are the only +hand sources.
- Rationale: From a 12-card deck across 3 Active Pokémon, hand 5 keeps all three usually
  represented and preserves the Confusion safety floor (which breaks at hand 4).
- Code impact: Set `BattleConfig.asset` **`BaseSkillCardsPerTurn: 5`** (currently 4). Re-verify
  the Confusion floor test (§4.2.3.1: 3 Confused → 2 skill + 2 consumable playable) passes at 5.
- Status: [n/a] GDD already specified 5   [✅] Code — `BattleConfig.asset` BaseSkillCardsPerTurn
  4→5; 1029/1029 tests green (Confusion floor holds).

### CL-006 — Move-acquisition: level-gated learnset, start with 2   (resolves Q13)
- Date: 2026-06-07
- Topic / §: Topic 5 §5.2 (add learnset), §5.3.6.1 (base kit 4→2 + learn curve), §5.10 (pool
  starts at 2; deck contribution = min(known, 4))
- Change: Base kit **4 → 2 moves**. Add a per-species **level-up learnset** (ordered
  `(level, move)`). A Pokémon knows all learnset moves ≤ its level; deck contribution =
  `min(known, 4)` (active-4 cap unchanged, Mastery = 5th). Pool still grows additively beyond 4
  via learnset/Tutor/TM/evolution. Recruited wilds derive known moves from their spawn level.
- Rationale: Leveling unlocks moves (feels special); lean natural learnset makes Tutors/TMs/
  evolution additions valuable (scarcity is the lever).
- Code impact: **Significant.** `PokemonSpeciesSO` needs a `LevelUpLearnset[]` field; base kits
  trimmed to 2. `PokemonInstance.CurrentMoves` must derive from learnset ≤ level (or track a
  learned-set); deck builder uses `min(knownActiveEligible, 4)`. `PokemonInstanceFactory` /
  recruitment must seed moves from spawn level. Evolution executor + Tutor/TM add to pool.
  Migration: existing 4-move base SOs must be re-authored to 2 + learnset. Cadence (exact levels)
  is `ProgressionConfigSO`-tunable placeholder.
- Implementation (2026-06-08): **COMPLETE for VS base forms.**
  - #1 `PokemonSpeciesSO.LevelUpLearnset` + pure `KnownMovesAtLevel(level)` (legacy fallback) + 4 tests.
  - #2 `PokemonInstanceFactory` seeds the pool from `KnownMovesAtLevel`; player paths (RunBootstrapper
    starter, WildEncounterController recruit/wild) fill the active 4 from the same source.
  - #3 `LevelUpResolver` learns newly-crossed moves on level-up (auto-equip while <4 active, else
    pool-only per §5.10.2) + 2 tests; new `Result.MovesLearned` for future "learned X!" UI.
  - #4 Authored learnsets for the **6 VS base forms** (Bulbasaur/Charmander/Squirtle/Caterpie/Geodude/
    Pidgey): 2 moves @ L1, then later moves **clamped below EvolveLevel** (Squirtle [1,1,8,11];
    Caterpie [1,1,5,6] @evolve 7; Pidgey [1,1,7,8] @evolve 9). A starter begins R1 with **2 moves**.
  - **QA iteration (2026-06-08):** added `SpeciesLearnsetContentTests` (content guard: every learnset
    level < EvolveLevel; ≤2 moves at L1). It caught a v1-cadence bug — Caterpie/Pidgey had moves at
    L8/L11 ≥ their early EvolveLevel, so those moves were unreachable (red→green fix above). Real-content
    playtest confirms Squirtle pool = 2/2/3/4/4 at L5/7/8/11/12. **1037/1037 EditMode green.**
- ³ VS base forms only. Follow-ups (not blockers): evolved-form move flow lands with CL-007;
  non-wild enemy controllers (Trainer/Elite/Gym) still copy full `BaseLearnset` (safe via fallback) —
  unify for consistency later; cadence levels are placeholder/tunable.
- Status: [✅] GDD updated (§5.12.1)   [✅] Code — VS base forms complete, 1035 green

### CL-007 — Evolution: free archetype per stage + lighter payload   (resolves Q15)
- Date: 2026-06-07
- Topic / §: Topic 5 §5.3.3 (branch selection), §5.3.4 (archetypes), §5.3.5 (what changes)
- Change: (1) Archetype is chosen **independently at each evolution** — stage-1 no longer locks
  stage-2; offer the species' available 2–3 archetypes each time. (2) Payload is **lighter**:
  stat upscale + improve 1–2 existing pool moves + maybe +1 new pool move (final-evo new =
  signature). Remove the heavy multi-upgrade + sub-branch (A1/A2) rewrite.
- Rationale: Q13 makes evolution no longer the move source, so it becomes a focused upgrade;
  per-stage freedom maximizes branch expression while staying coherent under the lighter payload.
- Code impact: `EvolutionExecutor` + `EvolutionBranch`/`PokemonSpeciesSO` restructure — drop the
  sub-branch (A1/A2) model; evolution offers an archetype list per stage; apply stat bump + 1–2
  move upgrades + optional +1. **Likely resolves/reshapes gap #46** (duplicate final-form
  SpeciesId). Content: re-author all evolution payloads lighter; archetype tables per stage.
  Passive grant is gated on Q14's outcome.
- **Design decisions (2026-06-08, user-chosen):**
  - **Archetype model = moves-only, ONE species SO per stage** (Squirtle→Wartortle→Blastoise; the
    archetype only picks which move-upgrade set applies; shared stats/type/sprite). Cleanest, fully
    resolves gap #46.
  - **No ability/crit grant** — removed now (clean); abilities come from the Dojo (CL-008/§7.14).
  - **Scope this pass = system + full Squirtle line, all 3 archetypes.**
  - **Squirtle-line kits (locked):** Stage 1 (2 upgrades) — Vanguard {Tackle→Skull Bash, Tail
    Whip→Aqua Jet}; Specialist {Water Gun→Water Pulse, Tail Whip→Charm}; Support {Withdraw→Iron
    Defense, Tail Whip→Aqua Ring}. Stage 2 (+1 signature, additive = mix-safe) — Vanguard +Hydro
    Crash; Specialist +Hydro Pump; Support +Aqua Fortress (self-sustain tank). New move assets:
    water_pulse, charm, iron_defense, aqua_fortress.
- **Increment A — DONE (commit, 1037 green):** `EvolutionExecutor` no longer grants ability/crit;
  `SelectedBranch` is a record, not a path lock. `ProgressionTests` updated (asserts the recorded
  branch ability is intentionally ignored).
- **Increment B — DONE (2026-06-09, 1036 green):** created 4 effect-bearing move SOs (water_pulse
  [Confusion rider], charm [−Atk], iron_defense [+Def, SB], aqua_fortress [regen +Def]); consolidated
  `Wartortle_Vanguard`→`Wartortle` and `Blastoise_A1/A2`→one `Blastoise` (unique SpeciesId → **gap #46
  closed for the Squirtle line**); authored 6 archetype branches (Squirtle×3→Wartortle, Wartortle×3→
  Blastoise, moves-only, no ability/crit); wired `.Branches`; deleted A1/A2 + old VA branches.
  **Rewrote `SquirtleLineContentTests`** to the new model (6 golden tests) + a **cross-archetype mix
  runtime test** (Vanguard s1 → Specialist s2 → Blastoise pool has both archetypes' moves, no ability).
  GDD §5.6 stamped superseded by §5.12.2.
- **Increment C — DONE (2026-06-09, 1047 green):** applied same restructure to all remaining VS lines.
  Bulbasaur: 3 archetypes (Vanguard {Tackle→Headbutt, VineWhip→VineLash}, Specialist {VineWhip→
  MegaDrain, LeechSeed→Toxic}, Support {Growl→SweetScent}) + Ivysaur stage-2 sigs (PowerWhip /
  SeedFlare / GigaDrain). Charmander: 3 archetypes (Vanguard {Scratch→DragonClaw, Ember→FlameWheel},
  Specialist {Ember→Flamethrower, Scratch→Slash}, Support {Scratch→FlameWheel}) + Charmeleon
  stage-2 sigs (DragonClaw+ / Flamethrower / Roost). Wild lines — 1 archetype each: Caterpie
  (SilkBind/PinShot → Psybeam sig), Geodude (RockBlast/Earthquake → BodyPress sig), Pidgey
  (AerialAce/Tailwind → Hurricane sig). **gap #46 fully closed**: Venusaur_A1/A2→Venusaur.asset,
  Charizard_A1/A2→Charizard.asset; all wild mid/final PrimaryAbility fields cleared. Tests:
  BulbasaurLineContentTests + CharmanderLineContentTests fully rewritten; WildLinesContentTests new
  (12 tests); Caterpie/Geodude/Pidgey old tests updated to CL-007 model. 1047/1047 green.
- Status: [✅] GDD updated (§5.12.2 + §5.6 banner)   [✅] Code — ALL VS lines complete, 1047 green

### CL-008 — Abilities kept, decoupled to an earned learner   (resolves Q14)
- Date: 2026-06-07
- Topic / §: Topic 5 §5.5 (ability system), §5.8 (ability catalog)
- Change: Abilities are **no longer auto-granted by evolution**. They are **earned via an
  ability-learner** (form deferred — likely folded into the Q16 Tutor/"Dojo" node). One passive
  slot per Pokémon retained. The ~30-ability roster stays as content.
- Rationale: removes free-rider passives; makes abilities a deliberate earned sculpt; avoids
  per-stage passive-combo balancing from Q15.
- Code impact: remove ability auto-grant from `EvolutionExecutor`; `PokemonSpeciesSO.PrimaryAbility`
  becomes an *available-abilities* pool for the learner (rather than an auto-assignment). Ability
  acquisition flow = the learner (deferred). `PokemonInstance.Ability` slot unchanged.
- Status: [ ] GDD updated   [ ] Code adapted (detail deferred → Dojo node, CL-009)

### CL-009 — Move Tutor → standalone paid "Dojo" node (moves + abilities)   (resolves Q16)
- Date: 2026-06-07
- Topic / §: Topic 7 §7.6/§7.8 (remove tutor from Centers) + new Dojo node §; Topic 5 §5.4.2
  (tutor relocated), §5.5 (ability acquisition = Dojo)
- Change: New **Dojo** map node — teaches an off-learnset move and/or an ability to a chosen
  Pokémon for **Poké Dollars** (scales by power). ~1 per Region. Pokémon Centers lose the tutor
  service (heal + Trauma therapy only). The Dojo is also the **ability-learner** (CL-008).
- Rationale: scarce moves (CL-006) make a dedicated teaching destination valuable; consolidates
  move + ability acquisition; gives ₽ a real sink.
- Code impact: new `NodeType.Dojo` + controller + UI; map-gen placement (~1/region); pricing in
  an economy config; ability-teach + move-teach flows (move = `TutorLearnset` add to pool, ability
  = set `PokemonInstance.Ability`). Remove tutor service from Center nodes. Content: Dojo offer +
  price tables.
- Status: [ ] GDD updated   [ ] Code adapted

### CL-011 — Unknown intents: Elite/Gym baseline + Dense Fog extension   (resolves Q7)
- Date: 2026-06-10
- Topic / §: Topic 4 §4.3.5 (Unknown Intent & Revelation System)
- Change: **Option B — Per-Species Reinforced.**
  - Wild/Trainer encounters: **no Unknown intents at baseline** (all intents Witnessed from turn 1).
  - Elite/Gym encounters: **1 Unknown intent per enemy per combat** — each enemy's first intent is
    Hidden (❓); once they fire (Witnessed tier), all subsequent intents that combat are revealed.
  - Dense Fog modifier extends the 1-Unknown-per-enemy rule to Wild/Trainer encounters too (run
    layer sets `CombatSetup.HideBaselineIntents = true` when Dense Fog is active).
  - Pokédex Familiar tier (§4.3.9.1) retains full value — Familiar species are exempt from the
    Elite/Gym Unknown (pre-revealed by cross-run knowledge). Wiring deferred to Epic-Pokédex pass.
  - Also closes VS gap #44 (Dense Fog HideAllEnemyIntents).
- Rationale: Pillars 1 + 4 — Elite/Gym fights feel tactically sharper (one Unknown per enemy
  creates a learn-by-doing beat) while Wild/Trainer stays fully transparent. Pokédex Familiar
  unlock earns its metaprogression value.
- Code impact: `CombatController.CombatSetup.HideBaselineIntents` (bool); `CombatController.
  CombatState.HideBaselineIntents + WitnessedEnemies` (HashSet tracking); `RebuildEnemyIntents()`
  hides intent per unwitnessed enemy when flag is true; `ExecuteEnemyIntent()` adds enemy to
  WitnessedEnemies on fire. `EliteTrainerController` + `GymLeaderController` set flag true.
  Run layer responsible for OR-ing with `DifficultyModifiers.HidesIntents()` for Dense Fog.
  +6 new EditMode tests in `IntentHidingTests.cs`.
- Status: [ ] GDD updated   [✅] Code adapted

### CL-017 — Trauma cap → two-zone curve (soft cap −75% at 10 stacks)   (resolves Q17)
- Date: 2026-06-10
- Topic / §: Topic 6 §6.2.1 (Trauma formula + table + soft-cap rationale), Topic 2 §2.6 (Trauma summary anchor)
- Change: **Option C — two-zone curve.** Stacks 1–5 = −5% each (→ −25%, unchanged); stacks 6–10 =
  −10% each (→ −75% floor). Soft cap 5 → 10. Formula:
  `EffectiveMaxHP = floor(BaseMaxHP × max(0.25, 1 − 0.05·min(s,5) − 0.10·max(0, min(s,10) − 5)))`.
- Rationale: gives Trauma real teeth (deeper consequence) while keeping the gentle early game and the
  anti-spiral protection where it matters; deep Trauma is recoverable via Box rotation (safe now that
  CL-010 keeps benched mons leveled) + clearing sources, so it reads as "rest/retire this Pokémon," not
  a run-loss.
- Code impact: update the Trauma `EffectiveMaxHP` multiplier (today `1 − 0.05·min(stacks,5)`) to the
  two-zone piecewise above; raise the soft cap constant 5 → 10. Pull both slopes + cap from a config
  (BattleConfig/ProgressionConfig) per data-driven rule — no inline literals. Re-verify all healing
  (§2.4.2 computes vs EffectiveMaxHP) + Trauma tests against the new ladder; the §6.2.6 edge cases
  (Sturdy/Last-Stand prevent faint = no stack) are unchanged. Consider scaling Therapy (removes 1
  stack/visit) vs the deeper cap (Salve/Daycare remove-all still cover it).
- ⁸ **Code complete (2026-06-10, 1080/1080 green).** `PokemonVitals.EffectiveMaxHP` now computes the
  two-zone curve (zone-1 `TraumaStackPenaltyPercent`×min(stacks,Zone1) + zone-2 `TraumaZone2PenaltyPercent`
  ×beyond, capped at `TraumaStackCap`), all integer math (PA0001). `EconomyConfigSO` gains
  `TraumaZone1StackCount=5` + `TraumaZone2PenaltyPercent=10`; cap 5→10 in the SO default, the
  `EconomyConfig.asset`, and `VS_ItemSeeder`. Backward-compatible: when cap==Zone1 the curve is linear
  (zone 2 empty), so all legacy cap=5 tests pass unchanged. `TraumaSystemTests` table rewritten to the
  −75%/10-stack ladder + a cap-at-boundary linearity invariant (+6 cases).
- Status: [✅] GDD updated (Notion §6.2.1 formula+table+rationale, §6.8.2 Trauma Surge, §6.13 glossary, T2 §2.6, re-exported 2026-06-10)   [✅] Code adapted — 1080/1080 green

### CL-016 — Region Modifiers → per-Region accent + 16-modifier pool   (resolves Q2)
- Date: 2026-06-10
- Topic / §: Topic 2 §2.1.1 (run setup — add R1 pick), §2.1.4.1 (stacking rule superseded), Topic 7
  §7.8.3 / §7.8.3.1 (pick mechanic + pool) / §7.8.3.2 (persistence)
- Change: **Option B — Per-Region accent.**
  - **1 modifier active per Region**, re-chosen each Region, applies to **that Region only**
    (non-accumulating). Picks: pre-R1 at run setup (§2.1.1) + City 1 (R2) + City 2 (R3); 3 offered →
    pick 1, weighted to team comp. R1 no longer vanilla.
  - **Supersedes** the §2.1.4.1 "up to 2 active, persist to run end" rule; the modifier descriptions'
    "for the next Region" wording becomes canonical (resolves the contradiction).
  - **Pool expanded 12 → 16:** + Glass Cannon (+20% dealt & taken), Quick Study (+15% combat XP),
    Bargain Hunter (Shop + Dojo −20%), Field Surveyor (choose neutral Battlefield, CL-012). The 12
    originals retained, retiered (Strong/Medium/Niche).
- Rationale: relics + Badges already cover run-long stacking; per-Region transient modifiers get a
  distinct decision texture and can carry bolder/double-edged effects; every Region (incl. R1) becomes
  a player-chosen accent (§2.2).
- Code impact: add a **Region-start modifier pick** at run setup (R1) + reuse the City Reflection flow
  for R2/R3; change modifier scope from run-long-accumulating to **single-active-per-Region**
  (apply on Region enter, clear on Region exit). `RegionModifierSO` pool grows to 16 (4 new effects:
  +20%/+20% damage band, +15% XP, −20% shop/Dojo price, player Battlefield choice). Reconcile any
  code/tests assuming 2 stacked persistent modifiers. **Post-VS** for R2/R3 (VS ends at Gym 1), but
  the **R1 run-setup pick is VS-relevant** — flag for systems-designer/lead-programmer.
- ¹³ **Code COMPLETE (2026-06-11, 1147 green) across inc1–inc3i.** Foundation: `RegionModifierSO` schema
  (Kind/Magnitude/Tier) + `RegionModifierResolver` query API + `RegionModifierPool.BuildAll`/`BuildOffer`
  (16-pool, seeded offer) + `RunStateSO.SetRegionModifier` single-active lifecycle + registry
  registration + save round-trip + `CombatSetup/State.ActiveRegionModifiers` threading. **All 16 effects
  wired + green:** Hand of Plenty, Lucky Draw, Glass Cannon, Quick Study, Coin Purse, Bargain Hunter
  (Shop + Dojo), Iron Skin, Sturdy Lead, Pokédex Whisper, Swap Fuel, Mass Mobilization, Status Mastery,
  Pocket Healer, **TraumaResistance** (`EffectiveMaxHP` optional penalty-reduction param, threaded
  through the heal-ceiling + display sites), **TypeAffinity** (chosen type auto-surfaced as the team's
  most-common move type → +Magnitude in ResolveDamage), **Field Surveyor** (wild/Trainer combats surface
  a favourable neutral Battlefield from the team type). **Pick UI:** new `RegionModifierSelectUI` (3-of-16
  card picker) wired into the New-Run flow for the R1 pick (`MapViewUI.BeginRunWithDifficulty`); reused
  for City Reflection R2/R3 when the post-VS City flow lands. Tests: RegionModifierResolverTests (12) +
  RegionModifierLifecycleTests (7) + RegionModifierEffectTests (4) + GlassCannon/SturdyLead/TypeAffinity
  integration + TraumaResistance cases. **Minor:** deep DoT/hazard helpers use base Trauma (forgiving);
  Type Affinity / Field Surveyor auto-target rather than offer an explicit sub-picker (GDD-aligned).
- Status: [✅] GDD updated (Notion §2.1.1/§2.1.4.1 + §7.8.3/.1/.2, re-exported 2026-06-10)   [✅] Code adapted — 16/16 effects + foundation/lifecycle + R1 pick UI (1147 green)

### CL-019 — Trainer XP → Hybrid Battle Pass   (resolves Q18)
- Date: 2026-06-11
- Topic / §: Topic 6 §6.3.4 (currency model rewrite), new §6.3.5 (Battle Pass reward track + 1–30
  table), §6.4.2 (Hub upgrades now track-granted), §6.5.2 (meta-starters track-granted), §6.6.1 (Tier-3
  = the Token lane)
- Change: **Option B — Hybrid Battle Pass.**
  - Trainer XP stays the single earn-source → Trainer Level (curve §6.3.3 unchanged). Each level grants
    an authored reward; **~80% auto-grants, ~20% Token milestones** (every 5th level: L5/10/15/20/25/30).
  - **Token earn changes:** the per-run `floor(run XP / 100)` (cap 50) rule is **superseded** — Tokens
    now come from **track milestones + select achievements**. Tokens are spent at the Pokémart **only**
    on the **Tier-3 Mastery-relic lane** (§6.6.1, 10 relics × 5 Tokens) in any order (retained agency).
  - **Hub upgrades (§6.4.2) + meta-starters (§6.5.2) move onto the track**, auto-granted on schedule;
    their individual **Token costs are removed**. The meta-starters' thematic criteria (Pikachu "reach
    R2", Eevee "win + recruit 4 evos", Riolu "Underdog Run") **become achievements** that grant bonus
    XP/Tokens instead of gating the starter (Q19 expands the achievement catalog).
  - **Tier-2 discovery layer (§6.6.1) unchanged** (achievement-triggered relic unlocks stay orthogonal).
  - **§6.1 hard rule preserved:** every reward is option/QoL/cosmetic — never power.
  - Full 1–30 track authored in §6.3.5 (placements + Token amounts are systems-designer placeholders).
- Rationale: delivers the user's "each level visibly unlocks X" (failure-is-fuel ★) while keeping the
  §6.3.4 agency that exists to dodge the XP-funnel trap — by shrinking Tokens to a focused Mastery-relic
  choice lane fed by milestones. Preserves the thematic starter criteria as achievements (Pillar 5).
- Code impact: **post-VS** (the VS ends at Gym 1 — Trainer Hub / meta-progression is a separate Epic).
  When built: remove the per-run Token earn (`floor(run XP / 100)`); add a **Battle Pass reward-track**
  data asset (level → reward[]), granted on level-up by the Trainer-Level service; reposition Hub
  upgrades + meta-starters as track grants (drop their Token-cost fields); restrict Token spend to the
  Tier-3 Mastery-relic lane; route the starter thematic criteria into the achievement system (Q19);
  add the Battle Pass track UI surface (ties Q23). Data-driven per PA0001 (no inline reward literals).
- Status: [✅] GDD updated (Notion §6.3.4/§6.3.5/§6.4.2/§6.5.2/§6.6.1, re-exported 2026-06-11)   [ ] Code adapted (post-VS)

### CL-018 — Biome↔Region binding confirmed + Naturalist's Lens   (resolves Q21)
- Date: 2026-06-11
- Topic / §: Topic 7 §7.3.1 (biome↔Region binding confirmation + modifier-steer note), §7.8.3.1
  (Region Modifier pool 16 → 17)
- Change: **Option C — opt-in biome-steer modifier.**
  - **Part 1 (confirmation, no spec change):** biomes are Region-bound (§7.3.1 per-Region eligible set +
    primary-biome weighting; §7.10 per-Region biome focus). Region Modifiers stay orthogonal to biomes
    **except** via the one new modifier below. Add a sentence to §7.3.1 stating this explicitly.
  - **Part 2 (new content):** **Naturalist's Lens** — new Region Modifier (pool 16 → 17, tier Medium).
    At Region start the player chooses one biome from the Region's **eligible** set; it becomes that
    Region's **primary biome** (dominant Wild-Area weighting) for the Region, overriding the default
    primary. Reuses the existing per-Region primary-biome weighting (§7.3.1) — no new sampling logic.
    Guard: picker offers only eligible biomes; every biome has a full Common/Uncommon/Rare pool
    (§7.3.3, ≥3 species) so the 3-species offer (§7.3.2) never starves; chosen biome is dominant, not
    exclusive (secondary biomes still appear).
- Rationale: lets players **sculpt their recruit pool** (Pillar 3) toward a desired type/species via a
  telegraphed, opt-in pick that costs a modifier slot — answering the "modifiers steer biomes?" half of
  Q21 without a hidden global tilt and without scope-creeping the other 16 modifiers.
- Code impact: add a **`Naturalist's Lens`** entry to the `RegionModifierSO` pool
  (`RegionModifierPool.BuildAll`) with a chosen-biome parameter (like Type Affinity's chosen type).
  The Wild-Area biome sampler (Region primary-biome weighting) must consult the active modifier:
  if Naturalist's Lens is active, the chosen biome **replaces the Region's default primary** for
  weighting. The modifier-pick UI (`RegionModifierSelectUI`) needs a biome sub-choice for this
  modifier (offer = Region's eligible biomes); biome eligibility comes from the Region/biome config.
  **VS-relevant** (R1 Meadow/Cave/River). All weights systems-designer-tunable. Add EditMode tests:
  primary-biome override, eligible-only guard, secondary biomes still sampled.
- ¹⁴ **Code complete (2026-06-11, 1155/1155 green).** `RegionModifierKind.NaturalistLens` + pool entry
  (`naturalist_lens`, Medium, Magnitude 5 = weight-boost factor) → pool 16→17. `RegionModifierResolver`
  gains `GrantsBiomeSteer` + `BiomeSteerBoost`. New pure helper `WildAreaBiomeWeighting`
  (`ResolveSteerBiome` + `BuildOptions`): the steered biome's weight ×boost (dominant), all other eligible
  biomes keep their weight (dominant, not exclusive → 3-species offer never starves); eligible-only guard;
  null/ineligible chosen biome falls back to the **top non-primary eligible biome** (auto-surface).
  `RunStateSO.NaturalistLensBiome` (per-Region; cleared on `SetRegionModifier`/reset). `WildAreaNode
  Controller.PickBiome` now routes through the helper using the active modifiers — so the modifier is
  **live in the VS via auto-surface** the moment it's picked (R1 run-setup pick already wired, CL-016).
  +8 EditMode tests (`WildAreaBiomeWeightingTests` ×6, resolver ×2). **Follow-up (not blocking):** an
  explicit biome **sub-picker** in `RegionModifierSelectUI` so the player chooses *which* biome to steer
  to (today it auto-surfaces the top non-primary) — exact parity with the Type Affinity sub-picker
  follow-up; + `NaturalistLensBiome` save round-trip (biomes aren't in the ID registry yet).
- Status: [✅] GDD updated (Notion §7.3.1 + §7.8.3.1, re-exported 2026-06-11)   [✅] Code adapted — 1155 green (logic + VS auto-surface; explicit biome sub-picker = UI follow-up)

### CL-015 — City → Choice Plaza + risky optional City Gym   (resolves Q1)
- Date: 2026-06-10
- Topic / §: Topic 2 §2.1.4 (City interstitials), Topic 7 §7.8 (City node detail), Topic 4 §4.5.3
  (bonus Badge sources)
- Change: **Option B — Choice Plaza.** The City is no longer a fixed linear Center→Shop→Reflection.
  - **Limited visit budget:** Shop + Reflection always available; the player visits **only 2 of** the
    premium nodes (budget tunable): **City Gym**, **Pokémon Center** (now optional), **Grand Dojo**,
    **Black Market**.
  - **City Gym:** optional Gym-tier fight (CL-013 power premium) vs a type outside the run pool →
    4th Badge + guaranteed Rare relic + ₽. Enters at current HP (no free pre-heal); a wipe is not a
    run-loss but applies Trauma to the fainted + forfeits the Badge attempt. Subsumes the post-VS
    Secret Tournament (§4.5.3.1) as the primary bonus-Badge source; Perfect Clear (§4.5.3.2) stays.
  - **Pokémon Center** becomes an optional visit (heal + therapy + Daycare, costs a visit + small ₽).
  - **Grand Dojo** = city-tier CL-009 Dojo. **Black Market** = Rare/Epic relic at HP/Trauma cost.
  - Badge cap unchanged (max 4/run; pool 12).
- Rationale: delivers the user's three asks at once — risky City Gym (4th Badge), Shop kept, Center
  made optional ("probably not") — and turns the City into an StS Act-end choice hub (Pillars 1/3/4/5),
  while giving the §4.5.3 bonus Badge an interactive home and reusing CL-013 Gym tech.
- Code impact: **post-VS** (the VS ends at Gym 1 — no immediate code). When built: a City scene/flow
  with a visit-budget selector; `NodeType.CityGym` (or a flagged Gym encounter) wired to the bonus-Badge
  award + Rare relic + the current-HP entry + wipe→Trauma/forfeit branch (non-run-loss); Center becomes
  a budget-gated optional node; `Grand Dojo` = a city-tier Dojo config; `Black Market` node (relic-for-
  HP/Trauma). Reflection unchanged. Reconcile §4.5.3 (City Gym replaces Secret Tournament as the bonus
  source). Content: City Gym roster (off-pool Gym leaders), Black Market pool.
- Status: [✅] GDD updated (Notion §2.1.4/§2.7 + §7.8/§7.8.4 + §4.5.3, re-exported 2026-06-10)   [ ] Code adapted

### CL-014 — Catch: deterministic Catchability Gauge   (resolves Q22)
- Date: 2026-06-10
- Topic / §: Topic 7 §7.3.4.1 (catch flow), §7.3.4.2 (ball tiers), §7.3.4.3 (rationale)
- Change: **Option D — deterministic Catchability Gauge** (catch-rate *feel*, no RNG; Pillar 1 intact).
  - 0–100 gauge on the wild Pokémon; **catch succeeds when gauge = 100**.
  - `CatchThreshold(HP%) = 30 + (anyStatus ? 20 : 0) + ballBonus (Great +15 / Ultra +30)`.
  - `gauge = clamp(0,100, round(100 × (100 − HP%) / (100 − CatchThreshold)))` (linear fill).
  - Basic ball: catch at HP ≤ 30% (no status) / ≤ 50% (status). **Removes** the old "status → catch
    at ANY HP" (status now = +20pt, non-stacking).
  - Throw at gauge < 100 → fail + ball spent; gauge = 100 → success → Victory + full XP (§7.3.4.1 step
    6 unchanged); HP ≤ 0 → faint, recruit lost.
- Rationale: the user wanted a catch-rate %, but a roll violates Pillar 1; a deterministic gauge gives
  the same satisfying "filling meter" feel while staying fully telegraphed, and the 30%/50% tightening
  makes status a real tool instead of a trivializer.
- Code impact: `PokeballConsumableSO.CatchHPThreshold` re-specs to base **30** (was 50); status adds
  +20pt. New pure `Catchability(hpPercent, hasStatus, ballThreshold) → (gauge 0–100, isCatchable)`;
  catch resolution checks `isCatchable` instead of the old `HP<50% / status→anyHP` rule (in the
  catch/Pokéball consumable handler). UI: catchability gauge on the wild Pokémon + Pokéball hover
  state (Topic 10 / ui-programmer). Update §7.3.4 EditMode tests to the new thresholds. Systems-designer
  to verify the 30%→0% band is hittable with lean CL-006 early decks.
- ⁹ **Code complete (2026-06-10, 1090/1090 green).** `WildCatchResolver.Catchability(wild, effect)`
  returns the 0–100 gauge (`round(100·(1−hpFrac)/(1−threshold))`, clamped); `Evaluate` now catches at
  gauge==100. `CatchConsumableEffectSO`: `CatchThresholdPercent` 0.5→0.30 + new `StatusCatchBonusPercent`
  (0.20), replacing the removed `CatchWithAnyStatus` (status is now +20pt, not catch-at-any-HP).
  `pokeball.asset` + `VS_ItemSeeder` (ConsCatch) + `VS_Verifier` updated to 0.30/0.20. `CombatScreenUI`
  Pokéball card shows the gauge ("Catchability N% — weaken or apply status" / "CATCH at gauge 100").
  Tests: `WildCatchResolverTests` rewritten to the gauge model (+gauge value cases); the old
  full-HP-with-status catch test re-specced to the +20pt window. **Follow-up:** a *visual* gauge bar on
  the wild portrait (ui-programmer, Topic 10) — the logic + text telegraph are done.
- Status: [✅] GDD updated (Notion §7.3.4.1–.3, re-exported 2026-06-10)   [✅] Code adapted — logic 1090 green (visual bar = UI follow-up)

### CL-013 — Gym phases: remove mid-evo, power premium + per-type signature Phase 2   (resolves Q9)
- Date: 2026-06-10
- Topic / §: Topic 4 §4.3.7 (phase types — evolution scope), §4.4.2 (tier table), §4.4.3 (phase
  template), §4.4.4.3 (Gym Leader design rules)
- Change: **Option D.**
  - **Remove mid-fight evolution from Gym aces** (§4.4.4.3) — reserved for rival/Champion only; the
    "Evolution Phase" type (§4.3.7) stays in the catalog but is Champion/rival-scoped.
  - **Gym power premium:** Gym Pokémon sit a defined level bump above the Region wild band (ace >
    non-ace) — tunable `ProgressionConfigSO`/encounter-config number (placeholder).
  - **Per-type signature Phase 2:** each of the 12 Gym types gets exactly one Phase-2 archetype from
    a 4-archetype menu — **Entrenchment** (Rock, Ground), **Status Siege** (Poison, Grass, Bug),
    **Onslaught** (Fire, Fighting, Normal), **Tempo Control** (Electric, Psychic, Ice, Water). Phase 1
    = setup for all; ace Phase 3 (≤20%) = last-stand minus evolution; non-ace stays 2-phase.
- Rationale: replaces the "epic" evolution beat with a learnable, telegraphed per-type identity that
  makes each Gym distinct (Pillar 1) and forces repositioning (Pillar 2), while "more powerful
  Pokémon" lands as a clean level premium. Reuses CL-012 Home Field + CL-011 intent-hide.
- Code impact: remove the 50%-HP evolution-eligibility branch from the Gym ace path in
  `GymLeaderController` (keep it on Champion/rival). Phase-2 archetype = a per-Gym-type enum/data
  field driving the forced phase behaviour (Entrenchment = +Def stage + Home-Field DR clause;
  Status Siege = Mass Status of the Gym's signature status; Onslaught = Mass Attack + Home-Field ×1.5;
  Tempo Control = AP/swap tax + Para/Freeze, optional intent-hide). Encounter-gen applies the Gym
  level premium. Most archetypes compose existing systems (phase types §4.3.7, Home Field CL-012,
  status §4.2, intent-hide CL-011) — limited net-new combat tech. Content: assign one archetype +
  signature status per Gym type (12 entries).
- ¹⁰ **Part 1 code complete (2026-06-10, 1090/1090 green) — Gym mid-evolution removed.** The generic
  mid-evo engine (`PokemonInstance.MidFightEvolutionTarget` + `CombatController.EvolveMidFight`) is
  KEPT for the future rival/Champion; only the Gym path stops using it. Removed `MidFightEvolution`
  from `GymPokemonSlot`; dropped the wiring in `GymLeaderController`; cleaned `VS_GymSeeder` +
  `VS_R1GymPoolSeeder` (+ flavor text) + `VS_Verifier`; flipped `GymLeaderControllerTests` /
  `GymLeaderAuditTests` to assert aces don't evolve (`GymBossPhaseTests` keeps testing the engine
  directly — valid for rival/Champion). Power premium = Gym levels already authored above route tier
  (lead L13–14 / ace L15–16 vs R1 wild L5–10) — authoring, no code. The 4 Gym `.assets` keep harmless
  orphaned `MidFightEvolution:` keys (Unity ignores removed-field keys; self-clean on reserialize).
  **Part 2 (per-type signature Phase 2 archetypes) DEFERRED** to the CL-012 Home Field pass (shared
  boss-combat substrate; full 12-type menu is post-VS content).
- ¹² **Code complete (2026-06-10, 1116 green).** Part 1 (mid-evo removal): `MidFightEvolution`
  dropped from `GymPokemonSlot`; `GymLeaderController` no longer sets `MidFightEvolutionTarget` (engine
  kept for rival/Champion); seeders/verifier/tests updated. Part 2 (per-type Phase 2): `Phase2Archetype`
  enum + `GymLeaderSO.Phase2ArchetypeForType` mapper; ace carries it (GymType fallback). CombatController —
  Entrenchment (+Def on Phase-2 entry), Onslaught/Status Siege (forced offensive / Status+Debuff intents
  via `FilterByPhase2Archetype`), Tempo Control (per-turn AP tax). `BattleConfigSO`
  +`Phase2EntrenchmentDefStages`(2) +`Phase2TempoApTax`(1). +18 GymBossPhaseTests. Power premium =
  authored Gym levels (no code).
- Status: [✅] GDD updated (Notion §4.3.7 + §4.4.4.3 + new §4.4.4.4, re-exported 2026-06-10)   [✅] Code adapted — 1116 green

### CL-012 — Field effects: tiered neutral Battlefield + enemy-owned Home Field   (resolves Q8)
- Date: 2026-06-10
- Topic / §: Topic 4 §4.3.8 (field effects) + §4.8.2 (category stacking note)
- Change: **Option D — Tiered.** Fields gain an `owner` flag (`Neutral` / `Enemy`).
  - **Neutral Battlefield** (wild / Region 3+): symmetric, both sides — current model sharpened.
    Launch set = Sunny Day (Fire ×1.5 / Water ×0.5), Rain Dance (Water ×1.5 / Fire ×0.5), Electric
    Terrain (Electric ×1.3 grounded + Paralysis blocked on grounded), **Sandstorm (new hazard class:
    Rock/Ground/Steel immune; all others −5% max HP at end of their turn)**.
  - **Enemy-owned Home Field** (Gym / Elite): same fields, `owner = Enemy` → the boss sets a Home
    Field of its own type at combat start (telegraphed badge); enemy moves of that type ×1.5, player
    same-type moves ×1.0 (no boost). No player-side suppression at launch. **Closes gap #33.**
  - **Counterplay:** new Shop consumable **"Smoke Ball"** clears the active field (any class) for the
    rest of combat. Follow-up content (not launch-blocking): player field-setting moves that overwrite
    a Home Field with a neutral Battlefield, and a rare "Weather Vane" relic that flips ownership.
- Rationale: symmetric always-on fields read as passive wallpaper; an `owner` flag makes boss fields a
  telegraphed threat you answer (Pillars 1+2) while wild fields stay light flavour, reusing the
  existing field engine + the CL-011 ownership pattern. Sandstorm ties fields to the faint/swap economy.
- Code impact: **Field model** gains an `owner` enum (`Neutral`/`Enemy`); the damage pipeline gates the
  field multiplier by owner + attacker side (today it applies to all). `GymLeaderController` +
  `EliteTrainerController` set an enemy-owned Home Field of their type at combat start. **Sandstorm** =
  new per-turn end-of-turn hazard tick (heaviest new piece; immune types Rock/Ground/Steel). New
  **"clear field" consumable** effect (Defog). Region-3 neutral Battlefield placement unchanged.
  Seize-moves + Weather Vane relic are deferred content.
- ¹¹ **Code complete (2026-06-10, 1116 green) across 3 increments.** A) `FieldState` gained an
  independent `Hazard` slot + `Sandstorm` kind; `FieldEffectResolver.GetDamageMultiplier(attackerIsEnemy)`
  applies the Home Field type ×1.5 to ENEMY attackers only (closes gap #33) + `GetEndOfTurnHazardDamage`
  /`IsSandstormImmune` (Rock/Ground/Steel); `BattleConfigSO` +`HomeFieldTypeMultiplier`(1.5)
  +`SandstormHazardPercent`(5); `CombatController` threads attacker side + ticks Sandstorm. B) New
  `ClearFieldConsumableEffectSO` + dispatch case clears the field; seeded as **"Defog"** (not "Smoke
  Ball" — collision with the smoke_ball relic; GDD §4.3.8.6 reconciled to Defog; the −20%-first-hit
  relic was then renamed **smoke_ball → Barrier Charm** (2026-06-10), freeing the "Smoke Ball" name —
  Focus Sash stays the separate survive-lethal Held Item (§8.4.4); GDD §8.3.3/§6.6.1 updated). C) Per-type Phase-2 archetypes (CL-013 Part 2) ride this — see CL-013.
- Status: [✅] GDD updated (Notion §4.3.8 + §4.8.2 + §4.4.4.3 + §4.3.8.6 Defog, re-exported 2026-06-10)   [✅] Code adapted — 1116 green

### CL-010 — XP: Active 100% / Box 75% baseline   (resolves Q12)
- Date: 2026-06-07
- Topic / §: Topic 5 §5.2.1 (XP sources); Topic 8 §8.3.3 (Exp Share relic re-spec)
- Change: All Box Pokémon earn combat XP — **Active 100%, benched 75%** baseline. **Exp Share**
  relic lifts benched to **100%** (was +50% to bench).
- Rationale: CL-006 makes leveling gate moves too, so Active-only would make benched Pokémon
  unusable; 75% keeps the Box viable with a slight active reward.
- Code impact: XP-award flow iterates the **whole Box** (×0.75 for non-Active); add
  `ProgressionConfigSO.BenchXpShare = 0.75`. Re-spec the Exp Share relic effect (50% → lift bench
  to 100%). Touches the combat-end XP award + relic hook.
- ⁷ **Code complete (2026-06-10, 1074/1074 green).** The Epic-10 XP-award system already existed and
  was wired (`MapViewUI.AwardXpAndLevelUp` → `XPAwarder`/`LevelUpResolver`) — the original "system
  doesn't exist" blocker was stale. Changes: `ProgressionConfigSO.BenchXpShare = 0.75f` (new) +
  `ExpShareBoxFraction` re-spec `0.5f → 1.0f` (Exp Share now lifts bench to 100%); new pure helper
  `XPAwarder.AwardToBench(box, activeTeam, activeXp, fraction, cfg)` credits every benched mon
  `floor(activeXp·fraction)` and runs `LevelUpResolver.Process` off-screen (bench mons now level up,
  which they previously never did); `MapViewUI` always credits the Box (fraction = 0.75 baseline, 1.0
  with Exp Share) instead of only when the relic was held. +4 `ProgressionTests` (AwardToBench:
  75% credit + skip-active, Exp-Share 100% lift, off-screen level-up, guards). `.asset` untouched —
  the float fields aren't serialized, so the new code defaults apply.
- Status: [✅] GDD updated (§5.12.5 override block)   [✅] Code adapted — 1074/1074 green

### CL-004 — Defer League / Champion (scope)   (resolves Q11)
- Date: 2026-06-05
- Topic / §: Topic 2 §2.1.6, Topic 4 §4.6/§4.7 (+ §4.5.2 Boons, Q10 parked)
- Change: Active build target is **R1 → City1 → R2 → City2 → R3 → Victory Road**. League +
  Champion spec is **kept but stamped `⚠️ DEFERRED — redesign after the R1→VR loop`**.
- Rationale: Finish and polish the core loop before designing/building the finale.
- Code impact: **No deletion.** Do not build League/Champion encounters yet; treat existing
  League stubs as parked. Re-open after the loop is solid.
- Status: [✅] GDD updated — DEFERRED banners on §2.1.6, §4.6, §4.7   [n/a] scope marker

---

## Anticipated change surface (preview, not yet decided)

As a heads-up for engineering on which areas are likely to move once the design
pass lands (subject to the actual decisions):

| Likely-affected area | Driven by | GDD topics |
|---|---|---|
| Hand/draw size constant | Q3 | Topic 3 §3.2.2/§3.7 |
| Wild catch = Victory outcome + XP | Q4 | Topic 3 §3.1, Topic 7 §7.3.4 |
| Stat-stage ladder constants | Q5 | Topic 4 §4.2.6 |
| **Bestiary → Pokédex** rename (system + UI + data) | Q6 | Topics 4, 6, 1, 10 |
| Unknown-intent frequency + knowledge-reveal rule | Q7 | Topic 4 §4.3.5/§4.3.9 |
| Field-effect model | Q8 | Topic 4 §4.3.8 |
| Gym phase model (remove Gym mid-evo) | Q9 | Topic 4 §4.4.3/§4.4.4 |
| League/Champion deferral (scope, no deletion) | Q11 | Topic 2 §2.1.6, Topic 4 §4.6/§4.7 |
| XP distribution (Active vs full Box) | Q12 | Topic 5 §5.2 |
| Starting move count + learn curve | Q13 | Topic 5 §5.2/§5.3.6/§5.10 |
| Ability system keep/cut/rework | Q14 | Topic 5 §5.5/§5.8 |
| Evolution payload + per-stage branch choice | Q15 | Topic 5 §5.3 |
| Move Tutor as standalone node | Q16 | Topic 7 §7.6/§7.8, Topic 5 §5.4.2 |
| Trauma cap / per-stack value | Q17 | Topic 6 §6.2 |
| Battle Pass replacing/absorbing Tokens (CL-019 ✅ decided) | Q18 | Topic 6 §6.3.4/§6.3.5 |
| Achievement catalog expansion | Q19 | Topic 6 §6.7 |
| Save/Load persistence manifest (new doc) | Q20 | Topic 9 §9.8, Topic 6 §6.10 |
| Biome↔Region binding + Naturalist's Lens (CL-018 ✅ decided) | Q21 | Topic 7 §7.3.1/§7.8.3.1 |
| Catch thresholds (30%/50%) or catch-rate% | Q22 | Topic 7 §7.3.4 |
| Full per-system UI spec | Q23 | Topic 10 |
| City Gym + new City nodes | Q1 | Topic 2 §2.1.4, Topic 7 §7.8 |
| Region Modifier timing + pool | Q2 | Topic 2 §2.1.4, Topic 7 §7.8.3 |
| League Boons → relic rarity (parked w/ league) | Q10 | Topic 4 §4.5.2, Topic 8 §8.3 |
