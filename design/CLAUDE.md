# design/ — Local Design Notes

Work-in-progress design notes BEFORE they are written to Notion.
Once a decision is written to Notion GDD, the local file is either
deleted or archived with a reference: "Archived — see Notion §N.N.N"

## What Belongs Here

- Scratch-pad notes for an in-progress GDD topic
- Architecture Decision Records (ADRs) in design/adr/
- System sketches and diagrams
- Playtest notes before they're distilled into a playtest-report

## What Does NOT Belong Here

- Canonical game design specifications → Notion GDD
- Finalized balance numbers → Notion GDD §4.1.1 and Topic 6
- Locked mechanics → Notion GDD (locked topics)

## ADR Format (design/adr/YYYY-MM-DD-title.md)

```markdown
# ADR: [Title]
Date: YYYY-MM-DD
Status: Proposed / Accepted / Deprecated / Superseded by [link]

## Context
[What problem are we solving?]

## Decision
[What did we decide?]

## Consequences
[What are the trade-offs?]

## GDD Reference
[§N.N.N if this decision is reflected in the GDD]
```
