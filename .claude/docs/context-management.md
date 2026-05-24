# Context Management

## Session Continuity

Context compacts on long sessions. Protect against drift:

1. `production/session-state/active.md` is the session memory. Update it after every significant decision.
2. Notion BACKLOG is the persistent canonical state. Re-read it after compaction.
3. The GDD topic pages are the spec. Re-read the relevant page if you're unsure what was decided.

## active.md Template

```markdown
# Session State — Project Ascendant

**Date:** YYYY-MM-DD
**Active topic:** Topic N — [name]
**Sprint goal:** [one sentence]
**GDD status:** [last written section]
**Open decisions:**
- [ ] [decision needed]

**Next action:** [specific, actionable]
**Blocked on:** [or "nothing"]
**Last commit:** [hash and message]
```

## Token Economy

- Read only the GDD topic page(s) relevant to the current task.
- Do not preload all 10 GDD topics at session start.
- Agents should load their reference files on demand, not upfront.
- If context is near limit: write active.md, commit, end session cleanly.
