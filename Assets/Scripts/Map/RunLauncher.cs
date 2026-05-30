using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §9.2 / §9.14 + Epic 9 runtime wiring — the run-layer composition root. Lives on the
    // Bootstrap GameObject in the Boot scene (DontDestroyOnLoad). Bootstrap (Core, order -100)
    // registers the core services; this (order -50) builds the run layer that Bootstrap can't
    // reference across the Core↔Map asmdef boundary: it constructs a RunController from the
    // RunContentCatalog, wires its dispatcher to the live GameStateMachine, and registers it in
    // Services. A "New Run" UI (Epic 13) will call Run.StartRun(); the dev flag auto-walks one now.
    //
    // Thin composition/wiring MonoBehaviour — holds no game logic (logic is RunController/RunContext).
    [DefaultExecutionOrder(-50)]
    public sealed class RunLauncher : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private RunContentCatalogSO _catalog;

        [Header("Run Setup")]
        [SerializeField] private int _runSeed = 20260529;
        [Tooltip("Index into the catalog's Starters list (0=Bulbasaur, 1=Charmander, 2=Squirtle by convention).")]
        [SerializeField] private int _starterIndex;
        [SerializeField] private int _starterLevel = 5;

        [Header("Dev")]
        [Tooltip("DEV ONLY: on boot, immediately auto-walk a full run (combat auto-resolved) and log it. No UI/real combat.")]
        [SerializeField] private bool _devAutoRunOnBoot;

        public RunController Run { get; private set; }
        public RunContext Context { get; private set; }

        private void Start()
        {
            if (_catalog == null)
            {
                Debug.LogWarning("[RunLauncher] No RunContentCatalog assigned — run-flow is not wired this session.");
                return;
            }
            if (!Services.Has<GameStateMachine>())
            {
                Debug.LogError("[RunLauncher] Core services not ready (is Bootstrap on the scene?). Aborting run wiring.");
                return;
            }

            GameStateMachine hsm = Services.Get<GameStateMachine>();
            PokemonInstanceFactory factory = Services.Get<PokemonInstanceFactory>();

            // Per §9.7.2 — seed the RNG streams from the run seed (Bootstrap registers a seed-0 stub).
            RNGStreams streams = new RNGStreams((uint)_runSeed);
            Services.Register(streams);

            RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>();
            run.RunSeed = _runSeed;
            Services.Register(run);

            Run = RunBootstrapper.CreateRunController(
                _catalog, run, factory, streams, evt => hsm.HandleEvent(evt), out RunContext ctx);
            Context = ctx;
            Services.Register(Run);
            Services.Register(ctx);

            PokemonSpeciesSO starter =
                (_catalog.Starters != null && _starterIndex >= 0 && _starterIndex < _catalog.Starters.Count)
                    ? _catalog.Starters[_starterIndex] : null;
            if (starter != null) RunBootstrapper.SeedStarter(ctx, starter, _starterLevel);

            Debug.Log($"[RunLauncher] Run wired (seed {_runSeed}, starter {(starter ? starter.DisplayName : "none")}). " +
                      "RunController registered in Services; awaiting StartRun() (UI trigger = Epic 13).");

            if (_devAutoRunOnBoot)
            {
                Debug.Log("[RunLauncher] DEV auto-run ON — walking a full run:");
                Run.StartRun();
                int steps = RunAutoPilot.WalkToEnd(Run, s => Debug.Log("[RunLauncher] " + s));
                Debug.Log($"[RunLauncher] DEV run finished — over={Run.RunOver}, nodes={steps}, " +
                          $"₽={run.PokeDollars}, relics={run.HeldRelics?.Count ?? 0}, badges={run.EarnedBadges?.Count ?? 0}.");
            }
        }
    }
}
