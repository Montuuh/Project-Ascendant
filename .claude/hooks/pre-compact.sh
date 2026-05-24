#!/usr/bin/env bash
# pre-compact.sh — dumps session state before context compaction
SESSION_STATE="production/session-state/active.md"

echo "=== Pre-Compact: Preserving Session State ==="
if [ -f "$SESSION_STATE" ]; then
  echo "Active session state:"
  cat "$SESSION_STATE"
else
  echo "No session state file found at $SESSION_STATE"
fi
echo ""
echo "After compaction, resume from this state."
echo "The Notion BACKLOG is canonical — re-read it at session resume."
exit 0
