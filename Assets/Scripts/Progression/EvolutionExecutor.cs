using ProjectAscendant.Core;

namespace ProjectAscendant.Progression
{
    // Per §5.12.2 (CL-007) — execute a chosen evolution branch on a PokemonInstance. A permanent,
    // irreversible mutation: the species advances (stat upscale via the evolved species), Learned Move
    // Pool moves upgrade in place + optional additions (§5.12.1/§5.10), and the Mastery card upgrades to
    // the evolved stage (§4.3.9.2). Trauma carries through (§6.2.3). Pure C#.
    //
    // CL-007 changes vs the old model:
    //  • Free archetype per stage — the next stage's options come from the evolved species' Branches
    //    (EvolutionOptions), so SelectedBranch is a RECORD, not a lock.
    //  • No ability or crit grant — evolution is a focused upgrade. Abilities are taught at the Dojo
    //    (§5.12.3 / §7.14, CL-008); branch.GrantedAbility / CritChanceBonus are retained as data but
    //    intentionally NOT applied here.
    //
    // Task 10.4.5 — on a successful evolution this publishes EvolutionTriggeredContext on the EventBus
    // (achievements/VFX/Pokedex may subscribe; no VS listener is required, but the hook is in place).
    public static class EvolutionExecutor
    {
        public struct Result
        {
            public bool Evolved;
            public PokemonSpeciesSO From;
            public PokemonSpeciesSO To;
        }

        public static Result Evolve(PokemonInstance p, EvolutionBranchSO branch)
        {
            Result r = default;
            if (p == null || p.Species == null || branch == null || branch.EvolvedSpecies == null) return r;
            r.From = p.Species;
            PokemonSpeciesSO evolved = branch.EvolvedSpecies;

            // §5.3.5 / §5.10.3 (approved 2026-06-02, pending Notion lock) — upgrade pool moves in place:
            // if the old move is in the active 4, the evolved version takes its slot automatically.
            if (branch.MoveUpgrades != null)
            {
                for (int i = 0; i < branch.MoveUpgrades.Count; i++)
                {
                    MoveUpgradePair up = branch.MoveUpgrades[i];
                    if (up.OldMove == null || up.NewMove == null) continue;
                    MoveLoadoutService.UpgradePoolMove(p, up.OldMove, up.NewMove);
                }
            }

            // §5.3.5 / §5.10.1 — additions: grow the pool with new moves. The player will reconfigure
            // their active 4 post-evolution (UI flow; MoveLoadoutService.SetActiveMoves).
            if (branch.NewMoves != null)
            {
                for (int i = 0; i < branch.NewMoves.Count; i++)
                {
                    MoveSO m = branch.NewMoves[i];
                    if (m == null) continue;
                    MoveLoadoutService.AddToPool(p, m); // dedups internally per §5.10.1
                }
            }

            p.Species = evolved;
            p.SelectedBranch = branch;                          // §5.12.2 — record only (free per-stage; does NOT lock the next stage)
            p.CurrentStage = NextStage(p.CurrentStage);
            if (evolved.MasteryMove != null) p.MasteryMove = evolved.MasteryMove; // §4.3.9.2 upgrade
            // Per §5.12.2/§5.12.3 (CL-007) — NO ability or crit grant. Abilities come from the Dojo
            // (§7.14, CL-008); branch.GrantedAbility / CritChanceBonus are data-only here.

            // TraumaStacks carry through unchanged (§6.2.3). Clamp HP into the (now larger) pool.
            int max = PokemonVitals.MaxHP(p);
            if (p.CurrentHP > max) p.CurrentHP = max;

            r.To = evolved;
            r.Evolved = true;

            // Task 10.4.5 — broadcast the evolution. Synchronous, deterministic (§9.4.2).
            EventBus.Publish(new EvolutionTriggeredContext(p, r.From, evolved, branch));
            return r;
        }

        private static EvolutionStage NextStage(EvolutionStage s) => s switch
        {
            EvolutionStage.Basic => EvolutionStage.Stage1,
            EvolutionStage.Stage1 => EvolutionStage.Stage2,
            _ => EvolutionStage.Stage2,
        };
    }
}
