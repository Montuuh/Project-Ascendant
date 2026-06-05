# Project Ascendant — Open Design Questions (Master Backlog)

> **Purpose.** The single, lossless record of every open design question raised
> while polishing the overall design + GDD. We answer these **slowly, one at a
> time**, with the designer team. Nothing here is decided until marked ✅ and
> logged in [`gdd-change-log.md`](gdd-change-log.md).
>
> **Owner of this file:** `gdd-steward` (keeps it current; never deletes a
> question — only updates status + appends the resolution).

## How we process each question

`Question → Options → Decision (user) → Draft → Approval → record`

On a ✅ decision:
1. Append the resolution under the question here (keep the question).
2. Add an entry to [`gdd-change-log.md`](gdd-change-log.md) **if** it changes the
   GDD — naming the topic/§ and the code impact, so engineering can adapt.
3. The GDD-Notion edit itself happens later, in batches (user-gated).

## Status legend

| Mark | Meaning |
|---|---|
| 🔵 Open | Not yet discussed |
| 🟣 Discussing | Options on the table, awaiting decision |
| ✅ Decided | Resolution recorded below + logged for engineering |
| 🩹 Canon-drift | Current GDD disagrees with reality — reconcile before/while deciding |

## Owners (designer team)

`gdd-steward` (doc + canon fidelity) · `game-designer` (pillars/mechanics) ·
`systems-designer` (numbers/curves) · `content-designer` (content/pools) ·
`ui-programmer` + `art-director` (UI/visual) · `producer` (scope/sequencing).

---

# Domain A — Run & Macro Structure

## Q1 — City: add a Gym option + rethink city nodes 🔵
**Owner:** game-designer + content-designer
**User:** Give players the chance to enter a Gym in the City too — a chance to win
a Badge + rewards at a **risky cost**? Then: which nodes/places can the City have?
Shop **yes** (unique consumables, TMs, Relics). Pokémon Center — *probably not*.
Think Pokémon fantasy **and** StS-inspired structure for better City nodes.
**Decision needed:** (a) optional risky City Gym yes/no + its cost/reward shape;
(b) the full City node roster.
**Steward note (canon):** today City = Center → Curated Shop → Reflection (§2.1.4 /
§7.8). Bonus Badge sources already exist (§4.5.3); a City Gym would be a new one.

## Q2 — Region Modifiers: timing + pool 🔵
**Owner:** systems-designer + content-designer
**User:** Modifiers start at Route 2, end on Route 3 (I think). Is this good design?
Rethink it, and produce a pool of possible Region Modifiers.
**Decision needed:** when modifiers are picked/active; how many stack; the launch pool.
**Steward note (canon):** today picked at City Reflection (after Gym 1 & 2), up to 2
active, persist to run end; 12-modifier pool (§2.1.4.1 / §7.8.3).

---

# Domain B — Core Combat Loop

