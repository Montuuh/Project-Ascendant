using ProjectAscendant.Core;

namespace ProjectAscendant.Progression
{
    // Per §5.3.5 + Epic 10 Task 10.4 — execute a chosen evolution branch on a PokemonInstance. A
    // permanent, irreversible mutation: the species advances, Learned Move Pool moves upgrade in place
    // per §5.10 (approved 2026-06-02, pending Notion lock), the branch's ability is granted, the Mastery
    // card upgrades to the evolved stage (§4.3.9.2), and the archetype path is locked for the next stage
    // (§5.3.5). Trauma carries through (§6.2.3). Pure C#.
    //
    // Task 10.4.5 — on a successful evolution this publishes EvolutionTriggeredContext on the EventBus
    // (achievements/VFX/Bestiary may subscribe; no VS listener is required, but the hook is in place).
    // Not yet wired (post-VS): branch.CritChanceBonus (no PokemonInstance.CritChance field yet) — no-op,
    // flagged for the crit/ability runtime pass (Epic 10 Task 10.9).
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
            p.SelectedBranch = branch;                          // §5.3.5 — lock the archetype path
            p.CurrentStage = NextStage(p.CurrentStage);
            if (evolved.MasteryMove != null) p.MasteryMove = evolved.MasteryMove; // §4.3.9.2 upgrade
            AbilitySO ability = branch.GrantedAbility != null ? branch.GrantedAbility : evolved.PrimaryAbility;
            if (ability != null) p.Ability = ability;           // §5.5.1 primary/secondary

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
