# Project Ascendant — Directory Structure

```tree
ProjectAscendant/
├── CLAUDE.md                          ← Master config (you are here)
├── .env                               ← Secrets (gitignored)
├── .env.example                       ← Secret template (committed)
├── package.json                       ← Node dependencies (dotenv for export script)
├── Pokemon Ascendant.slnx             ← Solution file for Rider/VS
│
├── .claude/
│   ├── settings.json                  ← Hooks, permissions, safety rules
│   ├── agents/                        ← 9 agent definitions (committed)
│   │   ├── producer.md
│   │   ├── lead-programmer.md
│   │   ├── game-designer.md
│   │   ├── unity-specialist.md
│   │   ├── systems-designer.md
│   │   ├── qa-lead.md
│   │   ├── content-designer.md
│   │   ├── ui-programmer.md
│   │   └── art-director.md
│   ├── hooks/                         ← automation hooks (committed)
│   │   ├── session-start.sh
│   │   ├── session-stop.sh
│   │   ├── detect-gaps.sh
│   │   ├── validate-commit.sh
│   │   ├── validate-push.sh
│   │   ├── validate-assets.sh
│   │   ├── validate-pillar.sh
│   │   ├── pre-compact.sh
│   │   ├── post-compact.sh
│   │   └── log-agent.sh
│   ├── rules/                         ← 7 path-scoped coding standards
│   │   ├── combat-systems.md          ← Assets/Scripts/Combat/**
│   │   ├── core-architecture.md       ← Assets/Scripts/Core/**
│   │   ├── ui.md                      ← Assets/Scripts/UI/**
│   │   ├── data-assets.md             ← Assets/ScriptableObjects/**, Assets/Data/**
│   │   ├── tests.md                   ← Assets/Tests/**
│   │   ├── prototypes.md              ← prototypes/**
│   │   └── design.md                  ← design/**
│   ├── skills/                        ← workflow skills (committed)
│   │   ├── gdd-read/                  ← BEFORE any GDD read (today's snapshot)
│   │   ├── orchestrate/
│   │   ├── unity-verify/              ← Coplay compile + EditMode tests
│   │   ├── project-ascendant-gdd/
│   │   ├── gdd-sync/
│   │   ├── sprint-plan/
│   │   ├── scope-check/
│   │   ├── balance-check/
│   │   ├── pillar-check/
│   │   ├── bug-report/
│   │   ├── code-review/
│   │   ├── design-review/
│   │   └── playtest-report/
│   └── docs/
│       ├── directory-structure.md     ← This file
│       ├── vertical-slice.md
│       ├── coordination-rules.md
│       ├── handoff-templates.md
│       ├── backlog-hint.md
│       ├── coding-standards.md
│       └── context-management.md
│
├── Assets/                            ← Unity project source (canonical casing)
│   ├── Scenes/                        ← Unity scenes
│   ├── Settings/                      ← URP pipeline settings
│   ├── Scripts/                       ← All C# game code (create this folder)
│   │   ├── Core/                      ← EventBus, HSM, Factory, GameRNG, SaveSystem
│   │   ├── Combat/                    ← Combat loop, damage, intents, status
│   │   ├── Deck/                      ← DeckManager, Hand, DiscardPile
│   │   ├── Progression/               ← XP, evolution, TMs, abilities
│   │   ├── Map/                       ← RegionMap, NodeController, LoadoutManager
│   │   ├── Roguelike/                 ← MetaProgression, TrainerHub, RelicManager
│   │   └── UI/                        ← View-layer MonoBehaviours only
│   ├── ScriptableObjects/             ← PokemonSpeciesSO, MoveSO, RelicSO, etc.
│   ├── Data/                          ← JSON data files (tooling use)
│   ├── Prefabs/                       ← Unity prefabs
│   ├── Sprites/                       ← Pixel art sprites
│   ├── Audio/                         ← Music and SFX
│   └── Tests/                         ← Unity Test Framework
│       ├── EditMode/                  ← NUnit logic tests (no MonoBehaviour)
│       └── PlayMode/                  ← Integration tests (requires scene)
│
├── Packages/                          ← Unity package manifest
│   ├── manifest.json
│   └── packages-lock.json
│
├── ProjectSettings/                   ← Unity project configuration (committed)
│
├── design/                            ← Local design notes, ADRs
│   ├── CLAUDE.md
│   ├── adr/                           ← Architecture Decision Records
│   └── systems-index.md               ← System enumeration and dependency map
│
├── docs/
│   ├── ai/                            ← Committed AI entry (orchestration, Cursor setup)
│   ├── gdd/                           ← GDD snapshots + README.md (read policy)
│   ├── scripts/
│   │   └── export-gdd.js              ← Notion → markdown exporter
│   └── engine-reference/
│       └── unity/
│           └── VERSION.md             ← Pinned version, best practices, deprecated APIs
│
├── prototypes/                        ← Isolated throwaway experiments (each needs README)
│
└── production/
    └── session-state/
        ├── active.md                  ← Current sprint state (update every session)
        ├── session-log.md             ← Session history (gitignored)
        └── agent-log.md               ← Subagent audit trail (gitignored)
```
