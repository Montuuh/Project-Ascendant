---
name: playtest-report
description: >
  Structure and analyse a playtest session for Project Ascendant. Use after
  playing the game, watching someone play, or reviewing recorded gameplay.
  Extracts actionable design findings, distinguishes tuning issues from design
  issues, and maps observations back to specific GDD sections and design pillars.
  Produces prioritised recommendations.
---

# Playtest Report — Project Ascendant

## Report Structure

### Session Info
- Date, build version, tester (self / other)
- Region reached, outcome (win / loss / quit)
- Approximate run length

### Raw Observations
Timestamped or sequenced notes. No interpretation yet — just what happened.

### Findings by Category

#### Feel / Pacing
- Did the Lead/Swap decision feel meaningful every turn? (Pillar 2)
- Did the player understand intents and plan accordingly? (Pillar 1)
- Did evolution feel like a meaningful identity moment? (Pillar 4)
- Was the tone cheerful and Pokémon-faithful? (Pillar 5)

#### Balance Issues
Note: distinguish **tuning** (number is wrong) from **design** (system is wrong).
Tuning issues → systems-designer for number adjustments.
Design issues → game-designer for mechanic review.

#### Friction Points
Where did the player get confused, frustrated, or stall?
Map each to a UI issue, a readability gap, or a design gap.

#### Bugs / GDD Violations
Any behaviour that contradicts a GDD §section.
Format: "Observed X. Expected Y per §N.N.N."

### Prioritised Recommendations

```
Priority 1 — Fix before next playtest (breaks the game):
- [issue] → [proposed fix] → [owner: lead-programmer / game-designer / ui-programmer]

Priority 2 — Fix this sprint (significant friction):
- [issue] → [proposed fix] → [owner]

Priority 3 — Backlog (polish / nice-to-have):
- [issue] → [proposed fix]
```

### Open Questions for Design
Things the playtest raised that aren't answered by the current GDD.
These become BACKLOG entries.
