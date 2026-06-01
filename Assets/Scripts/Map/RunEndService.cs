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
            public int AchievementsUnlocked; // §6.7 — newly-completed this run-end
        }

        public static RunSummary Finalize(RunStateSO run, Box box, MetaProgressionSO meta,
                                          MetaProgressionConfigSO cfg, RunOutcome outcome, int layersCleared,
                                          BestiaryProgressSO bestiary = null)
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

            // §6.8.1/§6.8.3 — difficulty modifiers multiply the run's Trainer-XP reward (after the
            // failed bonus, so the whole haul is boosted). Stacks multiplicatively across modifiers.
            run.TrainerXPEarnedThisRun =
                DifficultyModifiers.ApplyXP(run.TrainerXPEarnedThisRun, run.ActiveDifficultyModifiers);
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

                // §6.7 / Task 11.5.3 — fire the run-data-evaluable achievements, then persist their
                // completions in the same save.
                s.AchievementsUnlocked = ReportRunAchievements(run, box, s, meta, cfg, outcome);

                SaveSystem.SaveMeta(meta);
            }

            // §6.9 / Task 11.8.1 — persist the Bestiary alongside meta at run-end.
            if (bestiary != null) SaveSystem.SaveBestiary(bestiary);
            return s;
        }

        // §6.7 — achievements decidable from run/box/outcome at run-end. The combat-context ones
        // (Status Sniper / Tankmaster / Lead Specialist) and run-flag ones (No-Repeat / Speed Demon)
        // need their own in-combat / run-timer hooks — tracked as a follow-up (see Epic 11.5 notes).
        private static int ReportRunAchievements(RunStateSO run, Box box, RunSummary s,
            MetaProgressionSO meta, MetaProgressionConfigSO cfg, RunOutcome outcome)
        {
            System.Collections.Generic.IReadOnlyList<AchievementSO> reg = AchievementCatalog.All;
            int count = 0;
            int Fire(AchievementTrigger t) => AchievementService.Report(t, 1, reg, meta, cfg).Count;

            if (run.CombatsClearedThisRun >= 1) count += Fire(AchievementTrigger.WinCombat);  // First Steps
            if (run.EvolutionsThisRun >= 1)     count += Fire(AchievementTrigger.Evolve);      // Evolver
            // Heuristic: the Box grew past the lone starter ⇒ at least one recruit.
            if (box != null && box.Members != null && box.Members.Count > 1)
                count += Fire(AchievementTrigger.RecruitWild);                                  // Recruiter
            if (outcome == RunOutcome.Victory)
            {
                count += Fire(AchievementTrigger.DefeatGym);                                    // Boulder Breaker
                if (s.MaxTrauma >= 5) count += Fire(AchievementTrigger.WinRunHighTrauma);       // Trauma Survivor
            }
            return count;
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
