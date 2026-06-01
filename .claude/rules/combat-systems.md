---
globs: ["Assets/Scripts/Combat/**/*.cs", "Assets/Scripts/Systems/**/*.cs"]
---
# Combat & Systems Rules

Architecture only — mechanic rules live in `docs/gdd/topic-3-micro-loop.md` and
`docs/gdd/topic-4-combat-system.md`. Read the relevant § before editing.

- ALL balance values (damage, AP, HP, multipliers) from ScriptableObjects — no inline literals
- RNG via `GameRNG` (seeded). Never `UnityEngine.Random`
- Delta-time for time-based logic. No raw `Time.time` comparisons for gameplay
- No UI references in logic — fire events; UI listens
- Non-trivial state changes should have EditMode tests in `Assets/Tests/EditMode/`
- When implementing targeting, intents, faint, swap, or status: cite the GDD § in comments
