# Coding Standards — Project Ascendant

## C# Style

- C# 10+, Unity 2022 LTS minimum
- Namespaces: `ProjectAscendant.Combat`, `ProjectAscendant.Core`, etc.
- No `var` for non-obvious types. Explicit types aid readability.
- `private` fields: `_camelCase`. Public: `PascalCase`. Constants: `ALL_CAPS`.
- No public fields on MonoBehaviours. Use `[SerializeField] private`.

## Architecture Rules

- **ScriptableObjects** for all content data. No inline serialized balance values.
- **Event Bus** (SO channels) for all cross-system communication.
- **HSM** for game flow states. No nested if-chains for state logic.
- **Factory** for spawning PokemonInstance, CardInstance, IntentData.
- **GameRNG** wrapper for all randomness. Seed from RunState SO.

## GDD Cross-Reference Comments

Every non-trivial game logic block must have a GDD section comment:
```csharp
// Per §3.3.1 — Manual swap cost: 1st=1AP, 2nd=2AP, 3rd=3AP
// SF/SB do NOT increment this counter
private int CalculateSwapCost(int swapCount) { ... }
```

## Data-Driven Requirement

Never do this:
```csharp
const float STAB_MULTIPLIER = 1.5f;  // ❌ hardcoded
```

Always do this:
```csharp
[SerializeField] private BattleConfigSO _battleConfig;
// then use: _battleConfig.StabMultiplier
```
