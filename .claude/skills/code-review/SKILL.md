---
name: code-review
description: >
  Review C# code for Project Ascendant. Use when you've written a system and
  want it checked, before committing a significant feature, or when asked to
  review code. Validates architecture (Topic 9), coding standards, and GDD §
  compliance for the systems touched — by reading docs/gdd/, not a global rule list.
---

# Code Review — Project Ascendant

## Before reviewing

1. `node docs/scripts/check-gdd-snapshot.js` — warn if stale
2. Identify which GDD topics the diff touches (use `local-topic-map.md`)
3. Read the relevant § sections in `docs/gdd/topic-N-*.md`

## Review Checklist

### Architecture (Topic 9 / coding standards)
- [ ] No `FindObjectOfType` or `GameObject.Find` in production code
- [ ] No game logic in MonoBehaviours (view-layer only)
- [ ] Cross-system communication uses Event Bus (SO channels)
- [ ] Balance/config values from ScriptableObjects, not inline literals
- [ ] RNG uses `GameRNG`, not `UnityEngine.Random`
- [ ] Factory pattern for PokemonInstance, CardInstance, IntentData where applicable

### GDD compliance (topic-scoped — read the file, verify the diff)

For each system changed, open the matching `docs/gdd/topic-*.md` and confirm
the implementation matches the cited § sections. Examples of what to check
**when that system is in scope** (not on every review):

| If diff touches… | Read | Typical § |
|------------------|------|-----------|
| Faint / HP / Box | topic-2 | §2.4.x |
| Lead / swap / consumables | topic-3 | §3.3.x, §3.5 |
| Combat / intents / bosses | topic-4 | §4.3.x, §4.4.x, §4.7.x |
| Evolution / TMs / Mastery | topic-5 | §4.3.9.2, §5.x |
| Items / relics | topic-8 | §8.x |

Mark ❌ only when code contradicts the **§ you read** — cite file, line, and section.

### Code Quality
- [ ] GDD § comments on non-trivial game logic (`// Per §N.N.N`)
- [ ] TODO format: `// TODO: Pending GDD §N.N.N` or `// TODO: [#issue]`
- [ ] No magic numbers without named constant or SO field
- [ ] Namespaces: `ProjectAscendant.[Domain]`

### Testing
- [ ] Edge cases from the relevant GDD § have tests where practical
- [ ] Test names: `MethodName_Scenario_ExpectedResult`
- [ ] Tests reference GDD § when asserting spec behaviour

## Output Format

For each ❌: file, line range, **GDD § violated**, fix needed.
End with: **Approve** / **Approve with minor fixes** / **Needs rework**.
