#!/usr/bin/env bash
# validate-push.sh — warns on pushes to protected branches
INPUT="$1"
echo "$INPUT" | grep -qE "^git push" || exit 0

BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null)
if echo "$BRANCH" | grep -qE "^(main|master|develop)$"; then
  echo "⚠️  WARNING: Pushing directly to '$BRANCH' (protected branch)."
  echo "    Consider a feature branch and PR workflow."
fi
exit 0
