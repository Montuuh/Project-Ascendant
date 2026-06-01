using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §2.1.7 + Epic 11 Task 11.4 — the run-end flow. On Victory (Gym cleared, §7.13) or Defeat
    // (player wipe, §3.3.6): add the run-failed XP bonus (§6.3.2), COMMIT the run's accrued Trainer XP
    // into the MetaProgressionSO (Level + Tokens, §6.3.4), bump run counters, persist meta to disk
    // (§6.10 trigger), and produce a RunSummary for the result screen. Pure C# (testable); the caller
    // (RunController) invokes it once when RunOver flips true.
    public static class RunEndService
    {
        public struct RunSummary
        {
            public RunOutcome Outcome;
            public int LayersCleared;
            public int CombatsCleared;
            public int Evolutions;
            public int MaxTrauma;
            public int RunXpEarned;   // total meta XP banked this run (incl. failed bonus)
            public int TokensGained;
            public int OldLevel;
            public int NewLevel;
            public bool LeveledUp;
        }

        public static RunSummary Finalize(RunStateSO run, Box box, MetaProgressionSO meta,
                                          MetaProgressionConfigSO cfg, RunOutcome outcome, int layersCleared)
        {
            RunSummary s = default;
            if (run == null) return s;

            s.Outcome = outcome;
            s.LayersCleared = layersCleared < 0 ? 0 : layersCleared;
            s.CombatsCleared = run.CombatsClearedThisRun;
            s.Evolutions = run.EvolutionsThisRun;
            s.MaxTrauma = MaxTrauma(box);

            // §6.3.2 — failed runs still bank a progress-scaled bonus (failure is fuel, §6.1).
            if (outcome == RunOutcome.Defeat && cfg != null)
            {
                int bonus = s.LayersCleared * cfg.RunFailedXPPerLayer;
                if (bonus > cfg.RunFailedXPCap) bonus = cfg.RunFailedXPCap;
                run.TrainerXPEarnedThisRun += bonus;
            }
            s.RunXpEarned = run.TrainerXPEarnedThisRun;

            // §6.3.4 — bank XP → recompute Level → award Tokens, then persist (§6.10).
            if (meta != null && cfg != null)
            {
                TrainerProgression.CommitResult r =
                    TrainerProgression.CommitRun(meta, run.TrainerXPEarnedThisRun, cfg);
                s.TokensGained = r.TokensGained;
                s.OldLevel = r.OldLevel;
                s.NewLevel = r.NewLevel;
                s.LeveledUp = r.LeveledUp;

                meta.TotalRunsAttempted += 1;
                if (outcome == RunOutcome.Victory) meta.TotalRunsCompleted += 1;

                SaveSystem.SaveMeta(meta);
            }
            return s;
        }

        private static int MaxTrauma(Box box)
        {
            int max = 0;
            if (box?.Members != null)
                foreach (PokemonInstance p in box.Members)
                    if (p != null && p.TraumaStacks > max) max = p.TraumaStacks;
            return max;
        }
    }
}
