---
name: code-review
description: >
  Review C# code for Project Ascendant. Use when you've written a system and
  want it checked, before committing a significant feature, after implementing
  a GDD system, or when asked to "review this code", "check this", or "is this
  right?". Validates against architecture rules, design constraints from the GDD,
  coding standards, and pillar compliance. Produces actionable findings, not a
  vague "looks good".
---

# Code Review — Project Ascendant

## Review Checklist

Run every item. Mark ✅ pass / ⚠️ warning / ❌ fail.

### Architecture
- [ ] No `FindObjectOfType` or `GameObject.Find` in production code
- [ ] No game logic in MonoBehaviours (view-layer only)
- [ ] Cross-system communication uses Event Bus (SO channels), not direct references
- [ ] All balance/config values come from ScriptableObjects, not inline literals
- [ ] RNG uses `GameRNG` wrapper, not `UnityEngine.Random`
- [ ] Factory pattern used for spawning PokemonInstance, CardInstance, IntentData

### GDD Compliance (load-bearing rules — any failure = Critical)
- [ ] `CurrentHP == 0` is the fainted check — no `IsFainted` flag anywhere (§2.4.1)
- [ ] Faint resolution voids Freeze position-lock (§3.3.5.1)
- [ ] Mastery Move cannot be replaced by TM or Tutor (§4.3.9.2)
- [ ] Backstrike on empty slot: fizzle, no redirect (§4.3.4.1)
- [ ] Cleave: hits all non-fainted slots, min 1, never fizzles (§4.3.4.1)
- [ ] Manual swap counter: increments only on manual swaps, NOT on SF/SB (§3.3.1)
- [ ] Swap counter resets each turn start (§3.3.1)
- [ ] Intent resolution targets slot, not PokemonInstance reference (§4.3.2)
- [ ] Stat stages persist across boss phase transitions (§4.4.3.1)
- [ ] Champion buff hard-capped at +20% (§4.7.1)
- [ ] Consumables restore at combat end — not consumed permanently (§3.5)

### Code Quality
- [ ] GDD section comments on all non-trivial game logic (`// Per §N.N.N`)
- [ ] TODO format: `// TODO: Pending GDD §N.N.N` or `// TODO: [#issue]`
- [ ] No magic numbers without a named constant or SO field
- [ ] Namespaces correct (`ProjectAscendant.[Domain]`)
- [ ] Private fields use `_camelCase`, public use `PascalCase`
- [ ] No `var` for non-obvious types

### Testing
- [ ] Edge cases from qa-lead coverage list have corresponding unit tests
- [ ] Tests follow naming: `MethodName_Scenario_ExpectedResult`
- [ ] Each test has a GDD § comment

## Output Format

For each ❌ failure: state the file, line range, rule violated, and exact fix needed.
For each ⚠️ warning: state the concern and recommended action.
End with: **Approve** / **Approve with minor fixes** / **Needs rework**.
