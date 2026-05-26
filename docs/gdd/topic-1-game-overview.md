<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Source: https://www.notion.so/3610450715b481a287bdd5c72573b9d7 -->
<!-- Exported: 2026-05-26T16:27:23.500Z -->
<!-- To update: run `node docs/scripts/export-gdd.js` and commit -->

**Status:** 🔒 Locked


**Last Updated:** 2026-05-15 (migrated from Drive)


**Cross-references:** Topic 2 (vertical slice = Region 1 end-to-end), Topic 6 (meta-unlocks for starters & difficulty modifiers), Topic 9 (engineering pillars).


---


# §1.1 High-Level Pitch


Project Ascendant is a roguelike deckbuilder where your party _is_ your deck. Each of your three active Pokémon contributes four moves to a shared hand, and every turn forces a tactical conversation between your Lead — who absorbs incoming damage — and your bench. Swapping the Lead is never free and never automatic: it spends Action Points, reshapes your defensive options, and lets you exploit type matchups that a single Pokémon never could.


Branching evolutions permanently rewrite your deck mid-run, so the team you start with and the team you finish with rarely look the same. Each run sends you across themed Regions of the Pokémon world, culminating in Gym Leader showdowns and a final League gauntlet.


**Signature mechanic (moment-to-moment):** the Lead/Swap action-economy tension.


**Signature mechanic (run-to-run):** branching evolution paths that rewrite your deck.


---


# §1.2 Genre


Roguelike Deckbuilder / Tactical RPG hybrid.


---


# §1.3 Game Pillars


## §1.3.1 Design Pillars (player-facing)

1. **Telegraphed tactics over reactive RNG.** The player should always be able to plan; randomness lives in _what options exist_, not _whether plans work_.
2. **Every swap is a decision.** Lead-swapping carries a meaningful trade-off in AP, defense, and tempo. Never free, never punished arbitrarily.
3. **Synergy is sculpted, not drafted.** Power comes from how moves, evolutions, and relics interact — never from a single overpowered card.
4. **Identity through Evolution.** Branching evolutions are the primary creative expression within a run.
5. **Cheerful core, regional flavor.** Base tone is faithful-Pokémon; each Region layers its own aesthetic personality (music, palette, biome, enemy archetypes) without changing core mechanics.

## §1.3.2 Engineering Pillars (portfolio-facing)

1. **Data-driven content.** Pokémon, moves, relics, encounters, and regions all live as ScriptableObjects. No content in code.
2. **Decoupled systems.** Combat logic, UI, and audio communicate only through events. Any system is replaceable in isolation.
3. **Deterministic, replayable simulation.** Given a seed and an input log, any run replays bit-exact. Cheap to build in now, expensive to retrofit later; enables replay debugging and unlocks daily seeds and leaderboards as future features.

## §1.3.3 Anti-Pillars (what Project Ascendant is NOT)

- Not a Pokémon Showdown clone — turn-order, speed, accuracy, and PP are abstracted away.
- Not a card-acquisition game — you recruit Pokémon; cards come bundled with their species and evolutions.
- Not a mass-collection game — small, sculpted parties beat sprawling boxes.
- Not a numbers-go-up auto-battler — every turn requires a real decision.
- Not grimdark — the base tone stays cheerful; tonal variation lives in regional flavor only.

---


# §1.4 Player Fantasy


You are a tactician-trainer reading the battlefield two turns ahead — sculpting a small, evolving team into a decisive answer to whatever the Region throws at you.


---


# §1.5 Market Study


**Primary audience:** Slay the Spire / Monster Train / Balatro players seeking a fresh thematic frame with novel team-building.


**Secondary audience:** Pokémon fans wanting tactical, replayable challenge beyond mainline difficulty curves.


**Tertiary audience (portfolio context):** Recruiters and senior engineers evaluating Unity architecture and systems design.


When Primary and Secondary preferences conflict, **Primary wins on mechanics and pacing; Secondary wins on theming and emotional beats.**


---


# §1.6 Scope & Vertical Slice


**Run structure:** 3 Regions + League finale.

- Each Region culminates in a Gym Leader fight (3 Gyms total).
- The League finale comprises 4 Elite Four matches and 1 Champion match (5 fights).
- Total climactic encounters per run: 8 (21 boss-tier Pokémon fights total — see §4.4.2).
- Target run length: ~90–100 minutes for a winning run.

**Launch content target (Gen 1 only):**

