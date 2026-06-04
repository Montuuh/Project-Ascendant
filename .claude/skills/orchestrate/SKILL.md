---
name: orchestrate
description: >
  Multi-agent orchestration for Project Ascendant. Use when the user asks to
  "orchestrate", "delegate", "run as producer", coordinate art/design/code
  work, spawn specialist agents, or plan a multi-step epic with handoffs.
  Parent agent acts as producer; delegates via Task subagents; user approves
  each gate. Read docs/ai/orchestration.md for full protocol.
---

# Orchestrate — Multi-Agent Workflow

You are the **orchestrator** (usually acting as `producer`). You plan, delegate,
and synthesize. You do **not** implement code yourself unless the task is trivial
or the user explicitly says "implement directly."

## When to Use

- Epic-sized work spanning design + code + UI + art
- User says "orchestrate", "delegate to specialists", or "act as producer"
- Parallel review needed (GDD compliance + test plan + scope check)

## Protocol (mandatory gates)

```
1. CLARIFY  — Restate goal, Epic #, GDD §, VS in/out. Run `ensure-gdd-snapshot.js` if reading spec.
2. PLAN     — Break into subtasks with owner agent + acceptance criteria + dependencies.
3. APPROVE  — User picks plan (or revises). No implementation before this.
4. DELEGATE — Spawn Task subagents (see matrix below). Pass full context in each prompt.
5. SYNTHESIZE — Merge outputs; flag conflicts; present options to user.
6. EXECUTE  — Only after user approves: delegate writes to lead-programmer / ui-programmer.
7. VERIFY   — unity-verify skill OR qa-lead review before commit.
8. CLOSE    — Update production/session-state/active.md; suggest BACKLOG changelog.
```

## Delegation Matrix

| Task type | Subagent | Mode |
|-----------|----------|------|
| VS / scope | `producer` or scope-check skill | read-only |
| GDD / pillars | `game-designer` | read/write per user direction (GDD is a living doc) |
| Numbers / tuning | `systems-designer` | read-only |
| Content kits | `content-designer` | draft only |
| C# systems | `lead-programmer` | may edit after approval |
| UI | `ui-programmer` | may edit after approval |
| Unity API / Coplay | `unity-specialist` | may edit after approval |
| Tests / regression | `qa-lead` | read-only (files bugs, does not fix) |
| Codebase search | `explore` | read-only |
| Shell / git | `shell` | cautious |
| Sprites / visual brief | `art-director` | briefs only; human or external tool produces pixels |

## Task Prompt Template (copy into every delegation)

```markdown
## Context
- Epic: [N — name]
- GDD: §[X.Y]
- VS scope: [IN / OUT / DEFER — one line]
- User decision: [what was already approved]

## Your assignment
[Single focused task]

## Acceptance criteria
- [ ] …

## Constraints
- Read GDD § in `docs/gdd/` before proposing
- No commit without user instruction
- Flag genuine ambiguities; do not invent spec (but fold settled decisions into the GDD)

## Return format
1. Findings / draft
2. Risks / open questions
3. Recommended next step
```

## Parallel vs Sequential

**Parallel (safe):** explore + scope-check + qa-lead test plan + game-designer pillar review.

**Sequential (required):** design → implementation → tests → commit.

**Never parallelize** two agents editing the same files.

## Cursor-Specific Notes

- Subagents do **not** inherit chat history — paste context into each Task prompt.
- Use `run_in_background: true` for long explore/review tasks in Multitask Mode.
- Coplay MCP: see `unity-verify` skill; restart Unity if bridge freezes after many domain reloads.
- User is the integrator: merge subagent outputs before approving implementation.

## Output to User (after planning phase)

```markdown
## Orchestration Plan — [Epic / task name]

**Goal:** …
**VS verdict:** IN | OUT | DEFER

### Subtasks
| # | Owner | Task | Done when | Depends on |
|---|-------|------|-----------|------------|

### Parallel batch (optional)
- …

### User decision needed
- …
```

Wait for user approval before spawning implementation subagents.
