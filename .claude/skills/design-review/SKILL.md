---
name: design-review
description: >
  Review a design proposal, GDD section, or system concept for Project Ascendant.
  Use when you've written a design spec and want it reviewed before locking it,
  when a GDD section is ready to move from In Progress to Locked, or when asked
  to "review this design", "is this ready to lock?", or "check this spec". Runs
  the full 4-step simulation, pillar check, and gap analysis. Gives a lock/no-lock
  verdict with specific outstanding issues listed.
---

# Design Review — Project Ascendant

## Review Purpose

Determines whether a GDD section is ready to move to 🔒 Locked status, or
what specific issues must be resolved first.

## Review Protocol

### 1. Completeness Check
- [ ] All §N.N.N sub-sections that are referenced exist and have content
- [ ] No `[TBD]`, `[PLACEHOLDER]`, or `asd` entries remain
- [ ] Every cross-reference to another §section can be resolved
- [ ] Edge cases are explicitly handled (not implicitly assumed)

### 2. Consistency Check
- [ ] No contradictions with already-locked topics
- [ ] Terminology is consistent with existing locked sections
- [ ] Numbers/values are internally consistent (e.g., AP costs don't add up to impossible hands)

### 3. 4-Step Simulation (required — not optional)
Run the mechanic through:
- Early run scenario
- Mid run scenario
- Late run / League scenario
- Degenerate / adversarial scenario (min HP, max buff stacking, empty bench)

Document findings from each.

### 4. Pillar Check
Mark each of the 5 pillars: ✅ / ⚠️ / ❌
Any ❌ = cannot lock. Must resolve first.

### 5. Gap Log Review
Check the BACKLOG gap log. Are any open gaps in this topic's domain still unresolved?
Open Sev-1 or Sev-2 gaps = cannot lock.
Open Sev-3 gaps = can lock with notes.

## Lock Verdict Format

```
LOCK VERDICT: [READY / NOT READY]

Outstanding issues (must resolve before lock):
1. [issue] — [what needs to be written/decided]

Advisory notes (can lock with these open):
- [note]

Recommendation: [specific action to take]
```
