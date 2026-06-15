# Elite Node — Encounter Brainstorm (WIP, for game-designer / content-designer)

> **Status:** open design question, brainstorm only. Not canon. Decide via the Design Question
> Protocol (`.claude/docs/coordination-rules.md`), then write to Notion GDD (Topic 7 nodes / Topic 4
> bosses) + log a CL. Raised by user 2026-06-15.

## What an Elite node *is* (frame)
A **mini-boss**: tougher than a regular Trainer, **below** the Gym climax. A named or themed
opponent with a *readable signature threat* (Pillar 1), and a reward a cut above a normal trainer.
Distinct from the Gym (the layer climax, with the per-type Phase-2 system, CL-013).

## Candidate encounters

### A. Named antagonists (recurring / escalating)
- **Rival (Blue)** — recurring 2–3×/run, scaling each time, a *balanced* (multi-type) team → pure
  skill check. The "you again" beat is strong roguelike texture.
- **Giovanni / Team Rocket boss** — villain mini-boss, Ground-leaning. ⚠️ Fork: canonically also the
  Viridian **Gym** leader → pick a lane (Gym OR Elite-villain, not both).
- **Rocket Executive / Admin** — grunts are regular Trainers; an Admin (hideout/Silph set-piece) is
  the Elite-tier version (Poison/Ground).

### B. Elevated Gen-1 specialists (the variety pool — no named character needed)
A rotating pool keeps runs different (Pillar 3):
- **Karate King / Black-Belt master** (Saffron Dojo) — Fighting.
- **Channeler / Medium** (Pokémon Tower) — Ghost/Psychic, spooky tone.
- **Ace Trainer / Cooltrainer (♂/♀)** — skilled duelist, balanced strong team.
- **Veteran**, **Psychic master**, **Juggler / Tamer / Gambler** — flavourful classes, light gimmicks.
- **Type-specialist elites** (Bird Keeper = Flying, Hiker = Rock/Ground) — *foreshadow the next Gym's type*.

### C. Legendary & boss-wild set-pieces (the *catchable* mini-bosses)
A **different interaction** — survive + **catch** (CL-014 gauge), not just defeat:
- Legendary birds — Articuno (Ice/Flying), Zapdos (Electric/Flying), Moltres (Fire/Flying).
- **Mewtwo** (Psychic) — apex / end-game / roadmap.
- **Snorlax** — the iconic **route-blocker boss-wild**; a near-perfect Elite-node fit, very Gen-1.

## Design calls to make
1. **Split "Elite" into two node flavours?** *Recommended.* `Elite Trainer` (A+B → defeat for a
   **Rare-relic** choice) vs a separate **Legendary** node (C → the prize is the **catch**). They
   reward and play differently enough that one shared node muddies the read; legendaries may want
   their **own map marker**.
2. **Reward tier:** Elite > Trainer. Natural fit = **Rare-relic choice** at Elite; reserve the
   **Legendary-relic pick (CL-021)** for actual Gyms so the Gym climax stays special.
3. **Rival recurrence/scaling** rules (how many times, how it scales).
4. **Roadmap hook:** the **Elite Four** (Lorelei/Bruno/Agatha/Lance) are the obvious Elite-node
   encounters once the **deferred League (CL-004)** comes online — Elite node scales cleanly post-VS.

## VS scope note
VS ends at Gym 1, single Region. A minimal VS Elite set could be: **Rival** + 2–3 elevated
specialists + (optionally) **Snorlax** as a catchable set-piece. Legendaries are likely post-VS.

## Asset tie-in
Elite trainer sprites live in `Assets/Sprites/VS/Trainers/Elite/` (`trainer-elite-<name>.png`,
64×64 battle + 32×32 overworld). Legendary encounters reuse the creature portrait/battler pipeline.
