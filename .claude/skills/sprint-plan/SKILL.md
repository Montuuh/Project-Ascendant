---
name: sprint-plan
description: >
  Plan the next sprint for Project Ascendant. Use at the start of a work session,
  when deciding what to work on, or when asked "what should I do next?". Reads
  the BACKLOG and session state to give a grounded, prioritised task list.
  Protects the vertical slice definition ruthlessly.
---

# Sprint Plan — Project Ascendant

## Vertical Slice Definition (the north star)

Region 1 playable end-to-end:
- 6 starter Pokémon options
- 8 recruitable Pokémon
- 15 relics
- 1 Gym Leader (1 of the 4 Region-1-tier types)
- All node types implemented
- Working save/load

**If a proposed task does not serve this definition, flag it as post-VS scope.**

## Sprint Planning Protocol

1. Read `production/session-state/active.md` for current state
2. Read Notion BACKLOG for open topics, open decisions, and gap log
3. Identify the highest-priority unblocked task
4. Break it into 1-3 day chunks
5. State explicitly what "done" looks like for each chunk
6. Flag any blockers (open GDD decisions, unresolved Sev-2 gaps)

## Output Format

```markdown
## Sprint: [date]

**Goal:** [one sentence]

**Tasks:**
1. [Task] — [Done when: specific criterion] — [Estimated: X hours]
2. ...

**Blocked by:**
- [blocker] → [who resolves it / what decision is needed]

**Post-VS (do not start):**
- [item] — [why it's post-VS]
```
