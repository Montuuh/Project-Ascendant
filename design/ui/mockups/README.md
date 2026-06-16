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
