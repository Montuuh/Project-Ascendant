---
globs: ["Assets/Scripts/UI/**/*.cs"]
---
# UI Rules

- No game state ownership. UI reads from events and ScriptableObjects only.
- No direct calls to combat or progression systems.
- All strings must be localisation-ready (no hardcoded display text).
- Damage preview must show calculated final value (including STAB, type, crit).
- Grayed-out non-playable cards must remain visible (never hidden).
- Intent targeting display must always show: slot label + current occupant name.
- HP bars for boss-tier enemies must show phase-threshold markers.
