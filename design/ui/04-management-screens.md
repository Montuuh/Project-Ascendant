# Management Screens — Team, Inventory, Pokédex, Moves, Node Services

> Warm-light theme (§1.2), component library (§1.4). Template: **purpose · zones · components ·
> bindings · interactions · states · accessibility.** Team/Inventory/Pokédex open as **overlays**
> from the Map bottom bar (Map-only for Team, per OD-4); Shop/Center/Dojo are **node screens**.

---

## 4.1 Team / Loadout  (full overlay · Map-only, §2.3)

**Purpose.** Compose the Active Team of 3 from the Box and reorder it (the Lead is the front slot).
This is the central tension screen (a Box member contributes nothing to the deck — §2.3.132).
**Zones.** Active Team strip (3 slots, Lead front + crowned) · Box grid (all owned Pokémon) ·
detail panel (selected Pokémon: types, level/XP, stats, current moves, ability, Held Item, Trauma) ·
**Confirm** loadout bar. **Components.** `Pokémon card tile`, `drag-and-drop`, `stat radial`, `move
chip`, `held-item chip`, `Trauma badge`, `confirm button`. **Bindings.** `BoxChannel`,
`ActiveTeamChannel`, `PokemonInstance`, `TraumaChannel`. **Interactions.** drag Box↔Active to set the
3 + order; pick Lead; **Confirm** commits (Pillar 2). Unreachable once a node is entered (loadout
locked). **States.** dirty (Confirm highlights) · invalid (<1 healthy → warn) · Trauma'd member
(fieldable, badge) · full Box scroll. **Accessibility.** keyboard reorder (`[ ]`), each tile
announces name/level/HP/Trauma; stat radial has text table. **Loc:** `ui.team.*`, `species.*`.

---

## 4.2 Inventory  (full overlay · tabbed)

**Purpose.** Review/manage everything the run is carrying. **Zones.** segmented tabs **Relics ·
Consumables · Held Items** · content grid · detail panel. **Components.** `tabs`, `relic card`
(rarity border), `consumable card`, `held-item row`, detail panel. **Bindings.** `RunInventoryChannel`,
`RelicSO` / `ConsumableSO` / `HeldItemSO`, `RunStateSO` (Legendary count badge). **Interactions.**
*Relics* read-only (passive, show source + effect; Legendary flagged) · *Consumables* show count, used
in combat as hand cards (here read-only in VS, or "discard" if design allows) · *Held Items*
**equip/unequip onto a Box Pokémon** (drag onto a team tile or pick from a list). **States.** empty
tab (empty-state line) · equipped vs unequipped held items · relic cap indicators. **Accessibility.**
tabs are arrow-navigable, rarity by border+label, equip has keyboard path. **Loc:** `item.*`.

> `↻ flag` Whether consumables are discardable from Inventory and whether Held-Item swapping is
> mid-run unrestricted → confirm with `game-designer` against §8 before ratification.

---

## 4.3 Pokédex  (full overlay · reachable from Map, Hub, Main Menu)

**Purpose.** The persistent knowledge artifact (CL-001 rename Bestiary→Pokédex; §6.9). Drives the
"study the enemy" loop (Pillar 1): tiers reveal intents. **Zones.** filter/search bar (type, tier,
seen/caught) · species list (`list rows`: portrait, name, types, tier badge Familiar/Veteran/Master) ·
detail panel (sprite, types, base stats, **known intent pool by tier**, evolution line, catch/seen
counts, flavor). **Components.** `tabs/filter chips`, `list row`, `tier badge`, `stat radial`, `intent
chip` list. **Bindings.** `PokedexSO` / `bestiary.dat` (CL-022), `PokemonSpeciesSO`, `IntentPoolSO`,
tier thresholds (§6.9 / CL-011). **Interactions.** filter/search; select → detail; tier governs how
much intent detail is shown (locked entries show `???` + "battle/​catch to reveal"). **States.**
unseen (silhouette) · seen-not-caught · caught · tier progression bar per species. **Accessibility.**
list is a focusable column; `???` reveal rule explained in text; stat radial text-equivalent. **Loc:**
`species.*`, `ui.pokedex.*`.

---

## 4.4 TM / Move Manager  (full overlay · from Map or Dojo)

