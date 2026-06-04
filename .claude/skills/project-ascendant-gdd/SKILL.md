---
name: project-ascendant-gdd
description: >
  GDD workflow for Project Ascendant. Use when writing to Notion (OPEN flags,
  BACKLOG), syncing snapshots, or resolving write authority. For READING spec,
  use the gdd-read skill first (docs/gdd/README.md policy).
---

# Project Ascendant — GDD Workflow

## Read vs Write

| Action | Where |
|--------|--------|
| **Read** spec before coding/review/testing | `docs/gdd/topic-N-*.md` |
| **Write** spec, OPEN flags, BACKLOG | Notion (MCP) — see `references/page-index.md` |
| **Sync** Notion → local | `gdd-sync` skill / `export-gdd.js` |

Notion remains canonical for **edits**. Local files are the canonical **read surface**
for agents (faster, no MCP burn, matches git snapshot).

---

## 1. Before Reading GDD (every time)

**Policy:** `docs/gdd/README.md` — use the **`gdd-read`** skill.

```bash
npm run gdd:ensure
```

Mandatory before opening any `docs/gdd/topic-*.md`. See `gdd-read/SKILL.md`.

---

## 2. Before Implementing Any System

```
1. ensure-gdd-snapshot.js
2. Open references/local-topic-map.md → pick topic file for your §
3. Read the relevant §N.N.N section in docs/gdd/topic-N-*.md
4. If section missing or PENDING → stop, flag user, do not invent spec
5. Cite § in code: // Per §3.3.1 — …
```

Do **not** preload all 10 topics. Read one topic (two if cross-cutting).

Gameplay rules (faint, swap, targeting, consumables, boss phases, etc.) live **only**
in those topic files — not in CLAUDE.md or agent prompts.

---

## 3. Write Authority (Notion)

**The GDD is a living document throughout development.** Every topic is freely
editable in Notion — there is no "locked" status. When a playtest, implementation,
or design decision changes the spec, **edit the canonical Notion text directly** so
it permanently reflects the current version of the game. Then re-export the snapshot
(§6) so `docs/gdd/` matches.

| Status | Meaning |
| --- | --- |
| 🟢 In Progress | Active, editable. The default for all topics. |
| 🟡 Pending | Scoped but not yet written — scaffolding bullets only. |

Do not add `⚠️ OPEN:` deferral blocks for decisions that are already made — fold the
decision straight into the spec. Reserve `⚠️ OPEN` only for genuinely unresolved
questions awaiting a user call (see §4).

---

## 4. Flag-Don't-Resolve Protocol

On ambiguity in spec:

1. Add `> ⚠️ OPEN (date): …` to Notion page via MCP
2. Add BACKLOG gap entry
3. Tell user before continuing
4. Stub in code: `// TODO: Pending GDD §N.N.N — …`

---

## 5. BACKLOG (Notion)

Update when status changes, gaps resolve, or user confirms decisions.
Append Sequential Changelog — never delete rows.

---

## 6. Export / Snapshot

```bash
node docs/scripts/export-gdd.js          # manual export
node docs/scripts/ensure-gdd-snapshot.js # if stale today
```

Writes `docs/gdd/snapshot-status.json`. Commit with `docs: snapshot GDD — …`

Local markdown is **read-only** — never edit `docs/gdd/*.md` directly.

---

## 7. Section Numbering

Use §N.N.N in comments and commits. Grep code ↔ GDD via section numbers.

---

## 8. Topic Ownership

See `references/local-topic-map.md` for file paths and § prefix routing.

Notion URLs: `references/page-index.md`

---

## 9. Engineering Rules (not gameplay — Topic 9)

Codebase-wide architecture (not duplicated per mechanic):

- ScriptableObject-driven balance data
- `GameRNG` seeded determinism
- Event Bus for cross-system comms
- View layer does not own game state

Details: `docs/gdd/topic-9-technical-architecture.md` + `.claude/docs/coding-standards.md`
