using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §9.2 / §9.14 + Epic 9 runtime wiring — the run-layer composition root. Lives on the
    // Bootstrap GameObject in the Boot scene (DontDestroyOnLoad). Bootstrap (Core, order -100)
    // registers the core services; this (order -50) builds the run layer that Bootstrap can't
    // reference across the Core↔Map asmdef boundary: it constructs a RunController from the
    // RunContentCatalog, wires its dispatcher to the live GameStateMachine, and registers it +
    // itself in Services.
    //
    // Per gap #43 — boot does NOT auto-start or auto-resume. It builds a fresh, idle run (Map == null)
    // and the Main Menu (MapViewUI) drives the choice: ContinueSavedRun() (load the autosave) or
    // BeginNewRun() (fresh) → starter-select. The dev flag still auto-walks a run headlessly.
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

        // Built once from the catalog (+ Hub difficulty choices); resolves saved SO refs on Continue.
        private RunContentRegistry _registry;
        private PokemonInstanceFactory _factory;

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
            _factory = Services.Get<PokemonInstanceFactory>();

            // Per gap #43 — registry resolves saved SO refs (run-state + team) back to authored assets;
            // built from the catalog + Hub difficulty choices (which the catalog does not own).
            _registry = RunContentRegistry.FromCatalog(_catalog);
            _registry.RegisterDifficultyModifiers(RunBootstrapper.BuildDifficultyChoices());
            // Per §7.8.3.1 (CL-016) — register the 16 Region Modifiers so a saved ActiveRegionModifier
            // ID resolves back to its authored SO on resume.
            _registry.RegisterRegionModifiers(RegionModifierPool.BuildAll());
            // Per §8.3.7 (CL-021) gap B — Legendary relics are code-built (LegendaryRelicCatalog), NOT
            // in catalog.Relics, so FromCatalog never indexes them; without this a held Legendary's id
            // resolves to null on resume and is silently dropped. Register the catalog so it round-trips.
            _registry.RegisterRelics(LegendaryRelicCatalog.BuildAll());

            // Build a fresh, IDLE run (Map == null). The Main Menu picks Continue vs New Run.
            RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>();
            run.RunSeed = _runSeed;
            Services.Register(run);

            // Per §9.7.2 — seed the RNG streams from the run seed.
            RNGStreams streams = new RNGStreams((uint)run.RunSeed);
            Services.Register(streams);

            Run = RunBootstrapper.CreateRunController(
                _catalog, run, _factory, streams, evt => hsm.HandleEvent(evt), out RunContext ctx);
            Context = ctx;
            Services.Register(Run);
            Services.Register(ctx);
            Services.Register(_catalog);   // so the Map View can offer the starter choices
            Services.Register(this);       // so MapViewUI can drive Continue / New Run

            Debug.Log($"[RunLauncher] Run wired (seed {_runSeed}). Idle — awaiting Main Menu choice " +
                      $"(saved run present: {HasSavedRun()}).");

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

        // Per gap #43 — does a resumable in-progress save exist? Drives the Main Menu's Continue button.
        public bool HasSavedRun() => SaveSystem.HasRun();

        // Per gap #43 — load the autosave into the live RunContext (run-state in place + Box) and resume
        // into Map View. Returns false if no/corrupt save. Called by the Main Menu's Continue.
        public bool ContinueSavedRun()
        {
            if (Run == null || Context?.Run == null) return false;
            if (!SaveSystem.LoadRunInto(Context.Run, _registry, _factory,
                    out System.Collections.Generic.List<PokemonInstance> box, out int cap))
                return false;

            Context.Box.Members.Clear();
            if (box != null) Context.Box.Members.AddRange(box);
            if (cap > 0) Context.Box.Capacity = cap;

            // Reject a corrupt teamless save — a legit mid-run always has >=1 Box member (the starter is
            // seeded before the first node autosave). Without this, Continue loads an empty team and
            // every combat node silently auto-resolves. Discard it so the player isn't stuck.
            bool hasTeam = false;
            for (int i = 0; i < Context.Box.Members.Count; i++)
                if (Context.Box.Members[i] != null) { hasTeam = true; break; }
            if (!hasTeam)
            {
                SaveSystem.DeleteRun();
                Debug.LogWarning("[RunLauncher] Saved run had no team (corrupt) — discarded.");
                return false;
            }

            // Per gap #43 — reseed the RNG streams from the LOADED seed so the deterministic map
            // regenerates identically to the saved run (boot seeded streams from the idle placeholder
            // seed). Resume() rebuilds the region map by replaying MapRNG from its region-entry state.
            Context.Streams = new RNGStreams((uint)Context.Run.RunSeed);
            Run.Resume();
            // Per §9.8.6 (gap #45) — restore the 4 CONTENT cursors so encounters/loot/mystery/combat
            // continue exactly where the save left off instead of re-rolling from the stream start.
            // MapRNG is intentionally left as the replay rebuilt it (the map must NOT shift on resume).
            Context.Streams.RestoreContentCursors(Context.Run.RngCursors);
            Debug.Log($"[RunLauncher] Continued run (seed {Context.Run.RunSeed}) at " +
                      $"L{Context.Run.CurrentLayerIndex}/Lane{Context.Run.CurrentLaneIndex} — team={Context.Box.Count}.");
            return true;
        }

        // Per gap #43 — discard any in-progress save and reset the live RunContext to a clean, idle run
        // (fresh seed) so starter-select shows. StartRun() is called by MapViewUI after the starter pick.
        public void BeginNewRun()
        {
            if (Run == null || Context?.Run == null) return;
            SaveSystem.DeleteRun();

            int seed = NewSeed();
            Context.Run.ResetToNewRun(seed);
            Context.Box.Members.Clear();
            Context.Streams = new RNGStreams((uint)seed); // RegisterBuilders/StartRun read ctx.Streams at call time
            Run.ResetForNewRun(); // CRITICAL: clear the controller's Map/position so Refresh shows starter-select
            Debug.Log($"[RunLauncher] New run prepared (seed {seed}) — awaiting starter selection.");
        }

        // A fresh, varied seed per New Run (stored on RunState, so the run stays fully deterministic).
        private static int NewSeed() => unchecked((int)(System.DateTime.UtcNow.Ticks ^ (System.DateTime.UtcNow.Ticks >> 32)));
    }
}
