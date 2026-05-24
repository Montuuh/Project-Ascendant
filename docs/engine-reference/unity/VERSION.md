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
