# Meta & System Screens — Trainer Hub, Achievements, Settings, Pause

> Warm-light theme (§1.2). Template: **purpose · zones · components · bindings · interactions ·
> states · accessibility.** These sit in the **FrontEnd** scene (Hub/Achievements/Settings) or as an
> in-run overlay (Pause). All read the persisted `MetaProgressionSO` (view-only) — no run state.

---

## 5.1 Trainer Hub  (base panel · FrontEnd · §6.4)

**Purpose.** Between-run home: review your trainer identity and the meta you've earned. A clean **2D
warm kiosk menu**, not a 3D scene (§6.4). Replaces the temp `HubPanelUI`. VS ships two live kiosks —
**Trainer Card** and **PC Terminal** — plus greyed post-launch kiosks (Daycare, Mystery Door).
**Zones.** title · two kiosk cards side-by-side · post-launch greyed kiosks row · `Back`. **Components.**
`kiosk card`, `Trainer-Level bar` (segmented, milestone ticks CL-019), `metric tiles`, `button`.
**Bindings.** `MetaProgressionSO` (Trainer Level/XP/Tokens, runs won/lost), `MetaProgressionConfigSO`
(`CumulativeXPForLevel`), `AchievementService` (count). **Interactions.** Trainer Card = view stats;
PC Terminal → Achievements (5.2) and Pokédex (4.3); greyed kiosks show "Post-launch". Back → Main
Menu. **States.** level-up-available glow on the track · Token balance highlight · empty history
("Epic 13" → real run-history list, now in scope). **Accessibility.** kiosks are focusable; track has
a numeric readout; greyed kiosks are visible+labeled, not hidden. **Loc:** `ui.hub.*`.

> The temp Hub also hosts the **Token-spend / Mastery-relic lane** (CL-019 Tier-3) and **meta-starter
> unlocks** — add these as a third "Trainer Shop" kiosk (Tokens → permanent unlocks). `↻ flag` confirm
> the Token-spend catalog with `game-designer`/`systems-designer` (CL-019 §6.6).

---

## 5.2 Achievements  (full overlay · §6.7 / CL-020)

**Purpose.** Browse the 50-entry medal-tier catalog (CL-020). **Zones.** header (count + total) ·
category filter chips (8 categories) · medal-tier filter (Bronze/Silver/Gold/Platinum) · achievement
`list rows` (medal icon, name or `??? (hidden)` for hidden+incomplete §6.7.3, description, status: ✓
+reward / `prog/target` / locked) · summary of Tokens earned. **Components.** `filter chips`, `list
row`, `medal badge` ×4 tiers, `progress text`. **Bindings.** `AchievementCatalog`, `AchievementService`
(`IsCompleted`, `GetProgress`), `MetaProgressionSO`. **Interactions.** filter; rows are read-only;
completed rows celebrate (warm green). **States.** hidden+incomplete (`???`) · in-progress (counter) ·
complete (medal + reward). **Accessibility.** medal tier shown by icon+label not color-only; list
focusable; hidden-reveal rule in text. **Loc:** `achievement.*`.

---

## 5.3 Settings  (full overlay · reachable from Main Menu and Pause · §10.6)

**Purpose.** All player options, **accessibility-first** (§10.6 is mandatory-launch). Tabbed.
**Zones / tabs:**
- **Display & Accessibility** — colorblind mode (Deuter/Protan/Tritan, with type-pattern overlays
  §10.6.1.1) · text size 80/100/125/150% (live preview) · reduced-motion · damage-preview always-on
  · skip-combat-animations · SFX subtitles · photosensitivity-safe confirmation.
- **Audio** — master / music / SFX sliders (mapped to the AudioMixer snapshots §10.5.5), mute toggles.
- **Controls** — rebinding for every action, two profiles A/B (§10.6.1.7); device glyphs swap by
  active input.
**Components.** `tabs`, `toggle (pill)`, `slider`, `segmented control`, `rebind row` (action + current
binding + "press a key"), `Apply/Reset`. **Bindings.** `SettingsService` → `settings.json` (§9.8.7.6).
**Interactions.** change → live apply where safe → persist on Apply. **States.** unsaved-changes
(Apply highlights) · rebind-listening · conflict (warn). **Accessibility.** this *is* the accessibility
surface; every control keyboard/pad reachable; text-size preview updates the whole UI live. **Loc:**
`ui.settings.*`.

> **Implementation note (#47):** the design **assumes Settings exists and fully persists** (load on
> open, apply at boot). The backing load path isn't built yet — `SaveSettings` exists but
> `LoadSettings`/boot-restore is pending (§9.8.7.6, BACKLOG #47). This is a normal Epic-13 build item,
> **not a design blocker**: design the screen as if persistence works; lead-programmer closes #47 as
> part of wiring the screen.

---

## 5.4 Pause  (dim modal overlay · in-run, §10.6.1.8)

**Purpose.** Pause anywhere — between turns AND mid-Action-Phase (§10.6.1.8). **Zones.** centered
panel over `--scrim`: Resume · Settings · Save & Quit (→ Main Menu) · (optional) Abandon Run
(`--danger`, confirm). **Components.** `modal panel`, `button` stack, `confirm modal`. **Bindings.**
`PauseChannel`, `SaveSystem` (autosave on Save & Quit, §9.8). **Interactions.** Esc/Start toggles;
Resume returns to exact combat/map state; Save & Quit autosaves then routes out. **States.** mid-
combat (Save & Quit warns it autosaves the in-progress fight — CL-022 resume) vs map. **Accessibility.**
opening Pause does not lose focus context (restored on Resume); fully keyboard/pad. **Loc:**
`ui.pause.*`.

---

## Shared meta rules

- Hub/Achievements/Settings are **view-or-options only** — none mutate run state; Hub reads
  `MetaProgressionSO`, Settings writes `settings.json` via `SettingsService`.
- Settings is one screen reached from two places (Main Menu, Pause); it's the same panel, pushed at
  different layers by the UIRouter.
- Pause and all confirm modals share the `--scrim` warm dim + centered `--radius-xl` panel language.
