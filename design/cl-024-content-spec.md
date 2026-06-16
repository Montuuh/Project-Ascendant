# CL-024 — Elite Node Content Spec (authoring source for the seeder)

> Source for the editor seeder that authors the CL-024 species + Elite/EliteWild/roster assets.
> Canon = GDD Topic 7 §7.5.1/§7.5.2/§7.12 + Topic 4 §4.3.7/§4.4.4.3. All numbers below are
> **systems-designer-tunable placeholders** unless cited. Follow §5.3.6 move-kit rules + §5.12.1 lean
> learnset. Engine already built (commit 7934264): `EliteTrainerSO` (RareRelicChoices, IsRival,
> RivalEvoSpecies, RegionScaling), `EliteWildSO`, `EliteTrainerRosterSO`, weighted map-gen placement.

## Decisions locked
- **Marowak's Spirit catch → recruit a LIVING Ground-type Marowak** (+ a **Thick Club** held item, Marowak-only, +50% Melee). Not a "lay to rest → relic".
- **Pillar-1/2 fixes (apply during authoring):** deterministic multi-hit counts — Fury Swipes=3, Icicle Spear/Spike Cannon=5, Bonemerang=2, Thrash=fixed 2 turns. Dugtrio passive = **Sand Veil** (NOT Arena Trap). Snorlax **Rest heal capped 50% MaxHP**. Marowak's Spirit **Curse = 25% self-HP** (not 50%).
- **Sprites: placeholder/reused** (real art via brief later). Reference paths: `Assets/Sprites/VS/Portraits/NNN-Name.png`, `BoxIcons/NNN-Name.png`, `Trainers/Elite/trainer-elite-<name>.png`.

## Roster weights (EliteTrainerRosterSO per Region)
- R1: Rival 80 / Specialist 20
- R2: Rival 60 / Specialist 40
- R3: Rival 40 / Giovanni 30 / Specialist 30

## Reward (all Elite Trainers): 3 Rare relics (RareRelicChoices) + ~300/400/500₽ + Trainer XP (band).

---
## Rival "Blue" (IsRival=true) — scales by Region band (RegionScaling)
- **R1** (2 mons, 2-phase, no mid-fight evo): Pidgeotto (Flying/Normal, ~Lv12-14) + Ivysaur (Grass/Poison, ~Lv13-15).
- **R2** (3 mons, 2-phase): Pidgeot (~Lv22-24) + Gyarados (Water/Flying, ~Lv23-25, Intimidate) + Exeggutor (Grass/Psychic, ~Lv24-26).
- **R3** (3 mons; ACE mid-fight evo per §4.3.7): Pidgeot (~Lv32-34, Keen Eye) + Alakazam (Psychic, ~Lv33-35) + **Wartortle(Lv34) → Blastoise(Lv36) at 50% HP** (3-phase ace; RivalEvoSpecies=Blastoise).

## Giovanni — dual lane
- **Elite (R3, 30%)**, 2 mons 2-phase: Dugtrio (Ground, ~Lv32-34, **Sand Veil**) + Persian (Normal, ~Lv34-36, Tough Claws; signature).
- **Gym (Viridian Ground Leader)**, 2 mons; ace 3-phase no evo: Nidoqueen (Poison/Ground, ~Lv34-36) + **Rhydon (Ground/Rock, ~Lv36-38, ACE 3-phase, Solid Rock)**. Home Field = Ground ×1.5 (§4.4.4.3).

## Specialist pool
- **R1 (20%)**: Ace Trainer — Pidgeotto + Ivysaur (reuse existing `ace_trainer_r1`).
- **R2 (40%)**: Karate King — Hitmonchan (Fighting) + Primeape (Fighting); Channeler — Haunter (Ghost/Poison, Levitate) + Hypno (Psychic, Insomnia).
- **R3 (30%)**: Cooltrainer — Dewgong (Water/Ice, Thick Fat) + Cloyster (Water/Ice, Shell Armor). (+more post-VS.)

---
## Elite Wild boss-wilds (EliteWildSO, PhaseCount=2, no evo, DefeatRelic=Rare)
- **Snorlax** (#143, Normal, ~Lv14-16, boss HP ≈2× Elite mon): Rest(heal **50%** MaxHP, self-Sleep 2t, P1 only) / Snore(Ranged 60, Sleep-only) / Body Slam(Melee 85, 30% Paralysis) / Amnesia(+2 SpD). Passive Thick Fat + Immunity. P1 stall→P2 offense. Catch→recruit Snorlax.
- **Marowak's Spirit** (Ghost variant of #105, ~Lv14-16): Curse(**25%** self-HP → 3t DoT) / Confuse Ray(3t Confuse) / Shadow Bone(Melee 85 Ghost, 20% Def↓) / Lick(Melee 40 Ghost, 30% Paralysis). Passive Levitate + Cursed Body. **Catch → recruit a LIVING Ground Marowak** (Bone Club / Headbutt / Bonemerang[2 hits] / Growl; Rock Head; **Thick Club** auto-equipped +50% Melee).

---
## 18 net-new species (PokemonSpeciesSO + MoveSO; reuse shared MoveSO: Earthquake, Psychic, Hydro Pump, etc.)
Snorlax#143 (Normal), Marowak#105 (Ground), Marowak's-Spirit (Ghost, distinct SO), Pidgeot#018 (Flying/Normal), Blastoise#009 (Water), Alakazam#065 (Psychic), Gyarados#130 (Water/Flying), Exeggutor#103 (Grass/Psychic), Dugtrio#051 (Ground), Persian#053 (Normal), Nidoqueen#031 (Poison/Ground), Rhydon#112 (Ground/Rock), Hitmonchan#107 (Fighting), Primeape#057 (Fighting), Haunter#093 (Ghost/Poison), Hypno#097 (Psychic), Dewgong#087 (Water/Ice), Cloyster#091 (Water/Ice). Plus **Thick Club** HeldItemSO.

Full move kits + phase scripts + per-species stat bands are in the content-designer output (this session). Author R1 first (Snorlax, Marowak, Marowak's-Spirit, Rival R1), then R2, then R3. Keep the project COMPILING + EditMode green after each Region's batch.
