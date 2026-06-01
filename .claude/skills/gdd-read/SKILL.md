---
name: gdd-read
description: >
  Read Project Ascendant GDD spec from local docs/gdd/. ALWAYS run before opening
  any topic file. Enforces today's snapshot via ensure-gdd-snapshot.js. Use when
  implementing, reviewing, testing, or answering any design spec question. Never
  read GDD from Notion MCP for spec — Notion is for edits only. Policy: docs/gdd/README.md
---

# GDD Read — Local Spec Only

**Policy:** `docs/gdd/README.md`

## Step 1 — Snapshot today (required, every read)

```bash
npm run gdd:ensure
```

Do not open `docs/gdd/topic-*.md` until this succeeds.

- **Fresh:** proceed
- **Stale + no NOTION_TOKEN:** ask user to configure `.env` or run export manually
- **Failed export:** stop; do not guess spec from memory

Verify optionally:

```bash
npm run gdd:check
```

Status: `docs/gdd/snapshot-status.json` → `exportCalendarDate` must be today.

## Step 2 — Pick file

Use `.claude/skills/project-ascendant-gdd/references/local-topic-map.md`:

| § prefix | File |
|----------|------|
| §1.x | `docs/gdd/topic-1-game-overview.md` |
| §2.x | `docs/gdd/topic-2-core-gameplay-loop.md` |
| §3.x | `docs/gdd/topic-3-micro-loop.md` |
| §4.x | `docs/gdd/topic-4-combat-system.md` |
| §5.x | `docs/gdd/topic-5-progression.md` |
| §6.x | `docs/gdd/topic-6-roguelike-progression.md` |
| §7.x | `docs/gdd/topic-7-scenario-nodes.md` |
| §8.x | `docs/gdd/topic-8-items-relics.md` |
| §9.x | `docs/gdd/topic-9-technical-architecture.md` |
| §10.x | `docs/gdd/topic-10-art-ui-audio.md` |

Read only the § sections needed for the current task.

## Step 3 — Notion?

**No** — for reading spec. Use Notion MCP only to **write** GDD or read BACKLOG/Epics.

For writes: `project-ascendant-gdd` skill → `references/page-index.md`.
