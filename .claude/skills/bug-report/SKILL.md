---
name: bug-report
description: >
  File a structured bug report for Project Ascendant. Use when you've found
  a bug, a GDD violation in the code, an incorrect implementation, or an
  edge case that doesn't behave per spec. Always references the GDD section
  that specifies the correct behaviour.
---

# Bug Report — Project Ascendant

## Template

```markdown
## Bug: [Short descriptive title]

**Severity:** Critical (game-breaking) / High (major feature broken) / Medium (workaround exists) / Low (polish)
**GDD Reference:** §N.N.N — [section name]
**System:** [Combat / Deck / AI / Progression / UI / Save / Other]

### Steps to Reproduce
1.
2.
3.

### Expected Behaviour
(Per GDD §N.N.N:)

### Actual Behaviour


### Root Cause (if known)


### Suggested Fix (if known)


### Notes

```

## Severity Guidelines for Project Ascendant

**Critical:** Violates a load-bearing GDD rule (§§ listed in CLAUDE.md).
Examples: IsFainted flag exists, Backstrike redirects instead of fizzling,
swap counter increments on SF/SB, Mastery Move can be replaced.

**High:** Combat system produces wrong outcome. Player can reach unwinnable state.

**Medium:** Incorrect but recoverable. Visual glitch with functional impact.

**Low:** Visual/audio issue, text error, minor inconsistency.
