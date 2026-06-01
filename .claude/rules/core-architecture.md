---
globs: ["Assets/Scripts/Core/**/*.cs"]
---
# Core Architecture Rules

- Zero allocations in hot paths (combat loop, damage resolution, deck draw).
  Use object pooling for PokemonInstance, CardInstance, IntentData.
- Event Bus channels are ScriptableObject assets. Never instantiate them in code.
- MonoBehaviours are view-layer only. No game logic in MonoBehaviours.
- No `FindObjectOfType`, `GameObject.Find`, or `GetComponent` in production code paths.
  Use dependency injection via the Inspector or the ServiceLocator pattern.
- All save/load must go through `SaveSystem`. Never write to PlayerPrefs directly.
- The seeded RNG instance (`GameRNG`) must be initialised with the run seed
  before any combat begins. Seed is stored in RunState ScriptableObject.
