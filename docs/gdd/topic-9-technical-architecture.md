<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Source: https://www.notion.so/3610450715b4811b83cae23d6ed2a154 -->
<!-- Exported: 2026-05-19T23:10:26.762Z -->
<!-- To update: run `node docs/scripts/export-gdd.js` and commit -->

**Status:** 🟡 Pending


**Last Updated:** 2026-05-15 (migrated from Drive partial draft)


**Cross-references:** Topic 1 (§1.3.2 Engineering Pillars), every other Topic (architecture supports all systems).


---


# Scope


Full Unity architecture spec — ScriptableObject schemas, Event Bus implementation, Hierarchical State Machine for game flow, Factory Pattern for entity spawning, save/load system, determinism guarantees (seeded RNG, input log replay), build pipeline.


---


# Foundational Principles


To ensure this project serves as a highly impressive software engineering portfolio piece, the architecture must heavily emphasise **modularity**, **separation of concerns**, and **data-driven design**.


---


# Drive Original Draft (to be expanded)


## Data Management: ScriptableObjects


To avoid hardcoding, every Pokémon base species, moves, relics, items, and encounters should be **ScriptableObjects**. This allows for massive scalability and time efficiency.


_Example:_ `MoveData` contains the `Type` of the move, its `BaseDamage`, `ActionPoints` cost, and an array of `MoveEffect` (a base class for custom logic like applying Burn or drawing cards).


## Decoupled Logic: Event-Driven Architecture


Implement a robust Event Bus (or ScriptableObject-based event channels). The UI should never directly query the combat logic. For example, when a Pokémon takes damage, the `HealthSystem` fires an `OnDamageTaken` event. The UI Canvas listens to this and updates the health bar; the Audio Manager listens and plays a sound. This ensures tightly encapsulated, modular code.


## Game Flow: Hierarchical State Machines


_ToDo — to be expanded during deep-dive._


## Factory Pattern for Entities


Use a factory pattern to spawn enemies and construct the player's deck at the start of a battle based on the current party data. This should keep the initialisation logic centralised and clean.


---


# Required Deep-Dive Areas

1. **Determinism architecture:** Seeded RNG, input log replay, bit-exact reproducibility. Non-negotiable per Engineering Pillar 3.
2. **Save/load system:** Atomic combat assumption; auto-save between map nodes.
3. **Hierarchical state machine spec:** Game-flow states, combat sub-states, transitions.
4. **Event Bus implementation choice:** ScriptableObject-based channels vs central pub/sub vs hybrid.
5. **Build pipeline:** PC primary, mobile-portability commitments without dedicated effort.
6. **Mod-friendliness:** ScriptableObject-driven content lends itself to external authoring — in scope or out?
7. **Testing strategy:** Unit testability of combat resolution; replay-based regression testing using the determinism layer.
