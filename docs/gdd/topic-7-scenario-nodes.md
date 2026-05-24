<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Source: https://www.notion.so/3610450715b48146b3a0fe94ca2bd05c -->
<!-- Exported: 2026-05-19T23:10:24.063Z -->
<!-- To update: run `node docs/scripts/export-gdd.js` and commit -->

**Status:** 🟡 Pending


**Last Updated:** 2026-05-15 (scaffolded from BACKLOG)


**Cross-references:** Topic 2 (§2.1.2 node categories), Topic 4 (§4.5 Victory Road nodes).


---


# Scope


Full per-node-type design — encounter generation rules, reward tables, Wild Pokémon Area sub-biomes, Mystery Event design, Trainer Battle archetypes, City Shop curation algorithm, branching map seeding logic, Region biome aesthetic specs.


---


# Foundational Notes

- Foundational scaffolding exists in §2.1.2 (node categories) and §4.5 (Victory Road nodes).
- Wild Pokémon Area catching mechanics are completely undefined — major gap.
- Map View biome variants (cave/sea/river/meadow/power plant/abandoned tower/sky) listed as bullets in original draft.

---


# Scaffolding Bullets (from Drive original, to be developed)


## Nodes

- **Trainer Battles:** minibosses? May offer unique rewards.
- **Wild Pokémon encounters:** for catching Pokémon. Variants: caves, seas, rivers, meadows, power plants, abandoned towers, sky. The player should know which Pokémon can appear in each zone. Catching mechanic TBD.
- **Pokémon Center & Pokémon Shop.**
- **Legendary Pokémon events.**
- **Unique Pokémon trainer events.**
- **Gym:** Boss, end of stage.

---


# Open Questions (to develop during deep-dive)

1. Catching mechanic: deterministic vs probabilistic? Aligned with Telegraphed-Tactics pillar?
2. Wild biome → species pool mapping: full assignment table needed.
3. Mystery Event design: how many launch variants? Risk/reward calibration?
4. City Shop curation: weighting algorithm based on team composition?
5. Trainer archetypes for non-boss combat nodes.
