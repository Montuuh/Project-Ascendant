# Handoff Templates

Copy-paste into Task subagent prompts or chat when switching agents.

---

## Design → Engineering

```markdown
## Handoff: Design → lead-programmer

**GDD:** §[X.Y] — read `docs/gdd/topic-N-*.md` (ensure snapshot fresh)
**Epic:** [N.X.Y]
**VS scope:** IN — [one line]

**Spec summary:**
- …

**Acceptance criteria:**
- [ ] …
- [ ] EditMode tests cover [edge cases]
- [ ] No hardcoded balance values

**Open flags (if any):**
- ⚠️ OPEN: … — code default: …

**Do not:** invent spec beyond GDD; commit without user approval.
```

---

## Engineering → QA

```markdown
## Handoff: lead-programmer → qa-lead

**GDD:** §[X.Y]
**Files changed:** [list]
**Behavior summary:** …

**Test focus:**
- [ ] Edge case from GDD §…
- [ ] Regression: …

**Verification:** unity-verify skill — [Pass: N / not run]
```

---

## Engineering → UI

```markdown
## Handoff: lead-programmer → ui-programmer

**Events to subscribe:** [channel names]
**Screen:** [Combat / Map / Hub]
**Layout ref:** GDD §10 / ui-programmer combat layout

**Readability requirements:**
- AP visible on cards
- Intent shows slot + occupant
- Damage preview on hover

**Constraint:** view layer only — no game state in MonoBehaviour.
```

---

## UI / Design → Art

```markdown
## Handoff: ui-programmer → art-director

**Asset list:** [icons / portraits / node art]
**Dimensions / PPU:** …
**Regional palette:** Region 1 — [reference]
**Readability notes:** …

**Deliverable:** sprite brief(s) per art-director template; PNG production by human/tool.
```

---

## Producer → Any Specialist

```markdown
## Handoff: producer → [agent name]

**Epic:** [N — URL]
**GDD §:** …
**User approved:** [plan option / scope decision]

**Your single task:** …

**Return:** findings + risks + recommended next step (no scope expansion).
```
