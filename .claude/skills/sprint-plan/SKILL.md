---
name: sprint-plan
description: >
  Plan the next sprint for Project Ascendant. Use at the start of a work session,
  when deciding what to work on, or when asked "what should I do next?". Reads
  the BACKLOG, vertical-slice.md, and session state for a grounded, prioritised
  task list. Protects the vertical slice definition ruthlessly.
---

# Sprint Plan — Project Ascendant

## North Star

Read `.claude/docs/vertical-slice.md` — full VS scope and 17 Epic URLs.

**Current phase (2026-06):** Epics 1–12 complete → **Phase D** (13 UI, 14 Audio,
15 Accessibility, 16 QA, 17 Build).

**Open VS gaps error:** gaps #43, #44, achievement combat hooks — may gate ship.

## Sprint Planning Protocol

1. Read `production/session-state/active.md`
2. Read Notion BACKLOG (gap log, open decisions)
3. Read relevant Notion **Epic page** for the chosen Epic
4. Identify highest-priority **unblocked** task that advances VS
5. Break into 1–3 day chunks with explicit "done when"
6. Flag blockers (open GDD decisions, Coplay bridge down, etc.)

## Output Format

```markdown
## Sprint: [date]

**Goal:** [one sentence]
**Epic:** [N — name + Notion URL]

**Tasks:**
1. [Task] — Done when: [criterion] — Est: [hours] — Owner: [agent]

**Blocked by:**
- [blocker] → [resolution]

**Post-VS (do not start):**
- [item] — [why]

**Verify:**
- [ ] unity-verify (EditMode suite green)
- [ ] GDD § cited in commit/PR
```

## Agent Assignment Hint

Use `.claude/docs/coordination-rules.md` Epic → agent routing.
For multi-discipline tasks, use `orchestrate` skill first.
