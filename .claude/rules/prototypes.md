---
globs: ["prototypes/**"]
---
# Prototype Rules

- Every prototype directory MUST have a README.md documenting:
  - Hypothesis being tested
  - How to run it
  - What "success" looks like
  - Current status (active / concluded / abandoned)
- Relaxed coding standards apply — but NO prototype code may be
  moved to Assets/Scripts/ without a full refactor and code review.
- Prototypes are isolated from Assets/Scripts/. Never import from prototypes/ in Assets/Scripts/.
