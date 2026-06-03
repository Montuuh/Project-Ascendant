# Backlog Hint — Compact Session Context

**Auto-maintained snapshot.** Update when Epic phase changes or major milestone lands.
**Canonical source:** Notion BACKLOG + `.claude/docs/vertical-slice.md`

---

## Phase (2026-06-02)

| Status | Epics |
|--------|-------|
| ✅ Complete | 1–12 (foundation through items/relics/meta) |
| 🎯 **Next** | **13** UI/UX · **14** Audio · **15** Accessibility · **16** QA · **17** Build |

## Open VS Gaps (ship blockers / follow-ups)

| # | Summary | Owner hint |
|---|---------|------------|
| ~~43~~ | ✅ DONE (`0cdae4d`) — save/resume + full team persistence (DTO + ID-registry) | lead-programmer |
| 44 | Dense Fog (intent hide) + Iron Will (+enemy HP) combat effects | lead-programmer |
| 45 | Resume doesn't persist per-stream RNG cursors (encounters re-roll from seed) | lead-programmer |
| 46 | Duplicate final-form SpeciesId (Blastoise/Charizard/Venusaur A1≡A2) | content-designer |
| — | 5 achievement combat/timer trigger hooks | lead-programmer + qa-lead |

## Verification Baseline

- EditMode suite: **988/988** at last full green (`0cdae4d`)
- Unity: **6000.4.6f1**, Coplay MCP for compile + tests
- Bridge freezes after many domain reloads → restart editor

## Key Notion URLs

| Doc | URL |
|-----|-----|
| Vertical Slice | https://www.notion.so/36a0450715b48165a50ef130a244bb60 |
| BACKLOG | https://www.notion.so/3610450715b48109b2ebd15d97e69fa7 |
| GDD root | https://www.notion.so/3610450715b481588234e2e5f1b756ee |

## Design Pillars (quick ref)

1. Telegraphed tactics over reactive RNG
2. Every swap is a decision
3. Synergy is sculpted, not drafted
4. Identity through Evolution
5. Cheerful core, regional flavor

## Orchestration

Multi-agent work: `orchestrate` skill → user approves plan → Task subagents → `unity-verify`.
