---
globs: ["tests/**/*.cs"]
---
# Test Rules

- Test method naming: `MethodName_Scenario_ExpectedResult`
  e.g., `ResolveDamage_FrozenLeadFaints_FreezeVoided`
- Every test must have an explicit GDD §reference comment:
  `// Per §3.3.5.1 — Faint precedence rule`
- No test may depend on execution order. Tests must be independently runnable.
- Edge case coverage required for all combat rules listed in CLAUDE.md.
  These are not optional: they are the load-bearing rules of the game.
- Play Mode tests for visual/timing validation only.
  Edit Mode tests for all logic (damage, status, deck, swap counter).
