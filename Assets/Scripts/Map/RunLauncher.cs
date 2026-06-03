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

            // Per §9.8.1 + gap #43 — try to resume an in-progress run from disk. The registry resolves
            // every saved SO reference (run-state + team) back to the authored assets; it is built from
            // the catalog plus the Hub difficulty choices (which the catalog does not own).
            RunContentRegistry registry = RunContentRegistry.FromCatalog(_catalog);
            registry.RegisterDifficultyModifiers(RunBootstrapper.BuildDifficultyChoices());
            RunSaveData saved = SaveSystem.LoadRun(registry, factory);
            bool resuming = saved != null && saved.Run != null;

            RunStateSO run = resuming ? saved.Run : ScriptableObject.CreateInstance<RunStateSO>();
            if (!resuming) run.RunSeed = _runSeed;
            Services.Register(run);

            // Per §9.7.2 — seed the RNG streams from the run seed (the SAVED seed when resuming, so the
            // deterministic map regenerates identically; Bootstrap registers a seed-0 stub otherwise).
            RNGStreams streams = new RNGStreams((uint)run.RunSeed);
            Services.Register(streams);

            Run = RunBootstrapper.CreateRunController(
                _catalog, run, factory, streams, evt => hsm.HandleEvent(evt), out RunContext ctx);
            Context = ctx;
            Services.Register(Run);
            Services.Register(ctx);
            Services.Register(_catalog); // so the Map View can offer the starter choices

            if (resuming)
            {
                // Reinstall the saved Box (team) into the live RunContext, then resume into Map View.
                ctx.Box.Members.Clear();
                if (saved.Box != null) ctx.Box.Members.AddRange(saved.Box);
                if (saved.BoxCapacity > 0) ctx.Box.Capacity = saved.BoxCapacity;
                Run.Resume();
                Debug.Log($"[RunLauncher] Resumed run (seed {run.RunSeed}) at " +
                          $"L{run.CurrentLayerIndex}/Lane{run.CurrentLaneIndex} — team={ctx.Box.Count}, " +
                          $"₽={run.PokeDollars}, relics={run.HeldRelics?.Count ?? 0}.");
                return;
            }

            // Starter is NOT seeded here — the player picks one on the starter-select screen
            // (MapViewUI), which calls RunBootstrapper.SeedStarter then Run.StartRun().
            Debug.Log($"[RunLauncher] Run wired (seed {_runSeed}). RunController + Catalog registered; " +
                      "awaiting starter selection + StartRun (UI).");

            if (_devAutoRunOnBoot)
            {
                Debug.Log("[RunLauncher] DEV auto-run ON — seeding starter[0] + walking a full run:");
                if (_catalog.Starters != null && _catalog.Starters.Count > 0)
                    RunBootstrapper.SeedStarter(ctx, _catalog.Starters[Mathf.Clamp(_starterIndex, 0, _catalog.Starters.Count - 1)], _starterLevel);
                Run.StartRun();
                int steps = RunAutoPilot.WalkToEnd(Run, s => Debug.Log("[RunLauncher] " + s));
                Debug.Log($"[RunLauncher] DEV run finished — over={Run.RunOver}, nodes={steps}, " +
                          $"₽={run.PokeDollars}, relics={run.HeldRelics?.Count ?? 0}, badges={run.EarnedBadges?.Count ?? 0}.");
            }
        }
    }
}
