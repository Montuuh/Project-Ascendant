#!/usr/bin/env bash
# session-start.sh — Project Ascendant session context loader
# Fires on SessionStart. Loads sprint state and recent activity.

SESSION_STATE="production/session-state/active.md"
BACKLOG_HINT=".claude/docs/backlog-hint.md"

echo "=== Project Ascendant — Session Start ==="
echo ""

# Compact backlog hint (phase, gaps, URLs)
if [ -f "$BACKLOG_HINT" ]; then
  echo "--- Backlog Hint ---"
  cat "$BACKLOG_HINT"
  echo ""
fi

# Show session state if it exists
if [ -f "$SESSION_STATE" ]; then
  echo "--- Active Session State ---"
  head -n 25 "$SESSION_STATE"
  echo ""
else
  echo "No active session state found. Run sprint-plan skill or update active.md."
  echo ""
fi

# Show recent git activity
if git rev-parse --git-dir > /dev/null 2>&1; then
  echo "--- Recent Git Activity (last 5 commits) ---"
  git log --oneline -5 2>/dev/null || echo "(no commits yet)"
  echo ""

  echo "--- Uncommitted Changes ---"
  git status --short 2>/dev/null || echo "(git status failed)"
  echo ""
fi

# Remind about GDD workflow
echo "--- GDD Read Policy ---"
echo "  docs/gdd/README.md — Notion=edit, local=read, npm run gdd:ensure before every read"
node docs/scripts/check-gdd-snapshot.js 2>/dev/null || echo "  (stale — run: npm run gdd:ensure)"
echo ""
echo "Design Pillars:"
echo "  1. Telegraphed tactics over reactive RNG"
echo "  2. Every swap is a decision"
echo "  3. Synergy is sculpted, not drafted"
echo "  4. Identity through Evolution"
echo "  5. Cheerful core, regional flavor"
echo ""

exit 0
