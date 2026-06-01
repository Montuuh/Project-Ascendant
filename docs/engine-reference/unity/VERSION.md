# Unity Version Reference — Project Ascendant

- **Unity Version:** 6000.4.6f1 (Unity 6 LTS)
- **Render Pipeline:** URP 2D (com.unity.render-pipelines.universal 17.4.0)
- **Input System:** New Input System 1.19.0 (NOT legacy Input)
- **Scripting Backend:** Mono (editor), IL2CPP (builds)
- **C# Version:** C# 10

---

## Current Best Practices (post-training-cutoff additions)

- UI Toolkit is the preferred UI system. Legacy uGUI (Canvas/UGUI) is deprecated for new work.
- Addressables 1.21+ is the required asset loading system. `Resources.Load()` is forbidden in production.
- Use `UnityEngine.InputSystem` (new Input System). Old `Input.GetKey()` is forbidden.
- `async/await` with `UniTask` is preferred over Coroutines for async operations.
- `Awaitable` (Unity 2023+) may be used if pinned version supports it — verify.

## Deprecated APIs (do not use)

| Deprecated | Use Instead |
|---|---|
| `Resources.Load()` | Addressables |
| `Input.GetKey()` / `Input.GetAxis()` | New Input System |
| `GameObject.FindWithTag()` | Dependency injection / SO references |
| `UnityEngine.Random` | `GameRNG` wrapper (seeded) |
| Legacy Canvas/UGUI for new screens | UI Toolkit |

## ScriptableObject Patterns

```csharp
// Event channel (parameterless)
[CreateAssetMenu] public class GameEvent : ScriptableObject {
    private List<GameEventListener> _listeners = new();
    public void Raise() => _listeners.ForEach(l => l.OnEventRaised());
    public void Register(GameEventListener l) => _listeners.Add(l);
    public void Unregister(GameEventListener l) => _listeners.Remove(l);
}

// Typed event channel
[CreateAssetMenu] public class DamageEvent : ScriptableObject {
    public event System.Action<DamageResult> OnRaised;
    public void Raise(DamageResult result) => OnRaised?.Invoke(result);
}
```

## Performance Notes

- Object pooling required for: CardInstance, IntentData, DamageNumberVFX
- Avoid GC allocations in combat Update loops
- Profile with Unity Profiler before claiming "performance is fine"

---

## Live Editor Bridge (coplay-mcp)

The project has a **live MCP bridge to the running Unity Editor** via the
`coplay-mcp` server (tools prefixed `mcp__coplay-mcp__*`). When the editor is
open you can — and should — verify changes directly instead of asking the user
to run tests manually. Load the tools you need via
`ToolSearch({ query: "coplay-mcp", max_results: 30 })` if they appear deferred.

### Standard verification loop

1. **`list_unity_project_roots`** — confirm the editor is open on this project.
2. **`check_compile_errors`** — run after any code change. Cheap, sub-second.
3. **`execute_script`** — invoke an editor-side static method to drive work.
4. **`get_unity_logs`** — read results back (filter by `search_term`).

### Running the EditMode test suite

Helper: [Assets/Editor/Setup/RunEditModeTests.cs](../../../Assets/Editor/Setup/RunEditModeTests.cs) → `RunEditModeTests.Execute()`.

**Scene save dialog:** Unity Test Framework runs `SaveModifiedSceneTask` and
`RestoreSceneSetupTask`, which call `SaveCurrentModifiedScenesIfUserWantsTo()` when the
bootstrap **Untitled** scene is dirty — Coplay cannot click that dialog.

Fix (`TestFrameworkSceneBypass` patches the test job task list via reflection):

1. `TestFrameworkSceneBypass.InstallOn(api)` — replaces `SaveModifiedSceneTask` / `RestoreSceneSetupTask` delegates to clear dirtiness (`EditorSceneDiscardUtility`) and proceed without prompting.
2. `TestRunSceneGuard.Prepare()` / `Cleanup()` — capture and restore pre-run scene setup.
3. `runSynchronously = true` on `ExecutionSettings` so Coplay `execute_script` waits for completion.

```
execute_script({ filePath: "Assets/Editor/Setup/RunEditModeTests.cs",
                 methodName: "Execute" })
# then:
get_unity_logs({ search_term: "TestRunner", limit: 30 })
# look for:  [TestRunner] Finished — Pass: N, Fail: 0, Skip: 0
```

**Do NOT create a second `RunEditModeTests` class** — the existing one is a
`public class : ICallbacks` (not static). A duplicate static class will
collide and break the editor compile.

### Caveats

- `execute_script` returns when the static method returns. Unity's
  `TestRunnerApi.Execute` is **async / callback-driven**, so the script will
  return immediately while tests are still running. Wait ~10–30 s, then poll
  `get_unity_logs` for the `[TestRunner] Finished` line.
- Coplay tool calls **time out at 60 s**. A slow compile or a long test run
  can hit this — that's not a failure, just re-call after a short wait.
- Use the read-only tools liberally (`check_compile_errors`,
  `get_unity_logs`, `get_unity_editor_state`). Reserve `execute_script` for
  intentional editor-side work — every script invocation triggers a domain
  reload if it adds new files.
- `play_game` enters Play mode in the real editor; use only when explicitly
  asked to test runtime behaviour.

### When the bridge is unavailable

If `list_unity_project_roots` returns `count: 0`, the editor is closed. Ask
the user to open it, or fall back to verifying via reasoning + `git diff`.
Never claim "tests pass" without either running them through the bridge or
the user confirming.
