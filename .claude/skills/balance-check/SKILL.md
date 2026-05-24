---
name: balance-check
description: >
  Run a balance check on any Project Ascendant mechanic, card kit, relic effect,
  or system. Use when proposing new mechanics, tuning numbers, evaluating a relic,
  checking a Pokémon kit, or any time you want to know "is this broken?". Runs the
  4-step simulation protocol: outline mechanics, simulate a run, evaluate for exploits
  and pillar violations, document findings. Always checks against the 5 design pillars.
---

# Balance Check — Project Ascendant

Run a rigorous balance evaluation on any mechanic or content.

## Protocol

### Step 1 — Outline
State the mechanic as precisely as possible:
- What does it do?
- When does it trigger?
- What resources does it cost/grant?
- What systems does it interact with?

### Step 2 — Simulate
Walk through a real player scenario:
- Early run (weak team, low relic count)
- Mid run (1-2 evolutions, 2-3 relics)
- Late run / League (fully evolved, full relic build)

Ask: what does a skilled player do with this? What does a lucky player do?

### Step 3 — Evaluate

Check all of these explicitly:

| Check | Question |
|---|---|
| **Exploit** | Is there a degenerate combo that trivialises combat? |
| **AP economy abuse** | Does this break the 3 AP/turn budget? |
| **Dead draw** | Does this create turns where playing is impossible or meaningless? |
| **Soft-lock** | Can this create an unwinnable but not-yet-lost state? |
| **Snowball** | Does this compound on itself exponentially? |
| **Anti-snowball** | Does this punish success unfairly? |
| **Pillar 1** | Is the outcome telegraphed, or is it reactive RNG? |
| **Pillar 2** | Does this make swapping more or less meaningful? |
| **Pillar 3** | Does power come from interaction, or from a single source? |
| **Pillar 4** | Does this reinforce or dilute evolution identity? |
| **Pillar 5** | Does this feel good in a cheerful, Pokémon-adjacent game? |

Mark each: ✅ aligned / ⚠️ tension (explain) / ❌ violation (must flag)

### Step 4 — Recommendation

State ONE clear recommendation: Approve / Approve with changes / Reject.
If changes: list specifically what to change and why.
Never just list options — always recommend.

## Known Degenerate Patterns to Watch

- **Confusion cascade:** 3 Confused Pokémon = 3 discards/turn. Safety floor = 2 skill cards + 2 consumables. Any mechanic that bypasses the consumable immunity breaks this floor.
- **Swap counter bypass:** SF/SB must NOT increment the manual swap counter. Any mechanic that makes repeated manual swaps free breaks the action economy.
- **IsFainted flag:** If any mechanic depends on a separate fainted flag vs HP=0, it violates §2.4.1.
- **Backstrike redirect:** Backstrike on empty slot must fizzle. Any "redirect" mechanic breaks the positional system.
- **Mastery Move replacement:** Nothing can replace a Mastery Move. Period.
