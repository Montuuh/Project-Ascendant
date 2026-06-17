# UI Coverage Matrix — every system: Designed + Mocked?

> Audit of whether **every** game system has its UI **designed** (a written spec in `design/ui/`) AND
> **mocked** (a visual mockup widget / saved HTML). Goal: 100% of both. ✅ = done · 🟡 = partial · ⬜ = gap.
> Updated 2026-06-14.

## Legend
- **Designed** = spec section exists (file §ref).
- **Mocked** = a visual mockup was produced (widget shown and/or `mockups/*.html`).

---

## Front-end & meta

| System | Designed | Mocked | Notes |
|--------|:---:|:---:|-------|
| Boot / Splash | ✅ 03.1 | ⬜ | trivial; will add a simple lockup |
| Main Menu | ✅ 03.2 | ✅ | `main_menu_screen` |
| Trainer Hub (Trainer Card + PC Terminal + **Trainer Shop / Token spend** + **Battle-Pass track**) | ✅ 05.1 | ✅ | `trainer_hub_screen` |
| Achievements list (medal tiers, hidden) | ✅ 05.2 | ⬜ | **gap** |
| Settings (Display/Accessibility · Audio · Controls) | ✅ 05.3 | ⬜ | **gap** |

## New-Run configuration

| System | Designed | Mocked | Notes |
|--------|:---:|:---:|-------|
| Difficulty Select | ✅ 03.3a | ✅ | `newrun-1-difficulty.html` (modifier + reward per tier) |
| Starter Select | ✅ 03.3b | ✅ | `newrun-2-starter.html` (real portraits + type icons) |
| Starting Relic Select | ✅ 03.3c | ✅ | `newrun-3-relic.html` (rarity grid, scrollable) |
| Region Modifier Select | ✅ 03.3d | ✅ | `newrun-4-region.html` (3-offer + run-lock modal) |

## In-run core loop

| System | Designed | Mocked | Notes |
|--------|:---:|:---:|-------|
| Route Map View | ✅ 02.2 | ✅ | `map_screen` |
| Node-type icons (×9) | ✅ 06/icons | ✅ | `icon_system_sheet` |
| Node Preview popover | ✅ 03.4 | ⬜ | **gap** |
| **Combat Screen** (squad formation) | ✅ 02.1 | ✅ | `combat-screen-final.html` (squad layout, user-iterated) |
| — Move card (all states) | ✅ 09 | ✅ | `move_card_final_iter2` + file |
| — Consumable card (all states) | ✅ 04.2 | ✅ | `consumable_card_final` + file |
| — Player team HUD / squad tiles | ✅ 02.1 | ✅ | in `combat-screen-final.html` |
| — Enemy HUD + intent + tier badge | ✅ 02.1 | ✅ | in `combat-screen-final.html` |
| — Bag / Team / Dex toolbar buttons | ✅ 01/icons | ✅ | top-bar cluster in `combat-screen-final.html` |
| — Catch gauge (detached pill, wild-only) | ✅ 02.1/CL-014 | ✅ | in `combat-screen-final.html` |
| — Swap interaction + cost chip | ✅ 02.1 | ✅ | swap chip on bench tiles, `combat-screen-final.html` |
| — Damage preview (drag-over-target) | ✅ 09 | ✅ | beside-target preview in `combat-screen-final.html` |
| Post-Combat Reward (win XP) | ✅ 03.5 | ✅ | `post_combat_reward` |
| Evolution | ✅ 03.6 | ✅ | `evolution_screen` |
| Legendary Pick (1-of-3) | ✅ 03.7 | ⬜ | **gap** |

## Node service screens

| System | Designed | Mocked | Notes |
|--------|:---:|:---:|-------|
| Pokémon Center | ✅ 04.6 | ⬜ | **gap** |
| Shop / Mart | ✅ 04.5 | ⬜ | **gap** |
| Dojo / Tutor | ✅ 04.7 | ⬜ | **gap** |
| City / Choice Plaza (CL-015) | ✅ 00/07 | ⬜ | **gap** |
| Mystery Event | ✅ 00 | ⬜ | **gap** (event card layout) |

## Pokémon & loadout management

| System | Designed | Mocked | Notes |
|--------|:---:|:---:|-------|
| Manage Active Team / Loadout | ✅ 04.1 | ✅ | `team-loadout.html` (Active 3 + Box, drag-swap, detail, Confirm) |
| Inventory (Relics · Consumables · Held) | ✅ 04.2 | ✅ | `inventory_screen` |
| Manage moves + Held items (TM / Move Manager) | ✅ 04.4 | ✅ | `move-manager.html` (kit 4/4 + Mastery, teach/replace) |
| Pokédex | ✅ 04.3 | ✅ | `pokedex_screen` |

## Run end

| System | Designed | Mocked | Notes |
|--------|:---:|:---:|-------|
| Victory / Run-cleared summary | ✅ 03.8 | ⬜ | **gap** |
| Defeat / Run-over summary | ✅ 03.9 | ⬜ | **gap** |

## Cross-cutting overlays & foundations

| System | Designed | Mocked | Notes |
|--------|:---:|:---:|-------|
| Pause menu | ✅ 05.4 | ⬜ | **gap** |
| Confirmation modal | ✅ 01 | ✅ | in `ui_components_sheet` |
| Toast / notification | ✅ 01 | ✅ | in components |
| Tooltip | ✅ 01 | ✅ | in components |
| Loading / scene transition | ✅ 00 | ⬜ | trivial curtain |
| Icon system | ✅ 06 | ✅ | `icon_system_sheet` |
| Component library | ✅ 01 | ✅ | `ui_components_sheet` |

---

## Gaps to close (mocking backlog) — ✅ ALL CLOSED (2026-06-14)

| Batch | Screens | Mockup |
|-------|---------|--------|
| 1 | Victory · Defeat | `victory_defeat_screens` |
| 1 | Achievements | `achievements_screen` |
| 1 | Settings · Pause | `settings_and_pause` |
| 2 | Difficulty · Starter · Relic · Region-Mod · Legendary Pick | `new_run_select_screens` |
| 3 | Center · Shop · Dojo · City/Choice Plaza · Mystery · Node Preview | `node_service_screens` |
| 4 | Team/Loadout · Move/Held manager | `team_loadout_move_manager` |
| 4 | Catch gauge · Swap cost ladder · Damage-preview · Bag/Team buttons · Loading curtain | `combat_sub_elements` |

**Every system in the inventory is now ✅ Designed + ✅ Mocked.** (Boot/Splash = the loading-curtain
treatment in `combat_sub_elements`; trivial, no dedicated screen needed.)
