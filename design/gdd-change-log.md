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
| CL-007 | Q15 | Evolution: free archetype/stage + lighter payload | T5 §5.12.2 | ✅² | ☐ |
| CL-008 | Q14 | Abilities kept, decoupled to an earned learner | T5 §5.12.3 | ✅² | ☐ |
| CL-009 | Q16 | Move Tutor → paid "Dojo" node (moves + abilities) | T7 §7.14; T5 §5.12.4 | ✅² | ☐ |
| CL-010 | Q12 | XP: Active 100% / Box 75% baseline | T5 §5.12.5; T8 §8.3.3 | ✅² | ☐ |

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
    Pidgey): 2 moves @ L1, then L8/L11 (tunable). A starter now begins R1 with **2 moves**.
  - **1035/1035 EditMode green.**
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
- Status: [ ] GDD updated   [ ] Code adapted

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
- Status: [ ] GDD updated   [ ] Code adapted

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
| Battle Pass replacing/absorbing Tokens | Q18 | Topic 6 §6.3 |
| Achievement catalog expansion | Q19 | Topic 6 §6.7 |
| Save/Load persistence manifest (new doc) | Q20 | Topic 9 §9.8, Topic 6 §6.10 |
| Biome↔Region binding | Q21 | Topic 7 §7.3/§7.10 |
| Catch thresholds (30%/50%) or catch-rate% | Q22 | Topic 7 §7.3.4 |
| Full per-system UI spec | Q23 | Topic 10 |
| City Gym + new City nodes | Q1 | Topic 2 §2.1.4, Topic 7 §7.8 |
| Region Modifier timing + pool | Q2 | Topic 2 §2.1.4, Topic 7 §7.8.3 |
| League Boons → relic rarity (parked w/ league) | Q10 | Topic 4 §4.5.2, Topic 8 §8.3 |
