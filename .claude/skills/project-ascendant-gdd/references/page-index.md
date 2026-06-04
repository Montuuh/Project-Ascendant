# Project Ascendant — Notion Page Index

All canonical document URLs. Use these with `notion-fetch` and
`notion-update-page`. Page IDs are extracted from URLs for direct API use.

---

## Workspace Root

| Page | URL | Page ID |
| --- | --- | --- |
| Project Ascendant (workspace parent) | https://www.notion.so/3610450715b481c68021c8aae18d2ab6 | 3610450715b481c68021c8aae18d2ab6 |
| GDD (parent, contains topic index) | https://www.notion.so/3610450715b481588234e2e5f1b756ee | 3610450715b481588234e2e5f1b756ee |
| BACKLOG | https://www.notion.so/3610450715b48109b2ebd15d97e69fa7 | 3610450715b48109b2ebd15d97e69fa7 |

---

## GDD Topic Pages

| # | Topic | Status | URL | Page ID |
| --- | --- | --- | --- | --- |
| 1 | Game Overview | 🟢 In Progress | https://www.notion.so/3610450715b481a287bdd5c72573b9d7 | 3610450715b481a287bdd5c72573b9d7 |
| 2 | Core Gameplay Loop | 🟢 In Progress | https://www.notion.so/3610450715b481048a3bd46eb1d31a07 | 3610450715b481048a3bd46eb1d31a07 |
| 3 | Micro Loop | 🟢 In Progress | https://www.notion.so/3610450715b481e08404ded0b96924c9 | 3610450715b481e08404ded0b96924c9 |
| 4 | Combat System | 🟢 In Progress | https://www.notion.so/3610450715b4818bb876f6d9fd5d2ab0 | 3610450715b4818bb876f6d9fd5d2ab0 |
| 5 | Progression | 🟢 In Progress | https://www.notion.so/3610450715b4813ea29ae0c992898d01 | 3610450715b4813ea29ae0c992898d01 |
| 6 | Roguelike Progression | 🟢 In Progress | https://www.notion.so/3610450715b4816c83d2c74682cef77c | 3610450715b4816c83d2c74682cef77c |
| 7 | Scenario & Nodes | 🟢 In Progress | https://www.notion.so/3610450715b48146b3a0fe94ca2bd05c | 3610450715b48146b3a0fe94ca2bd05c |
| 8 | Items & Relics | 🟢 In Progress | https://www.notion.so/3610450715b48173bab9e5239b63f813 | 3610450715b48173bab9e5239b63f813 |
| 9 | Technical Architecture | 🟢 In Progress | https://www.notion.so/3610450715b4811b83cae23d6ed2a154 | 3610450715b4811b83cae23d6ed2a154 |
| 10 | Art, UI, Audio | 🟢 In Progress | https://www.notion.so/3610450715b4815192fae42ed745b3d0 | 3610450715b4815192fae42ed745b3d0 |

---

## Status Update Protocol

The GDD is a **living document** during development — topics stay editable; there is
no "locked" state. When a topic's status changes, update this table AND the topic
page header AND the BACKLOG completion status table. Three places, one operation.

**Status values:**
- 🟢 In Progress — active, editable (the default for every topic during development)
- 🟡 Pending — scoped but not yet written
- ❌ Blocked — waiting on an external dependency

---

## Key Section Cross-Reference

Frequently-needed sections for implementation work:

| System | GDD Section | Topic Page |
| --- | --- | --- |
| Faint state definition | §2.4.1 | Topic 2 |
| Faint precedence over Freeze | §3.3.5.1 | Topic 3 |
| Lead swap cost scaling | §3.3.1 | Topic 3 |
| Step-Forward / Step-Backward rules | §3.3.2 / §3.3.3 | Topic 3 |
| Damage formula | §4.1.1 | Topic 4 |
| Type chart | §4.1.2 | Topic 4 |
| All status conditions | §4.2.2 / §4.2.3 | Topic 4 |
| Stat stage modifiers | §4.2.6 | Topic 4 |
| Intent types & position-targeting | §4.3.2 | Topic 4 |
| AI scoring function | §4.3.3 | Topic 4 |
| Cleave / Backstrike empty-slot | §4.3.4.1 | Topic 4 |
| Mastery Move system | §4.3.9.2 | Topic 4 |
| Boss phase structure | §4.4.3 | Topic 4 |
| Badge effects (all 12) | §4.4.5 | Topic 4 |
| Champion buff cap | §4.7.1 | Topic 4 |
| Branching evolution rules | §5.3 | Topic 5 |
| Move kit construction | §5.3.6 | Topic 5 |
| Ability system | §5.5 | Topic 5 |
| Lead Aura | §5.5.4 | Topic 5 |
| Trauma System (Option E hybrid) | §6.2 | Topic 6 |
| Trainer XP & Hub | §6.3 / §6.4 | Topic 6 |
| Starter unlocks | §6.5 | Topic 6 |
| Difficulty modifiers | §6.8 | Topic 6 |
| Catching mechanic (deterministic) | §7.3.4 | Topic 7 |
| Wild biomes → species map | §7.3.3 | Topic 7 |
| Mystery Event catalog | §7.9 | Topic 7 |
| City Shop curation algorithm | §7.8.2.1 | Topic 7 |
| Consumable catalog | §8.2 | Topic 8 |
| Relic catalog (50 launch) | §8.3 | Topic 8 |
| Held Items (1 slot per Pokémon) | §8.4 | Topic 8 |
| TM catalog | §8.5 | Topic 8 |
| ScriptableObject schemas | §9.3 | Topic 9 |
| Event Bus (hybrid model) | §9.4 | Topic 9 |
| HSM tree | §9.5 | Topic 9 |
| Seeded RNG streams | §9.7 | Topic 9 |
| Save system | §9.8 | Topic 9 |
| Combat screen layout | §10.2 | Topic 10 |
| Type palette (WCAG) | §10.1.3.2 | Topic 10 |
| Audio bible | §10.5 | Topic 10 |
| Accessibility mandates | §10.6 | Topic 10 |
