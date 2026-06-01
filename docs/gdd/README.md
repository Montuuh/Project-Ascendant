# GDD — Read Policy

**Canonical rule for all agents and humans.** Do not read GDD from Notion MCP.

## Two surfaces

| Action | Where |
|--------|--------|
| **Read** (implement, review, test, design review) | `docs/gdd/topic-N-*.md` |
| **Edit** (spec changes, OPEN flags, lock/unlock) | **Notion** |

Notion is the authoring source. Local markdown is the **read surface**.

---

## Mandatory: snapshot today before any read

Every time you are about to read GDD — new session, new task, or after a break — run:

```bash
npm run gdd:ensure
```

Equivalent:

```bash
node docs/scripts/ensure-gdd-snapshot.js
```

This checks `docs/gdd/snapshot-status.json`. If `exportCalendarDate` is not **today**
(local calendar date), it re-exports all 10 topics from Notion (requires `NOTION_TOKEN` in `.env`).

| Script | When |
|--------|------|
| `npm run gdd:ensure` | **Before reading** — auto-export if stale |
| `npm run gdd:check` | Check only — exit 1 if stale |
| `npm run gdd:export` | Force export |

**Freshness record:** `docs/gdd/snapshot-status.json`

```json
{
  "exportCalendarDate": "2026-06-02",
  "exportRunAt": "2026-06-01T23:06:22.453Z",
  "topicCount": 10
}
```

---

## Read workflow

```
1. npm run gdd:ensure
2. Open local-topic-map (§ → file): .claude/skills/project-ascendant-gdd/references/local-topic-map.md
3. Read docs/gdd/topic-N-<slug>.md — search for §N.N.N
4. Cite § in code: // Per §3.3.1 — …
```

Use the **`gdd-read`** skill (`.claude/skills/gdd-read/SKILL.md`) as the agent entry point.

---

## Do not

- Fetch Notion pages to **read** spec during implementation (wastes context; may drift from git)
- Edit `docs/gdd/*.md` by hand (read-only snapshots; edit in Notion, then re-export)
- Skip the freshness check because "I read it earlier today in chat" — run `gdd:ensure` anyway

---

## After editing GDD in Notion

```bash
npm run gdd:export
git add docs/gdd/
git commit -m "docs: snapshot GDD — <what changed>"
```

Or `npm run gdd:ensure` if you also need to refresh today's date stamp.