- ~30 fully-implemented Pokémon evolution lines.
- ~50 relics.
- ~25 enemy/trainer encounter archetypes.
- 3 Region biomes with distinct aesthetic theming (music, palette, enemy roster).
- 12 Gym types (4 per regional tier; 3 earned per run from a structured-random pool).
- Multi-gen content is the planned scaling lever if Gen 1 proves insufficient.

**Vertical Slice definition:** Region 1, playable end-to-end. Includes 6 starter Pokémon options, 8 recruitable Pokémon, 15 relics, 1 Gym Leader (one of the 4 Region-1-tier types), all node types implemented, working save/load. This milestone proves the full stack before content scaling begins.


---


# §1.7 Platform, Input, Session

- **Launch platform:** PC (Windows). Mac / Linux / Steam Deck considered if low-cost.
- **Roadmap:** mobile port (iOS / Android) acknowledged as a long-term aspiration. Not in launch scope; influences architecture only insofar as good general practice (data-driven UI scaling, no input coupled to mouse-only assumptions) preserves the option without dedicated effort.
- **Input:** mouse-primary, full keyboard shortcuts, gamepad-friendly.
- **Run length target:** ~90–100 minutes for a winning run.
- **Save model:** auto-save between map nodes. Combat is atomic — once a battle starts, the player must complete or lose it. No mid-combat resume.
- **Difficulty philosophy:** TBD via playtesting. Architectural commitment now: a stackable difficulty-modifier system (Ascension/heat-style) so escalating difficulty is data-driven, not hardcoded.

---


# §1.8 Social & Replayability

- **Launch:** singleplayer only.
- **Post-launch (planned):** daily-seed runs with a global shared seed driving leaderboard competition; persistent leaderboards for fastest clears and highest difficulty cleared. Determinism pillar exists primarily to keep this option open at low future cost.
- **Far-future:** multiplayer modes (format TBD).

---


# §1.9 IP & Legal

