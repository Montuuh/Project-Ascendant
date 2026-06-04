<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Last updated from Notion: 2026-06-04T23:51:00.000Z -->

**Status:** 🟢 In Progress


**Last Updated:** 2026-05-24 (full style guide, combat screen spec, iconography, audio bible, accessibility)


**Cross-references:** Topic 1 (cheerful core + regional flavor pillar), Topic 3 (combat phases, Lead/bench), Topic 4 (intent display, status icons, type effectiveness UI), Topic 6 (Trauma UI), Topic 7 (Region palettes), Topic 9 (UI Toolkit architecture).


---


# §10.1 Visual Style


## §10.1.1 Art Style Statement


**Clean, modern 2D pixel art with high-saturation type palettes.** Reference baseline: HD-2D illustration sensibility (depth via lighting, not 3D), Gen 5/6-style sprite craftsmanship, deckbuilder UI minimalism (Slay the Spire / Monster Train clarity over decoration).


**Tonal alignment:**

- Base mode: **cheerful, warm, vibrant**. Pillar 5 alignment.
- Regional accent: each Region introduces its own palette and lighting, never breaking the cheerful base — even Volcanic Highlands is "intense and dramatic," not "grim."

## §10.1.2 Sprite Specifications


| Asset                              | Resolution           | Notes                              |
| ---------------------------------- | -------------------- | ---------------------------------- |
| Pokémon portrait (battle)          | 96×96 px             | Center-anchored; 1.5× scaled in UI |
| Pokémon portrait (map view, small) | 32×32 px             | Pixel-accurate downscale           |
| Enemy battle sprite                | 96×96 px             | Same as player Pokémon             |
| Move card art                      | 144×96 px            | Landscape; framed by card chrome   |
| Relic icon                         | 64×64 px             | Square; rarity-tinted border       |
| Held Item icon                     | 32×32 px             | Small; equipped-state overlay      |
| Consumable icon                    | 48×48 px             | In-hand: 64×96 (card-framed)       |
| Background (combat)                | 1920×1080 px logical | Parallax-layer-ready               |
| Background (map view)              | 1920×1080 px logical | Single-layer; pannable             |


## §10.1.3 Color Palette (Master)


### §10.1.3.1 Core UI Palette


| Token               | Hex     | Use                              |
| ------------------- | ------- | -------------------------------- |
| `--bg-primary`      | #1A1F2E | Combat backdrop fade, card back  |
| `--bg-secondary`    | #2A3142 | Panel backgrounds                |
| `--bg-tertiary`     | #3D4861 | Tooltip / overlay backgrounds    |
| `--text-primary`    | #FFFFFF | Headings, key data               |
| `--text-secondary`  | #C5CDE0 | Body text                        |
| `--text-muted`      | #8A95B0 | Disabled, watermark              |
| `--accent-action`   | #FFD451 | Playable card glow, AP icons     |
| `--accent-positive` | #5CE181 | Heals, buffs, positive change    |
| `--accent-negative` | #FF6B6B | Damage, debuffs, faints          |
| `--accent-warning`  | #FFB13C | Status conditions, Trauma badges |


### §10.1.3.2 Pokémon Type Palette (15 types — Gen I)


| Type     | Hex     | Use                |
| -------- | ------- | ------------------ |
| Normal   | #A8A878 | Beige, neutral     |
| Fire     | #F08030 | Saturated orange   |
| Water    | #6890F0 | Sky blue           |
| Electric | #F8D030 | Lemon yellow       |
| Grass    | #78C850 | Apple green        |
| Ice      | #98D8D8 | Pale cyan          |
| Fighting | #C03028 | Brick red          |
| Poison   | #A040A0 | Saturated purple   |
| Ground   | #E0C068 | Sandy yellow-brown |
| Flying   | #A890F0 | Lavender           |
| Psychic  | #F85888 | Pink magenta       |
| Bug      | #A8B820 | Olive              |
| Rock     | #B8A038 | Tan-brown          |
| Ghost    | #705898 | Muted purple       |
| Dragon   | #7038F8 | Deep violet        |


All type colors verified for WCAG AA contrast against `--bg-primary` and `--bg-secondary`.


---


# §10.2 Combat Screen Layout


