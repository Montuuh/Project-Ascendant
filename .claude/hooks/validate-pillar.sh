#!/usr/bin/env bash
# validate-pillar.sh — Project Ascendant design pillar guardrail
# Fires on PostToolUse(Write|Edit). Checks C# files for known pillar violations.

INPUT="$1"
echo "$INPUT" | grep -qE '"path".*\.cs"' || exit 0

FILEPATH=$(echo "$INPUT" | grep -oE '"path":\s*"[^"]*\.cs"' | head -1 | sed 's/"path": "//' | tr -d '"')
[ -f "$FILEPATH" ] || exit 0

VIOLATIONS=0

# Pillar 1: Telegraphed tactics — no hidden RNG in damage path
if grep -qE "Random\.(Range|value)" "$FILEPATH" 2>/dev/null; then
  echo "⚠️  PILLAR 1 CHECK: Direct Random call in '$FILEPATH'."
  echo "    Use GameRNG (seeded) for determinism. Pillar 1: Telegraphed tactics."
  VIOLATIONS=$((VIOLATIONS+1))
fi

# Pillar 3: Synergy is sculpted — warn if a single card/relic has >2 independent effects
EFFECT_COUNT=$(grep -cE "(ApplyStatus|DealDamage|DrawCard|RestoreHP|ModifyStat)" "$FILEPATH" 2>/dev/null || echo 0)
if [ "$EFFECT_COUNT" -gt "4" ]; then
  echo "⚠️  PILLAR 3 CHECK: '$FILEPATH' has $EFFECT_COUNT effect method calls."
  echo "    High density of effects may indicate a card/relic doing too much."
  echo "    Verify this is intentional. Pillar 3: Synergy is sculpted, not drafted."
fi

if [ "$VIOLATIONS" -eq "0" ]; then
  : # Silent pass — don't spam on every file write
fi

exit 0