**Purpose.** Teach TMs and manage a Pokémon's move list within the move-kit construction rules
(§5.3.6) and CL-006 level-gated learnset. Merges temp `TMPanelUI` + `MoveManagerUI`. **Zones.** target
Pokémon selector (Box) · its current moves (up to the kit cap) · available pool (TMs owned + level-up
moves learnable now) · move detail (type, power, AP, tags, riders). **Components.** `Pokémon
selector`, `move chip`/`move card`, `TM card`, swap/replace control. **Bindings.** `MoveManagerService`,
`LearnsetSO`, `TMInventoryChannel`, kit-rules from §5.3.6. **Interactions.** teach a TM/level-up move
→ if at cap, choose a move to replace (clear before/after diff); respects archetype/kit constraints
(CL-007). **States.** at-cap (must replace) · illegal teach (blocked + reason) · no-target. 
**Accessibility.** before/after diff is text; replace is keyboard-drivable. **Loc:** `move.*`.

> `↻ flag` Exact move-kit cap and which moves are legal to replace come from §5.3.4/§5.3.6 — bind to
> the data, don't hardcode; `content-designer` confirms edge rules.

---

## 4.5 Shop  (node screen · §7 Shop)

**Purpose.** Spend ₽ on relics/consumables/Held Items/TMs; the CL-015 City "Black Market" variant
sells Legendaries. **Zones.** header (₽ balance) · stock grid (`cards` with price tags) · detail/buy
panel · optional "sell/services" area (if §7 allows) · Leave. **Components.** `item card` (rarity
border), `price chip`, `buy button`, `confirm` for big spends. **Bindings.** `ShopStockChannel`
(seeded), `RunEconomyChannel`, item SOs. **Interactions.** select → buy (deduct ₽, toast) → stock
greys out; Leave → Map. **States.** can't-afford (price red, buy disabled-visible) · sold-out (greyed)
· Black-Market Legendary (gold treatment + max-2 rule, CL-021). **Accessibility.** prices are text;
buy has keyboard path; affordability not color-only (add "can't afford" label). **Loc:** `ui.shop.*`.

---

## 4.6 Pokémon Center  (node screen · §7 Center)

**Purpose.** Heal/recovery hub — the warmest screen, sets the Pillar-5 tone. Heals HP, may reduce
Trauma per §6.2 / §7 rules. **Zones.** warm Center interior · team row with HP/Trauma before→after ·
service buttons (Heal, Trauma care if applicable) · Leave. **Components.** `Pokémon row`, `HP bar`,
`Trauma badge`, `service button`, `confirm`. **Bindings.** `HealService`, `ActiveTeamChannel`/`BoxChannel`,
`TraumaChannel`, costs from `EconomyConfigSO`. **Interactions.** choose service → animated heal (bars
rise) → Leave. **States.** already-full (service disabled) · cost-gated (if any) · Trauma-care
availability (per design). **Accessibility.** heal bars instant in reduced-motion; chime has subtitle
option (§10.6.1.6). **Loc:** `ui.center.*`.

> `↻ flag` Whether the Center reduces Trauma (and at what cost) is a §6.2/§7 design point — confirm
> with `game-designer`; UI supports both (show the service only if enabled).

---

## 4.7 Dojo / Tutor  (node screen · CL-009)

**Purpose.** Paid move/ability tutoring (CL-009 standalone Dojo). **Zones.** tutor framing · target
Pokémon · teachable moves/abilities with ₽ costs · before/after · Leave. **Components.** reuses the
**TM/Move Manager** body (4.4) plus a price layer + an ability-teach row (CL-008 earned-learner).
**Bindings.** `DojoServiceChannel`, `LearnsetSO`, `AbilityCatalog`, `RunEconomyChannel`. **Interactions.**
pick service → pay → teach (kit rules apply) → Leave. **States.** can't-afford · at-cap (replace) ·
ability already known. **Accessibility.** same as 4.4 + price text. **Loc:** `ui.dojo.*`.

---

## Shared management rules

- Team/Inventory/Pokédex share one **overlay chrome** (title bar + Close ✕, Esc closes, focus
  trapped) pushed by the UIRouter over Map.
- Shop/Center/Dojo share one **node-screen chrome** (header + content + Leave→Map) so node services
  feel consistent.
- Every "buy/teach/equip" mutation goes through a service channel and emits a `toast` confirmation;
  the UI never mutates `RunStateSO` directly (`ui.md`).
