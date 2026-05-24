# Session State — Project Ascendant

**Date:** 2026-05-24
**Active topic:** Epic 2 — Core Architecture (Phase A)
**Sprint goal:** Complete all 8 Epic 2 tasks; advance to Epic 3 (Data Layer)

**Open decisions:** none

**Next action:** Task 2.5 — GameRNG (5 Streams)

**Blocked on:** nothing

**Last commit:** feat(core): Task 2.4 — Factory Pattern + Object Pooling (§9.6)

---

## Epic 2 status (Tasks 2.1–2.8)

- [x] **2.1** Service Locator — Services.cs + 6 unit tests
- [x] **2.2** EventBus Hybrid Model — static EventBus + GameEventSO<T> + 8 channels + 9 SO assets + 20 tests
- [x] **2.3** Hierarchical State Machine — GameStateNode + GameStateMachine + full state tree (§9.5.1 + §9.5.2) + 12 HSM tests (32/32 total)
- [x] **2.4** Factory + Object Pooling — Pool\<T\> + 5 factories (PokemonInstance/MoveCard/IntentData/DamageContext/Enemy) + 4 stub SOs + 19 tests (51/51 total)
- [ ] 2.5 GameRNG (5 Streams)
- [ ] 2.6 InputLog Recorder
- [ ] 2.7 SaveSystem Skeleton
- [ ] 2.8 ScriptableHook Framework

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
