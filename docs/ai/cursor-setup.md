# Cursor Setup — Project Ascendant

Local only (`.cursor/` is gitignored). Copy into user Cursor config.

## GDD — Read Local (not Notion MCP)

**Policy:** [docs/gdd/README.md](../gdd/README.md)

```bash
npm run gdd:ensure    # required before every GDD read
```

Use **`gdd-read`** skill. Notion MCP = BACKLOG + **editing** GDD only.

## MCP Servers

### Coplay (Unity editor bridge)

```json
{
  "mcpServers": {
    "coplay-mcp": {
      "command": "uvx",
      "args": ["--python", ">=3.11", "coplay-mcp-server@latest"],
      "env": { "MCP_TOOL_TIMEOUT": "720000" }
    }
  }
}
```

Requires Unity open + `com.coplaydev.coplay` in Packages.

### Notion (BACKLOG + GDD writes)

Notion Cursor plugin or `@notionhq/notion-mcp-server` with `NOTION_TOKEN` in `.env`.

## Optional Project Rule

Copy `docs/ai/cursor-rules/project-ascendant.mdc` → `.cursor/rules/`

## Session Checklist

1. Unity open (for verification)
2. `npm run gdd:ensure`
3. Read `production/session-state/active.md`

## Local-Only Paths

| Path | Why |
|------|-----|
| `.cursor/` | MCP, personal toggles |
| `.claude/settings.local.json` | Permissions |
| `CLAUDE.md` | Personal master config |
| `.env` | `NOTION_TOKEN` for export |