## §10.2.1 Master Layout (1920×1080 reference)


```javascript
┌─────────────────────────────────────────────────────────────────┐
│  [Region Modifiers]    [Boons]    [Field Effect]   ⏸ Menu       │ ← 60px top bar
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│                            ENEMY ZONE                            │ ← Enemy battle area
│        [Enemy Sprite + Intent + HP Bar + Status Icons]           │   (top 40% height)
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│        [Bench L]    [LEAD POKÉMON]   [Bench R]                   │ ← Player Active Team
│         HP bar      HP bar (large)    HP bar                     │   (mid 30% height)
│         status      status            status                     │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  AP: ●●● │ Deck: 8 │ Discard: 3 │ Hand:                          │ ← Bottom bar
│                                                                  │
│   [Card1] [Card2] [Card3] [Card4] [Card5] │ [Cons1] [Cons2]      │ ← Hand
│   skill cards (5)                            consumables (2)     │
│                                                                  │
│                                                       [End Turn] │
└─────────────────────────────────────────────────────────────────┘
```


## §10.2.2 Zone Specifications


### §10.2.2.1 Top Bar (60px)


Persistent status row. Always visible. Hover any badge to see full text. Stacks left-to-right: Region Modifiers → Boons → Field Effects → Pause/Menu.


### §10.2.2.2 Enemy Zone (40% height, ~410px)

- Single enemy: centered, sprite 240×240 scaled.
- Multi-enemy: 1 Lead + 1–2 supports. Lead enemy in center; supports flank.
- Intent display: directly above each enemy. Format: `[icon] [magnitude] → [target label]`.
- HP bar: directly below sprite. Phase markers visible for boss-tier (§4.4.3).
- Status condition icons: row below HP bar.
- Bestiary tier badge: top-right corner of enemy frame (Familiar / Veteran / Master).

### §10.2.2.3 Player Active Team Zone (30% height, ~324px)

- 3 portraits: Bench-Left, LEAD (raised forward + larger), Bench-Right.
- Lead is visually distinguished: 1.25× scale, slight forward offset, gold-tinted frame.
- HP bar below each portrait. Trauma badge (§6.2.5) overlaid on portrait.
- Status condition icons row below HP bar.
- Stat stage icons (separate row): up/down arrows with stage numbers (`Atk +1`).
- Lead Aura indicator (§5.5.4): persistent buff icon under Lead portrait, tinted to Aura's type.

### §10.2.2.4 Bottom Bar / Hand Zone (~340px)

- AP pip display (left): 3 base pips, lit/dimmed based on remaining AP.
- Deck/Discard counts.
- Skill cards (5): fanned, card-game style.
- Consumable cards (2): visually distinct (rounded vs angular).
- "End Turn" button (right): prominent, color-shifts to gold when player has no playable affordable cards.

## §10.2.3 Card Anatomy


```javascript
┌───────────────────┐
│ [Type] [AP cost]  │ ← Header
│ ┌───────────────┐ │
│ │   Card Art    │ │ ← 144×96 art slot
│ │               │ │
│ └───────────────┘ │
│ Move Name         │ ← Title (large)
│ [Tags: R, SF...]  │ ← Tag row
│ Effect text...    │ ← Body
│                   │
│ Pwr 80 (× STAB)   │ ← Stats footer
└───────────────────┘
```

- AP cost: yellow pip stack, top-right.
- Type: 15-type color band, top-left.
- Range icon: ⚔ Melee / 🏹 Ranged.
- Modifier icon: ▲ Step-Forward / ▼ Step-Backward (only when present).
- Status rider: small condition icon at bottom-right of art slot.
- Unplayable state: 60% desaturated, NOT hidden (per `ui.md` rule).

## §10.2.4 Hover State (Damage Preview)


Hovering a card with a target selected reveals:


```javascript
┌─────────────────────────────────┐
│ Tackle on Charmander            │
│                                 │
│ Predicted damage: 48            │
│ Base 40 × STAB 1.5 × Type 1.0   │
│                                 │
│ ⚡ Crit chance: 25%             │
│ ☑ Will apply: (nothing)         │
└─────────────────────────────────┘
```


Always shows: final calculated damage, the breakdown, crit chance, status rider details, redundancy warnings (e.g., "Already Burned" if attempting to re-apply).


