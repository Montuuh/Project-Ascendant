# Context Management

## Session Continuity

Context compacts on long sessions. Protect against drift:

1. **`production/session-state/active.md`** — session memory; update after significant decisions
2. **Notion BACKLOG** — persistent state (still Notion MCP)
3. **GDD spec** — `docs/gdd/README.md` policy → `gdd-read` skill → `npm run gdd:ensure` → topic file

## active.md Template

```markdown
# Session State — Project Ascendant

**Date:** YYYY-MM-DD
**Phase / Epic:** Epic N — [name]
**Sprint goal:** [one sentence]
**Next action:** [specific, actionable]
**Blocked on:** [or nothing]
**Last commit:** [hash — message]
**Open gaps:** [#43, …]
**Test status:** [N/N EditMode — date verified]
```

Keep it **short** (≤30 lines). Archive detail to Notion BACKLOG changelog.

## Token Economy

- Read only GDD topic pages relevant to the current task
- Do not preload all 10 topics at session start
- Agents load reference files on demand
- Near context limit: write `active.md`, stop cleanly

## Cursor-Specific

| Concern | Practice |
|---------|----------|
| Subagent memory | Paste full handoff into each Task prompt (`.claude/docs/handoff-templates.md`) |
| Rules | Project rules in user `.cursor/rules/` — templates in `docs/ai/cursor-rules/` |
| Skills | `.claude/skills/` (Claude Code) + read `docs/ai/README.md` in Cursor |
| MCP | Notion + Coplay in **local** `.cursor/mcp.json` (gitignored) — see `docs/ai/cursor-setup.md` |
| Verification | `unity-verify` skill after C# edits; never claim tests pass without evidence |
| Multitask | Background Task for explore/review; serialize file edits |

## Claude Code-Specific

- Hooks in `.claude/settings.json` fire on SessionStart, commit, write, compact
- On **Windows**, hooks require **Git Bash** in PATH
- `session-start.sh` loads `active.md` + `.claude/docs/backlog-hint.md`

## After Compaction

1. Re-read `production/session-state/active.md`
2. Re-read Notion BACKLOG gap log
3. Confirm last commit via `git log -1`
4. Do not assume prior chat decisions — verify in files or Notion
