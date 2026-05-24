# Coordination Rules

## Agent Roster (8 agents)

| Agent | Domain | When to Use |
|---|---|---|
| `lead-programmer` | C# architecture, system implementation | Any coding task |
| `game-designer` | Pillar compliance, balance review, mechanic proposals | Design decisions |
| `unity-specialist` | Unity API, Addressables, UI Toolkit, performance | Unity-specific Qs |
| `systems-designer` | Formulas, stat curves, number tuning | Balance math |
| `qa-lead` | Test plans, edge cases, bug reports | Quality checks |
| `content-designer` | Move kits, evolution branches, relic content | Content authoring |
| `ui-programmer` | Combat screen, card display, readability | UI implementation |
| `producer` | Sprint planning, scope management, progress | Planning sessions |

## Escalation Rules

- **Pillar conflict** → `game-designer` is the decision point. User breaks ties.
- **Architecture conflict** → `lead-programmer` decides. Escalate to user if cross-system.
- **Scope creep** → `producer` flags it. User approves or rejects explicitly.
- **GDD gap discovered during implementation** → `lead-programmer` adds ⚠️ OPEN flag to Notion, stubs in code, flags to user.

## Collaboration Protocol (all agents)

1. **Question** — clarify scope before starting
2. **Options** — present 2-3 approaches with pros/cons
3. **Decision** — user picks (or you recommend and user confirms)
4. **Draft** — produce draft, do not commit yet
5. **Approval** — user approves → commit/write

No agent may write to a file without asking first.
No agent may commit without explicit user instruction.
