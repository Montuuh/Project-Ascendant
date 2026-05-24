---
name: gdd-sync
description: >
  Sync the GDD from Notion to local markdown files in docs/gdd/. Use when you
  want to snapshot the current GDD state to git, before a major implementation
  sprint, after locking a topic, or when asked to "export the GDD", "snapshot
  the docs", or "sync to git".
---

# GDD Sync — Export Notion to Local Markdown

Exports all 10 GDD topic pages from Notion to `docs/gdd/` as markdown files.

## Steps

1. Check if `docs/scripts/export-gdd.js` exists. If not, create it from
   `.claude/skills/project-ascendant-gdd/references/export-script-template.js`.

2. Run the export:
   ```bash
   NOTION_TOKEN=$(cat .env | grep NOTION_TOKEN | cut -d= -f2) node docs/scripts/export-gdd.js
   ```

3. Verify output files exist in `docs/gdd/`:
   ```bash
   ls docs/gdd/
   ```

4. Ask user: "Commit the snapshot now?"
   If yes:
   ```bash
   git add docs/gdd/
   git commit -m "docs: snapshot GDD — <describe what changed since last snapshot>"
   ```

## Notes

- The exported files are READ-ONLY archives. Never edit them directly.
- All canonical edits go through Notion.
- Notion is the source of truth. The local files are a portfolio artifact.
