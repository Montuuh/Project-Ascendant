# Project Ascendant — Systems Index

**Status:** Stub — populate as systems are implemented.
**Owner:** lead-programmer + game-designer

This document enumerates every game system, its current implementation status,
and its dependency map. Required before Pre-production → Production gate.

---

## System Categories

### Combat Systems
| System | GDD § | Status | Owner | Notes |
|---|---|---|---|---|
| DamageCalculator | §4.1.1 | Not started | lead-programmer | |
| TypeChart | §4.1.2 | Not started | lead-programmer | |
| CritSystem | §4.1.3 | Not started | lead-programmer | |
| StatusSystem | §4.2 | Not started | lead-programmer | |
| SwapController | §3.3 | Not started | lead-programmer | |
| IntentResolver | §4.3 | Not started | lead-programmer | |
| CombatHSM | §3.2 | Not started | lead-programmer | |
| DeckManager | §3.4 | Not started | lead-programmer | |
| BossPhaseController | §4.4.3 | Not started | lead-programmer | |

### Progression Systems
| System | GDD § | Status | Owner | Notes |
|---|---|---|---|---|
| XPSystem | §5.2 | Not started | lead-programmer | |
| EvolutionController | §5.3 | Not started | lead-programmer | |
| TmSystem | §5.4.1 | Not started | lead-programmer | |
| AbilitySystem | §5.5 | Not started | lead-programmer | |
| MasteryMoveSystem | §4.3.9.2 | Not started | lead-programmer | |

### Map & Roguelike Systems
| System | GDD § | Status | Owner | Notes |
|---|---|---|---|---|
| RegionMap | §2.1.2 | Not started | lead-programmer | |
| NodeController | §2.1.2 | Not started | lead-programmer | |
| BoxManager | §2.3 | Not started | lead-programmer | |
| LoadoutManager | §2.1.2.1 | Not started | lead-programmer | |
| RelicManager | Topic 8 | Not started | lead-programmer | Blocked: Topic 8 pending |
| TraumaSystem | Topic 6 | Not started | lead-programmer | Blocked: Topic 6 lock-in pending |
| SaveSystem | §1.7 | Not started | lead-programmer | |

### UI Systems
| System | GDD § | Status | Owner | Notes |
|---|---|---|---|---|
| CombatScreen | §10 | Not started | ui-programmer | |
| HandDisplay | §3.2.2 | Not started | ui-programmer | |
| IntentDisplay | §4.3.2 | Not started | ui-programmer | |
| HPBarSystem | §2.4 | Not started | ui-programmer | |
| MapView | §2.1.2.1 | Not started | ui-programmer | |

---

## Dependency Map

```
SaveSystem          ← All systems (must come first)
GameRNG             ← All systems (must come first)
EventBus            ← All systems (must come first)
DamageCalculator    ← TypeChart, CritSystem, StatusSystem
CombatHSM           ← DamageCalculator, SwapController, DeckManager, IntentResolver
BossPhaseController ← CombatHSM
EvolutionController ← XPSystem, DeckManager
TraumaSystem        ← XPSystem, HPBarSystem [blocked: Topic 6]
RelicManager        ← CombatHSM, EventBus [blocked: Topic 8]
```
