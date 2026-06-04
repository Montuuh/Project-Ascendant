# Vertical Slice — Active Build Target

**This is the current sprint.** Every coding decision, scope question, content authoring task, or design tweak you make as an agent should be evaluated against the Vertical Slice scope below. If a proposal does not advance the Vertical Slice, it is OUT and must be deferred to post-VS.

The canonical, deeply-structured project plan lives in Notion. **Always read the relevant Epic page before starting a task in its domain.**

---

## Canonical Notion Surface

| Document | URL |
| --- | --- |
| 🎯 Vertical Slice — Region 1 End-to-End (parent) | https://www.notion.so/36a0450715b48165a50ef130a244bb60 |
| 📋 VS — Cross-Epic Dependency Graph + Risks + Cadence | https://www.notion.so/36a0450715b481fb95b9fcf6dfafda87 |
| BACKLOG | https://www.notion.so/3610450715b48109b2ebd15d97e69fa7 |
| GDD root | https://www.notion.so/3610450715b481588234e2e5f1b756ee |

---

## The 17 Epics (with Notion URLs)

Every Epic page contains: phase, owner agent, goal, GDD references, general tasks (`N.X`), specific subtasks (`N.X.Y`), and a Definition of Done.

| # | Epic | Phase | Owner agent(s) | URL |
| --- | --- | --- | --- | --- |
| 1 | Foundation & Project Setup | A | unity-specialist, lead-programmer | https://www.notion.so/36a0450715b4815b9da0f79c772a1e77 |
| 2 | Core Architecture | A | lead-programmer | https://www.notion.so/36a0450715b4811c8fb4e935922ec7c2 |
| 3 | Data Layer (ScriptableObjects) | A→B | lead-programmer, content-designer | https://www.notion.so/36a0450715b4810bbd02c69610804d12 |
| 4 | Combat System | B | lead-programmer, systems-designer, qa-lead | https://www.notion.so/36a0450715b48178a964d4ba6cccf4c0 |
| 5 | Deck & Hand System | B | lead-programmer | https://www.notion.so/36a0450715b48199b057d87a609c738a |
| 6 | Lead & Swap Mechanic | B | lead-programmer | https://www.notion.so/36a0450715b481b0b16cddf8d19cbece |
| 7 | Pokémon Content (VS Scope) | B | content-designer, systems-designer | https://www.notion.so/36a0450715b4818ab6b5e0e7da18ae77 |
| 8 | Encounters & AI | B/C | lead-programmer, content-designer, systems-designer | https://www.notion.so/36a0450715b48159aa1ac254f88bbfe5 |
| 9 | Map & Nodes | C | lead-programmer, ui-programmer, content-designer | https://www.notion.so/36a0450715b48195bd49f95b03952127 |
| 10 | Progression | C | lead-programmer, content-designer | https://www.notion.so/36a0450715b4813bbe62c0780ffa05ea |
| 11 | Roguelike Meta | C | lead-programmer, producer | https://www.notion.so/36a0450715b48124b802c58381e192c0 |
| 12 | Items, Relics, Consumables | B/C | lead-programmer, content-designer, systems-designer | https://www.notion.so/36a0450715b4819abb38fab12e192400 |
| 13 | UI / UX | B/C/D | ui-programmer | https://www.notion.so/36a0450715b481b29f3ee8e82641b163 |
| 14 | Audio | D | unity-specialist | https://www.notion.so/36a0450715b4812496d3f2ea400478ed |
| 15 | Accessibility | D | ui-programmer | https://www.notion.so/36a0450715b481efa7b3d466ae73018a |
| 16 | Testing & QA | continuous | qa-lead, lead-programmer | https://www.notion.so/36a0450715b4816da297d1fd175cf316 |
| 17 | Build & Release | D | unity-specialist, producer | https://www.notion.so/36a0450715b481948d88fc0838ab3775 |

---

## VS Scope — One-Line Summary

**Region 1 end-to-end** with: **3 starters** (Bulbasaur/Charmander/Squirtle, **Vanguard branches only**), **3 wild recruitable lines** (Caterpie, Pidgey, Geodude — all 3 stages each), **6 node types** (Wild, Trainer, Center, Shop, Mystery, Gym), **15 relics**, **10 consumables**, **5 Held Items**, **3 TMs**, **4 Mystery Events**, **4 trainer archetypes**, **1 Elite**, **R1 Gym Leader 3-phase boss** (Rock-type default), full **Trauma System**, **Trainer Hub stub** (2 kiosks), **3 difficulty modifiers**, **~10 achievements**, all **6 status conditions**, **5-stream GameRNG determinism**, **save/load between every node**, full **accessibility tier** (colorblind/reduced motion/rebinding/subtitles).

**Out of scope** (defer to post-VS unless user explicitly overrides): Regions 2/3, Victory Road, League, Champion, Specialist/Support branches, Tier 2/3 relic meta-unlocks, Bestiary Veteran/Master tiers, meta-unlocked starters, City interstitials, full Mystery catalog, full relic catalog, all Held Items beyond the 5, multi-profile saves, localization beyond en-US, mobile portability work.

