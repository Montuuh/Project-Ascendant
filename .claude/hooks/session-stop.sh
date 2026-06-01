#!/usr/bin/env bash
# session-stop.sh — logs session accomplishments
LOG="production/session-state/session-log.md"

mkdir -p production/session-state

DATE=$(date "+%Y-%m-%d %H:%M")

echo "" >> "$LOG"
echo "## Session ended: $DATE" >> "$LOG"

if git rev-parse --git-dir > /dev/null 2>&1; then
  LAST_COMMIT=$(git log --oneline -1 2>/dev/null || echo "no commits")
  echo "Last commit: $LAST_COMMIT" >> "$LOG"

  CHANGED=$(git diff --name-only HEAD 2>/dev/null | head -10)
  if [ -n "$CHANGED" ]; then
    echo "Uncommitted changes:" >> "$LOG"
    echo "$CHANGED" >> "$LOG"
  fi
fi

echo "" >> "$LOG"
echo "=== Session End: $DATE ==="
echo "Session log appended to $LOG"
echo "Remember to update Notion BACKLOG changelog before closing."
exit 0
