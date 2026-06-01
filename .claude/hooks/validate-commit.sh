#!/usr/bin/env bash
# validate-commit.sh — Project Ascendant commit validator
# Fires on PreToolUse(Bash). Exits 0 (no-op) if not a git commit command.

INPUT="$1"

# Only run on git commit commands
echo "$INPUT" | grep -qE "^git commit" || exit 0

echo "=== Commit Validation ==="

FAIL=0

# Get staged files
STAGED=$(git diff --cached --name-only 2>/dev/null)

if [ -z "$STAGED" ]; then
  echo "No staged files."
  exit 0
fi

echo "Staged files: $STAGED"
echo ""

# 1. Check for hardcoded game values in C# files
CS_STAGED=$(echo "$STAGED" | grep "\.cs$")
if [ -n "$CS_STAGED" ]; then

  # Check for UnityEngine.Random (must use GameRNG)
  if git diff --cached -- $CS_STAGED 2>/dev/null | grep "^+" | grep -qE "UnityEngine\.Random\.(Range|value|insideUnitSphere)"; then
    echo "❌  FAIL: Direct use of UnityEngine.Random detected."
    echo "    Use GameRNG wrapper for determinism. (Engineering Pillar 3)"
    FAIL=1
  fi

  # Check for FindObjectOfType in production code (not in Editor/ or tests/)
  PROD_CS=$(echo "$CS_STAGED" | grep -v "Editor/" | grep -v "Tests/")
  if [ -n "$PROD_CS" ]; then
    if git diff --cached -- $PROD_CS 2>/dev/null | grep "^+" | grep -qE "FindObjectOfType|GameObject\.Find\b"; then
      echo "❌  FAIL: FindObjectOfType / GameObject.Find in production code."
      echo "    Use dependency injection or Event Bus instead."
      FAIL=1
    fi
  fi

  # Check for MonoBehaviour with game logic (heuristic: public methods that aren't Unity callbacks)
  # Warn only — not a hard fail
  if git diff --cached -- $PROD_CS 2>/dev/null | grep "^+" | grep -qE "public.*void.*(Attack|Damage|Swap|PlayCard|ApplyStatus)"; then
    echo "⚠️  WARNING: Game logic method signature in a potential MonoBehaviour."
    echo "    MonoBehaviours should be view-layer only. Verify this is intentional."
  fi

fi

# 2. Check TODO format (must reference a GDD section or be flagged as known)
if git diff --cached 2>/dev/null | grep "^+" | grep -qE "//\s*TODO:" ; then
  TODOS=$(git diff --cached 2>/dev/null | grep "^+" | grep -E "//\s*TODO:")
  BAD_TODOS=$(echo "$TODOS" | grep -vE "TODO: Pending GDD|TODO: \[#[0-9]+\]|TODO: Playtest")
  if [ -n "$BAD_TODOS" ]; then
    echo "⚠️  WARNING: Non-standard TODO format found:"
    echo "$BAD_TODOS"
    echo "    Format: '// TODO: Pending GDD §N.N.N' or '// TODO: [#issue]' or '// TODO: Playtest'"
  fi
fi

# 3. Validate JSON ScriptableObject data files if staged
JSON_STAGED=$(echo "$STAGED" | grep "\.json$")
if [ -n "$JSON_STAGED" ] && command -v jq &>/dev/null; then
  for f in $JSON_STAGED; do
    if ! jq empty "$f" 2>/dev/null; then
      echo "❌  FAIL: Invalid JSON in $f"
      FAIL=1
    fi
  done
fi

# 4. Check for .env files being committed
if echo "$STAGED" | grep -qE "\.env$|\.env\.local$"; then
  echo "❌  FAIL: .env file staged for commit. Never commit secrets."
  FAIL=1
fi

# 5. Local-only paths must not be committed
if echo "$STAGED" | grep -qE "^\.cursor/|^CLAUDE\.md$"; then
  echo "❌  FAIL: Local AI config staged (.cursor/ or CLAUDE.md)."
  echo "    Use docs/ai/cursor-setup.md for MCP templates; keep .cursor/ gitignored."
  FAIL=1
fi

# 6. Check commit message length (warn if too short)
MSG=$(git log --format=%B -n 1 HEAD 2>/dev/null | head -1)
if [ -n "$MSG" ] && [ ${#MSG} -lt 10 ]; then
  echo "⚠️  WARNING: Commit message is very short ('$MSG'). Be descriptive."
fi

if [ "$FAIL" -eq "0" ]; then
  echo "✅  Commit validation passed."
fi

echo ""
exit $FAIL
