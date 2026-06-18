# UI Mockups (Q23)

Self-contained HTML mockups for the UI design pass. **Open any `.html` in a browser** to view full
size; screenshot for the GDD (Notion embeds PNGs — the GDD is markdown/Mermaid, so we embed exported
images, not live HTML) or keep as living design references.

Built from the warm/rounded design system (`../01-design-system.md`) and the locked card spec
(`../09-component-move-card.md`). **Art slots are placeholders** — real sprites come from the asset
pipeline (`../08-asset-generation-tracker.md`). Fonts: Baloo 2 (display) + Nunito (body), loaded from
Google Fonts; icons from Tabler (stand-ins for the final SVG icon set).

| File | Shows | Status |
|------|-------|--------|
| `combat-screen-final.html` | **Full combat screen** (§2.1): squad formation (Bench 1 top-left + Bench 2 bottom-left, Lead leading right, no overlap), enlarged single enemy + detached catch-gauge pill, intent chip (no arrows), hand tray + small damage preview. User-iterated, 2026-06-17. | 🔒 canonical (spec `02 §2.1`) |
| `newrun-1-difficulty.html` | **New-Run 1/4 — Difficulty Select** (§3.3a): stepper chrome + warm-light front-end theme; tier cards show Modifier + Trainer-XP **Reward**; locked tiers. | 🔒 canonical (spec `03 §3.3a`) |
| `newrun-2-starter.html` | **New-Run 2/4 — Starter Select** (§3.3b): real portraits + real type icons (type top-left on tiles; type chip off-photo in detail); stats, 2 starting moves, evo line. | 🔒 canonical (spec `03 §3.3b`) |
| `newrun-3-relic.html` | **New-Run 3/4 — Starting Relic Select** (§3.3c): rarity-tinted grid (scrollable), detail effect/source. | 🔒 canonical (spec `03 §3.3c`) |
| `newrun-4-region.html` | **New-Run 4/4 — Region Modifier Select** (§3.3d): curated 3-offer + the run-lock confirm modal. | 🔒 canonical (spec `03 §3.3d`) |
| `team-loadout.html` | **Team & Loadout** overlay (§4.1): Active 3 (Lead crowned) + scrollable Box (Trauma/fainted), detail panel, Confirm. Real portraits. | 🔒 canonical (spec `04 §4.1`) |
| `move-manager.html` | **TM & Move Manager** overlay (§4.4): target selector, active kit 4/4 + Mastery, teachable pool, at-cap replace preview. | 🔒 canonical (spec `04 §4.4`) |
| `settings-basic.html` | **Settings — bare basics** (§5.3↻): Audio (Master/Music/SFX) + Fullscreen + Language. Accessibility deferred post-VS (§10.6 override, user 2026-06-17). | 🔒 canonical (spec `05 §5.3`) |
| `achievements.html` | **Achievements** (§5.2): medal-tier rows — complete / in-progress / hidden / locked; count + Tokens; filters. | 🔒 canonical (spec `05 §5.2`) |
| `boot-splash.html` | **Boot / Splash** (§3.1): logo lockup + fan disclaimer + loading bar. | 🔒 canonical (spec `03 §3.1`) |
| `victory-summary.html` | **Victory / Run-Cleared** (§3.8): banner, run-stat tiles, Trainer-XP + Tokens + level-up unlock, unlocked achievements, Continue. | 🔒 canonical (spec `03 §3.8`) |
| `defeat-summary.html` | **Defeat / Run-Over** (§3.9): warm-not-punishing banner, how-far tiles, rewards still earned, encouraging line, Continue. | 🔒 canonical (spec `03 §3.9`) |
| `node-preview.html` | **Node Preview popover** (§3.4): anchored to a map node; Wild example (encounters + rarity + Pokédex tier + reward), Enter/Cancel. | 🔒 canonical (spec `03 §3.4`) |
| `legendary-pick.html` | **Legendary Pick** (§3.7 / CL-021): 1-of-3 Legendary relics, gold treatment, max-2-per-run counter, skip option. | 🔒 canonical (spec `03 §3.7`) |
| `pokemon-center.html` | **Pokémon Center** (§4.6): warm pink interior, heal before&rarr;after bars (current + healed segments), Full Heal + Trauma Care, Leave. | 🔒 canonical (spec `04 §4.6`) |
| `shop.html` | **Shop / Poké Mart** (§4.5): scrollable compact stock (relic/item/TM/Held), class + price chips, can't-afford / sold-out, buy panel + spend preview. | 🔒 canonical (spec `04 §4.5`) |
| `dojo-tutor.html` | **Dojo / Tutor** (§4.7 · CL-009): 4 move slots (filled + empty) + locked Mastery 5th, pick-slot teach, scrollable priced services incl. CL-008 ability. | 🔒 canonical (spec `04 §4.7`) |
| `city-plaza.html` | **City / Choice Plaza** (CL-015): aerial Pokémon town with clickable buildings (Center/Mart/Dojo/Black Market) — StS-style stop, pick one. | 🔒 canonical (spec `04` / CL-015) |
| `mystery-event.html` | **Mystery Event** (? node): scene art + flavor + 2–3 choices with telegraphed-outcome tags (gain/cost/risk). | 🔒 canonical (event template) |
| `move-card-final.html` | **Final move card**, one per status: playable · not-enough-AP · out-of-melee. Generic `Pokémon art` placeholder. | 🔒 canonical (spec `09`) |
| `consumable-card-final.html` | **Final consumable card**: playable · not-enough-AP. Shares the move-card grammar. | 🔒 canonical |
| `move-card-full.html` | A fully-loaded card + the complete icon legend (category/range/modifier/status). | legend ref |
| `combat-hand.html` | The combat-screen hand in context (move + consumable, states). | superseded by `combat-screen-final.html` (kept for history) |
| `consumable-card.html` | Expanded consumable + its two drag targets (teammate / field). | drag-target ref |
| `move-card.html` | Modifier + rider placement reference. | ⚠ pre-final layout |

> **Canonical layout** is `move-card-final.html` + `consumable-card-final.html` + spec `09`. The
> `⚠ pre-final` files predate the corner-overhang badges / AP-left-power-right footer and are kept only
> for history; they'll be regenerated during the combat-screen assembly pass.

> These are **design references**, not the implementation. Epic 13 builds them in UI Toolkit
> (UXML/USS) per the specs.
