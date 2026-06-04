# R1 Gym Pool — Implementation Spec (map redesign v2)

WIP design note (→ Notion once locked). Completes the R1 4-gym pool {Rock, Water, Bug, Normal}
for the §7.2 v2 map fork. Rock = existing `rock_gym_r1`. Badge effects are already canon in GDD §4.4.5.1.
All species below ALREADY EXIST in Assets/ScriptableObjects/VS/Species/.

## Gyms to author (GymLeaderSO, template = rock_gym_r1; rewards 50 XP / 500₽ / rare relic)

| GymLeaderId | GymType | Slot1 (2-phase) | Ace (3-phase, Sturdy, mid-fight evo) | Badge |
|---|---|---|---|---|
| water_gym_r1 | Water | Squirtle L13 | Wartortle_Vanguard L15 → Blastoise_VanguardA1 | cascade_badge |
| bug_gym_r1 | Bug | Caterpie L12 | Metapod L15 → Butterfree | hive_badge |
| normal_gym_r1 | Normal | Pidgey L13 | Pidgeotto L16 → Pidgeot | normal_badge |

(Rock baseline: Geodude L14 + Graveler L16 ace.)

## Badges to author (BadgeSO, template = boulder_badge; effects per §4.4.5.1)

| BadgeId | DisplayName | GymSource | Effect (EffectDescription) | Hook |
|---|---|---|---|---|
| cascade_badge | Cascade Badge | R1-Gym2 | After a MANUAL Lead swap, draw 1 extra skill card this turn. | OnLeadChanged |
| hive_badge | Hive Badge | R1-Gym3 | When a card cycles discard→deck, 20% to make a free copy next turn. | OnSkillCardCycled |
| normal_badge | Normal Badge | R1-Gym4 | Base stats treated +10% for damage dealt AND received. | OnDamageCalculation |

## Dependencies / deferrals (NOT blocking the map fork)
- **Badge passive EFFECTS need combat code** (hooks OnSkillCardCycled / OnDamageCalculation + subscribers).
  The badge is *awarded* on gym win regardless; its passive can land in a follow-up (like Boulder's
  LeadIncomingDamageReduction is wired in CombatController). Cascade/Hive/Normal effects = follow-up task.
- ScriptableHook assets: OnLeadChanged (may exist), OnSkillCardCycled (new), OnDamageCalculation (new).
- GuaranteedRareRelic: leave placeholder until Epic 12 relic pool, or assign a generic rare.
- Gym TYPE field effect: marker-only (gap #33). Boss AI intent sequences: Epic 8 Task 8.6 (deferred).

## Open questions surfaced by content-designer (defaults chosen; user may override)
1. Bug ace: Caterpie L14→Butterfree (dramatic/weak) vs **Metapod L15→Butterfree (chosen, safer)**.
2. Rare relic: placeholder vs generic now. (Default: placeholder until Epic 12.)
3. Field effect (gap #33) marker-only for VS — acceptable?
4. Boss AI custom intents deferred to Epic 8 Task 8.6 — acceptable?
