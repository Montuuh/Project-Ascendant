# AI Tooling — Project Ascendant

This folder is the **committed** AI layer for the repo. Machine-local config
(`.cursor/`, `.claude/settings*.json`, `CLAUDE.md`) stays gitignored.

## Layout

| Path | Purpose |
|------|---------|
| `.claude/agents/` | 9 specialist agent definitions (producer, lead-programmer, …) |
| `.claude/skills/` | Workflows: orchestrate, unity-verify, gdd-sync, scope-check, … |
| `.claude/hooks/` | Claude Code automation (SessionStart, commit validation, …) |
| `.claude/docs/` | Coordination, vertical slice, handoffs, backlog hint |
| `.claude/rules/` | Path-scoped coding standards (Combat, UI, Tests, …) |
| `docs/ai/` | This folder — Cursor setup + orchestration guide |
| `docs/gdd/` | GDD snapshots + **`README.md` read policy** |
| `production/session-state/active.md` | Live sprint state (update each session) |

## Quick Start (Cursor)

1. Read [orchestration.md](./orchestration.md) — how to multiorchestrate
2. Read [cursor-setup.md](./cursor-setup.md) — MCP (Notion, Coplay), optional rules
3. At session start: read `production/session-state/active.md` + `.claude/docs/backlog-hint.md`
4. Before implementing: **`gdd-read`** → `npm run gdd:ensure` → `docs/gdd/topic-*.md` ([policy](../gdd/README.md))
5. After C# edits: **unity-verify** (Coplay compile + EditMode tests)

## Quick Start (Claude Code)

1. Copy `.env.example` → `.env` with `NOTION_TOKEN`
2. Hooks in `.claude/settings.json` run on session start / commit / write
3. Requires **Git Bash** on Windows for shell hooks

## Invoke Orchestration

In any Agent chat:

> Act as **producer**. Use the **orchestrate** skill. Plan Epic 13 task X — do not implement until I approve.

> **Delegate** to lead-programmer: [handoff from handoff-templates.md]

## Skills Index

| Skill | Trigger |
|-------|---------|
| **`gdd-read`** | **Before any GDD read** — ensures today's snapshot |
| `orchestrate` | Multi-agent, delegate, "run as producer" |
| `unity-verify` | After C# changes, verify compile/tests |
| `sprint-plan` | "What should I do next?" |
| `scope-check` | "Is this in VS scope?" |
| `gdd-sync` | Snapshot GDD from Notion to `docs/gdd/` |
| `project-ascendant-gdd` | Read local GDD; write Notion; freshness workflow |
| `pillar-check` / `balance-check` / `design-review` | Design validation |
| `code-review` / `bug-report` / `playtest-report` | Quality |

Skill files: `.claude/skills/<name>/SKILL.md`

From Topic 9 + coding standards — **not** duplicated gameplay rules:

- ScriptableObject-driven content
- Seeded `GameRNG`
- User approves commits and multi-file changes

Gameplay rules: read the relevant `docs/gdd/topic-N-*.md` § section.
