using ProjectAscendant.Core;

namespace ProjectAscendant.Progression
{
    // Per §5.3.5 + Epic 10 Task 10.4 — execute a chosen evolution branch on a PokemonInstance. A
    // permanent, irreversible mutation: the species advances, pool moves upgrade in place (VS uses
    // the replace-a-slot model — the §5.10 additive Learned Move Pool is unbuilt, gap #36), the
    // branch's ability is granted, the Mastery card upgrades to the evolved stage (§4.3.9.2), and the
    // archetype path is locked for the next stage (§5.3.5). Trauma carries through (§6.2.3). Pure C#.
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

            // §5.3.5 / §5.10.3 — upgrade pool moves in place: if the old move is in the active 4, the
            // evolved version takes its slot.
            if (branch.MoveUpgrades != null)
            {
                for (int i = 0; i < branch.MoveUpgrades.Count; i++)
                {
                    MoveUpgradePair up = branch.MoveUpgrades[i];
                    if (up.OldMove == null || up.NewMove == null) continue;
                    int idx = p.CurrentMoves.IndexOf(up.OldMove);
                    if (idx >= 0) p.CurrentMoves[idx] = up.NewMove;
                }
            }

            // §5.3.5 — additions. VS replace-a-slot: append while there is room (Vanguard stage-1
            // adds none; the §5.10 growing pool is post-VS).
            if (branch.NewMoves != null)
            {
                for (int i = 0; i < branch.NewMoves.Count; i++)
                {
                    MoveSO m = branch.NewMoves[i];
                    if (m == null || p.CurrentMoves.Contains(m)) continue;
                    if (p.CurrentMoves.Count < 4) p.CurrentMoves.Add(m);
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
