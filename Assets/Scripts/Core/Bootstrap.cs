using System.Collections;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.14 + Epic 1 Task 1.4.3 — Boot scene entry point.
    // Registers all canonical services, then transitions to MainMenu.
    // Per §9.4 Task 2.2.7 — execution order -100 guarantees Services + EventBus
    // are ready before any other MonoBehaviour Start() fires.
    [DefaultExecutionOrder(-100)]
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private string _firstScene = "MainMenu";

        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);

            Services.Clear();

            // Per §9.14 — canonical service registrations.
            // Per §9.7.2 — placeholder RNGStreams with seed 0; Epic 3 overwrites with RunStateSO.RunSeed.
            Services.Register<RNGStreams>(new RNGStreams(0u));
            // Per §9.8 — SaveSystem is a static facade; no Services registration needed.
            // TODO: Epic 3  — load and register RunStateSO via Addressables.
            // TODO: Epic 3  — load and register MetaProgressionSO via Addressables.

            EventBus.Initialise();

            GameStateMachine hsm = new GameStateMachine();
            Services.Register<GameStateMachine>(hsm);

            // Per §9.6 — register pooled factories for hot-path allocations.
            Services.Register<PokemonInstanceFactory>(new PokemonInstanceFactory());
            Services.Register<MoveCardInstanceFactory>(new MoveCardInstanceFactory());
            Services.Register<IntentDataFactory>(new IntentDataFactory());
            Services.Register<DamageContextFactory>(new DamageContextFactory());
            Services.Register<EnemyInstanceFactory>(new EnemyInstanceFactory());

            yield return SceneLoader.LoadAsync(_firstScene);
        }
    }
}