## §10.2.5 Intent Display Vocabulary


| Icon | Intent              | Display                                                      |
| ---- | ------------------- | ------------------------------------------------------------ |
| ⚔️   | Attack(N, slot)     | `⚔ N → Lead` or `⚔ N → Bench-L (Squirtle)`                   |
| ⚔🌐  | Cleave(N)           | `⚔ N → ALL SLOTS` (with sweep arrow visual)                  |
| 🎯   | Backstrike(N, slot) | `🎯 N → Bench-L (Squirtle)`                                  |
| ⬆    | Buff(stat)          | `⬆ Atk +1`                                                   |
| 🛡   | Stall               | `🛡 +1 stage Def`                                            |
| 💢   | Status(condition)   | `💢 BURN → Lead`                                             |
| ❓    | Unknown             | `❓` (interactable: tooltip says "Unrevealed — see Bestiary") |


---


# §10.3 Map View Layout


```javascript
┌────────────────────────────────────────────────────────────────┐
│ Region 1 — Verdant Route       💰 350₽   ⭐ 240 XP   🎒 Items │ ← Top bar
├───────────────────────┬────────────────────────────────────────┤
│                       │                                        │
│   [Active Team]       │              [Map Graph]               │
│   [Lead] [B][B]       │       Layer 0  ●                       │
│                       │       Layer 1   ● ● ●                  │
│   [Box (Reorderable)] │       Layer 2   ●─●─●                  │
│   ─────────────       │       ...                              │
│   [⚪ Box-1]          │       Layer 7   👑 ← Gym Layer         │
│   [⚪ Box-2]          │                                        │
│   [⚠ Box-3 Trauma]   │  Current location: Layer 2             │
│   ...                 │                                        │
├───────────────────────┴────────────────────────────────────────┤
│ [Inventory]  [Bestiary]  [Settings]                  [Save&Quit]│
└────────────────────────────────────────────────────────────────┘
```

- Active Team panel (left): drag-and-drop reordering; visual Lead distinction.
- Map graph (right): branching, with layer markers; current location highlighted; future nodes show their type icons.
- Bottom bar: utility access.

---


# §10.4 UI Iconography Reference


## §10.4.1 Node Type Icons (Map View)


| Node              | Icon              |
| ----------------- | ----------------- |
| Wild Pokémon Area | 🌿 (biome-tinted) |
| Trainer Battle    | 👤                |
| Elite Trainer     | 👤⭐               |
| Pokémon Center    | ❤️                |
| Shop              | 🛒                |
| Tutor / Daycare   | 📜                |
| Mystery Event     | ❓                 |
| Gym Leader        | 👑                |
| City              | 🏙️               |


## §10.4.2 Resource & State Icons

- AP: ● (yellow pip)
- Poké Dollar: ₽
- Trainer XP: ⭐
- Trainer Token: 🪙
- Trauma stack: ⚠
- Mastery tier: 🏆

## §10.4.3 Status Condition Icons


| Condition | Icon | Color tint       |
| --------- | ---- | ---------------- |
| Burn      | 🔥   | --type-fire      |
| Poison    | ☠️   | --type-poison    |
| Paralysis | ⚡    | --type-electric  |
| Sleep     | 💤   | desaturated blue |
| Freeze    | 🧊   | --type-ice       |
| Confusion | 💫   | yellow-magenta   |


---


# §10.5 Audio Design


## §10.5.1 Audio Direction Statement


**Layered orchestral-electronic hybrid**, regionally themed. Combat audio is high-density (multiple stems for setup/aggression/desperation). Map audio is ambient-forward.


**Core principles:**

- Every player action has audio feedback (card play, swap, end-turn).
- Status conditions have musical motifs (Burn = subtle crackle layer, etc.).
- Boss fights have unique tracks; standard combat reuses Region tracks.

## §10.5.2 Audio Stems (Combat)


| Stem                           | Trigger                       |
| ------------------------------ | ----------------------------- |
| `combat_base`                  | Combat start; always playing  |
| `combat_low_hp_layer`          | Active Team total HP < 30%    |
| `combat_setup_layer`           | First 2 turns of a boss fight |
| `combat_signature_phase_layer` | Boss Phase 2/3                |
| `combat_victory_sting`         | Combat win                    |
| `combat_defeat_sting`          | Run loss                      |


