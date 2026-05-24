#!/usr/bin/env bash
# validate-assets.sh — validates asset naming conventions
# Fires on PostToolUse(Write|Edit). Exits immediately if not an asset file.

INPUT="$1"
echo "$INPUT" | grep -qE '"path".*"assets/' || exit 0

FILEPATH=$(echo "$INPUT" | grep -oE '"path":\s*"[^"]*"' | head -1 | sed 's/"path": "//' | tr -d '"')

# ScriptableObject data files must be PascalCase
if echo "$FILEPATH" | grep -qE "assets/.*\.asset$"; then
  FILENAME=$(basename "$FILEPATH" .asset)
  if echo "$FILENAME" | grep -qE "^[a-z]"; then
    echo "⚠️  ASSET NAMING: ScriptableObject asset '$FILENAME' should be PascalCase."
  fi
fi

# JSON data files must be kebab-case or snake_case (not PascalCase — they're data, not Unity assets)
if echo "$FILEPATH" | grep -qE "assets/data/.*\.json$"; then
  FILENAME=$(basename "$FILEPATH" .json)
  if echo "$FILENAME" | grep -qE "[A-Z]"; then
    echo "⚠️  DATA NAMING: JSON data file '$FILENAME' should be kebab-case (e.g., squirtle-base.json)."
  fi
fi

exit 0
