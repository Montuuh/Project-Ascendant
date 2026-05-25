# Session State — Project Ascendant

**Date:** 2026-05-25
**Active topic:** Epic 3 — Data Layer (ScriptableObjects)
**Sprint goal:** Implement full SO schemas, author VS content (3 starters + 3 wild lines)

**Open decisions:** none

**Next action:** Task 3.4 — Editor Tooling (custom inspectors, asset-creation menus, bulk validator)

**Blocked on:** nothing

**Last commit:** feat(content): Task 3.3 — VS content authoring (6 Pokémon lines, items, world)

---

## Epic 2 status — ✅ COMPLETE (Tasks 2.1–2.8)

- [x] **2.1** Service Locator — Services.cs + 6 unit tests
- [x] **2.2** EventBus Hybrid Model — static EventBus + GameEventSO<T> + 8 channels + 9 SO assets + 20 tests
- [x] **2.3** Hierarchical State Machine — GameStateNode + GameStateMachine + full state tree + 12 tests (32/32)
- [x] **2.4** Factory + Object Pooling — Pool\<T\> + 5 factories + 4 stub SOs + 19 tests (51/51)
- [x] **2.5** GameRNG (5 Streams) — GameRNG xorshift32 + RNGStreams + BannedApiValidator + 13 tests (64/64)
- [x] **2.6** InputLog Recorder — InputLogEntry/InputLog/InputLogRecorder/InputLogReplayer + 7 tests (71/71)
- [x] **2.7** SaveSystem Skeleton — SaveHeader + facade + atomic write + stub SOs + 10 tests (81/81)
- [x] **2.8** ScriptableHook Framework — ScriptableHook + EventContext + 6 hook subclasses + HookSubscriber + 8 tests (89/89)

**Total EditMode tests: 89/89 ✅**

---

## Epic 3 status — 🟡 IN PROGRESS

- [x] **3.1** SO Schema Design — 25 SO types implemented (§9.3.2.1–9.3.2.6)
  - StatGrowthCurveSO, PokemonSpeciesSO (expanded), EvolutionBranchSO, MoveSO (expanded)
  - AbilitySO (expanded), HeldItemSO (expanded), RelicSO, ConsumableSO, TMSO, BadgeSO
  - MoveEffectSO + 6 subclasses, ConsumableEffectSO + 6 subclasses
  - BattleConfigSO, EconomyConfigSO, MapGenerationConfigSO
  - RunStateSO (expanded), MetaProgressionSO (expanded), SettingsSO (expanded)
  - BestiaryProgressSO, BiomeSO, MysteryEventSO, TrainerArchetypeSO
  - EncounterTableSO, DifficultyModifierSO, RegionModifierSO, LeagueBoonSO
  - GameTypes.cs expanded (BranchArchetype, Biome, SynergyCategory, AbilityCategory + Support, NodeType, StringIntPair)
  - PokemonInstance.SelectedBranch → EvolutionBranchSO reference
- [x] **3.2** Data Schema Tests — 7 new tests (96/96 total)
  - StatGrowthCurve: Level1=0, accumulate, null-safe
  - PokemonInstance.Reset: HP=0, status cleared, stat stages cleared, SelectedBranch=null
- [x] **3.3** Content Authoring — ✅ COMPLETE (63/63 verifier checks, 0 failed)
  - **3.3.A+B** VS_ContentSeeder.cs — 6 Pokémon lines, 86 moves, 12 abilities, 6 growth curves, 15 branches, 21 species
  - **3.3.C** VS_ItemSeeder.cs — 10 consumables (Potion→Super Potion upgrade chain, status cures, Ether, X Attack, Pokéball)
  - **3.3.D** 15 relics (10 Common + 5 Uncommon; hook stubs — wired in Epic 4)
  - **3.3.E** 5 held items (Charcoal, Mystic Water, Magnet, Miracle Seed, Leftovers; type Lead Aura wiring per §5.5.4)
  - **3.3.F** 3 TMs (TM05 Surf, TM11 Body Slam, TM15 Foresight; 2 new TM-exclusive moves)
  - **3.3.G** 3 biomes, 4 mystery events, 4 trainer archetypes, 3 difficulty modifiers, 4 badges, BattleConfigSO, EconomyConfigSO, MapGenerationConfigSO
  - VS_Verifier.cs — 63 spot-checks across all authored content; all pass
- [ ] **3.4** Editor Tooling — Custom inspectors, asset-creation menus, bulk validator
- [ ] **3.5** Lint & Data Discipline — Roslyn analyzer, naming convention enforcement

**Total EditMode tests: 96/96 ✅**

---

## Asset inventory (Assets/ScriptableObjects/VS/)

```
Abilities/          12 assets
GrowthCurves/        6 assets
Moves/              88 assets (86 species + 2 TM-exclusive)
Branches/           15 assets
Species/Starters/   12 assets
Species/Wild/        9 assets
Consumables/        10 assets
Relics/             15 assets
HeldItems/           5 assets
TMs/                 3 assets
Biomes/              3 assets
MysteryEvents/       4 assets
TrainerArchetypes/   4 assets
DifficultyModifiers/ 3 assets
Badges/              4 assets
Configs/             3 assets
─────────────────────────────
Total:            ~196 .asset files
```

---

## HSM state tree (Task 2.3 — implemented)

```
GameRootState (root)
├── MainMenuState
├── HubState
├── RunState
│   ├── MapViewState
│   ├── NodeState
│   │   └── CombatState
│   │       ├── CombatStartState
│   │       ├── TurnLoopState
│   │       │   ├── DrawPhaseState
│   │       │   ├── IntentPhaseState
│   │       │   ├── ActionPhaseState
│   │       │   ├── ResolutionPhaseState
│   │       │   └── TurnEndState
│   │       ├── CombatVictoryState
│   │       └── CombatDefeatState
│   ├── EvolutionScreenState
│   └── RunEndState
└── GameOverState
```

---

**GDD status:** Topics 1–10: 🔒 Locked

**Notion BACKLOG:** https://www.notion.so/3610450715b48109b2ebd15d97e69fa7
**Notion GDD:** https://www.notion.so/3610450715b481588234e2e5f1b756ee
**Epic 2:** https://www.notion.so/36a0450715b4811c8fb4e935922ec7c2
**Epic 3:** https://www.notion.so/36a0450715b4810bbd02c69610804d12
