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
| ⏸ Parked | Deferred; coupled to another decision or out-of-scope for now |
| 🩹 Canon-drift | Current GDD disagrees with reality — reconcile before/while deciding |

## Owners (designer team)

`gdd-steward` (doc + canon fidelity) · `game-designer` (pillars/mechanics) ·
`systems-designer` (numbers/curves) · `content-designer` (content/pools) ·
`ui-programmer` + `art-director` (UI/visual) · `producer` (scope/sequencing).

---

# Domain A — Run & Macro Structure

## Q1 — City: add a Gym option + rethink city nodes ✅ DECIDED 2026-06-10
**Owner:** game-designer + content-designer
**User:** Give players the chance to enter a Gym in the City too — a chance to win
a Badge + rewards at a **risky cost**? Then: which nodes/places can the City have?
Shop **yes** (unique consumables, TMs, Relics). Pokémon Center — *probably not*.
Think Pokémon fantasy **and** StS-inspired structure for better City nodes.
**Decision needed:** (a) optional risky City Gym yes/no + its cost/reward shape;
(b) the full City node roster.
**Steward note (canon):** today City = Center → Curated Shop → Reflection (§2.1.4 /
§7.8). Bonus Badge sources already exist (§4.5.3); a City Gym would be a new one.
**✅ Resolution — Option B (Choice Plaza: StS-style limited-visit hub):**
- The City stops being a fixed linear Center→Shop→Reflection. It becomes a **plaza with a limited
  visit budget**: **Curated Shop** and **Reflection** (Region Modifier) are always available (Shop
  yes; Reflection always closes the City), but the player may visit **only 2 of** the premium nodes
  (visit budget tunable) — a real "what do I prioritise?" choice:
  - **City Gym (risky, optional):** a Gym-tier fight (full CL-013 power premium) vs a type **outside
    the run's Gym pool**. **Reward:** the 4th Badge + a guaranteed **Rare relic** + ₽. **Risk:** you
    enter at **current HP** (no free pre-heal — healing means spending a Center visit you might skip);
    a **team wipe is NOT a run-loss** (it's optional) but the fainted gain **Trauma** and you **forfeit
    the Badge attempt** for this City. This is the interactive home of the §4.5.3 bonus-Badge slot —
    it **subsumes the post-VS Secret Tournament (§4.5.3.1)** as the primary bonus source (Victory Road
    Perfect Clear §4.5.3.2 stays).
  - **Pokémon Center (now OPTIONAL — the user's "probably not"):** full heal + Trauma therapy +
    Daycare; choosing it spends one of the 2 visits (and a small ₽ fee). Not a guaranteed freebie.
  - **Grand Dojo:** city-tier CL-009 Dojo (premium off-learnset move / ability teaching).
  - **Black Market:** a Rare/Epic relic at an HP or Trauma cost (StS risk flavour).
- **Badge cap unchanged:** max 4/run (3 Gyms + 1 City Gym), pool of 12 (§4.4.5).
- **Scope:** the City is **post-VS** (the VS ends at Gym 1) — this locks forward-looking structure;
  build lands with the multi-Region loop (post-CL-004 League work stays deferred).
- **Pillars:** 1 (telegraphed menu + risk ★), 3 (resource sculpting ★), 4 (Badge = identity), 5
  (city fantasy). **Tunable:** visit budget (default 2), City Gym loss penalty, premium-node pricing.
  → logged **CL-015**.

## Q2 — Region Modifiers: timing + pool ✅ DECIDED 2026-06-10
**Owner:** systems-designer + content-designer
**User:** Modifiers start at Route 2, end on Route 3 (I think). Is this good design?
Rethink it, and produce a pool of possible Region Modifiers.
**Decision needed:** when modifiers are picked/active; how many stack; the launch pool.
**Steward note (canon):** today picked at City Reflection (after Gym 1 & 2), up to 2
active, persist to run end; 12-modifier pool (§2.1.4.1 / §7.8.3).
**✅ Resolution — Option B (Per-Region accent) + 16-modifier pool:**
- **Timing/persistence:** exactly **1 modifier active per Region**, **re-chosen each Region** and
  applying **only to that Region** (non-accumulating). Picks: a **pre-R1 pick at run setup (§2.1.1)**
  + **City 1** (R2) + **City 2** (R3). 3 offered → pick 1, weighted to team composition. **R1 is no
  longer vanilla.**
- **Resolves the canon contradiction:** modifiers are now canonically **per-Region** (the modifier
  descriptions' "for the next Region" wording is correct; the old §2.1.4.1 "persist to run end /
  stack to 2" rule is **superseded**).
- **Rationale:** relics + Badges already provide run-long stacking power; making Region Modifiers a
  **transient, re-chosen Region accent** (ties §2.2) gives them a distinct decision texture and lets
  the pool carry **bolder/double-edged** effects.
- **Pool (16 — current 12 retuned + 4 new, tiered):** Strong — Hand of Plenty (+1 hand), Sturdy Lead
  (survive lethal 1/combat), Type Affinity (+10% chosen type), Trauma Resistance (−4%/stack not −5%);
  Medium — Swap Fuel (Lead +5 HP/swap), Lucky Draw (+1 consumable T1), Status Mastery (player statuses
  +1 turn), Pocket Healer (+5% team heal on node's 1st victory), Coin Purse (₽ ×1.5), **Glass Cannon**
  🆕 (+20% dealt **and** taken — double-edge), **Quick Study** 🆕 (+15% combat XP), **Bargain Hunter**
  🆕 (Shop + Dojo −20%); Niche — Iron Skin (−1 from Cleave), Mass Mobilization (SF/SB draw 1), Pokédex
  Whisper (reveal 1st Unknown, CL-011), **Field Surveyor** 🆕 (choose the neutral Battlefield each
  wild/Region combat, CL-012). All numbers placeholder (systems-designer tuning).
- **Pillars:** 1 (telegraphed pick), 2/3 (resource sculpting), 5 (Region flavour). → logged **CL-016**.

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

## Q7 — Unknown intents: frequency + reveal metaprogression ✅ DECIDED 2026-06-10
**Owner:** game-designer (+ systems-designer for unlocks)
**User:** Should Unknown intents be more common? Only on ELITE/GYM/TRAINER nodes? What
metaprogression unlocks "intent shown instead of Unknown"? Proposal: if you've **seen a
Pokémon use an ability**, the next time it uses the same ability its intent is visible.
**Decision needed:** (a) where Unknowns appear; (b) the knowledge-based reveal rule
(Pokédex-driven "I've seen this before → I can read it").
**Steward note (canon):** today 3-tier reveal Witnessed/Scouted/Researched (§4.3.5),
Pokédex Familiar tier reveals a species' intents (§4.3.9.1). Your proposal sharpens the
"Witnessed → permanently readable" loop.
**✅ Resolution — Option B (Per-Species Reinforced):**
- **Wild / Trainer encounters:** no Unknown intents at baseline — all intents Witnessed from turn 1.
- **Elite / Gym encounters:** **1 Unknown intent per enemy per combat** at baseline. Once the
  enemy fires any move (Witnessed tier), all subsequent intents that combat are revealed.
- **Dense Fog modifier:** extends the 1-Unknown-per-enemy rule to Wild and Trainer encounters
  too. Run layer sets `HideBaselineIntents = true` on the CombatSetup when Dense Fog is active.
- Pokédex Familiar tier (§4.3.9.1) cross-run unlock retains full value (removes the baseline
  Unknown from Elite/Gym encounters for Familiar species).
- Also closes **VS gap #44** (Dense Fog HideAllEnemyIntents + Iron Will HP combat effects).
→ logged **CL-011**.

## Q8 — Field effects: are they positive? Redesign ✅ DECIDED 2026-06-10
**Owner:** game-designer + content-designer
**User:** Field effects are positive? Redesign them.
**Decision needed:** the field-effect model — who benefits, player-controllable vs enemy-
set, the launch set.
**Steward note (canon):** today 3 fields (Sunny/Rain Weather + Electric Terrain), set by
encounter, symmetric multipliers (§4.3.8); Gym type-fields are inert (gap #33).
**✅ Resolution — Option D (Tiered: neutral Battlefields + enemy-owned Home Fields):**
- **Field gains an `owner` flag** (`Neutral` / `Enemy`) — one engine, two classes.
- **Battlefield (neutral, wild / Region 3+):** symmetric, both sides — the current model sharpened.
  Launch set: ☀️ Sunny Day (Fire ×1.5 / Water ×0.5), 🌧️ Rain Dance (Water ×1.5 / Fire ×0.5),
  ⚡ Electric Terrain (Electric ×1.3 grounded + Paralysis blocked on grounded), and 🪨 **Sandstorm**
  (new **hazard class** — Rock/Ground/Steel immune; every other mon loses **5% max HP at end of its
  turn**, pressuring low-HP + freshly-swapped mons → ties fields to the faint/swap economy).
- **Home Field (enemy-owned, Gym / Elite):** same fields with `owner = Enemy`, so the boost is
  **one-sided** — the boss sets a Home Field of **its own type** at combat start (telegraphed
  `🏠 Home Field: [Type]` badge); **enemy** moves of that type ×1.5, **player** same-type moves ×1.0
  (no boost — their turf). **No player-side suppression at launch** (threat = amplified enemy offense,
  not a tax). **Closes gap #33** (Gym type-fields finally bite).
- **Counterplay:** (a) resist/avoid the boss's type; (b) new Shop consumable **"Smoke Ball"** clears
  the active field (any class) for the rest of combat — the guaranteed answer. **Follow-up content
  (not launch-blocking):** a few player **field-setting moves** that overwrite a Home Field with a
  neutral Battlefield (strip ownership), and a rare **"Weather Vane" relic** that flips an enemy
  Home Field to player-owned.
- **Pillars:** 1 (telegraphed boss threat ★), 2 (swap/resist to answer; hazard pressures swap-ins ★),
  3 (resist-stacking + type comp), 5 (weather/terrain flavour). → logged **CL-012**.

---

# Domain E — Bosses & Challenge Arc

## Q9 — Gym Leader phase systems (no mid-fight evolution) ✅ DECIDED 2026-06-10
**Owner:** game-designer
**User:** Gyms shouldn't have "epic" phases like mid-fight evolution. Prefer Gyms field
**more powerful Pokémon than the route**, and reserve mid-evolution for very unique fights
(rival, Champion). Design nice, interesting Gym phase systems instead.
**Decision needed:** the Gym phase model (what a 2-phase Gym does without evolving) + a
menu of interesting phase archetypes.
**Steward note (canon):** today Gym ace is 3-phase + evolution-eligible at 50% (§4.4.3.1 /
§4.4.4.3). This removes Gym mid-evo and reframes Gym threat as raw power + smart phases.
**✅ Resolution — Option D (Power premium + per-type signature Phase 2):**
- **Mid-fight evolution is REMOVED from Gyms entirely** — reserved for the unique fights (rival /
  Champion, which already carries it §4.4.2). The "Evolution Phase" type (§4.3.7) stays in the catalog
  but is Champion/rival-only.
- **Power premium:** Gym Pokémon sit at a defined **level bump over the Region's wild band** (ace >
  non-ace); a tunable systems-designer number (placeholder, like the XP curve). This is the "more
  powerful Pokémon" threat the user wants.
- **Per-type signature Phase 2 (the menu):** each of the 12 Gym types gets **exactly one** Phase-2
  identity drawn from a 4-archetype menu — telegraphed, learnable, replayable:

  | Archetype | Phase-2 behaviour | Gym types | Counterplay |
  |---|---|---|---|
  | **Entrenchment** | +Def stages + damage-reduction Home-Field clause; race the wall | Rock, Ground | DoT, stat-strip, Def-ignoring moves |
  | **Status Siege** | Mass Status — floods the Lead with the Gym's signature status | Poison, Grass, Bug | cleanse, swap statused Lead, immune typing |
  | **Onslaught** | Mass Attack + Home-Field ×1.5; burst race | Fire, Fighting, Normal | resist wall, defensive swaps, heal |
  | **Tempo Control** | AP/swap taxes + Paralysis/Freeze locks (or intent-hide, ties CL-011) | Electric, Psychic, Ice, Water | AP mgmt, immunity, telegraphed play |

- **Phase shape:** Phase 1 (>50%) stays "setup / read the player" for all Gyms; **Phase 2 (≤50%)** fires
  the type's signature archetype; the **ace Phase 3 (≤20%)** keeps the last-stand (cooldown reset +
  uncapped signature + Sturdy) **minus evolution**. Non-ace Gym Pokémon stay 2-phase.
- **Reuses CL-012** (the Home Field is the substrate for Entrenchment/Onslaught clauses) and **CL-011**
  (Tempo Control can use intent-hiding). **Closes gap #33's intent** (Gym type-field now has teeth).
- **Pillars:** 1 (telegraphed, learnable per-type signature ★), 2 (Phase 2 forces swaps/repositioning ★),
  3 (rewards resist/type comp), 5 (clean type fantasy). → logged **CL-013**.

## Q10 — League Boons → just better relics (Epic/Legendary)? ✅ DECIDED 2026-06-11
**Owner:** game-designer + content-designer
**User:** Can League Boons just be **better relics** (EPIC / LEGENDARY rarity)? Unify the
systems and make the game a bit easier.
**Decision needed:** collapse Boons into a higher relic-rarity tier, or keep separate.
**Steward note (canon):** today Boons are a distinct League-only pick (6 pool, 1 of 3,
§4.5.2); relics are 3 rarities (§8.3.1). Unifying simplifies but loses League-scoped flavor.
**⏸ Was parked with Q11 / CL-004 (2026-06-05); de-deferred for DESIGN by the user 2026-06-11**
(League *combat* stays deferred per CL-004; this resolves the Boon→relic redesign only).
**✅ Resolution — Option C (choice-only Legendary rarity tier) + 4 sub-decisions:**
- **New rarity class `Legendary`** above Rare — **not** in the random drop table (Common 60 / Uncommon
  30 / Rare 10 untouched). Legendary is a *rarity class*, not a meta-tier; all 10 available from run 1.
- **Acquisition = choice-only (Pillar 1 ★):** each **Gym victory** offers a **Legendary pick (1 of 3)**;
  the **Victory Road Summit** pick (the former Boon slot); the **Black Market** (CL-015) may stock one.
  Never an RNG drop (~4 pick-moments/run = reliable, telegraphed access = the "a bit easier" the user wanted).
- **Hold cap: max 2 Legendaries/run** (Q10c) — at cap, pick-moments offer a Rare relic / skip instead.
  Keeps Legendaries a deliberate apex sculpt (Pillar 3).
- **Effects retuned ~⅔** for permanent run-long scope (Q10a) — they were League-only/5-fights.
- **Pool = 10** (Q10d): 6 ported Boons + 4 new (Grandmaster's Tempo, Living Legend, Unbreakable Will,
  Apex Predator), spread across all 5 synergy categories.
- **Boons removed:** §4.5.2 Boon system → Legendary relics; §4.5.1.4 Summit "Boon selection" → "Legendary
  pick." Excluded from Starting Relics (§6.6.3) + shop random stock.
- **Pillars:** 1 ★ (choice, never reactive RNG), 3 ★ (2-cap apex sculpt), 5. Numbers tunable;
  **code post-VS** (relic tier + League both deferred). → logged **CL-021**.

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

## Q12 — XP distribution (Box vs Active-only) ✅ DECIDED 2026-06-07
**Owner:** systems-designer + game-designer
**User:** Should XP go only to the Active Team? My view: **level all Pokémon**, or benched ones
fall behind and never get used.
**✅ Resolution:** **Active Team earns 100%; all other Box Pokémon earn 75% baseline.** The Exp
Share relic lifts benched Pokémon to **100%** (re-spec from its old +50%). Keeps the Box viable
(near-pace on stats AND the new level-gated move unlocks, CL-006) while preserving a small reward
for active use. Trade: team selection loses most of its *progression* cost (now a tactical/type
decision) — the investment tension lives in Trauma, the Dojo's gold cost, and held items instead.
→ logged **CL-010**.

## Q13 — Move-acquisition curve (start fewer, earn more) ✅ DECIDED 2026-06-07
**Owner:** systems-designer + content-designer + game-designer
**User:** I dislike Pokémon starting R1 with 4 moves. Leveling up should **feel special** —
gain new moves over time. Scarcer move acquisition makes **Move Tutors and TM shops** more
meaningful. Redesign the Pokémon learning curves.
**✅ Resolution — Level-gated learnset:**
- A base-form Pokémon **starts with 2 moves** (Q13a) and learns more at level thresholds via a
  per-species **level-up learnset** (the classic "leveling unlocks a move" beat).
- A Pokémon knows all learnset moves with `level ≤ current level`. **Deck contribution =
  `min(known, 4)`** per Pokémon — active-4 cap **unchanged** (Q13b), Mastery = immutable 5th.
- So the deck **thickens as you level**: early-R1 ~6 cards (3 base-form starters × 2) → ~12 by
  Gym 1 (×4). The thin early deck is intentional tutorial simplicity.
- **Recruited wilds** enter at their Region's level band, so they already know their learnset up
  to that level (3–4 moves mid/late game) → late recruits stay viable.
- **Scarcity is the lever:** the natural learnset is lean, so **Tutors (Q16) + TMs** add
  *off-learnset* moves that genuinely matter, and **evolution (Q15)** adds/upgrades a little.
- **Cadence** (exact learn levels — e.g. 3rd move mid-R1, 4th by first evolution) is
  systems-designer tuning via per-species learnset + `ProgressionConfigSO` (placeholder, like the
  XP curve). → logged **CL-006**.

## Q14 — Passive abilities: keep, decoupled to a learner ✅ DECIDED 2026-06-07
**Owner:** game-designer
**User:** Do we need passive abilities at all? … If we keep them, discuss later. Maybe an
**ability-learner** could help?
**✅ Resolution:** **Keep** abilities (iconic depth — Sturdy/Intimidate/Keen Eye/Levitate…), but
**decouple from evolution** — they are **earned via an ability-learner**, not auto-granted. One
passive slot per Pokémon (existing `PokemonInstance.Ability`). This kills the "free rider on
evolution" + the per-stage passive-combo mess (Q15) and fits the earn/sculpt theme.
**Deferred (per user):** the learner's *form* — likely **folded into the Move Tutor node (Q16)**
as a combined "Dojo," with economy/frequency designed there. → logged **CL-008**.

## Q15 — Evolution: free archetype per stage + lighter payload ✅ DECIDED 2026-06-07
**Owner:** game-designer + content-designer
**User:** Could a Pokémon pick **Vanguard on first evolution and Specialist on last**? … why not
let the player choose **again**? Evolutions should just: **upscale stats, improve 1–2 moves**,
and **maybe add one new move**.
**✅ Resolution:**
- **(a) Free archetype each stage (Q15a):** the player picks an archetype **independently at every
  evolution** from the species' available 2–3 — stage 1 no longer locks stage 2 (Vanguard→Specialist
  allowed). Strengthens Identity-through-Evolution + Synergy-sculpted; each pick still permanent.
- **(b) Lighter payload (Q15b):** each evolution = **stat upscale + improve 1–2 existing pool moves
  + maybe +1 new pool move** (the final-evolution new move = the species **signature**). Replaces
  the heavy multi-upgrade/sub-branch (A1/A2) rewrite of §5.3.5.
- Passive ability per archetype is **pending Q14**.
- Likely **moots/reshapes gap #46** (duplicate final-form SpeciesId from A1≡A2 sub-branches).
  → logged **CL-007**.

## Q16 — Move Tutor → standalone "Dojo" node ✅ DECIDED 2026-06-07
**Owner:** content-designer + game-designer
**User:** Remove Move Tutor from Pokémon Centers; make it a **unique node**. Redesign it.
**✅ Resolution — "The Dojo"** (non-combat utility node on the Region map):
- Teaches, per Pokémon: an **off-learnset move** (stage-aware tutor pool) **and/or an ability**
  (Q16a — this is the home of Q14's ability-learner; teach/swap the one passive).
- **Poké Dollar cost** (Q16b), scaling by move/ability power — a real economic choice + the
  game's main **gold sink**; teach multiple if affordable.
- **Removed from Pokémon Centers** → Centers = heal + Trauma therapy only.
- Telegraphed on the map (Pillar 1); the key **deliberate-sculpt** stop (Pillar 3); ~1 per Region
  (frequency = tuning). → logged **CL-009** (also completes Q14's deferred "form").

---

# Domain G — Cross-Run Meta-Progression

## Q17 — Trauma cap increased to ~−75% ✅ DECIDED 2026-06-10
**Owner:** systems-designer
**User:** Trauma should have a higher cap — up to about **−75%**.
**Decision needed:** new cap + per-stack value + stack count; re-check the soft-lock rationale.
**Steward note (canon):** today −5%/stack, cap 5 = **−25%** (§6.2.1). A −75% cap is a major
swing (e.g. 15 stacks ×5%, or steeper per-stack) — needs a spiral/soft-lock re-evaluation.
**✅ Resolution — Option C (Two-zone curve, soft cap −75% at 10 stacks):**
- **Zone 1 (stacks 1–5): −5% each → −25% at 5** — *unchanged from today.* Normal play feels identical;
  the gentle early game (and its anti-spiral protection) is preserved.
- **Zone 2 (stacks 6–10): −10% each → −75% floor at 10 stacks.** Soft cap moves 5 → 10.
- **Formula:** `EffectiveMaxHP = floor(BaseMaxHP × max(0.25, 1 − 0.05·min(s,5) − 0.10·max(0, min(s,10) − 5)))`.
  Multiplier ladder: 0→1.00, 1→.95, 2→.90, 3→.85, 4→.80, 5→.75, 6→.65, 7→.55, 8→.45, 9→.35, 10+→.25.
- **Spiral safety (the §6.2.1 rationale, re-evaluated):** deep Trauma is now safe to allow because it's
  **per-instance** and the Box (6–8 + recruitment) lets you **bench/retire** a breaking-down Pokémon;
  **CL-010** keeps benched mons leveled so rotation is painless; clearing sources (Salve/Therapy/Daycare,
  §6.2.4) still recover. The deep zone reframes as a deliberate *"rest or retire this Pokémon"* signal,
  not a run-loss. Therapy (removes 1/visit) may need a tune vs the deeper cap — Salve/Daycare (remove all)
  still cover it.
- **Pillars:** consequence-of-faint depth (the Trauma threat now has real teeth) while Pillar-1 telegraph
  (Map-View badge + pre-combat Effective Max HP preview) is unchanged. All numbers systems-designer-tunable.
  → logged **CL-017**.

## Q18 — Trainer XP as a full "Battle Pass" ✅ DECIDED 2026-06-11
**Owner:** systems-designer + producer + content-designer
**User:** Player XP should work like a **Battle Pass** — each level visibly unlocks X (new
starters, relics, nodes, etc.). **Design the full Battle Pass now.**
**Decision needed:** the level→unlock track (every level, what it grants), and how it
relates to / replaces Trainer Tokens.
**Steward note (canon):** today two-track (XP→Level gates + Tokens→manual spend, §6.3).
A Battle Pass likely collapses Tokens into a fixed per-level reward track.
**✅ Resolution — Option B (Hybrid Battle Pass: fixed track + Token choice milestones):**
- **Single earn-source:** Trainer XP only (§6.3.2 sources unchanged) → Trainer Level (curve `500×N^1.6`,
  §6.3.3 unchanged). Each level grants its authored reward instantly on level-up.
- **~80% auto-grants / ~20% Token milestones:** every 5th level (5/10/15/20/25/30) grants **Trainer
  Tokens**; all other levels auto-grant an option-expanding reward.
- **Tokens repurposed to an agency lane only:** the old per-run `floor(run XP / 100)` earn is
  **superseded** — Tokens now come from **track milestones + select achievements**. Tokens are spent at
  the Pokémart on the **Tier-3 Mastery-relic lane** (§6.6.1, 10 relics × 5 Tokens) in any order — the
  retained choice that avoids the §6.3.4 XP-funnel trap.
- **Hub upgrades + meta-starters move onto the track** (auto-granted on schedule); their old **Token
  costs are removed** (§6.4.2 / §6.5.2). The meta-starters' thematic criteria (e.g. Riolu's "Underdog
  Run") **survive as achievements** granting bonus XP/Tokens rather than gating the starter (→ Q19).
- **Discovery layer intact:** achievement-triggered **Tier-2** relic unlocks (§6.6.1) stay orthogonal.
- **No power (§6.1 hard rule preserved):** every reward expands options / QoL / cosmetics — never
  +damage/HP/baseline.
- **Full 1–30 track authored** (see §6.3.5): starters Pikachu(L4)/Eevee(L8)/Riolu(L12); 7 Hub upgrades
  at L3/6/7/9/11/13/18; difficulty mods at L14/17/21; relic-pool drips; cosmetics; Token milestones
  L5(+5)/10(+5)/15(+8)/20(+8)/25(+10)/30(+10) ≈ 44 Tokens (achievements top up the long-tail). All
  numbers systems-designer-tunable placeholders.
- **Pillars:** §6.1 philosophy (failure-is-fuel ★, options-never-power), Pillar 5 (cheerful reveals),
  Pillar 3 (expanded sculpt options). **Code:** meta-progression is post-VS (VS ends at Gym 1) — GDD now,
  code deferred. → logged **CL-019**.

## Q19 — Expand + improve the achievement list ✅ DECIDED 2026-06-11
**Owner:** content-designer
**User:** The achievement list is small and not well implemented. Add new interesting ones —
some easily achievable, some very hard.
**Decision needed:** the expanded achievement catalog (with difficulty spread + rewards).
**Steward note (canon):** today ~50 target across 8 categories (§6.7); only ~10 wired (VS).
Coupled to Q18 (achievements may feed Battle Pass / unlocks).
**✅ Resolution — Option B (Medal-tier framework + 50-entry catalog):**
- **Medal tiers set the reward band:** 🥉 Bronze (easy, 50–100 XP) · 🥈 Silver (medium, 150–250 XP) ·
  🥇 Gold (hard, 250–400 XP **+2 Tokens**) · 💎 Platinum (very hard, 400–500 XP **+5 Tokens**, occasional
  Tier-2 relic / cosmetic). XP always; **Tokens on Gold/Platinum** (CL-019-aligned — hard achievements
  fund the §6.6.1 Mastery-relic long-tail the §6.3.5 track leaves short).
- **50 achievements** authored across the 8 canon categories (First Steps 5 · Recruitment 6 · Evolution 6
  · Mastery 6 · Combat 7 · Boss 7 · Build Identity 7 · Endurance 6). **~20% Hidden** (revealed on
  completion, §6.7.3).
- **Starter criteria folded in** (★): The Long Road (reach R2 → Pikachu), Many Faces (win + 4 evos → Eevee),
  Underdog (Champion, no fully-evolved → Riolu) — flavor markers; the starter itself unlocks on the
  §6.3.5 track (CL-019). **◆** = deferred-League achievements (Champion/Speedrunner) catalogued but
  earn-gated until the League ships (CL-004).
- **Pillars:** §6.1 (options never power — rewards are XP/Tokens/cosmetics), Pillar 5 (cheerful medal
  flavor). All numbers systems-designer-tunable. **Code:** post-VS (meta Epic; VS ships ~10).
  → logged **CL-020**.

## Q20 — Document the Save/Load design fully ✅ DECIDED 2026-06-12
**Owner:** lead-programmer (+ gdd-steward to author the doc)
**User:** Document the Save/Load design — **every system and object** that must be saved and
loaded.
**Decision needed:** a complete persistence manifest (what's in Meta save vs Run save, field
by field, incl. RNG cursors).
**Steward note (canon):** today §9.8 + §6.10 outline layers/schema; gap #45 (per-stream RNG
cursors not persisted on resume) is the known hole. This is a *documentation* task, not a
redesign — produces a save/load spec doc.
**✅ Resolution — full field-by-field persistence manifest (§9.8.6/§9.8.7 + §6.10) + 5 gap fixes:**
- **Documentation (the ask):** authored the complete manifest — for every persisted object, its
  save layer, every field, type, how SO refs are stored (**stable string IDs via the registry, never
  instanceIDs**), what is intentionally **transient**, schema-versioning/migration (§9.8.3,
  SCHEMA_VERSION stays 1 — new fields are additive/back-compatible), atomicity/corruption (§9.8.4),
  and the **mid-combat rule** (§9.8.5 — saves fire on **node entry, before resolution**; combat is
  atomic, so every combat-scoped field is transient by construction). Layers: Meta (`meta.dat`,
  whole-object JSON) · Pokédex (`bestiary.dat`) · Run (`run-current.dat` → `RunSaveDTO`) · Settings.
- **Gap closure (reconcile manifest ↔ reality) — all VS-relevant, +6 tests, 1187 green:**
  - **A · #45 RNG cursors** — `GameRNG.State` get/set; `RNGStreams.Capture/RestoreContentCursors`;
    `RunStateSO.RngCursors` round-trips via the DTO; autosave captures all 5, resume **restores the 4
    content streams and re-derives MapRNG** (decision below).
  - **B · CL-021 Legendary** — `RunLauncher` registers `LegendaryRelicCatalog.BuildAll()` so a held
    Legendary's id resolves on resume instead of silently dropping.
  - **C · CL-018 biome** — `RunContentRegistry` biome index + `RunStateDTO.NaturalistLensBiomeId`
    (the steered biome now round-trips instead of relying on auto-surface).
  - **D · ShieldHP** — `PokemonInstance.Reset()` zeroes it (combat-transient; never carried between
    nodes on a save-restore).
  - **E · CL-019 Tokens/ClaimedLevelMilestones** — verified to round-trip (whole-object Meta JSON).
- **Decision (user-approved 2026-06-12): map topology re-derives from seed, NOT from a saved cursor.**
  The map is rebuilt by **deterministic replay** of MapRNG (`RegionMapGenerator` in `Resume`), so MapRNG
  must stay at its region-entry state; restoring its save-time (post-build) cursor would regenerate a
  *different* map. Therefore only the **4 content cursors** (Combat/Loot/Mystery/Encounter) are restored;
  MapRNG re-derives. *(Post-VS: multi-region resume needs per-region MapRNG **entry** state — the GameRNG
  overload doesn't re-salt by regionIndex — flagged in BACKLOG.)* → logged **CL-022**.

---

# Domain H — Nodes, Content & Economy

## Q21 — Wild biomes follow Region theming/modifiers ✅ DECIDED 2026-06-11
**Owner:** content-designer
**User:** The biomes of Wild Areas should follow the Region (modifiers/theme), right?
**Decision needed:** confirm biome↔Region binding (and whether Region Modifiers influence
biome/species weighting).
**Steward note (canon):** today biomes are Region-gated with a per-Region "primary" biome
(§7.3.1 / §7.10); Region Modifiers don't currently steer biomes. Coupled to Q2.
**✅ Resolution — Option C (Opt-in biome-steer modifier):**
- **Part 1 — binding confirmed (no change):** biomes **are** Region-bound today and stay so. §7.3.1
  gates each of the 7 biomes by Region with a per-Region *primary* biome (weighted more often); §7.10
  locks each Region's biome focus (R1 Meadow-primary, R2 Sea-primary, R3 Volcano-primary). The user's
  intuition is canon — biomes follow the Region.
- **Part 2 — modifiers may steer biomes, but only opt-in:** baseline Region Modifiers stay orthogonal
  to encounters, **plus** one new modifier — **Naturalist's Lens** — lets the player steer that
  Region's Wild-Area biome weighting. No hidden global tilt; the steer costs your one modifier slot
  and is fully telegraphed.
- **Naturalist's Lens (pool 16 → 17, tier Medium):** at Region start the player chooses one biome from
  the Region's **eligible** set; it becomes that Region's **primary biome** (dominant Wild-Area
  weighting) for the Region, **overriding** the default primary. Reuses the existing per-Region
  primary-biome weighting machinery (§7.3.1) — no new sampling logic.
- **Recruit-starvation guard:** the picker offers **only the Region's eligible biomes**, and every
  biome carries a full Common/Uncommon/Rare pool (§7.3.3, ≥3 species), so a primary-swap can never
  starve the 3-species offer (§7.3.2). The chosen biome is **dominant, not exclusive** — secondary
  biomes still appear, preserving variety.
- **VS-relevant:** R1 is in the VS with 3 eligible biomes (Meadow/Cave/River) — Naturalist's Lens lets
  a VS player make Cave or River primary (steer toward Rock/Ground/Fighting or Water recruits).
- **Pillars:** 1 (telegraphed pick; biome visible on each Wild node), **3 ★ (sculpt the recruit pool —
  hunt the type/species you want)**, 5 (Region flavour — specialise the Region). All weights
  systems-designer-tunable. → logged **CL-018**.

## Q22 — Catch condition: lower threshold + catch-rate %? ✅ DECIDED 2026-06-10
**Owner:** systems-designer + game-designer
**User:** Dislike the current catch condition. Want ~**below 30%** to capture; status gives a
"+20%" window so a statused Pokémon needs **50%**. Should we make catching a **catch-rate %**?
**Decision needed:** deterministic thresholds (30% / 50%-with-status) vs a probabilistic
catch-rate; reconcile with Pillar 1 (telegraphed, not RNG).
**Steward note (canon):** today deterministic — HP < 50% (or any status → any HP), no roll
(§7.3.4.1). Your 30%/50% is a tighter deterministic version; a % catch-rate would reintroduce
RNG (tension with Pillar 1 — flag for game-designer).
**✅ Resolution — Option D (Catchability Gauge: deterministic, catch-rate *feel*, no RNG):**
- Answers "catch-rate %?" with the catch-rate **feel as a deterministic gauge** — Pillar 1 stays
  intact (no roll). The probabilistic Option B was rejected (would violate Pillar 1).
- **0–100 Catchability gauge** on the wild Pokémon; **catch succeeds when gauge = 100** (computed,
  not rolled).
- **CatchThreshold (HP%)** = `30 (base) + 20 (if ANY status, non-stacking) + ball bonus
  (Great +15 / Ultra +30, post-launch)`.
- **Gauge fill (linear):** `gauge = clamp(0,100, round(100 × (100 − HP%) / (100 − CatchThreshold)))`
  — full HP → 0; HP% at threshold → 100/READY. Status visibly jumps the gauge (threshold 30→50).
- **Numbers honoured:** basic ball, no status → catchable at HP ≤ 30%; with status → HP ≤ 50%.
- **Tightening:** the old "status → catch at ANY HP" is **removed** — status is now a meaningful
  +20pt window, not a trivializer.
- **Throw rule unchanged:** gauge < 100 → fail + ball spent; gauge = 100 → success → Victory + full XP
  (§7.3.4.1 step 6); HP ≤ 0 → faint, recruit lost.
- **Display (Pillar 1):** catchability bar (gold "READY ✓" at 100) + Pokéball hover state ("Catch:
  READY ✓" / "Catchability 78% — weaken or apply status").
- **Pillars:** 1 ★ (deterministic, fully telegraphed gauge), 2 (status-then-throw planning), 5
  (catch-rate flavour). → logged **CL-014**.

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

*Updated 2026-06-11. Q1–Q19, Q21, Q22 are ✅ DECIDED and in the change log (21 of 23); Q10 design
resolved (CL-021, League combat still deferred per CL-004). Remaining open work:*

1. ✅ **Done:** Combat feel (Q8/Q9/Q22), World structure (Q1/Q2/Q21), Trauma (Q17), progression
   cascade (Q12–Q16), Unknown intents (Q7), Battle Pass (Q18), achievements (Q19), **Boons→Legendary (Q10)**.
2. **Documentation:** Q20 (save/load manifest — now also covers Battle Pass track + achievement state).
3. **Last:** **Q23 (full UI spec)** — after mechanics settle.

*Remaining: **Q20 (doc) → Q23 last.** (Q10 done; only Q20 + Q23 remain — 2 of 23 open.)*
