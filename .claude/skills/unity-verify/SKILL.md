---
name: unity-verify
description: >
  Verify Unity changes via the Coplay MCP bridge. Use after any C# edit,
  before claiming tests pass, or when asked to "check compile", "run EditMode
  tests", or "verify in Unity". Requires Unity Editor open on this project
  and coplay-mcp configured in local Cursor MCP settings.
---

# Unity Verify — Coplay MCP Bridge

Do **not** claim "tests pass" or "compiles clean" without running this loop
or the user confirming manually.

## Prerequisites

1. Unity **6000.4.6f1** open on Project Ascendant
2. Coplay plugin installed (`com.coplaydev.coplay` in manifest)
3. `coplay-mcp` in local Cursor MCP config (`uvx --python ">=3.11" coplay-mcp-server@latest`)

If bridge unavailable (`list_unity_project_roots` → count 0): ask user to open Unity.

## Standard Loop

### 1. Confirm editor

```
list_unity_project_roots
```

Expect this project path in results.

### 2. Compile check (after every code change)

```
check_compile_errors
```

Fix all errors before proceeding.

### 3. Run EditMode suite

Pre-existing helper — **do not duplicate this class:**

`Assets/Editor/Setup/RunEditModeTests.cs` → `RunEditModeTests.Execute()`

Uses `TestRunSceneGuard` to avoid blocking on "Scene(s) have been modified" during MCP runs.

```
execute_script({ filePath: "Assets/Editor/Setup/RunEditModeTests.cs", methodName: "Execute" })
```

Wait 10–30 s (async TestRunner), then:

```
get_unity_logs({ search_term: "TestRunner", limit: 30 })
```

**Pass line:** `[TestRunner] Finished — Pass: N, Fail: 0, Skip: 0`

### 4. Play mode (only when asked)

```
play_game
```

Use sparingly — real editor Play mode.

## Caveats

- Coplay calls **timeout at ~60 s**; slow compiles ≠ failure — retry after wait.
- `execute_script` may trigger domain reload if it adds files.
- Bridge **freezes** after many reloads — user should restart Unity.
- Read-only tools are cheap: `get_unity_editor_state`, `get_unity_logs`.

## When Bridge Down

Fall back to: `git diff` review + reasoning. State explicitly that tests were **not** run.

## Reference

Full details: `docs/engine-reference/unity/VERSION.md` § Live Editor Bridge.
