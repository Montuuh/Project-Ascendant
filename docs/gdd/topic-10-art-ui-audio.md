<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Source: https://www.notion.so/3610450715b4815192fae42ed745b3d0 -->
<!-- Exported: 2026-05-19T23:10:27.195Z -->
<!-- To update: run `node docs/scripts/export-gdd.js` and commit -->

**Status:** 🟡 Pending


**Last Updated:** 2026-05-15 (migrated from Drive partial draft)


**Cross-references:** Topic 1 (cheerful core, regional flavor), Topic 3 (Lead/bench combat layout), Topic 4 (intent display, type/AP iconography), Topic 9 (data-driven UI scaling).


---


# Scope


Visual style guide, combat screen layout final spec, UI iconography (type/AP/intents/status), audio direction (regional palettes), accessibility (colorblind palettes, screen reader hooks).


---


# Drive Original Draft (to be expanded)


## Visuals


### Art Style


Clean, modern 2D pixel art.


### Combat Screen Layout


The battle screen should divide the screen space efficiently: Enemies on the top right, the player's lead Pokémon in the middle of the 3 chosen Pokémon of the player's, ahead of the other 2. Party status in a sidebar, and the "Hand" of moves clearly laid out across the bottom third of the screen.


## UX Priorities


### Readability


Readability is paramount. Type effectiveness, ActionPoint costs, and enemy intents must be instantly recognisable via _(draft cuts off in Drive original — to be completed during deep-dive)_.


---


# Required Deep-Dive Areas

1. **Type iconography:** 15 distinct, instantly-recognisable type icons. Color palette must work for colorblind players.
2. **Intent display vocabulary:** ⚔️, 🎯, ⬆️, 💢, ❓, etc. — finalize icon set.
3. **Damage preview UI:** how hover state shows calculated damage, type effectiveness, STAB, crit chance.
4. **Region aesthetic palettes:** Verdant Route, Coastal Cliffs, Volcanic Highlands — music, color, biome, enemy visual themes.
5. **Audio direction:** combat-loop audio, intent-reveal stings, swap sound, victory/defeat motifs.
6. **Accessibility:** colorblind modes, scaling, key-rebinding, optional reduced-motion mode.
7. **Mobile portability:** UI scaling, touch-input affordances (acknowledged future-state, not launch).
