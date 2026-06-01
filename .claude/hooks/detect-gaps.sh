#!/usr/bin/env bash
# detect-gaps.sh — Project Ascendant gap detector
# Fires on SessionStart. Warns when code exists without GDD coverage.

echo "=== Gap Detection ==="

HAS_GAPS=0

# Check for C# files with TODO: Pending GDD comments (stubs from flag-dont-resolve protocol)
if find Assets/Scripts -name "*.cs" -exec grep -l "TODO: Pending GDD" {} \; 2>/dev/null | grep -q .; then
  echo ""
  echo "⚠️  OPEN GDD STUBS FOUND — these C# files have unresolved GDD gaps:"
  find Assets/Scripts -name "*.cs" -exec grep -l "TODO: Pending GDD" {} \; 2>/dev/null
  HAS_GAPS=1
fi

# Check if Assets/Scripts has .cs files but design/systems-index.md does not exist
CS_COUNT=$(find Assets/Scripts -name "*.cs" 2>/dev/null | wc -l | tr -d ' ')
if [ "$CS_COUNT" -gt "0" ] && [ ! -f "design/systems-index.md" ]; then
  echo ""
  echo "⚠️  MISSING: design/systems-index.md"
  echo "   $CS_COUNT C# files exist but no systems index. Run /map-systems."
  HAS_GAPS=1
fi

# Check for hardcoded magic numbers in gameplay code
if find Assets/Scripts -name "*.cs" 2>/dev/null | xargs grep -l "= 3;" 2>/dev/null | grep -qi "combat\|damage\|ap\|swap"; then
  echo ""
  echo "⚠️  POTENTIAL HARDCODED VALUE — found '= 3;' in combat-related files."
  echo "   All game values must be ScriptableObject fields. Verify these files:"
  find Assets/Scripts -name "*.cs" 2>/dev/null | xargs grep -l "= 3;" 2>/dev/null | head -5
  HAS_GAPS=1
fi

# Check if prototypes exist without a README
PROTO_DIRS=$(find prototypes -mindepth 1 -maxdepth 1 -type d 2>/dev/null)
if [ -n "$PROTO_DIRS" ]; then
  for dir in $PROTO_DIRS; do
    if [ ! -f "$dir/README.md" ]; then
      echo ""
      echo "⚠️  MISSING: $dir/README.md"
      echo "   Prototypes require a README documenting the hypothesis."
      HAS_GAPS=1
    fi
  done
fi

if [ "$HAS_GAPS" -eq "0" ]; then
  echo "✅  No gaps detected."
fi

echo ""
exit 0
