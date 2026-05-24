# Project Ascendant — Claude Code Configuration

Unity roguelike deckbuilder. Fan-made Pokémon portfolio piece.
Managed through coordinated Claude Code agents. Each agent owns a specific
domain, enforcing separation of concerns and design fidelity.

## Technology Stack

- **Engine:** Unity (latest LTS)
- **Language:** C#
- **Version Control:** Git, trunk-based development
- **Design Canon:** Notion GDD (read/write via Notion MCP)
- **Build:** Unity Build System
- **Asset Pipeline:** Unity Addressables

## Project Vision

A roguelike deckbuilder where the party IS the deck. Three active Pokémon
each contribute 4 moves to a shared hand. The Lead/Swap action-economy
tension is the signature moment-to-moment mechanic.

## Design Pillars (immutable — violations require explicit user override)

1. **Telegraphed tactics over reactive RNG**
2. **Every swap is a decision**
3. **Synergy is sculpted, not drafted**
4. **Identity through Evolution**
5. **Cheerful core, regional flavor**

**Tiebreaker:** When Pokémon thematic faithfulness conflicts with deckbuilder
pacing or pillar integrity, pacing and pillars win. Flag the conflict.

## GDD Canonical Document

The GDD lives in Notion. Always read the relevant topic page before
implementing any system. See `.claude/skills/project-ascendant-gdd/` for
the full workflow skill and page index.

@.claude/skills/project-ascendant-gdd/SKILL.md

## Active Build Target — Vertical Slice

The Vertical Slice is the current sprint. Every implementation, content
authoring, design tweak, or scope question must advance the VS. The
canonical, deeply-structured plan (17 Epics × ~340 subtasks, all linked
to GDD sections) lives in Notion under the "🎯 Vertical Slice" page.

**Before starting any task, open the relevant Epic page in Notion.**

@.claude/docs/vertical-slice.md

## Project Structure

@.claude/docs/directory-structure.md

## Unity Reference

@docs/engine-reference/unity/VERSION.md

## Coordination Rules

@.claude/docs/coordination-rules.md

## Coding Standards

@.claude/docs/coding-standards.md

## Context Management

@.claude/docs/context-management.md

## Collaboration Protocol

**User-driven collaboration, not autonomous execution.**
Every task follows: **Question → Options → Decision → Draft → Approval**

- Multi-file changes require explicit approval for the full changeset
- No commits without user instruction
- Before implementing any system: read the GDD section first

## Key Design Constraints (hardcoded — never violate in code)

- 0 HP = Fainted. No separate IsFainted flag. (§2.4.1)
- Faint overrides Freeze position-lock. (§3.3.5.1)
- Mastery Moves are immutable — TMs/Tutors cannot replace them. (§4.3.9.2)
- Backstrike on empty slot fizzles. Does not redirect to Lead. (§4.3.4.1)
- Cleave never fizzles — hits all non-fainted slots, minimum 1. (§4.3.4.1)
- Manual swap cost: 1st=1AP, 2nd=2AP, 3rd=3AP. SF/SB do NOT increment. (§3.3.1)
- Stat stages persist across boss phase transitions. (§4.4.3.1)
- Champion buff cap: +5% per defeated ally, max +20%. (§4.7.1)
- Consumables restore at combat end — they are NOT expendable. (§3.5)
- Intents target POSITIONS (slots), not specific Pokémon. (§4.3.2)
- All content must be ScriptableObject-driven — no hardcoded game values.
- Seeded RNG everywhere — determinism is non-negotiable. (Engineering Pillar 3)
