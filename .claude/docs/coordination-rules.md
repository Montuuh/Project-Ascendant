# Coordination Rules

## Agent Roster (9 agents)

| Agent | Domain | When to Use |
|---|---|---|
| `producer` | Sprint planning, scope, **orchestration** | "What next?", multi-agent workflows |
| `lead-programmer` | C# architecture, system implementation | Any coding task |
| `game-designer` | Pillar compliance, balance review, mechanics | Design decisions |
| `systems-designer` | Formulas, stat curves, number tuning | Balance math |
| `content-designer` | Move kits, evolutions, relic content | Content authoring |
| `ui-programmer` | Combat screen, card display, readability | UI implementation |
| `unity-specialist` | Unity API, Addressables, UI Toolkit, Coplay | Unity-specific work |
| `qa-lead` | Test plans, edge cases, bug reports | Quality checks |
| `art-director` | Sprite briefs, palette, visual readability | Art direction (not C#) |

## Skills (slash / on-demand)

| Skill | Use when |
|---|---|
| **`gdd-read`** | **Before any GDD read** — ensures today's snapshot (`docs/gdd/README.md`) |
| `orchestrate` | Multi-agent delegation, producer-led workflows |
| `unity-verify` | After C# changes — compile + EditMode via Coplay |
| `project-ascendant-gdd` | Write Notion GDD / BACKLOG; sync workflow |
| `gdd-sync` | Snapshot GDD to `docs/gdd/` |
| `scope-check` | Is this in VS scope? |
| `sprint-plan` | Session planning |
| `pillar-check` / `balance-check` / `design-review` | Design validation |
| `code-review` / `bug-report` / `playtest-report` | Quality workflows |

## Escalation Rules

- **Pillar conflict** → `game-designer` decides; user breaks ties
- **Architecture conflict** → `lead-programmer` decides; user if cross-system
- **Scope creep** → `producer` flags; user approves/rejects
- **GDD gap during implementation** → ⚠️ OPEN flag in Notion, stub in code, flag user
- **Visual readability** → `art-director` + `ui-programmer`; user approves mock/brief

## Multi-Agent Orchestration (Cursor)

**Human = conductor.** Producer (parent agent) plans and delegates; user approves gates.

```
User → Producer (plan) → [User approves]
     → Task subagents (parallel explore/review OR sequential implement)
     → Producer (synthesize) → [User approves]
     → lead-programmer / ui-programmer (writes)
     → unity-verify + qa-lead → [User approves commit]
```

**Rules:**
- Subagents need **full context in the Task prompt** (no shared chat memory)
- Never parallelize two agents editing the same files
- Read-only parallel: `explore` + `game-designer` + `qa-lead` + `scope-check`
- Handoff templates: `.claude/docs/handoff-templates.md`
- Full protocol: `docs/ai/orchestration.md`

## Collaboration Protocol (all agents)

1. **Question** — clarify scope before starting
2. **Options** — present 2–3 approaches with pros/cons
3. **Decision** — user picks (or agent recommends + user confirms)
4. **Draft** — produce draft; do not commit
5. **Approval** — user approves → commit/write

No agent may write without asking first (except trivial fixes user requested inline).
No agent may commit without explicit user instruction.

## IDE Split

| Tool | Config location | Notes |
|---|---|---|
| Claude Code | `.claude/` agents, hooks, skills | Hooks need Git Bash on Windows |
| Cursor | Local `.cursor/` (gitignored) | Copy MCP from `docs/ai/cursor-setup.md` |
| Shared | `docs/ai/`, `.claude/docs/`, `docs/gdd/` | Committed guidance + GDD snapshots |
