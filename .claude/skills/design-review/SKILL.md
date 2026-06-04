---
name: design-review
description: >
  Review a design proposal, GDD section, or system concept for Project Ascendant.
  Use when you've written a design spec and want it reviewed before adopting it
  into the GDD, when a GDD section is ready to move from Pending to In Progress,
  or when asked to "review this design", "is this ready to ship?", or "check this
  spec". Runs the full 4-step simulation, pillar check, and gap analysis. Gives a
  ready/not-ready verdict with specific outstanding issues listed.
---

# Design Review — Project Ascendant

## Review Purpose

The GDD is a **living document** during development — sections are always editable,
never "locked." This review judges whether a section is **solid enough to build on
and ship**, or what specific issues to resolve first. A passing review means: adopt
it into the canonical Notion GDD as the current spec.

## Review Protocol

### 1. Completeness Check
- [ ] All §N.N.N sub-sections that are referenced exist and have content
- [ ] No `[TBD]`, `[PLACEHOLDER]`, or `asd` entries remain
- [ ] Every cross-reference to another §section can be resolved
- [ ] Edge cases are explicitly handled (not implicitly assumed)

### 2. Consistency Check
- [ ] No contradictions with other GDD topics
- [ ] Terminology is consistent with existing sections
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
Any ❌ = not ready. Must resolve first.

### 5. Gap Log Review
Check the BACKLOG gap log. Are any open gaps in this topic's domain still unresolved?
Open Sev-1 or Sev-2 gaps = not ready.
Open Sev-3 gaps = ready, with notes.

## Verdict Format

```
VERDICT: [READY / NOT READY]

Outstanding issues (must resolve first):
1. [issue] — [what needs to be written/decided]

Advisory notes (ship with these open):
- [note]

Recommendation: [specific action to take]
```