## Q3 — Hand size (skill cards drawn per turn) ✅ DECIDED 2026-06-05
**Owner:** systems-designer (+ game-designer pillar)
**User:** "Hand is 4 cards by default. This is bad." Rethink it, do the numbers.
**Finding:** code ships `BaseSkillCardsPerTurn: 4`; GDD §3.2.2/§3.7 say **5**. From a 12-card
deck (4 moves × 3 Active), hand 4 ≈ 1.33 cards/Pokémon → often draws **zero** from one of the
three Pokémon (bad for the party-is-hand identity); also collapses the Confusion floor (3 Confused
→ only 1 skill card left). Hand 5 ≈ 1.67/Pokémon (usually all three represented); hand 6 softens
swap scarcity.
**✅ Resolution:** **5 skill cards/turn, fixed** (consumable hand stays 2). Revert code
`BaseSkillCardsPerTurn 4→5` to match GDD. Relics (Reactor Core, Sage's Tome) remain the only way
above 5. Revisit only if playtest shows the 3-Pokémon read still feels starved. → logged **CL-005**.

## Q4 — Capturing a wild counts as Victory ✅ DECIDED 2026-06-05
**Owner:** game-designer (rule) / lead-programmer (impl)
**User:** If it's a wild encounter, **capturing is also a Victory.**
**✅ Resolution:** A successful catch ends the wild combat as a **Victory** and grants
the Active Team **full combat XP** (same as defeating the wild) — recruitment is never
an XP penalty. → logged **CL-003**.

---

# Domain C — Combat Resolution

## Q5 — Stat-stage modifier numbers (stale GDD) ✅ DECIDED 2026-06-05
**Owner:** gdd-steward (verify/sync) + systems-designer
**User:** I think we recently updated the stat-modifier numbers.
**Finding:** `BattleConfig.asset` ships a **linear 13-entry ±6 ladder**: stage −6…+6 →
`0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6` (±0.1/stage). GDD §4.2.6
still lists the old multiplicative ×0.25…×3.0 ladder — stale.
**✅ Resolution:** Adopt the **implemented linear ladder** as canon (gentler, bounded,
non-degenerate). Sync GDD §4.2.6 + codex + catalog to the 13 real values. → logged **CL-002**.

---

# Domain D — Enemy & AI

## Q6 — "Bestiary" renamed to "Pokédex" ✅ DECIDED 2026-06-05
**Owner:** gdd-steward (doc) / lead-programmer (backend rename)
**User:** I still see Bestiary references, but we renamed the system to **Pokédex**.
**Finding:** code is **half-renamed** — UI is Pokédex (`PokedexPanelUI`, Hub button) but
the backend is still Bestiary (`BestiaryProgressSO`, `BestiaryShinyUnlock`,
`BestiaryMasteryUnlock`, `SaveSystem` refs).
**✅ Resolution:** **Pokédex everywhere.** Docs (GDD §4.3.9/§6.9 + glossary, codex,
catalog) renamed now; backend class rename `Bestiary*` → `Pokedex*` is a code task. →
logged **CL-001**.

## Q7 — Unknown intents: frequency + reveal metaprogression 🔵
**Owner:** game-designer (+ systems-designer for unlocks)
**User:** Should Unknown intents be more common? Only on ELITE/GYM/TRAINER nodes? What
metaprogression unlocks "intent shown instead of Unknown"? Proposal: if you've **seen a
Pokémon use an ability**, the next time it uses the same ability its intent is visible.
**Decision needed:** (a) where Unknowns appear; (b) the knowledge-based reveal rule
(Pokédex-driven "I've seen this before → I can read it").
**Steward note (canon):** today 3-tier reveal Witnessed/Scouted/Researched (§4.3.5),
Pokédex Familiar tier reveals a species' intents (§4.3.9.1). Your proposal sharpens the
"Witnessed → permanently readable" loop.

## Q8 — Field effects: are they positive? Redesign 🔵
**Owner:** game-designer + content-designer
**User:** Field effects are positive? Redesign them.
**Decision needed:** the field-effect model — who benefits, player-controllable vs enemy-
set, the launch set.
**Steward note (canon):** today 3 fields (Sunny/Rain Weather + Electric Terrain), set by
encounter, symmetric multipliers (§4.3.8); Gym type-fields are inert (gap #33).

---

# Domain E — Bosses & Challenge Arc

## Q9 — Gym Leader phase systems (no mid-fight evolution) 🔵
**Owner:** game-designer
**User:** Gyms shouldn't have "epic" phases like mid-fight evolution. Prefer Gyms field
**more powerful Pokémon than the route**, and reserve mid-evolution for very unique fights
(rival, Champion). Design nice, interesting Gym phase systems instead.
**Decision needed:** the Gym phase model (what a 2-phase Gym does without evolving) + a
menu of interesting phase archetypes.
**Steward note (canon):** today Gym ace is 3-phase + evolution-eligible at 50% (§4.4.3.1 /
§4.4.4.3). This removes Gym mid-evo and reframes Gym threat as raw power + smart phases.

## Q10 — League Boons → just better relics (Epic/Legendary)? 🔵
**Owner:** game-designer + content-designer
**User:** Can League Boons just be **better relics** (EPIC / LEGENDARY rarity)? Unify the
systems and make the game a bit easier.
**Decision needed:** collapse Boons into a higher relic-rarity tier, or keep separate.
**Steward note (canon):** today Boons are a distinct League-only pick (6 pool, 1 of 3,
§4.5.2); relics are 3 rarities (§8.3.1). Unifying simplifies but loses League-scoped flavor.
*(Coupled with Q11 — League is deferred, so this is a "design-ahead" note.)*

## Q11 — Defer League + Champion ✅ DECIDED 2026-06-05
**Owner:** producer
**User:** League and Champion fights are **deferred** — not yet implemented. First nail the
loop **R1 → City1 → R2 → City2 → R3 → Victory Road.** When that's solid, redesign the League.
**✅ Resolution:** Accept. The pre-League loop is the active build target. League/Champion
spec (Topic 4 §4.6/§4.7, Topic 2 §2.1.6) is **kept intact but stamped `⚠️ DEFERRED — redesign
after the R1→VR loop`** so nothing is lost and engineering won't build it yet. Q10 (Boons→relics)
is parked with it. → logged **CL-004**.

---

# Domain F — In-Run Progression

## Q12 — XP to whole Box, not just Active Team? 🔵
**Owner:** systems-designer + game-designer
**User:** Should XP go only to the Active Team? My view: **level all Pokémon**, or benched
ones fall behind and never get used.
**Decision needed:** XP distribution model (Active-only / full-Box / Active-full + bench-%).
**Steward note (canon):** today Active-only earns; Exp Share relic = 50% to bench (§5.2.1).
This is the core of the "Scaling Spine" tension in the systems catalog.

## Q13 — Pokémon shouldn't start with 4 moves; scarcer move acquisition 🔵
**Owner:** systems-designer + content-designer + game-designer
**User:** I dislike Pokémon starting R1 with 4 moves. Leveling up should **feel special** —
gain new moves over time. Scarcer move acquisition makes **Move Tutors and TM shops** more
meaningful. Redesign the Pokémon learning curves.
**Decision needed:** starting move count + the level-up / tutor / TM acquisition curve.
**Steward note (canon):** today base kit = 4 moves = full pool at base; pool grows via
evolution/TM/Tutor (§5.3.6.1 / §5.10). This reframes early game around *earning* moves.
Heavily coupled to Q15 (evolution) and Q16 (tutor node).

## Q14 — Are passive abilities necessary? Redesign 🔵
**Owner:** game-designer
**User:** Do we need passive abilities at all? Redesign. If we keep them, discuss later.
Maybe an **ability-learner** (node/system) could help?
**Decision needed:** keep / cut / rework abilities; if kept, how they're acquired.
**Steward note (canon):** today ~30 abilities, granted by evolution (§5.5 / §5.8). Cutting
or gating them via a learner node changes evolution payload + build identity.

## Q15 — Evolution branching: free archetype per stage + lighter evolutions 🔵
**Owner:** game-designer + content-designer
**User:** Could a Pokémon pick **Vanguard on first evolution and Specialist on last**? If
not, why not let the player choose **again** at last evolution? I think evolutions should
just: **upscale stats, improve 1–2 moves** of the current species pool, and **maybe add one
new move** to the species pool.
**Decision needed:** (a) per-stage independent branch choice vs locked path; (b) the lighter
evolution payload (stats + 1–2 move upgrades + maybe 1 new move).
**Steward note (canon):** today the stage-1 branch **locks** the stage-2 direction (one sub-
choice within it), and evolution is a heavier deck rewrite (§5.3.5). Your model is lighter +
more player-driven. Tightly coupled to Q13 + Q14.

## Q16 — Move Tutor as its own node (remove from Pokémon Center) 🔵
**Owner:** content-designer + game-designer
**User:** Remove Move Tutor from Pokémon Centers; make it a **unique node**. Redesign what
that node does.
**Decision needed:** the Tutor node's identity, offer, cost, frequency.
**Steward note (canon):** today Tutor is a Center service (§7.6.1 / §7.8.1); VS interim
*replaces* a move slot (additive pool post-VS, gap #36). Coupled to Q13.

---

# Domain G — Cross-Run Meta-Progression

## Q17 — Trauma cap increased to ~−75% 🔵
**Owner:** systems-designer
**User:** Trauma should have a higher cap — up to about **−75%**.
**Decision needed:** new cap + per-stack value + stack count; re-check the soft-lock rationale.
**Steward note (canon):** today −5%/stack, cap 5 = **−25%** (§6.2.1). A −75% cap is a major
swing (e.g. 15 stacks ×5%, or steeper per-stack) — needs a spiral/soft-lock re-evaluation.

## Q18 — Trainer XP as a full "Battle Pass" 🔵
**Owner:** systems-designer + producer + content-designer
**User:** Player XP should work like a **Battle Pass** — each level visibly unlocks X (new
starters, relics, nodes, etc.). **Design the full Battle Pass now.**
**Decision needed:** the level→unlock track (every level, what it grants), and how it
relates to / replaces Trainer Tokens.
**Steward note (canon):** today two-track (XP→Level gates + Tokens→manual spend, §6.3).
A Battle Pass likely collapses Tokens into a fixed per-level reward track.

## Q19 — Expand + improve the achievement list 🔵
**Owner:** content-designer
**User:** The achievement list is small and not well implemented. Add new interesting ones —
some easily achievable, some very hard.
**Decision needed:** the expanded achievement catalog (with difficulty spread + rewards).
**Steward note (canon):** today ~50 target across 8 categories (§6.7); only ~10 wired (VS).
Coupled to Q18 (achievements may feed Battle Pass / unlocks).

## Q20 — Document the Save/Load design fully 🔵
**Owner:** lead-programmer (+ gdd-steward to author the doc)
**User:** Document the Save/Load design — **every system and object** that must be saved and
loaded.
**Decision needed:** a complete persistence manifest (what's in Meta save vs Run save, field
by field, incl. RNG cursors).
**Steward note (canon):** today §9.8 + §6.10 outline layers/schema; gap #45 (per-stream RNG
cursors not persisted on resume) is the known hole. This is a *documentation* task, not a
redesign — produces a save/load spec doc.

---

# Domain H — Nodes, Content & Economy

## Q21 — Wild biomes follow Region theming/modifiers 🔵
**Owner:** content-designer
**User:** The biomes of Wild Areas should follow the Region (modifiers/theme), right?
**Decision needed:** confirm biome↔Region binding (and whether Region Modifiers influence
biome/species weighting).
**Steward note (canon):** today biomes are Region-gated with a per-Region "primary" biome
(§7.3.1 / §7.10); Region Modifiers don't currently steer biomes. Coupled to Q2.

## Q22 — Catch condition: lower threshold + catch-rate %? 🔵
**Owner:** systems-designer + game-designer
**User:** Dislike the current catch condition. Want ~**below 30%** to capture; status gives a
"+20%" window so a statused Pokémon needs **50%**. Should we make catching a **catch-rate %**?
**Decision needed:** deterministic thresholds (30% / 50%-with-status) vs a probabilistic
catch-rate; reconcile with Pillar 1 (telegraphed, not RNG).
**Steward note (canon):** today deterministic — HP < 50% (or any status → any HP), no roll
(§7.3.4.1). Your 30%/50% is a tighter deterministic version; a % catch-rate would reintroduce
RNG (tension with Pillar 1 — flag for game-designer).

---

# Domain J — Presentation

## Q23 — Full UI design of every system 🔵
**Owner:** ui-programmer + art-director
**User:** With the full design of every topic (cards, map, trainers, wild, etc.), fully design
the UI: what data appears and **how** it's shown, plus the **scene flows**. Examples: what data
a **move card** shows, what a **Pokémon HP bar** shows, the **Pokédex**, the **Trainer Hub** —
**every system.**
**Decision needed:** a complete UI/UX spec per screen + the scene-flow graph. (Large — best
done *after* the gameplay questions above settle, so the UI reflects final mechanics.)
**Steward note (canon):** today Topic 10 has combat layout, card anatomy, map view, iconography
(§10.2–10.4). This expands it to a full per-system UI spec. **Sequencing:** do this last.

---

## Suggested processing order (producer recommendation)

1. **Quick reconciliations first** (clear the drift): Q4, Q5, Q6, Q11 — fast ✅/sync.
2. **Core gameplay identity** (these cascade into everything): Q3 (hand), Q13 (move curves)
   → Q15 (evolution) → Q14 (abilities) → Q16 (tutor node) → Q12 (XP distribution).
3. **Combat feel:** Q7 (unknown intents), Q8 (fields), Q9 (gym phases), Q22 (catching).
4. **Meta & economy:** Q17 (trauma), Q18 (battle pass) → Q19 (achievements), Q1/Q2/Q21
   (city, region mods, biomes), Q10 (boons — parked w/ league).
5. **Documentation:** Q20 (save/load doc), then **Q23 (full UI)** last, on settled mechanics.
