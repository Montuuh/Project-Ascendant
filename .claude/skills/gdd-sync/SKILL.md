---
name: gdd-sync
description: >
  Sync the GDD from Notion to local markdown in docs/gdd/. Use when snapshot is
  stale, before a major sprint, after locking a topic, or when asked to export/
  snapshot/sync GDD. Updates snapshot-status.json. Prefer ensure-gdd-snapshot.js
  for automatic stale detection.
---

# GDD Sync — Export Notion to Local Markdown

## Quick path (recommended)

```bash
node docs/scripts/ensure-gdd-snapshot.js
```

Exports only if today's snapshot is missing or stale. Requires `NOTION_TOKEN` in `.env`.

## Manual export

```bash
# PowerShell — load token from .env first if needed
node docs/scripts/export-gdd.js
```

## Verify

```bash
node docs/scripts/check-gdd-snapshot.js
ls docs/gdd/topic-*.md
cat docs/gdd/snapshot-status.json
```

## After export

Ask user: "Commit the snapshot now?"

```bash
git add docs/gdd/
git commit -m "docs: snapshot GDD — <what changed>"
```

## Agent read path

After sync, **read** from `docs/gdd/topic-N-*.md` — not Notion MCP.
Map: `.claude/skills/project-ascendant-gdd/references/local-topic-map.md`

## Notes

- Local files are read-only archives; canonical **edits** stay in Notion
- `snapshot-status.json` records `exportCalendarDate` (local date) for freshness checks