Stems crossfade with 250ms ramps.


## §10.5.3 Audio Cues — SFX Bible


| Action                   | SFX                                                  |
| ------------------------ | ---------------------------------------------------- |
| Play skill card          | Type-coded "whoosh" (Fire = crackle, Water = splash) |
| Play consumable          | Soft chime                                           |
| Manual swap              | Confident step + soft chord                          |
| Step-Forward effect      | Forward whoosh                                       |
| Step-Backward effect     | Reverse whoosh                                       |
| Faint                    | Soft "fall" + dim color                              |
| Status applied           | Type-coded sting                                     |
| Crit                     | Crystalline "ping" overlay                           |
| Super-effective hit      | Heavy impact with high-frequency layer               |
| Resisted hit             | Muted thud                                           |
| Card draw                | Paper-shuffle                                        |
| Hand refresh             | Cardstock fan                                        |
| End turn confirm         | Solid bell                                           |
| Boss evolution telegraph | Rising electronic tension                            |
| Boss evolution complete  | Orchestral hit + screen flash                        |


## §10.5.4 Regional Music Themes

- **Region 1 — Verdant Route:** Light flute + acoustic strings + birdsong ambient. ~110 BPM. Major key.
- **Region 2 — Coastal Cliffs:** Brass + crashing-wave foley + tense strings. ~115 BPM. Minor key.
- **Region 3 — Volcanic Highlands:** Percussion + brass + electronic sub-bass. ~125 BPM. Modal minor.
- **City interstitials:** Each City has a "rest hub" jingle — warm, looped, evocative of Pokémon Center theme tradition.
- **Victory Road:** Sparse, cinematic strings + sustained brass. Slow tempo (~85 BPM). Builds tension.
- **League:** Each Elite has a unique track; Champion has a 3-phase dynamic composition.

## §10.5.5 Audio Mix Targets

- Master bus: -14 LUFS integrated loudness (modern game standard).
- Music bus: -18 LUFS.
- SFX bus: -16 LUFS peak.
- All audio normalized at authoring time; runtime mixing via Unity AudioMixer snapshots.

---


# §10.6 Accessibility


## §10.6.1 Mandatory Launch Accessibility Features

1. **Colorblind modes:** Deuteranopia, Protanopia, Tritanopia palette swaps. Selectable at boot or via Settings. Type colors remap; type icons gain pattern overlays (stripes/dots) so they're distinguishable WITHOUT color.
2. **Text size:** UI text scales 80% / 100% / 125% / 150% via Settings. All UI elements designed at 150% to ensure no clipping at max scale.
3. **Reduced motion mode:** Disables card-fan animations, screen shakes, parallax, particle flourishes. Game-state animations (HP bar fill, faint animation) play in instant mode.
4. **Damage preview always-on option:** Toggle to keep damage preview visible without requiring hover.
5. **Skip animations option:** Combat resolution animations can be set to instant.
6. **Subtitles for audio cues:** Optional text feedback for SFX-only signals (e.g., "Crit!", "Super effective!").
7. **Key rebinding:** Every input action remappable. Two profiles (Profile A / B).
8. **Pause anywhere:** Pause is available between turns AND mid-Action-Phase.

## §10.6.2 Screen-Reader Support (Post-launch)


Architecture commitment: every interactive UI element has a `data-aria-label` analog (Unity UI Toolkit `accessibility` properties). Screen-reader plumbing deferred to post-launch.


## §10.6.3 Cognitive Accessibility

- Tutorial mode: longer telegraphs, slower pacing for first-run players.
- "Hint" overlay: optional, suggests strong card plays for that turn. Off by default; can be enabled in Settings without telemetry penalty.
- Bestiary "study mode": pre-combat preview of enemy intent pool against current Active Team's type matchups.

## §10.6.4 Photosensitivity

- All flashes/screen-shakes capped at frequencies safe under PEAT (Photosensitive Epilepsy Analysis Tool) guidelines.
- Reduced motion mode disables all flashes entirely.

---


# §10.7 UI Toolkit Architecture


## §10.7.1 UI Framework


