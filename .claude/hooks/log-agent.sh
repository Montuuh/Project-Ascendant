#!/usr/bin/env bash
# log-agent.sh — audit trail for subagent invocations
AGENT_NAME="${1:-unknown}"
LOG="production/session-state/agent-log.md"
DATE=$(date "+%Y-%m-%d %H:%M:%S")

mkdir -p production/session-state
echo "[$DATE] Agent started: $AGENT_NAME" >> "$LOG"
exit 0
