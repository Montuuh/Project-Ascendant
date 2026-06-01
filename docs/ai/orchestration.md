# Multi-Agent Orchestration

How to run art, design, and programming as a coordinated team in Cursor (or Claude Code).

## Roles

```
You (human) ──approve──► Producer (parent agent)
                              │
         ┌────────────────────┼────────────────────┐
         ▼                    ▼                    ▼
   game-designer      lead-programmer        art-director
   content-designer    ui-programmer          (briefs only)
   systems-designer    unity-specialist
                       qa-lead (review)
```

**You** are the conductor. Agents do not auto-commit or auto-merge each other's work.

## Three Patterns

### A — Single chat, producer-led (recommended)

```
Act as producer. Orchestrate Epic 13.2 — combat HUD Lead indicator.
1. scope-check
2. Plan subtasks with owners
3. Wait for my approval before any code
```

Parent spawns Task subagents, synthesizes, you approve each gate.

### B — Multiple chats

| Chat | Persona |
|------|---------|
| Planning | producer |
| Code | lead-programmer |
| Design | game-designer |
| Art | art-director |

Paste the same Epic link + handoff into each. You integrate outputs.

### C — Parallel review only

Safe to run in parallel (read-only):

- `explore` — find existing Lead UI wiring
- `game-designer` — confirm GDD §3.3
- `qa-lead` — list test gaps
- `scope-check` — VS in/out

Then sequential: implement → verify → commit.

## Gate Sequence

| Gate | Who | Output |
|------|-----|--------|
| 1 Scope | producer / scope-check | IN / OUT / DEFER |
| 2 Spec | game-designer | GDD § + acceptance criteria |
| 3 Plan | producer | Subtask table + agents |
| **User approve** | You | Pick option |
| 4 Implement | lead-programmer, ui-programmer | Draft diff |
| **User approve** | You | OK to commit |
| 5 Verify | unity-verify + qa-lead | Tests green, no spec drift |
| 6 Close | producer | Update active.md, BACKLOG note |

## Handoffs

Templates: `.claude/docs/handoff-templates.md`

Every Task subagent prompt must include:

- Epic + GDD §
- User-approved decision
- Single task + acceptance criteria
- "Return format" (findings, risks, next step)

Subagents **do not** see prior chat — paste context every time.

## Cursor Task Tool

Parent agent can delegate to typed subagents matching `.claude/agents/` names:

- `lead-programmer`, `ui-programmer`, `unity-specialist`
- `game-designer`, `content-designer`, `systems-designer`
- `qa-lead`, `producer`, `art-director`
- `explore`, `shell`, `generalPurpose`

Use `run_in_background: true` for long explore/review in Multitask Mode.

**Do not** parallelize two agents editing the same files.

## Art in the Loop

Art is brief-first:

1. `ui-programmer` — wireframe / layout
2. `art-director` — sprite brief (dimensions, PPU, palette)
3. Human or external tool — PNG production
4. `unity-specialist` — import / Addressables

Agents do not replace pixel production unless you explicitly use image tools.

## Verification

Never claim "tests pass" without:

1. Coplay `check_compile_errors`
2. `RunEditModeTests.Execute()` → logs show `Fail: 0`

See `.claude/skills/unity-verify/SKILL.md`.

## Anti-Patterns

- Skipping GDD read before implementation
- Parent implementing large diffs without delegating
- Auto-committing subagent output without your review
- Stale VS definitions (always use `.claude/docs/vertical-slice.md`)
- Ignoring open gaps #43 / #44 when touching save or difficulty combat
