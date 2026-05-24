---
globs: ["Assets/Scripts/Combat/**/*.cs", "Assets/Scripts/Systems/**/*.cs"]
---
# Combat & Systems Rules

- ALL game values (damage, AP costs, HP, stat multipliers) must come from
  ScriptableObjects. No inline literals for balance values.
- RNG must use `GameRNG` (seeded wrapper). Never `UnityEngine.Random`.
- Use delta-time for any time-based logic. Never `Time.time` raw comparisons.
- No UI references. Combat logic fires events; UI listens.
- Every public method that can affect game state must have a corresponding
  unit test in `tests/unit/`.
- Intent targeting: always operate on slot index, never on PokemonInstance
  reference directly from the intent target.
- `CurrentHP == 0` means fainted. Never add an `IsFainted` flag.
- Swap counter: increment only on manual swaps, never on SF/SB resolution.
