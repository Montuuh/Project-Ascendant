# Contributing to Project Ascendant

## Branch Policy

- Trunk-based development on `main`.
- Feature branches are optional for isolated work; merge via pull request.
- **Never force-push `main`.**
- Tag foundation milestones: `v0.0.0-foundation`, `v0.1.0-combat-vs`, etc.

## Agent Collaboration Protocol

All implementation follows the **Question → Options → Decision → Draft → Approval** cycle:

1. **Question** — Clarify scope and constraints before starting.
2. **Options** — Present 2–3 approaches with trade-offs.
3. **Decision** — User picks (or agent recommends and user confirms).
4. **Draft** — Produce draft; do not commit yet.
5. **Approval** — User approves → commit.

No agent may commit without explicit user instruction.

## Coding Standards

See [`.claude/docs/coding-standards.md`](.claude/docs/coding-standards.md) for full C# style guide.

Key rules:
- All game values in ScriptableObjects — no hardcoded balance values.
- RNG via `GameRNG` wrapper — never `UnityEngine.Random`.
- Event Bus (SO channels) for cross-system communication.
- GDD section references in every non-trivial logic block (`// Per §3.3.1`).
- No `MonoBehaviour` game logic — view layer only.

## Unity Setup

1. Install **Unity 6000.4.6f1** (Unity 6 LTS) via Unity Hub.
2. Open the project from this folder.
3. Required packages install automatically via `Packages/manifest.json`.
4. Open `Assets/Scenes/Boot.unity` to start.

## GDD Canonical Source

The Game Design Document lives in Notion. Read the relevant topic page before
implementing any system. See `.claude/skills/project-ascendant-gdd/SKILL.md`
for the full workflow.