- Non-commercial, fan-made portfolio project.
- No distribution of Game Freak / Nintendo / The Pokémon Company copyrighted assets in public builds. Placeholder or original art used during development; final public-facing portfolio version will use either a fully reskinned monster set or original creature designs.
- No Steam or commercial storefront release. Portfolio demonstration only, optionally hosted on [itch.io](http://itch.io/) with explicit fan-project disclaimer.
- All trademarks acknowledged as belonging to their respective owners.
- **Architectural alignment:** because all creature, move, and region content is ScriptableObject-driven, swapping the IP-bound layer for original designs is an asset-and-data operation rather than a code refactor — itself a portfolio-worthy demonstration of the data-driven pillar.

---


# §1.10 Glossary (cross-topic anchor — added 2026-05-24)


This glossary consolidates the most-referenced terms across the GDD. Each entry links to the canonical defining section.


| Term                         | Defined in      | One-liner                                                                   |
| ---------------------------- | --------------- | --------------------------------------------------------------------------- |
| Active Team                  | §2.3            | The 3 Pokémon brought into a combat from the Box.                           |
| AP (Action Points)           | §3.7            | Per-turn budget; 3 baseline, modified by relics/Badges/Region Modifiers.    |
| Backstrike                   | §4.3.4          | Position-targeted intent bypassing Lead; fizzles on empty slot (§4.3.4.1).  |
| Badge                        | §4.4.5          | Permanent run-modifier earned from Gym Leaders (12 total; 3 per run base).  |
| Bestiary                     | §4.3.9          | Cross-run knowledge accumulation; gates Mastery rewards.                    |
| Box                          | §2.3            | Persistent run-roster of Pokémon (capacity 6, upgradable to 8).             |
| Branch (evolution)           | §5.3.3          | Permanent choice at evolution; rewrites move kit.                           |
| Champion buff                | §4.7.1          | +5% Attack per defeated ally, cap +20%.                                     |
| City                         | §2.1.4 / §7.8   | Post-Gym 1/2 rest zone — Pokémon Center + Shop + Reflection.                |
| Cleave                       | §4.3.4          | All-slot damage intent; never fizzles (min 1 target).                       |
| Combat phase                 | §3.2            | Five-phase loop: Start → Draw → Intent → Action → Resolution.               |
| Confusion                    | §4.2.3.1        | Secondary status; discards 1 random skill card per Confused Pokémon.        |
| Effective Max HP             | §6.2            | `BaseMaxHP × (1 − 0.05 × min(Trauma, 5))`.                                  |
| Faint                        | §2.4.1          | `CurrentHP == 0`. No separate flag. Applies Trauma stack.                   |
| Field effect                 | §4.3.8          | Weather/terrain effect persisting full combat.                              |
| Held Item                    | §8.4            | Per-Pokémon equipment. One slot per Pokémon.                                |
| Hub upgrade                  | §6.4.2          | Permanent QoL/option-expanding unlock from the Pokémart.                    |
| Intent                       | §4.3.2          | Telegraphed enemy action targeting a slot, not a Pokémon.                   |
| Lead                         | §3.3            | The Pokémon absorbing damage; gates Melee cards.                            |
| Lead Aura                    | §5.5.4          | Ability/Held-Item-gated type bonus to bench while wearer is Lead.           |
| League                       | §2.1.6          | Final 5 fights: 4 Elite + Champion.                                         |
| League Boon                  | §4.5.2          | Selected at Victory Road Summit; active only in League.                     |
| Manual swap cost             | §3.3.1          | 1st=1AP, 2nd=2AP, 3rd=3AP; counter resets per turn.                         |
| Mastery Move                 | §4.3.9.2        | Immutable 5th move from Bestiary Master tier; evolves with the Pokémon.     |
| Meta currency                | §6.3            | Trainer XP (drives Level) + Trainer Tokens (manual unlock spend).           |
| Pokéball                     | §7.3.4          | Catching consumable; deterministic threshold.                               |
| Region                       | §2.1.2          | One of 3 branching ladders per run.                                         |
| Region Modifier              | §2.1.4 / §7.8.3 | Persistent buff picked at City Reflection; up to 2 active.                  |
| Relic                        | §8.3            | Persistent run-state modifier. ~50 launch.                                  |
| Run                          | §2.1            | Single playthrough; Pre-Run → Region×3 → Victory Road → League.             |
| Step-Forward / Step-Backward | §3.3.2 / §3.3.3 | Melee modifier; bundles Lead change with effect; no swap counter increment. |
| TM                           | §5.4.1 / §8.5   | Consumable that teaches a move. Mastery Moves excluded.                     |
| Trauma stack                 | §6.2            | Per-Pokémon-per-run penalty per faint; multiplicative −5% MaxHP, cap 5.     |
| Trauma Salve                 | §6.2.4 / §8.3.4 | Uncommon relic that removes all Trauma from one Pokémon.                    |
| Trainer Hub                  | §6.4            | Pre/post-run menu space; 5 kiosks.                                          |
| Trainer Token                | §6.3.4          | Per-run currency for chosen unlocks; capped 50/run.                         |
| Type chart                   | §4.1.2          | Gen I 15-type matrix; immunities and 4× multipliers preserved.              |
| Vertical Slice               | §1.6            | Region 1 end-to-end at launch quality.                                      |
| Victory Road                 | §2.1.5 / §4.5   | Pre-League prep zone; 3 specialized node types + Summit.                    |


# §1.11 Topic-Lock Status — Master Table (updated 2026-05-24)


All 10 GDD topics are 🔒 Locked as of 2026-05-24. Future modifications require explicit user re-lock acknowledgment per the GDD workflow skill.


| Topic                      | Status | Lock Date                                          | Owner                               |
| -------------------------- | ------ | -------------------------------------------------- | ----------------------------------- |
| 1 — Game Overview          | 🔒     | 2026-05-15 (initial) / 2026-05-24 (glossary added) | game-designer                       |
| 2 — Core Gameplay Loop     | 🔒     | 2026-05-15                                         | game-designer                       |
| 3 — Micro Loop             | 🔒     | 2026-05-15                                         | game-designer                       |
| 4 — Combat System          | 🔒     | 2026-05-15                                         | game-designer + systems-designer    |
| 5 — Progression            | 🔒     | 2026-05-15                                         | game-designer + content-designer    |
| 6 — Roguelike Progression  | 🔒     | 2026-05-24                                         | game-designer                       |
| 7 — Scenario & Nodes       | 🔒     | 2026-05-24                                         | content-designer                    |
| 8 — Items & Relics         | 🔒     | 2026-05-24                                         | content-designer + systems-designer |
| 9 — Technical Architecture | 🔒     | 2026-05-24                                         | lead-programmer                     |
| 10 — Art, UI, Audio        | 🔒     | 2026-05-24                                         | ui-programmer                       |

