# Local GDD Topic Map — Read Path for Agents

**Use these files for all GDD reads.** Do not fetch Notion for implementation spec.

Run freshness check first: `node docs/scripts/ensure-gdd-snapshot.js`

| Topic | Local file | Owns (summary) |
| --- | --- | --- |
| 1 | `docs/gdd/topic-1-game-overview.md` | Pillars, scope, IP |
| 2 | `docs/gdd/topic-2-core-gameplay-loop.md` | Macro loop, Box, HP, faint (§2.4.1), Trauma hook |
| 3 | `docs/gdd/topic-3-micro-loop.md` | Combat phases, Lead, swap cost (§3.3.1), consumables (§3.5) |
| 4 | `docs/gdd/topic-4-combat-system.md` | Damage, status, intents (§4.3.2), targeting (§4.3.4.1), bosses (§4.4.3.1), Champion (§4.7.1) |
| 5 | `docs/gdd/topic-5-progression.md` | XP, evolution, TMs, Mastery Moves (§4.3.9.2), abilities |
| 6 | `docs/gdd/topic-6-roguelike-progression.md` | Meta XP, Hub, Trauma (§6.2), difficulty |
| 7 | `docs/gdd/topic-7-scenario-nodes.md` | Nodes, wild, catching |
| 8 | `docs/gdd/topic-8-items-relics.md` | Consumables, relics, held items |
| 9 | `docs/gdd/topic-9-technical-architecture.md` | ScriptableObjects, Event Bus, HSM, GameRNG, save |
| 10 | `docs/gdd/topic-10-art-ui-audio.md` | Art, combat layout, accessibility |

## § Section → File (quick lookup)

| § prefix | Read file |
| --- | --- |
| §1.x | topic-1 |
| §2.x | topic-2 |
| §3.x | topic-3 |
| §4.x | topic-4 |
| §5.x | topic-5 |
| §6.x | topic-6 |
| §7.x | topic-7 |
| §8.x | topic-8 |
| §9.x | topic-9 |
| §10.x | topic-10 |

Within a file, search for `# §N.N` or `§N.N.N` headings.

## Notion (writes only)

URLs for editing and BACKLOG: `references/page-index.md`