The full carve-out tables live per-Topic in the GDD (each Topic's "Vertical Slice Carve-Out" section).

---

## Phase Sequence

1. **Phase A — Foundation (Weeks 1–3):** Epics 1, 2, 3 (schemas). Critical path: 1 → 2 → 3.
2. **Phase B — Combat VS (Weeks 4–8):** Epics 4, 5, 6, 7, parts of 12. Critical path: 4 → 6 → first playable encounter.
3. **Phase C — Macro Loop VS (Weeks 9–12):** Epics 8, 9, 10, 11, 12 finalization. Critical path: 9 + 11 gate the full run experience.
4. **Phase D — Polish & Playtest (Weeks 13–16):** Epics 13, 14, 15, 16 (integration + playtest), 17 (RC + release).

Epic 16 (Testing & QA) runs continuously across all phases.

---

## Per-Agent Direction

When you are invoked, find the row(s) below that match your specialty. **Open the linked Epic before starting work.** Do NOT design or implement anything outside that Epic's listed task scope without explicit user approval — that is scope creep, and `producer` will flag it.

### lead-programmer
- **Primary Epics:** 2 (Core Architecture), 4 (Combat), 5 (Deck), 6 (Lead/Swap), 8 (Encounters framework), 9 (Map/Nodes), 10 (Progression), 11 (Roguelike Meta), 12 (Items runtime).
- **Always-on:** Maintain Topic 9 architecture invariants — SO-driven data, EventBus decoupling, HSM for game-flow state, no `UnityEngine.Random`, no `Resources.Load`, no `GameObject.Find` / `FindObjectOfType`.
- **Hard rule:** No combat-system or progression-system code lands without unit tests per Epic 16 Task 16.2.

### unity-specialist
- **Primary Epics:** 1 (Foundation), 14 (Audio integration), 17 (Build & Release).
- **Support Epics:** 13 (UI Toolkit specifics), 9 (Addressables loading for region content).
- **Always-on:** Verify Unity-API choices against `docs/engine-reference/unity/VERSION.md` (Unity 6000.4.6f1, URP 2D, IL2CPP). Flag deprecated API usage.

### systems-designer
- **Primary Epics:** 4 (combat balance), 7 (Pokémon kit balance), 8 (boss tuning), 12 (relic/consumable balance), 11 (XP/Trauma calibration).
- **Always-on:** Every number you touch belongs in a `*ConfigSO`. Never hardcode balance values. Justify recommendations with simulation math.

### content-designer
- **Primary Epics:** 3 (asset authoring — Section 3.3), 7 (Pokémon move kits + abilities), 8 (trainer archetypes + R1 Gym Leader), 9 (Mystery Events + biome pools), 12 (relic + consumable + Held Item content).
- **Always-on:** Move kit construction must obey §5.3.6 templates. Branch archetype must follow §5.3.4.

### ui-programmer
- **Primary Epics:** 9 (Map View UI), 11 (Hub menu UI), 13 (full UI/UX suite), 15 (Accessibility).
- **Always-on:** Per `ui.md` — UI never owns game state. UI reads SOs + subscribes to event channels. Unplayable cards are greyed out, never hidden. All text is localisation-key-bound.

### qa-lead
- **Primary Epic:** 16 (Testing & QA, continuous).
- **Cross-Epic mandate:** Every commit touching combat or progression must have unit tests per Epic 16 Task 16.2. Replay-regression fixtures added per major bug.
- **Always-on:** Author bug reports via the `bug-report` skill. Never silently fix bugs — file them clearly.

### producer
- **Primary Epic:** 11 (scope discipline) + Cross-Epic governance.
- **Always-on:** Every new idea or "what if we also" gets the `scope-check` skill before it enters any Epic. Protect the VS carve-out. Update BACKLOG + active.md after every significant decision. Run weekly `sprint-plan` reviews.

### game-designer
- **Primary action:** Pillar compliance review for any proposal that adds, modifies, or removes mechanics. Run `pillar-check` skill on any design question.
- **Cross-Epic mandate:** When a pillar conflict arises in implementation, `game-designer` is the decision point before any code lands.
- **Always-on:** Read the relevant GDD Topic page before answering. Never invent design that contradicts a GDD Topic — and when a decision changes the spec, edit the canonical Notion GDD so it stays current (the GDD is a living document during development).

---

## Working Protocol for Every VS Task

```
1. Identify which Epic + Task number this work belongs to.
2. Open the Epic page in Notion. Read the General Task + relevant Subtasks.
3. Read the linked GDD §section(s).
4. Confirm scope with the user using the Q→O→D→D→A protocol.
5. Implement / author / decide.
6. Check off subtasks in Notion as completed.
7. If you discover a gap, ambiguity, or spec contradiction:
   a. Do not silently resolve in code.
   b. Add a `⚠️ OPEN` flag to the affected GDD page.
   c. Add a BACKLOG gap entry (Sev-2 if blocking implementation).
   d. Stub the code with a TODO + §reference.
   e. Tell the user.
```

---

## What Counts as Scope Creep (auto-deferred to post-VS unless user overrides)

Examples — by no means exhaustive:

- "Let's also add a Specialist branch for Squirtle while we're at it." → **Post-VS.** Vanguard only.
- "What if relics had a fusion system?" → **Post-VS.** Not in GDD; not in Epic 12.
- "Region 2 prototype to see how multi-Region pacing feels." → **Post-VS.** Region 1 must be solid first.
- "More than 4 Mystery Events because they're easy to author." → **Post-VS.** Easy ≠ in scope.
- "Sprite polish pass on Charizard." → **Post-VS.** Placeholder art is correct for VS per Epic 7 Task 7.8.
- "Daily Seed mode demo." → **Post-VS.** Architecture supports it (Epic 2 §9.7.4); UX work deferred.
- "Mobile build target." → **Post-VS.** Architecture-compatible, not launch-targeted.

If you're unsure whether a task is in or out, invoke the `scope-check` skill.

---

## Cross-References

- **Per-Topic VS carve-outs:** Each GDD Topic page has a `Vertical Slice Carve-Out` section listing what's IN vs OUT for VS.
- **BACKLOG `Implementation Sequence`:** Mirror of Phase A→D plan; updated weekly.
- **active.md:** session-scoped pointer to the current Epic/Task in-flight.

When the VS is signed off (Epic 17 Task 17.9 sign-offs), update this file to reflect the next active sprint target.