**UI Toolkit (Unity 6)** is the canonical UI system. Legacy uGUI / Canvas is **forbidden for new screens**. Per `unity/VERSION.md`.


## §10.7.2 USS / UXML Conventions

- Per-screen `.uxml` files for layout.
- Shared `theme.uss` for tokens (color tokens defined in §10.1.3 as CSS variables).
- Per-component USS for component-specific styles.
- BEM-style class naming: `.combat-card`, `.combat-card--unplayable`, `.combat-card__type-band`.

## §10.7.3 Event-Driven Updates


UI components subscribe to ScriptableObject event channels (§9.4.1.1) for state updates. **No polling in UI Update loops.** Per `ui.md`: UI never owns game state.


## §10.7.4 Damage Preview Implementation Note


The hover damage preview is computed by calling `DamageCalculator.Preview(card, target)` which returns a `DamageBreakdown` struct. UI binds to this struct. The calculator is pure (no side effects); calling it from hover events is cheap.


---


# §10.8 Mobile Portability (Acknowledged, Not Launch)


UI Toolkit's layout system uses logical units (em / %), making rescaling straightforward.


**Mobile-portability commitments WITHOUT dedicated effort:**

- All hit targets ≥ 44×44 dp equivalent (touch-friendly even on desktop).
- No mouse-hover-only interactions (every hover has a tap-equivalent for mobile parity).
- Persistent tooltip toggle (so a tap can lock-show a tooltip).
- Layout reflows tested at 4:3, 16:9, 16:10, 18:9, 21:9 aspect ratios.

Full mobile port is a roadmap item, not a launch commitment.


---


# §10.9 Animation Style


## §10.9.1 Pokémon Animation

- **Idle:** 2-frame breath loop.
- **Attack:** 4-frame lunge or projectile spawn.
- **Damaged:** 1-frame flash + 2-frame recoil.
- **Faint:** 3-frame fade.
- **Evolution:** Special 8-frame animation triggered in Map View on evolution.

## §10.9.2 Card Animation

- Draw: slide-in from deck position, 200ms.
- Play: lift, glow, fade-out toward target, 350ms total.
- Discard: subtle fade to discard pile, 150ms.
- Hand refresh: cascade fan-in.

## §10.9.3 Camera & Screen Effects

- Combat: locked camera; no movement. Subtle 1px screen-shake on heavy hits.
- Map: smooth pan to selected node.
- Boss intro: 1.5s camera zoom + dramatic chord.
- Reduced motion mode: eliminates all of the above.

---


# §10.10 Localization Architecture


## §10.10.1 String Authoring

- **Every user-facing string** lives in a localization table — no hardcoded display text. Per `ui.md` rule.
- Localization keys use namespace dot notation: `combat.card.tackle.name`, `ui.button.end_turn`.
- Unity Localization package backs the implementation.

## §10.10.2 Launch Locales


| Locale           | Launch  | Notes                                                         |
| ---------------- | ------- | ------------------------------------------------------------- |
| English (en-US)  | ✅       | Primary                                                       |
| Spanish (es-ES)  | Stretch | Half of original Drive design is in Spanish — easy to surface |
| French (fr-FR)   | Roadmap | —                                                             |
| Japanese (ja-JP) | Roadmap | Requires CJK font integration                                 |


---


# §10.11 Vertical Slice Carve-Out


| System                              | In VS                                           | Out of VS                            |
| ----------------------------------- | ----------------------------------------------- | ------------------------------------ |
| Combat screen layout                | ✅ Full                                          | —                                    |
| Card anatomy + hover damage preview | ✅ Full                                          | —                                    |
| Map view layout                     | ✅ Region 1 scope                                | City + Victory Road + League screens |
| Iconography                         | ✅ Full type + status + node icons               | Bestiary mastery visual polish       |
| Audio bible                         | ✅ Combat stems (R1) + 1 boss track              | R2/R3 themes, full SFX bible polish  |
| Accessibility                       | ✅ Colorblind modes + reduced motion + rebinding | Screen reader, hint overlay          |
| UI Toolkit theme system             | ✅ Full                                          | —                                    |
| Animation suite                     | ✅ All combat anims                              | Evolution animation polish           |
| Localization architecture           | ✅ Hookup                                        | Only en-US locale shipped            |

